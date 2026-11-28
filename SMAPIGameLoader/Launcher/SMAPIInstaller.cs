using Octokit;
using SMAPIGameLoader.Tool;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
                return new Version(0, 0, 0, 0);
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

    public async static Task<Version?> AsyncGetGitLatestVersion()
    {
        try
        {
            var github = new GitHubClient(new ProductHeaderValue("SMAPI-Android-Launcher"));
            var release = await github.Repository.Release.GetLatest(
                SMAPIInstaller.GithubOwner, SMAPIInstaller.GithubRepoName);

            return ParseVersion(release.TagName);
        }
        catch
        {
        }
        return null;
    }

    public static Version ParseVersion(string tag)
    {
        var match = Regex.Match(tag ?? "", @"\d+(?:\.\d+)+");
        return match.Success && Version.TryParse(match.Value, out var version) ? version : null;
    }

    static bool IsSMAPIZipFromPickFile(FileResult pick)
    {
        var fileName = pick.FileName;

        if (fileName.EndsWith(".zip") is false)
            return false;

        if (fileName.Contains("SMAPI") && fileName.Contains("Android"))
        {
            //check file size should less than PC
            //on PC SMAPI-4.1.10-installer-for-developers.zip
            //file size 40mb

            //on Android SMAPI-4.1.10.2-(1735397682).zip
            //file size 1.5mb
            var fileInfo = new FileInfo(pick.FullPath);

            //less than 30mb
            return FileTool.ConvertBytesToMB(fileInfo.Length) <= 10;
        }


        return false;
    }

    public static Action OnInstalledSMAPI;
    public static async void OnClickInstallSMAPIZip(object sender, EventArgs eventArgs)
    {
        try
        {

            var pick = await FilePickerTool.PickZipFile(title: "Please Pick File SMAPI-Android-4.x.x.xxxx.zip Android");
            if (pick == null)
                return;

            //assert SMAPI it's android
            if (IsSMAPIZipFromPickFile(pick) is false)
            {
                DialogTool.Show("SMAPI Installer Error", "Please select file SMAPI-Android-4.x.x.xxxx.zip for Android");
                return;
            }

            InstallSMAPIFromZipFile(pick.FullPath);
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

    }
    public static void UninstallSMAPI()
    {
        if (IsInstalled is false)
            return;
        File.Delete(GetInstallFilePath);
    }

    public const string StardewModdingAPIFileName = "StardewModdingAPI.dll";
    public static string GetInstallFilePath => Path.Combine(GameAssemblyManager.AssembliesDirPath, StardewModdingAPIFileName);
    public static bool IsInstalled => File.Exists(GetInstallFilePath);
}
