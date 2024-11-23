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

            var pick = await FilePickerHelper.PickZipFile(title: "Please Pick File SMAPI-4.x.x.x.zip");
            if (pick == null)
                return;

            if (pick.FileName.StartsWith("SMAPI-") == false)
            {
                ToastNotifyTool.Notify("Please select file SMAPI-4.x.x.x.zip!!");
                return;
            }

            //var fileStream = await pick.OpenReadAsync();
            using var zip = ZipFile.OpenRead(pick.FullPath);
            foreach (var entry in zip.Entries)
            {
                var baseFolderName = new DirectoryInfo(entry.FullName).Name;
                var destFilePath = entry.FullName.Replace(baseFolderName + "/", "");
                destFilePath = Path.Combine(GameAssemblyManager.AssembliesDirPath, destFilePath);
                FileTool.MakeSureFilePath(destFilePath);
                entry.ExtractToFile(destFilePath, true);
                Console.WriteLine("extract destFilePath:" + destFilePath);
            }

            ToastNotifyTool.Notify("Successfully Install SMAPI!");
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify(ex.ToString());
            Console.WriteLine(ex);
        }
    }
}
