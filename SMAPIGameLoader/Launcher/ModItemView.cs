using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.Json.Nodes;

namespace SMAPIGameLoader.Launcher;

public class ModItemView
{
    public string NameText => $"{modName}";
    public string VersionText => $"Version: {modVersion}";
    public string FolderPathText { get; private set; }

    public readonly string modName;
    public readonly string modVersion;
    public readonly string modFolderPath;

    public ModItemView(string manifestFilePath)
    {
        var manifestText = File.ReadAllText(manifestFilePath);
        var manifest = JObject.Parse(manifestText);

        this.modName = manifest["Name"].ToString();
        this.modVersion = manifest["Version"].ToString();

        var manifestDir = Path.GetDirectoryName(manifestFilePath);
        this.modFolderPath = manifestDir;

        var relativeModDir = modFolderPath.Substring(modFolderPath.IndexOf("/Mods"));
        FolderPathText = $"Folder: {relativeModDir}";
    }
}
