using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace SMAPIGameLoader.Launcher;

public class ModInfo
{
    public string NameUIText = "Unknow";
    public string VersionUIText = "Unknow";

    public readonly string modName = "unknow";
    public readonly string modVersion = "unknow";
    public string ModFolderPath => modFolderPath;
    public string ManifestPath => modManifestPath;

    /// <summary>เวลาแก้ไขล่าสุดของ manifest.json ใช้สำหรับ sort by date</summary>
    public DateTime LastModified { get; private set; } = DateTime.MinValue;

    /// <summary>ขนาดรวมของโฟลเดอร์ mod (ไบต์) ใช้สำหรับ sort by size</summary>
    public long SizeBytes { get; private set; }
    public bool HasSize { get; private set; }

    public string DateUIText => LastModified == DateTime.MinValue
        ? "—"
        : LastModified.ToString("dd/MM/yyyy");

    public string SizeUIText
    {
        get
        {
            if (HasSize is false)
                return "—";

            double kb = SizeBytes / 1024d;
            return kb < 1024d ? $"{kb:0} KB" : $"{kb / 1024d:0.0} MB";
        }
    }

    string modFolderPath;
    string modManifestPath;
    public bool Enabled
    {
        get => IsEnable();
        set => ToggleMod(value);
    }
    public void ToggleMod(bool setNewActive)
    {
        if (setNewActive == Enabled) return;

        Console.WriteLine($"try toggle mod to: {setNewActive} for mod: {modFolderPath}");
        try
        {
            var oldPath = modFolderPath;
            var parent = Path.GetDirectoryName(oldPath)!;
            var folderName = Path.GetFileName(oldPath);

            // ตัดจุดนำหน้าออกแค่ตัวเดียว (ถ้ามี) เพื่อไม่ทำชื่อจริงที่ขึ้นต้นด้วย "." เสียหาย
            var baseName = folderName.StartsWith(".") ? folderName.Substring(1) : folderName;
            var newName = setNewActive ? baseName : "." + baseName;
            var newPath = Path.Combine(parent, newName);

            if (Directory.Exists(newPath))
                throw new IOException($"Target already exists: {newPath}");

            Directory.Move(oldPath, newPath);

            //update paths
            UpdatePaths(newPath);
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex, "Error try set mod active: " + modFolderPath);
        }
    }

    void UpdatePaths(string newFolderPath)
    {
        modFolderPath = newFolderPath;
        modManifestPath = Path.Combine(newFolderPath, "manifest.json");
    }

    private bool IsEnable()
    {
        var folderName = Path.GetFileName(modFolderPath);
        return !string.IsNullOrEmpty(folderName) && !folderName.StartsWith('.');
    }

    public void ComputeSize()
    {
        try
        {
            long total = 0;
            foreach (var filePath in Directory.EnumerateFiles(modFolderPath, "*", SearchOption.AllDirectories))
                total += new FileInfo(filePath).Length;
            SizeBytes = total;
            HasSize = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error try compute size of mod: {modFolderPath}\n{ex}");
        }
    }

    public ModInfo(string manifestFilePath)
    {
        try
        {
            this.modManifestPath = manifestFilePath;
            this.modFolderPath = Path.GetDirectoryName(manifestFilePath);
            this.LastModified = File.GetLastWriteTime(manifestFilePath);

            var manifestText = File.ReadAllText(manifestFilePath);
            var manifest = JObject.Parse(manifestText);

            this.modName = manifest["Name"]?.ToString();
            this.modVersion = manifest["Version"]?.ToString();

            this.NameUIText = modName;
            this.VersionUIText = $"Version: {modVersion}";
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex, "Error try parser mod folder path: " + this.modFolderPath);
        }

        this.NameUIText = modName;
        this.VersionUIText = $"Version: {modVersion}";
    }

}
