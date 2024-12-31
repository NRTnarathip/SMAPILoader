using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text.Json.Nodes;

namespace SMAPIGameLoader.Launcher;

public class ModItemView
{
    public string NameText = "Unknow";
    public string VersionText = "Unknow";
    public string FolderPathText = "Unknow";

    public readonly string modName = "unknow";
    public readonly string modVersion = "unknow";
    public readonly string modFolderPath = "unknow";

    public ModItemView(string manifestFilePath, int modListIndex)
    {
        try
        {
            var manifestText = File.ReadAllText(manifestFilePath);
            var manifest = JObject.Parse(manifestText);

            this.modName = manifest["Name"].ToString();
            this.modVersion = manifest["Version"].ToString();

            this.NameText = $"[{modListIndex + 1}]: {modName}";
            this.VersionText = $"Version: {modVersion}";

            this.modFolderPath = Path.GetDirectoryName(manifestFilePath);
            var relativeModDir = modFolderPath.Substring(modFolderPath.IndexOf("/Mods") + 5);
            FolderPathText = $"Folder: {relativeModDir}";
        }
        catch (Exception ex)
        {
            this.modFolderPath = Path.GetDirectoryName(manifestFilePath);
            FolderPathText = modFolderPath;
            ErrorDialogTool.Show(ex, "Error try parser mod folder path: " + this.modFolderPath);
        }

        this.NameText = $"[{modListIndex + 1}]: {modName}";
        this.VersionText = $"Version: {modVersion}";
    }
}
