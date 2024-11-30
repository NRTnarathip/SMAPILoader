using Newtonsoft.Json.Linq;
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

    public ModItemView(JObject manifest, string modFolderPath)
    {
        //setup readonly
        this.modFolderPath = modFolderPath;
        modName = manifest["Name"].ToString();
        modVersion = manifest["Version"].ToString();


        //setup text for ItemView
        var modPath = modFolderPath.Substring(modFolderPath.IndexOf("/Mods"));
        FolderPathText = $"Mod Path: {modPath}";
    }
}
