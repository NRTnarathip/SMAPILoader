using Android.App;
using Newtonsoft.Json.Linq;
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
    public static async void OnClickInstallMod()
    {
        try
        {
            var pickFile = await FilePickerTool.PickZipFile();
            if (pickFile == null)
                return;

            using var zip = ZipFile.OpenRead(pickFile.FullPath);
            var entries = zip.Entries;
            var manifestEntry = entries.FirstOrDefault(file => file.Name == "manifest.json");
            if (manifestEntry == null)
            {
                ToastNotifyTool.Notify("Not found manifest.json");
                return;
            }

            //try unpack into mods dir
            ToastNotifyTool.Notify("Wait for install...");
            var manifestText = ReadManifest(manifestEntry);
            var manifestJsonObject = JObject.Parse(manifestText);
            string modName = manifestJsonObject["Name"]?.ToString();
            if (modName == null)
                throw new Exception("Error manifest key Name is null");

            var extractDestDir = Path.Combine(ModDir, modName);
            FileTool.MakeSureDirectory(extractDestDir);
            zip.ExtractToDirectory(extractDestDir, true);
            zip.Dispose();

            var modVersion = manifestJsonObject["Version"]?.ToString();
            ToastNotifyTool.Notify("Installed mod" + $" Name:{modName}" + $" Version:{modVersion}");
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify(ex.ToString());
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
}
