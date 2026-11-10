using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.BottomNavigation;
using SMAPIGameLoader.Launcher.Pages;
using SMAPIGameLoader.Tool;
using System;
using System.Text;
using Xamarin.Essentials;
using static Android.Provider.DocumentsContract;


namespace SMAPIGameLoader.Launcher;

[Activity(
    Label = "SMAPI Launcher",
    MainLauncher = true,
    Theme = "@style/Theme.Material3.DayNight.NoActionBar",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.FullSensor
)]
public class LauncherActivity : AppCompatActivity
{
    public static LauncherActivity Instance { get; private set; }

    private static bool IsDeviceSupport => IntPtr.Size == 8;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Instance = this;
        base.OnCreate(savedInstanceState);

        Platform.Init(this, savedInstanceState);
        ActivityTool.Init(this);

        //assert
        AssertRequirement();

        //ready
        SetupLauncherPage();
        SetDarkMode();

        //run utils scripts
        ProcessAdbExtras();
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        FilePickerTool.OnActivityResult(requestCode, resultCode, data);
    }

    private void SetDarkMode()
    {
        AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightYes;
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
    }

    private const int HomeId = 1;
    private const int ModsId = 2;

    private FrameLayout _container = null!;
    private View _homePage = null!;
    private ModManagerPage _modPage = null!;
    private BottomNavigationView _nav = null!;

    private void SetupLauncherPage()
    {
        _container = new FrameLayout(this);
        _container.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 0, 1f);

        _homePage = new HomePage(this);
        _modPage = new ModManagerPage(this);
        _container.AddView(_homePage);
        _container.AddView(_modPage);

        // Bottom navigation อยู่ล่างสุด
        _nav = new BottomNavigationView(this);
        _nav.SelectedItemId = HomeId;
        _nav.Menu.Add(0, HomeId, 0, "Home").SetIcon(Android.Resource.Drawable.IcMenuMyCalendar);
        _nav.Menu.Add(0, ModsId, 1, "Mods").SetIcon(Android.Resource.Drawable.IcMenuAgenda);
        _nav.ItemSelected += (s, e) =>
        {
            ShowPage(e.Item!.ItemId);
            e.Handled = true;
        };


        // Root: แนวตั้ง ใช้เต็มจอ
        var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        root.LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        root.AddView(_container);
        root.AddView(_nav);
        SetContentView(root);

        // run first page
        ShowPage(HomeId);
    }

    private void ShowPage(int itemId)
    {
        _homePage.Visibility = itemId == HomeId ? ViewStates.Visible : ViewStates.Gone;
        _modPage.Visibility = itemId == ModsId ? ViewStates.Visible : ViewStates.Gone;
    }

    public void OnClickStartGame()
    {
        Console.WriteLine("On click start game");
        EntryGame.LaunchGameActivity(this);
        Console.WriteLine("done continue UI runner");
    }
}