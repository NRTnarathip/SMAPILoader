using Android.Content;
using System;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher;

public class GameInfoCard : InfoCard
{
    public GameInfoCard(Context context) : base(context)
    {
        var installed = GameApkTool.IsInstalled;

        var status = AddHeader("Launcher & Game");
        SetStatus(status, installed, "Installed", "Missing");
        AddDivider();

        AddRow("Launcher Version").Text = AppInfo.VersionString;
        AddRow("Launcher Build").Text = GetLauncherBuildText();
        AddRow("Game Version").Text = GameApkTool.CurrentGameVersion.ToString();
        AddRow("Minimum Requirements Version").Text = GameApkTool.SupportMinimumGameVersion.ToString();
        AddRow("Is Game Support").Text = GameApkTool.IsGameVersionSupport.ToString();
        AddRow("Verify").Text = GetVerifyText(installed);
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
        return $"{buildTime:HH:mm:ss dd/MM/yyyy} (d/m/y)";
    }
}
