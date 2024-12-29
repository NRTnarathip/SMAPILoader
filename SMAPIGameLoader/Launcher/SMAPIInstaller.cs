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
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher;

internal static class SMAPIInstaller
{
    public const string GithubOwner = "NRTnarathip";
    public const string GithubRepoName = "SMAPI-Android-1.6";
    public static long GetBuildCode()
    {
        try
        {
            if (IsInstalled is false)
            {
                return 0;
            }

            using var stream = File.OpenRead(GetInstallFilePath);
            var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(stream);
            var SMAPIAndroidBuild = assembly.MainModule.Types.Single(t => t.FullName == "StardewModdingAPI.Mobile.SMAPIAndroidBuild");
            string buildString = SMAPIAndroidBuild.Fields.Single(p => p.Name == "BuildCode").Constant as string;
            return long.Parse(buildString);
        }
        catch (Exception ex)
        {
            //ErrorDialogTool.Show(ex);
            return 0;
        }
    }

    public static Version GetCurrentVersion()
    {
        try
        {
            if (IsInstalled is false)
            {
                return null;
            }

            using var stream = File.OpenRead(GetInstallFilePath);
            var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(stream);
            var constantsType = assembly.MainModule.Types.Single(t => t.FullName == "StardewModdingAPI.EarlyConstants");
            var RawApiVersionForAndroidField = constantsType.Fields.Single(p => p.Name == "RawApiVersionForAndroid");
            string version = RawApiVersionForAndroidField.Constant as string;
            return new Version(version);
        }
        catch
        {
            return new Version(0, 0, 0, 0);
        }
    }


#if false
    public static async void OnClickInstallSMAPIOnline()
    {
        try
        {
            TaskTool.Run(ActivityTool.CurrentActivity, async () =>
            {
                try
                {
                    TaskTool.SetTitle("Install SMAPI Online");
                    var github = new GitHubClient(new ProductHeaderValue("SMPAI-Installer"));
                    TaskTool.NewLine("try get all release..");
                    var releases = await github.Repository.Release.GetAll(GithubOwner, GithubRepoName);
                    TaskTool.NewLine("found release count: " + releases.Count);
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
                        TaskTool.NewLine("found SMAPI latest file: " + smapiAssetFile.Name);
                        var smapiZipFilePath = Path.Combine(FileTool.ExternalFilesDir, smapiAssetFile.Name);
                        TaskTool.NewLine("starting download & install");
                        TaskTool.NewLine($"file size: {FileTool.ConvertBytesToMB(smapiAssetFile.Size):F2} MB");

                        using (var netClient = new HttpClient())
                        {
                            Console.WriteLine($"Retrieving {smapiAssetFile.Name}");
                            var fileData = await netClient.GetByteArrayAsync(smapiAssetFile.BrowserDownloadUrl);
                            File.WriteAllBytes(smapiZipFilePath, fileData);
                            Console.WriteLine("done save zip file at: " + smapiZipFilePath + ", file size: " + fileData.Length);
                        }

                        InstallSMAPIFromZipFile(smapiZipFilePath);

                        TaskTool.NewLine("Successfully install SMAPI: " + smapiAssetFile.Name);
                        DialogTool.Show("Successfully Install SMAPI",
                            $"done install zip: {smapiAssetFile.Name}." +
                            $"\nyou can close this");
                    }
                    else
                    {
                        TaskTool.NewLine("Not found any SMAPI");
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
#endif

    static bool IsSMAPIZipFromPickFile(FileResult pick)
    {
        var fileName = pick.FileName;

        if (fileName.EndsWith(".zip") is false)
            return false;

        if (fileName.StartsWith("SMAPI-") || fileName.StartsWith("SMAPI_"))
        {
            //check file size should less than PC
            //on PC SMAPI-4.1.10-installer-for-developers.zip
            //file size 40mb

            //on Android SMAPI-4.1.10.2-(1735397682).zip
            //file size 1.5mb
            var fileInfo = new FileInfo(pick.FullPath);

            //less than 30mb
            return FileTool.ConvertBytesToMB(fileInfo.Length) <= 30;
        }


        return false;
    }

    public static Action OnInstalledSMAPI;
    public static async void OnClickInstallSMAPIZip(object sender, EventArgs eventArgs)
    {
        try
        {

            var pick = await FilePickerTool.PickZipFile(title: "Please Pick File SMAPI-4.x.x.xxxx.zip Android");
            if (pick == null)
                return;

            //assert SMAPI it's android
            if (IsSMAPIZipFromPickFile(pick) is false)
            {
                DialogTool.Show("SMAPI Installer Error", "Please select file SMAPI-4.x.x.xxxx.zip for Android");
                return;
            }

            InstallSMAPIFromZipFile(pick.FullPath);

            DialogTool.Show("Successfully Install SMAPI",
                "done installed SMAPI from zip file: " + pick.FileName);
            OnInstalledSMAPI?.Invoke();

        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify(ex.ToString());
            Console.WriteLine(ex);
        }
    }
    static void InstallSMAPIFromZipFile(string smapiZipFilePath)
    {
        using (var zip = ZipFile.OpenRead(smapiZipFilePath))
        {
            var stardewDir = GameAssemblyManager.AssembliesDirPath;
            foreach (var entry in zip.Entries)
            {
                //remove first dir name
                //example
                //from 'SMAPI-4.1.10.2/Hello.dll'
                //to 'Hello.dll'

                string entryDirName = Path.GetDirectoryName(entry.FullName);
                string[] directoryNames = entryDirName.Split(Path.DirectorySeparatorChar);
                var rootDirName = directoryNames[0];
                var newEntryFileName = entry.FullName.Remove(0, rootDirName.Length + 1);
                var destExtractFilePath = Path.Combine(stardewDir, newEntryFileName);
                ZipFileTool.Extract(entry, destExtractFilePath);
            }
        }

        FileTool.ClearCache();
    }
    public const string StardewModdingAPIFileName = "StardewModdingAPI.dll";
    public static string GetInstallFilePath => Path.Combine(GameAssemblyManager.AssembliesDirPath, StardewModdingAPIFileName);
    public static bool IsInstalled => File.Exists(GetInstallFilePath);
}
