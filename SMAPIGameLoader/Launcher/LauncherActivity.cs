using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Xamarin.Essentials;


namespace SMAPIGameLoader.Launcher;

[Activity(
    Label = "SMAPI Launcher",
    MainLauncher = true,
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.SensorPortrait
)]
public class LauncherActivity : Activity
{
    bool AssetGameVerify()
    {
        try
        {
            if (StardewApkTool.IsInstalled == false)
            {
                ToastNotifyTool.Notify("Please Download Game From Play Store");
                return false;
            }
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify("err;" + ex);
            return false;
        }

        return true;
    }
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Platform.Init(this, savedInstanceState);

        SetContentView(Resource.Layout.layout1);

        if (AssetGameVerify() == false)
        {
            Finish();
            return;
        }

        // Create your application here
        var installSMAPIBtn = FindViewById<Button>(Resource.Id.InstallSMAPI);
        installSMAPIBtn.Click += (sender, e) =>
        {
            SMAPIInstaller.OnClickInstall();
        };
        var startGameBtn = FindViewById<Button>(Resource.Id.StartGame);
        startGameBtn.Click += (sender, e) =>
        {
            OnClickStartGame();
        };
        var installModZipBtn = FindViewById<Button>(Resource.Id.InstallMod);
        installModZipBtn.Click += (sender, e) =>
        {
            ModInstaller.OnClickInstallMod();
        };

        try
        {

            //set app version
            var appVersionTextView = FindViewById<TextView>(Resource.Id.appVersionTextView);
            appVersionTextView.Text = "Launcher Version: " + AppInfo.VersionString;
            DateTimeOffset buildDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(int.Parse(AppInfo.BuildString));
            var localDateTimeOffset = buildDateTimeOffset.ToLocalTime();
            var localDateTimeString = localDateTimeOffset.ToString("HH:mm:ss dd/MM/yyyy");
            FindViewById<TextView>(Resource.Id.appBuildDate).Text = $"Build: {localDateTimeString} (Day/Month/Year)";

            //set support game version
            var supportGameVersionTextView = FindViewById<TextView>(Resource.Id.supportGameVersionTextView);
            supportGameVersionTextView.Text = $"Support Game Version: {StardewApkTool.GameVersionSupport} Or Above";
            var yourGameVersion = FindViewById<TextView>(Resource.Id.yourGameVersion);
            yourGameVersion.Text = "Your Game Version: " + StardewApkTool.CurrentGameVersion;
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify("Error:Try setup app text info: " + ex);
        }
    }

    void OnClickStartGame()
    {
        EntryGameActivity.LaunchGameActivity(this);
    }
}