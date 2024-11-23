using Android.App;
using Android.Content.PM;
using Android.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader;

internal static class StardewApkTool
{
    public const string PackageName = "com.chucklefish.stardewvalley";
    public static PackageInfo PackageInfo => GetContext.PackageManager.GetPackageInfo(PackageName, 0);
    public static ApplicationInfo ApplicationInfo
    {
        get
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                return GetContext.PackageManager.GetApplicationInfo(PackageName,
                    PackageManager.ApplicationInfoFlags.Of(0));

            return GetContext.PackageManager.GetApplicationInfo(PackageName, 0);
        }
    }
    public static bool IsInstalled
    {
        get
        {
            try
            {
                var version = PackageInfo.VersionName;
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
    public static void StartGame()
    {
        var intent = GetContext.PackageManager.GetLaunchIntentForPackage(PackageName);
        GetContext.StartActivity(intent);
    }
    public static Android.Content.Context GetContext => Application.Context;
    public static string BaseApkPath => StardewApkTool.PackageInfo.ApplicationInfo.PublicSourceDir;
    public static IList<string> SplitApks => StardewApkTool.PackageInfo.ApplicationInfo.SplitSourceDirs;
    public static string ContentApkPath = SplitApks.First(path => path.Contains("split_content"));
    public static string ConfigApkPath => SplitApks.First(path => path.Contains("split_config"));

    public readonly static Version GameVersionSupport = new Version("1.6.14.3");
    public readonly static Version CurrentGameVersion = new Version(PackageInfo.VersionName);
    public static bool IsGameVersionSupport => CurrentGameVersion >= GameVersionSupport;
}
