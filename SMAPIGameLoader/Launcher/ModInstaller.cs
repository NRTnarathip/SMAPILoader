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
            var manifestText = ReadManifest(manifestEntry);
            var manifestJson = JObject.Parse(manifestText);
            string modName = manifestJson["Name"].ToString();
            //check mod support
            if (AssertModISupport(manifestJson) is false)
            {
                return;
            }


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

            DialogTool.Show("Successfully Install Mod", modLogBuilder.ToString());
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
}
