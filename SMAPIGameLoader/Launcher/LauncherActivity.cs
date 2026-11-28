using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using SMAPIGameLoader.Launcher.Pages;
using SMAPIGameLoader.Tool;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Essentials;
using static Android.Provider.DocumentsContract;


namespace SMAPIGameLoader.Launcher;

[Activity(
    Label = "SMAPI Launcher",
    MainLauncher = true,
    Theme = "@style/Theme.SMAPILauncher",
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
        LauncherTheme.Load();
        SetupLauncherPage();
        SetDarkMode();

        //run utils scripts
        ProcessAdbExtras();

#if DEBUG
        RegisterMcpReceiver();
#endif
    }

#if DEBUG
    private McpDebugReceiver? _mcpReceiver;

    private void RegisterMcpReceiver()
    {
        _mcpReceiver = new McpDebugReceiver();
        var filter = new IntentFilter(McpDebugReceiver.Action);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            RegisterReceiver(_mcpReceiver, filter, ReceiverFlags.Exported);
        else
            RegisterReceiver(_mcpReceiver, filter);
    }

    private void UnregisterMcpReceiver()
    {
        if (_mcpReceiver is null)
            return;

        UnregisterReceiver(_mcpReceiver);
        _mcpReceiver = null;
    }
#endif

    protected override void OnDestroy()
    {
#if DEBUG
        UnregisterMcpReceiver();
#endif
        base.OnDestroy();
    }

    private void SetDarkMode()
    {
        AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightYes;
        Window?.SetStatusBarColor(LauncherTheme.Parse(LauncherTheme.HeaderBackgroundHex));
        Window?.SetNavigationBarColor(LauncherTheme.Parse(LauncherTheme.NavBarBackgroundHex));

        // ไอคอน status/nav bar ต้องตรงกับความสว่างของพื้นหลัง
        if (Window?.DecorView is null)
            return;

        const SystemUiFlags statusBar = SystemUiFlags.LightStatusBar;
        const SystemUiFlags navBar = SystemUiFlags.LightNavigationBar;
        var flags = Window.DecorView.SystemUiVisibility;
        flags = LauncherTheme.StatusBarDarkIcons
            ? flags | (StatusBarVisibility)statusBar
            : flags & ~(StatusBarVisibility)statusBar;
        flags = LauncherTheme.NavBarDarkIcons
            ? flags | (StatusBarVisibility)navBar
            : flags & ~(StatusBarVisibility)navBar;
        Window.DecorView.SystemUiVisibility = flags;
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
    }

    public const int HomeId = 1;
    public const int ModManagerId = 2;
    public const int LogPageId = 3;
    public const int ContactPageId = 4;
    public const int SettingsPageId = 5;

    /// <summary>ชื่อหน้าปัจจุบัน (ใช้โดย McpDebugReceiver/GetState)</summary>
    public string CurrentPageName { get; private set; } = "home";

    private FrameLayout _container = null!;
    private View _homePage = null!;
    private ModManagerPage _modPage = null!;
    private LogPage _logPage = null!;
    private ContactPage _contactPage = null!;
    private SettingsPage _settingsPage = null!;
    private ModConfigPage _configPage = null!;
    private LinearLayout _nav = null!;
    private LinearLayout _navContainer = null!;
    private readonly List<(int Id, ImageView Icon, TextView Label)> _navItems = new();
    // static เพื่อคงแท็บเดิมหลัง Recreate ตอนสลับธีม
    private static int _selectedNavId = HomeId;

    private void SetupLauncherPage()
    {
        _container = new FrameLayout(this);
        _container.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 0, 1f);

        _homePage = new HomePage(this);
        _modPage = new ModManagerPage(this);
        _logPage = new LogPage(this);
        _contactPage = new ContactPage(this);
        _settingsPage = new SettingsPage(this);
        _container.AddView(_homePage);
        _container.AddView(_modPage);
        _container.AddView(_logPage);
        _container.AddView(_contactPage);
        _container.AddView(_settingsPage);

        // sub-page: เพิ่มเป็นตัวสุดท้ายเพื่อให้วาดทับหน้าที่ถูกซ่อนอยู่
        _configPage = new ModConfigPage(this) { Visibility = ViewStates.Gone };
        _container.AddView(_configPage);

        // Bottom navigation ทำเองแบบ compact (M3 BottomNavigationView ล็อก minHeight 80dp
        // และเหลือแถบว่างใต้ label ที่ปรับไม่ได้)
        _nav = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        _nav.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.NavBarBackgroundHex));
        _nav.SetPadding(0, UI.Dp(this, 4), 0, UI.Dp(this, 4));
        AddNavItem(HomeId, Resource.Drawable.ic_nav_home, "Home");
        AddNavItem(ModManagerId, Resource.Drawable.ic_nav_mods, "Mods");
        AddNavItem(LogPageId, Resource.Drawable.ic_nav_log, "Log");
        AddNavItem(ContactPageId, Resource.Drawable.ic_nav_network, "Social");
        AddNavItem(SettingsPageId, Resource.Drawable.ic_nav_settings, "Settings");

        // เส้นคั่นบาง ๆ ระหว่าง content กับ bottom nav
        var navDivider = new View(this);
        navDivider.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.DividerHex));
        _navContainer = new LinearLayout(this) { Orientation = Orientation.Vertical };
        _navContainer.AddView(navDivider, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, UI.Dp(this, 1)));
        _navContainer.AddView(_nav);

        // Root: แนวตั้ง ใช้เต็มจอ
        var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        root.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.BackgroundHex));
        root.LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        root.AddView(_container);
        root.AddView(_navContainer);
        SetContentView(root);

        // run first page
        SelectNavItem(_selectedNavId);
    }

    void AddNavItem(int id, int iconRes, string label)
    {
        var item = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical,
            Clickable = true,
            Focusable = true
        };
        item.SetGravity(GravityFlags.Center);
        item.SetPadding(0, UI.Dp(this, 4), 0, UI.Dp(this, 4));
        item.LayoutParameters = new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f);
        item.Background = UI.CreateRippleDrawable(this, null, LauncherTheme.ChipRadiusDp);

        var icon = new ImageView(this);
        icon.SetImageResource(iconRes);
        icon.LayoutParameters = new LinearLayout.LayoutParams(
            UI.Dp(this, 22), UI.Dp(this, 22));
        item.AddView(icon);

        var text = new TextView(this) { Text = label, Gravity = GravityFlags.Center };
        text.SetTextSize(ComplexUnitType.Sp, 12);
        text.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        text.SetPadding(0, UI.Dp(this, 2), 0, 0);
        item.AddView(text);

        item.Click += (s, e) => SelectNavItem(id);

        _nav.AddView(item);
        _navItems.Add((id, icon, text));
    }

    /// <summary>ไฮไลต์ tab ที่เลือก + สลับหน้า</summary>
    void SelectNavItem(int id)
    {
        _selectedNavId = id;
        var selected = LauncherTheme.Parse(LauncherTheme.AccentHex);
        var normal = LauncherTheme.Parse(LauncherTheme.NavInactiveHex);

        foreach (var (itemId, icon, label) in _navItems)
        {
            var color = itemId == id ? selected : normal;
            icon.SetColorFilter(color);
            label.SetTextColor(color);
        }

        ShowPage(id);
    }

    private void ShowPage(int itemId)
    {
        _configPage.Visibility = ViewStates.Gone;
        _navContainer.Visibility = ViewStates.Visible;

        _homePage.Visibility = itemId == HomeId ? ViewStates.Visible : ViewStates.Gone;
        _modPage.Visibility = itemId == ModManagerId ? ViewStates.Visible : ViewStates.Gone;
        _logPage.Visibility = itemId == LogPageId ? ViewStates.Visible : ViewStates.Gone;
        _contactPage.Visibility = itemId == ContactPageId ? ViewStates.Visible : ViewStates.Gone;
        _settingsPage.Visibility = itemId == SettingsPageId ? ViewStates.Visible : ViewStates.Gone;

        if (itemId == LogPageId)
            _logPage.Reload();

        CurrentPageName = itemId switch
        {
            ModManagerId => "mods",
            LogPageId => "log",
            ContactPageId => "contact",
            SettingsPageId => "settings",
            _ => "home",
        };
    }

    /// <summary>นำทางไปหน้าที่กำหนด (ใช้โดย McpDebugReceiver) — ต้องเรียกบน main thread</summary>
    public void NavigateTo(int itemId)
    {
        if (_selectedNavId == itemId)
            ShowPage(itemId);
        else
            SelectNavItem(itemId);
    }

    /// <summary>เปิดหน้าแก้ JSON ของ mod (sub-page ของแท็บ Mods)</summary>
    public void ShowModConfig(ModInfo mod)
    {
        SelectNavItem(ModManagerId);
        _modPage.Visibility = ViewStates.Gone;
        _configPage.Open(mod);
        _configPage.Visibility = ViewStates.Visible;
        _navContainer.Visibility = ViewStates.Gone;
        CurrentPageName = "mod-config";
    }

    public override void OnBackPressed()
    {
        if (_configPage.Visibility == ViewStates.Visible)
        {
            if (_configPage.HandleBack())
                return;

            // อยู่โหมด list แล้ว → กลับแท็บ Mods
            SelectNavItem(_selectedNavId);
            return;
        }

        base.OnBackPressed();
    }

    public void OnClickStartGame()
    {
        Console.WriteLine("On click start game");
        LaunchGameActivity(this);
        Console.WriteLine("done continue UI runner");
    }

    public static void LaunchGameActivity(Activity launcherActivity)
    {
        TaskTool.Run(launcherActivity, async () =>
        {
            LaunchGameActivityInternal(launcherActivity);
        });
    }

    static void LaunchGameActivityInternal(Activity launcherActivity)
    {
        //ToastNotifyTool.Notify("Starting Game..");
        //check game it's can launch with version

        try
        {
            TaskTool.NewLine("Checking game version");
            if (GameApkTool.IsGameVersionSupport is false)
            {
                TaskTool.Fail("Game version " + GameApkTool.CurrentGameVersion
                    + " is not supported. Please update Stardew Valley.");
                return;
            }

            TaskTool.NewLine("Checking SMAPI installation");
            if (SMAPIInstaller.IsInstalled is false)
            {
                TaskTool.Fail("SMAPI is not installed. Install SMAPI from the SMAPI card first.");
                return;
            }

            GameCloner.Setup();

            // Dev เป็น debug build (define DEBUG) จึงไม่ควรเข้าเกมได้
#if DEBUG
            TaskTool.Fail("The game can't start in a Debug build. Use a Release build to play.");
            return;
#endif

            TaskTool.NewLine("Launching game...");
            StartSMAPIActivity(launcherActivity);
        }
        catch (Exception ex)
        {
            TaskTool.Fail(ex.Message);
        }
    }
    //prevent Load Game Assembly in scope function LaunchGameActivityInternal()
    static void StartSMAPIActivity(Activity launcherActivity)
    {
        var intent = new Intent(launcherActivity, typeof(SMAPIActivity));
        intent.AddFlags(ActivityFlags.ClearTask);
        intent.AddFlags(ActivityFlags.NewTask);
        launcherActivity.StartActivity(intent);
        launcherActivity.Finish();
    }

}