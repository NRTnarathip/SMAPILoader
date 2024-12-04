using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using Mono.Cecil;
using SMAPIGameLoader.Tool;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
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
    public static LauncherActivity Instance { get; private set; }
    static bool IsDeviceSupport => IntPtr.Size == 8;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.LauncherLayout);
        Platform.Init(this, savedInstanceState);

        //setup my sdk
        Instance = this;
        ActivityTool.Init(this);

        //ready
        if (AssetGameVerify() == false)
        {
            Finish();
            return;
        }

        //check if 32bit not support
        if (IsDeviceSupport is false)
        {
            ToastNotifyTool.Notify("Not support on device 32bit");
            Finish();
            return;
        }

        // Create your application here
        FindViewById<Button>(Resource.Id.InstallSMAPIZip).Click += (sender, e) =>
        {
            SMAPIInstaller.OnClickInstallSMAPIZip();
        };
        FindViewById<Button>(Resource.Id.InstallSMAPIOnline).Click += (sender, e) =>
        {
            SMAPIInstaller.OnClickInstallSMAPIOnline();
        };

        var startGameBtn = FindViewById<Button>(Resource.Id.StartGame);
        startGameBtn.Click += (sender, e) =>
        {
            OnClickStartGame();
        };
        var modManagerBtn = FindViewById<Button>(Resource.Id.ModManagerBtn);
        modManagerBtn.Click += (sender, e) =>
        {
            ActivityTool.SwapActivity<ModManagerActivity>(this, false);
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
            ErrorDialogTool.Show(ex);
        }
    }

    void OnClickStartGame()
    {
        EntryGame.LaunchGameActivity(this);
        Console.WriteLine("done continue UI runner");
    }
}