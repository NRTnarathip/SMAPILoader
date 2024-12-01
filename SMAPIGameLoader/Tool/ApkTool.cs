using Android.App;
using Android.Content.PM;
using Android.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader;

internal static class ApkTool
{
    public static int AppBuildCode => int.Parse(AppInfo.BuildString);
    public static Version AppVersion => AppInfo.Version;

    public static PackageInfo GetPackageInfo(string PackageName)
    {
        try
        {
            var ctx = Application.Context;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                return ctx.PackageManager.GetPackageInfo(PackageName, PackageManager.PackageInfoFlags.Of(PackageInfoFlagsLong.None));
            else
                return ctx.PackageManager.GetPackageInfo(PackageName, 0);
        }
        catch (Exception e)
        {
            ToastNotifyTool.Notify("Err:GetPackageInfo(); " + e.ToString());
            return null;
        }
    }
}
