using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Launcher;

internal static class SMAPIInstaller
{
    public static async void OnClickInstall()
    {
        try
        {

            var pick = await FilePickerTool.PickZipFile(title: "Please Pick File SMAPI-4.x.x.x.zip");
            if (pick == null)
                return;

            if (pick.FileName.StartsWith("SMAPI-") == false)
            {
                ToastNotifyTool.Notify("Please select file SMAPI-4.x.x.x.zip!!");
                return;
            }

            ToastNotifyTool.Notify("Starting Install SMAPI");
            using var zip = ZipFile.OpenRead(pick.FullPath);
            foreach (var entry in zip.Entries)
            {
                string baseFolderName = Path.GetDirectoryName(entry.FullName).Split(Path.DirectorySeparatorChar)[0];
                var destFilePath = entry.FullName.Replace(baseFolderName + "/", "");
                destFilePath = Path.Combine(GameAssemblyManager.AssembliesDirPath, destFilePath);
                FileTool.MakeSureFilePath(destFilePath);
                ZipFileTool.Extract(entry, destFilePath);
            }

            ToastNotifyTool.Notify("Successfully Install SMAPI!");
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify(ex.ToString());
            Console.WriteLine(ex);
        }
    }
    public const string StardewModdingAPIFileName = "StardewModdingAPI.dll";
    public static bool IsInstalled =>
         File.Exists(Path.Combine(GameAssemblyManager.AssembliesDirPath, StardewModdingAPIFileName));
}
