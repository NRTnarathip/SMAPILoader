using Octokit;
using SMAPIGameLoader.Tool;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Launcher;

internal static class SMAPIInstaller
{
    public const string GithubOwner = "NRTnarathip";
    public const string GithubRepoName = "SMAPI-Android-1.6";
    public static async void OnClickInstall()
    {
        try
        {
            TaskTool.Run(ActivityTool.CurrentActivity, async () =>
            {
                TaskTool.SetTitle("Install SMAPI Online");
                var github = new GitHubClient(new ProductHeaderValue("SMPAI-Installer"));
                var releases = await github.Repository.Release.GetAll(GithubOwner, GithubRepoName);
                ReleaseAsset smapiAssetFile = null;
                foreach (var release in releases)
                {
                    smapiAssetFile = release.Assets.FirstOrDefault(asset => asset.Name.StartsWith("SMAPI-"));
                    if (smapiAssetFile != null)
                    {
                        break;
                    }
                }

                if (smapiAssetFile != null)
                {
                    TaskTool.AddNewLine("Found SMAPI latest file: " + smapiAssetFile.Name);
                    var smapiZipFilePath = Path.Combine(FileTool.ExternalFilesDir, smapiAssetFile.Name);
                    TaskTool.AddNewLine("Starting download & install");
                    TaskTool.AddNewLine($"File size: {FileTool.ConvertBytesToMB(smapiAssetFile.Size):F2} MB");

                    using (var netClient = new HttpClient())
                    {
                        Console.WriteLine($"Retrieving {smapiAssetFile.Name}");
                        var fileData = await netClient.GetByteArrayAsync(smapiAssetFile.BrowserDownloadUrl);
                        File.WriteAllBytes(smapiZipFilePath, fileData);
                        Console.WriteLine("done save zip file at: " + smapiZipFilePath + ", file size: " + fileData.Length);
                    }

                    InstallSMAPIFromZipFile(smapiZipFilePath);

                    //cleanup
                    File.Delete(smapiZipFilePath);

                    TaskTool.AddNewLine("Successfully install SMAPI: " + smapiAssetFile.Name);
                }
                else
                {
                    TaskTool.AddNewLine("Not found any SMAPI");
                }

                await Task.Delay(3000);
            });
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }
    }
    public static async void OnClickInstallOld()
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
            InstallSMAPIFromZipFile(pick.FullPath);
            ToastNotifyTool.Notify("Successfully Install SMAPI!");
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify(ex.ToString());
            Console.WriteLine(ex);
        }
    }
    static void InstallSMAPIFromZipFile(string filePath)
    {
        using (var zip = ZipFile.OpenRead(filePath))
        {
            foreach (var entry in zip.Entries)
            {
                string baseFolderName = Path.GetDirectoryName(entry.FullName).Split(Path.DirectorySeparatorChar)[0];
                var destFilePath = entry.FullName.Replace(baseFolderName + "/", "");
                destFilePath = Path.Combine(GameAssemblyManager.AssembliesDirPath, destFilePath);
                FileTool.MakeSureFilePath(destFilePath);
                ZipFileTool.Extract(entry, destFilePath);
            }
        }
    }
    public const string StardewModdingAPIFileName = "StardewModdingAPI.dll";
    public static bool IsInstalled =>
         File.Exists(Path.Combine(GameAssemblyManager.AssembliesDirPath, StardewModdingAPIFileName));
}
