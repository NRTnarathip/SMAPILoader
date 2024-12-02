using Android.App;
using Newtonsoft.Json.Linq;
using SMAPIGameLoader.Tool;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher;

internal static class ModInstaller
{
    public static string ModDir = Path.Combine(FileTool.ExternalFilesDir, "Mods");
    public static Version GetMinGameVersion(JObject manifest)
    {
        try
        {
            return new Version(manifest["MinimumGameVersion"].ToString());
        }
        catch
        {
            return null;
        }
    }
    public static Version GetMinSMAPIVersion(JObject manifest)
    {
        try
        {
            return new Version(manifest["MinimumApiVersion"].ToString());
        }
        catch
        {
            return null;
        }
    }
    public static bool AssertModISupport(JObject manifest)
    {
        if (SMAPIInstaller.IsInstalled is false)
        {
            ToastNotifyTool.Notify("Can't check mod, please install SMAPI first!!");
            return false;
        }

        //must be 1.6++
        var minGameVersion = GetMinGameVersion(manifest);
        if (minGameVersion != null && minGameVersion < new Version(1, 6, 0))
        {
            ToastNotifyTool.Notify("Not support for game version 1.6");
            return false;
        }

        //check smapi must be 4.0.00++
        var minSMAPIVersion = GetMinSMAPIVersion(manifest);
        if (minSMAPIVersion != null & minSMAPIVersion < new Version(4, 0, 0))
        {
            ToastNotifyTool.Notify("Not support for game version 1.6");
            return false;
        }

        bool isContentPack = manifest.ContainsKey("ContentPackFor");
        return true;
    }

    //Install Mod Zip Single, Pack Mods
    public static void InstallModPackZip(ZipArchive zip)
    {
        //extract mod
        zip.ExtractToDirectory(ModTool.ModsDir, true);
        //done

        //print log
        var entries = zip.Entries;
        var manifestEntires = entries.Where(entry => entry.Name == ModTool.ManifiestFileName).ToArray();
        var logBuilder = new StringBuilder();
        logBuilder.AppendLine("List Mods: " + manifestEntires.Length);
        for(int i = 0;i< manifestEntires.Length;i++)
        {
            var manifestEntry = manifestEntires[i];
            var modDir = manifestEntry.FullName.Replace($"/{ModTool.ManifiestFileName}", "");
            var dirInfo = new DirectoryInfo(modDir);
            logBuilder.AppendLine($"[{i + 1}]: {dirInfo.Name}");
        }

        DialogTool.Show("Installed Mod Pack", logBuilder.ToString());
    }
    public static async void OnClickInstallMod(Action OnInstalledCallback = null)
    {
        try
        {
            var pickFile = await FilePickerTool.PickZipFile();
            if (pickFile == null)
                return;

            using var zip = ZipFile.OpenRead(pickFile.FullPath);
            var entries = zip.Entries;
            var manifestEntires = entries.Where(entry => entry.Name == ModTool.ManifiestFileName).ToArray();
            if (manifestEntires.Length == 0)
            {
                ToastNotifyTool.Notify("Not found manifest.json");
                OnInstalledCallback?.Invoke();
                return;
            }

            bool isModPack = manifestEntires.Length != 1;
            if (isModPack)
            {
                InstallModPackZip(zip);
                return;
            }

            //try unpack into mods dir
            var manifestText = ReadManifest(manifestEntires[0]);
            var manifestJson = JObject.Parse(manifestText);
            string modName = manifestJson["Name"].ToString();

            var extractDestDir = Path.Combine(ModDir);
            zip.ExtractToDirectory(extractDestDir, true);
            zip.Dispose();

            var modVersion = manifestJson["Version"].ToString();
            var author = manifestJson["Author"].ToString();
            var modLogBuilder = new StringBuilder();
            modLogBuilder.AppendLine($"Name: {modName}");
            modLogBuilder.AppendLine($"Version: {modVersion}");
            modLogBuilder.AppendLine($"Author: {author}");

            var minGameVersion = GetMinGameVersion(manifestJson);
            if (minGameVersion != null)
                modLogBuilder.AppendLine($"Minimum Game Version: " + minGameVersion);

            var minSMAPIVersion = GetMinSMAPIVersion(manifestJson);
            if (minSMAPIVersion != null)
                modLogBuilder.AppendLine($"Minimum SMAPI Version: " + minSMAPIVersion);


            DialogTool.Show("Installed Mod", modLogBuilder.ToString());
            OnInstalledCallback?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }
    }
    public static string ReadManifest(ZipArchiveEntry entry)
    {
        string result;
        using (StreamReader reader = new StreamReader(entry.Open()))
        {
            result = reader.ReadToEnd();
        }
        return result;
    }
    internal static bool TryDeleteMod(string folderPath)
    {
        try
        {
            if (Directory.Exists(folderPath) is false)
                return false;

            Directory.Delete(folderPath, true);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}
