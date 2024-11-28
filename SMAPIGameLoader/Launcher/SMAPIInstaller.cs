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
    public static Version GetCurrentVersion()
    {
        try
        {
            if (IsInstalled is false)
            {
                return null;
            }

            var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(GetInstallFilePath);
            var constantsType = assembly.MainModule.Types.Single(t => t.FullName == "StardewModdingAPI.EarlyConstants");
            var RawApiVersionForAndroidField = constantsType.Fields.Single(p => p.Name == "RawApiVersionForAndroid");
            string version = RawApiVersionForAndroidField.Constant as string;
            return new Version(version);
        }
        catch
        {
            return null;
        }
    }

    public static async void OnClickInstall()
    {
        try
        {
            TaskTool.Run(ActivityTool.CurrentActivity, async () =>
            {
                try
                {
                    TaskTool.SetTitle("Install SMAPI Online");
                    var github = new GitHubClient(new ProductHeaderValue("SMPAI-Installer"));
                    TaskTool.AddNewLine("try get all release..");
                    var releases = await github.Repository.Release.GetAll(GithubOwner, GithubRepoName);
                    TaskTool.AddNewLine("found release count: " + releases.Count);
                    var latestRelease = releases.FirstOrDefault();
                    if (latestRelease is null)
                    {
                        ErrorDialogTool.Show(new Exception("Failed install SMAPI, not found any release file"));
                        return;
                    }

                    var smapiAssetFile = latestRelease.Assets.FirstOrDefault(
                         asset => asset.Name.StartsWith("SMAPI-")
                         && asset.Name.EndsWith(".zip"));

                    if (smapiAssetFile != null)
                    {
                        TaskTool.AddNewLine("found SMAPI latest file: " + smapiAssetFile.Name);
                        var smapiZipFilePath = Path.Combine(FileTool.ExternalFilesDir, smapiAssetFile.Name);
                        TaskTool.AddNewLine("starting download & install");
                        TaskTool.AddNewLine($"file size: {FileTool.ConvertBytesToMB(smapiAssetFile.Size):F2} MB");

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
                        DialogTool.Show("Successfully Install SMAPI",
                            $"done install zip: {smapiAssetFile.Name}." +
                            $"\nyou can close this");
                    }
                    else
                    {
                        TaskTool.AddNewLine("Not found any SMAPI");
                    }

                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    ErrorDialogTool.Show(ex);
                    Console.WriteLine("error try to install SMAPI Zip: " + ex);
                }
            });
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
            Console.WriteLine("error try to install SMAPI Zip: " + ex);
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
    public static string GetInstallFilePath => Path.Combine(GameAssemblyManager.AssembliesDirPath, StardewModdingAPIFileName);
    public static bool IsInstalled => File.Exists(GetInstallFilePath);
}
