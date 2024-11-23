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
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Platform.Init(this, savedInstanceState);

        SetContentView(Resource.Layout.layout1);

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

        //set app version
        var appVersionTextView = FindViewById<TextView>(Resource.Id.appVersionTextView);
        PackageInfo packageInfo = PackageManager.GetPackageInfo(this.PackageName, 0);
        Version appVersion = new(packageInfo.VersionName);
        appVersionTextView.Text = "App Version: " + appVersion;

        //set support game version
        var supportGameVersionTextView = FindViewById<TextView>(Resource.Id.supportGameVersionTextView);
        supportGameVersionTextView.Text = $"Support Game Version: {StardewApkTool.GameVersionSupport} Or Above";
        var yourGameVersion = FindViewById<TextView>(Resource.Id.yourGameVersion);
        yourGameVersion.Text = "Your Game Version: " + StardewApkTool.CurrentGameVersion;
    }

    void OnClickStartGame()
    {
        EntryGameActivity.LaunchGameActivity(this);
    }
}