using System;
using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using SMAPIGameLoader.Tool;
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
    public static LauncherActivity Instance { get; private set; }

    private static bool IsDeviceSupport => IntPtr.Size == 8;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Instance = this;
        base.OnCreate(savedInstanceState);
        SetContentView(ResourceConstant.Layout.LauncherLayout);
        Platform.Init(this, savedInstanceState);
        ActivityTool.Init(this);

        //assert
        AssertRequirement();

        //ready
        OnReadyToSetupLayoutPage();

        //run utils scripts
        ProcessAdbExtras();
    }

    /// <summary>
    ///     Receive argument launch activity
    /// </summary>
    private void ProcessAdbExtras()
    {
        if (AdbExtraTool.IsClickStartGame(this))
        {
            OnClickStartGame();
        }
    }

    private static bool AssetGameVerify()
    {
        try
        {
            if (StardewApkTool.IsInstalled == false)
            {
                var currentPackage = StardewApkTool.CurrentPackageInfo;
                if (currentPackage != null)
                    switch (currentPackage.PackageName)
                    {
                        case StardewApkTool.GamePlayStorePackageName:
                            ToastNotifyTool.Notify("Please Download Game From Play Store");
                            break;
                        case StardewApkTool.GameGalaxyStorePackageName:
                            ToastNotifyTool.Notify("Please Download Game From Galaxy Store");
                            break;
                    }
                else
                    ToastNotifyTool.Notify("Please Download Game From Play Store Or Galaxy Store");

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

    private void AssertRequirement()
    {
        //check if 32bit not support
        if (IsDeviceSupport is false)
        {
            ToastNotifyTool.Notify("Not support on device 32bit");
            Finish();
            return;
        }

        //Assert Game Requirement
        if (AssetGameVerify() == false)
        {
            Finish();
            return;
        }

        //ready to apply patch bytes
        BypassAccessException.Apply();
    }

    private void OnReadyToSetupLayoutPage()
    {
        // Create your application here
        FindViewById<Button>(ResourceConstant.Id.InstallSMAPIZip).Click += (sender, e) =>
        {
            SMAPIInstaller.OnClickInstallSMAPIZip();
        };
        FindViewById<Button>(ResourceConstant.Id.InstallSMAPIOnline).Click += (sender, e) =>
        {
            SMAPIInstaller.OnClickInstallSMAPIOnline();
        };

        var startGameBtn = FindViewById<Button>(ResourceConstant.Id.StartGame);
        startGameBtn.Click += (sender, e) => { OnClickStartGame(); };
        var modManagerBtn = FindViewById<Button>(ResourceConstant.Id.ModManagerBtn);
        modManagerBtn.Click += (sender, e) => { ActivityTool.SwapActivity<ModManagerActivity>(this, false); };

        try
        {
            //set app version
            var appVersionTextView = FindViewById<TextView>(ResourceConstant.Id.appVersionTextView);
            appVersionTextView.Text = "Launcher Version: " + AppInfo.VersionString;
            var buildDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(int.Parse(AppInfo.BuildString));
            var localDateTimeOffset = buildDateTimeOffset.ToLocalTime();
            var localDateTimeString = localDateTimeOffset.ToString("HH:mm:ss dd/MM/yyyy");
            FindViewById<TextView>(ResourceConstant.Id.appBuildDate).Text =
                $"Build: {localDateTimeString} (Day/Month/Year)";

            //set support game version
            var supportGameVersionTextView = FindViewById<TextView>(ResourceConstant.Id.supportGameVersionTextView);
            supportGameVersionTextView.Text = $"Support Game Version: {StardewApkTool.GameVersionSupport} Or Above";
            var yourGameVersion = FindViewById<TextView>(ResourceConstant.Id.yourGameVersion);
            yourGameVersion.Text = "Your Game Version: " + StardewApkTool.CurrentGameVersion;
        }
        catch (Exception ex)
        {
            ToastNotifyTool.Notify("Error:Try setup app text info: " + ex);
            ErrorDialogTool.Show(ex);
        }
    }

    private void OnClickStartGame()
    {
        Console.WriteLine("On click start game");
        EntryGame.LaunchGameActivity(this);
        Console.WriteLine("done continue UI runner");
    }
}