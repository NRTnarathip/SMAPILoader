using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using Octokit;
using System;
using System.Text.RegularExpressions;

namespace SMAPIGameLoader.Launcher;

public class SMAPIInfoCard : InfoCard
{
    const string NexusUrl = "https://www.nexusmods.com/stardewvalley/mods/44436";

    enum UpdateState { Idle, Checking, CanUpdate, Latest }

    readonly TextView _currentVersion;
    readonly TextView _gitLatestVersion;
    readonly TextView _buildCode;
    readonly TextView _buildDate;
    readonly TextView _status;
    readonly TextView _newVersionBadge;
    readonly Button _installButton;
    readonly Button _uninstallButton;
    readonly Button _updateButton;
    UpdateState _updateState;

    public SMAPIInfoCard(Context context) : base(context)
    {
        _status = AddHeader("SMAPI");

        _newVersionBadge = AddHeaderBadge(_status);
        _newVersionBadge.Text = "New Version";
        _newVersionBadge.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextOnPrimaryHex));
        if (_newVersionBadge.Background is GradientDrawable badgeBg)
            badgeBg.SetColor(LauncherTheme.Parse(LauncherTheme.UpdateButtonHex));
        _newVersionBadge.Visibility = ViewStates.Gone;

        AddDivider();

        _currentVersion = AddRow("Installed Version");
        _gitLatestVersion = AddRow("Download Latest Version");
        _buildCode = AddRow("Build Code");
        _buildDate = AddRow("Build Date");

        var actions = new LinearLayout(Context) { Orientation = Orientation.Horizontal };
        actions.SetGravity(GravityFlags.CenterHorizontal);
        actions.SetPadding(0, UI.Dp(Context, 16), 0, 0);

        _uninstallButton = CreateActionButton("Uninstall", OnClickUninstall, primary: false);
        actions.AddView(_uninstallButton, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        {
            RightMargin = UI.Dp(Context, 8)
        });

        _installButton = CreateActionButton("Install", OnClickInstall, primary: true);
        actions.AddView(_installButton, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        {
            RightMargin = UI.Dp(Context, 8)
        });

        _updateButton = CreateActionButton("Check Update", OnClickCheckUpdate, primary: false);
        actions.AddView(_updateButton, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));

        Content.AddView(actions);

        Refresh();
    }

    public void Refresh()
    {
        var installed = SMAPIInstaller.IsInstalled;
        var buildCode = SMAPIInstaller.GetBuildCode();

        _currentVersion.Text = SMAPIInstaller.GetCurrentVersion().ToString();
        _gitLatestVersion.Text = "unknow";
        _buildCode.Text = buildCode.ToString();
        _buildDate.Text = FormatBuildDate(buildCode);
        SetStatus(_status, installed, "Installed", "Missing");

        _installButton.Text = installed ? "Reinstall" : "Install";
        _uninstallButton.Enabled = installed;
        _uninstallButton.Alpha = installed ? 1f : 0.4f;

        // ข้อมูล release เก่าอาจไม่ตรงกับเวอร์ชันที่เพิ่งติดตั้ง ให้เช็คใหม่ก่อนเสมอ
        SetUpdateState(UpdateState.Idle);
        OnClickCheckUpdate(null, null);
    }

    async void OnClickCheckUpdate(object sender, EventArgs e)
    {
        if (_updateState == UpdateState.CanUpdate)
        {
            OpenUpdateLink();
            return;
        }

        if (_updateState != UpdateState.Idle)
            return;

        SetUpdateState(UpdateState.Checking);
        try
        {
            var latestVersion = await SMAPIInstaller.AsyncGetGitLatestVersion();
            if (latestVersion == null)
            {
                SetUpdateState(UpdateState.Idle);
                return;
            }
            _gitLatestVersion.Text = latestVersion.ToString();
            SetUpdateState(latestVersion > SMAPIInstaller.GetCurrentVersion()
                ? UpdateState.CanUpdate
                : UpdateState.Latest);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Check SMAPI update failed: " + ex);
            SetUpdateState(UpdateState.Idle);
            Toast.MakeText(Context, "Failed to check update", ToastLength.Short)?.Show();
        }
    }

    void SetUpdateState(UpdateState state)
    {
        _updateState = state;

        string text, backgroundHex, textHex;
        bool enabled = true;
        switch (state)
        {
            case UpdateState.Checking:
                text = "Checking…";
                backgroundHex = LauncherTheme.SurfaceVariantHex;
                textHex = LauncherTheme.TextSecondaryHex;
                enabled = false;
                break;
            case UpdateState.CanUpdate:
                text = $"Download";
                backgroundHex = LauncherTheme.UpdateButtonHex;
                textHex = LauncherTheme.TextOnPrimaryHex;
                break;
            case UpdateState.Latest:
                text = "Latest";
                backgroundHex = LauncherTheme.SurfaceVariantHex;
                textHex = LauncherTheme.TextSecondaryHex;
                enabled = false;
                break;
            default:
                text = "Check Update";
                backgroundHex = LauncherTheme.SurfaceVariantHex;
                textHex = LauncherTheme.TextPrimaryHex;
                break;
        }

        _newVersionBadge.Visibility =
            state == UpdateState.CanUpdate ? ViewStates.Visible : ViewStates.Gone;

        _updateButton.Text = text;
        _updateButton.Enabled = enabled;
        _updateButton.Alpha = enabled ? 1f : 0.6f;
        _updateButton.SetTextColor(LauncherTheme.Parse(textHex));
        _updateButton.Background = UI.CreateRippleDrawable(
            Context, backgroundHex, LauncherTheme.ChipRadiusDp);
    }

    void OpenUpdateLink()
    {
        try
        {
            Context.StartActivity(new Intent(
                Intent.ActionView, Android.Net.Uri.Parse(NexusUrl)));
        }
        catch (Exception)
        {
            Toast.MakeText(Context, "No app can open this link", ToastLength.Short)?.Show();
        }
    }

    void OnClickUninstall(object sender, EventArgs e)
    {
        SMAPIInstaller.UninstallSMAPI();
        Refresh();
    }

    void OnClickInstall(object sender, EventArgs e)
    {
        SMAPIInstaller.OnInstalledSMAPI = Refresh;
        SMAPIInstaller.OnClickInstallSMAPIZip(sender, e);
    }

    Button CreateActionButton(string text, EventHandler onClick, bool primary)
    {
        var button = new Button(Context) { Text = text };
        button.SetAllCaps(false);
        button.SetTextSize(ComplexUnitType.Sp, 14);
        button.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        button.SetTextColor(primary
            ? LauncherTheme.Parse(LauncherTheme.TextOnPrimaryHex)
            : LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        // สามปุ่มต้องอยู่บรรทัดเดียวกัน: ลด padding/min width ให้ "Check Update" พอดีการ์ด
        button.SetMinWidth(UI.Dp(Context, 80));
        button.SetMinHeight(UI.Dp(Context, 44));
        button.StateListAnimator = null;
        button.SetPadding(
            UI.Dp(Context, 12), 0,
            UI.Dp(Context, 12), 0);
        button.Background = primary
            ? UI.CreateRippleDrawable(Context, LauncherTheme.PrimaryHex, LauncherTheme.ChipRadiusDp)
            : UI.CreateRippleDrawable(Context, LauncherTheme.SurfaceVariantHex, LauncherTheme.ChipRadiusDp);
        button.Click += onClick;
        return button;
    }

    static string FormatBuildDate(long buildCode)
    {
        var localTime = DateTimeOffset.FromUnixTimeSeconds(buildCode).ToLocalTime();
        return $"{localTime:HH:mm:ss dd/MM/yyyy} (d/m/y)";
    }
}
