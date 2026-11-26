using Android.Content;
using System;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher;

public class GameInfoCard : InfoCard
{
    const string InstalledStatusText = "Installed";
    const string NotInstalledStatusText = "Not Installed";

    public GameInfoCard(Context context) : base(context)
    {
        var installed = GameApkTool.IsInstalled;

        var status = AddHeader("App Game");
        SetStatus(status, installed, InstalledStatusText, NotInstalledStatusText);
        AddDivider();

        AddTextLine($"Your Game Version: {GameApkTool.CurrentGameVersion}", 15, emphasized: true);
        AddTextLine($"Support Minimum Version: {GameApkTool.SupportMinimumGameVersion}");
        AddTextLine($"Verify: {GetVerifyText(installed)}");
        AddTextLine($"Launcher Version: {AppInfo.VersionString}");
        AddTextLine(GetLauncherBuildText());
    }

    static string GetVerifyText(bool installed)
    {
        if (installed)
            return $"Installed From {GameApkTool.GameInstalledFromStoreName}";

        if (GameApkTool.CurrentPackageInfo != null)
            return $"Please Download Game Official From {GameApkTool.GameInstalledFromStoreName}";

        return "Please Download Game Official From Play Store Or Galaxy Store";
    }

    static string GetLauncherBuildText()
    {
        var buildTime = DateTimeOffset
            .FromUnixTimeSeconds(long.Parse(AppInfo.BuildString))
            .ToLocalTime();
        return $"Build: {buildTime:HH:mm:ss dd/MM/yyyy} (d/m/y)";
    }
}
