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
    public const string GamePlayStorePackageName = "com.chucklefish.stardewvalley";
    public const string GameSamsungPackageName = "com.chucklefish.stardewvalleysamsung";
    static PackageInfo _currentPackageInfo;
    static bool _isCacheCurrentPackageInfo = false;
    public static PackageInfo CurrentPackageInfo
    {
        get
        {
            if (_isCacheCurrentPackageInfo is false)
            {
                _isCacheCurrentPackageInfo = true;

                var playStore = ApkTool.GetPackageInfo(GamePlayStorePackageName);
                if (playStore != null)
                    _currentPackageInfo = playStore;

                //check other package from galaxy store
                var samsung = ApkTool.GetPackageInfo(GameSamsungPackageName);
                if (samsung != null)
                    _currentPackageInfo = samsung;
            }

            Console.WriteLine("current package: " + _currentPackageInfo?.PackageName);
            return _currentPackageInfo;
        }
    }

    public static bool IsInstalled
    {
        get
        {
            try
            {
                //check if found package
                var version = CurrentPackageInfo.VersionName;
                //check if we have 3 apks: [base, split_content & split_config]
                bool haveApksValid = SplitApks?.Count == 2;
                return haveApksValid;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
    public static Android.Content.Context GetContext => Application.Context;
    public static string? BaseApkPath => CurrentPackageInfo?.ApplicationInfo?.PublicSourceDir;
    public static IList<string>? SplitApks => CurrentPackageInfo?.ApplicationInfo?.SplitSourceDirs;

    public static string? ContentApkPath => SplitApks.First(path => path.Contains("split_content"));
    public static string? ConfigApkPath => SplitApks.First(path => path.Contains("split_config"));

    public readonly static Version GameVersionSupport = Constants.GameVersionSupport;
    public static Version CurrentGameVersion => new Version(CurrentPackageInfo?.VersionName);
    public static bool IsGameVersionSupport => CurrentGameVersion >= GameVersionSupport;
}
