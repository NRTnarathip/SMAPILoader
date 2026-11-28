using Android.Content;
using Android.Graphics;
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

    enum UpdateState { Idle, Checking, Update, Latest }

    readonly TextView _version;
    readonly TextView _buildCode;
    readonly TextView _buildDate;
    readonly TextView _status;
    readonly Button _installButton;
    readonly Button _uninstallButton;
    readonly Button _updateButton;
    UpdateState _updateState;

    public SMAPIInfoCard(Context context) : base(context)
    {
        _status = AddHeader("SMAPI");
        AddDivider();

        _version = AddRow("Version");
        _buildCode = AddRow("Build Code");
        _buildDate = AddRow("Build Date");

        var actions = new LinearLayout(Context) { Orientation = Orientation.Horizontal };
        actions.SetGravity(GravityFlags.CenterHorizontal);
        actions.SetPadding(0, UI.Dp(Context, 16), 0, 0);

        _uninstallButton = CreateActionButton("Uninstall", OnClickUninstall, primary: false);
        actions.AddView(_uninstallButton, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        {
            RightMargin = UI.Dp(Context, 10)
        });

        _installButton = CreateActionButton("Install", OnClickInstall, primary: true);
        actions.AddView(_installButton, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));

        Content.AddView(actions);

        _updateButton = CreateActionButton("Check Update", OnClickCheckUpdate, primary: false);
        _updateButton.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        { TopMargin = UI.Dp(Context, 10) };
        Content.AddView(_updateButton);

        Refresh();
    }

    public void Refresh()
    {
        var installed = SMAPIInstaller.IsInstalled;
        var buildCode = SMAPIInstaller.GetBuildCode();

        _version.Text = SMAPIInstaller.GetCurrentVersion().ToString();
        _buildCode.Text = buildCode.ToString();
        _buildDate.Text = FormatBuildDate(buildCode);
        SetStatus(_status, installed, "Installed", "Missing");

        _installButton.Text = installed ? "Reinstall" : "Install";
        _uninstallButton.Enabled = installed;
        _uninstallButton.Alpha = installed ? 1f : 0.4f;

        // ข้อมูล release เก่าอาจไม่ตรงกับเวอร์ชันที่เพิ่งติดตั้ง ให้เช็คใหม่ก่อนเสมอ
        SetUpdateState(UpdateState.Idle);
    }

    async void OnClickCheckUpdate(object sender, EventArgs e)
    {
        if (_updateState == UpdateState.Update)
        {
            OpenUpdateLink();
            return;
        }

        if (_updateState != UpdateState.Idle)
            return;

        SetUpdateState(UpdateState.Checking);
        try
        {
            var github = new GitHubClient(new ProductHeaderValue("SMAPI-Android-Launcher"));
            var release = await github.Repository.Release.GetLatest(
                SMAPIInstaller.GithubOwner, SMAPIInstaller.GithubRepoName);

            var latest = ParseVersion(release.TagName);
            if (latest is null)
            {
                SetUpdateState(UpdateState.Idle);
                return;
            }

            SetUpdateState(latest > SMAPIInstaller.GetCurrentVersion()
                ? UpdateState.Update
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
            case UpdateState.Update:
                text = "Update";
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

    /// <summary>tag บางตัวมี prefix เช่น "SMAPI-4.3.2.1-(1762834431)" — ดึงเอาเลขเวอร์ชันแรก</summary>
    static Version ParseVersion(string tag)
    {
        var match = Regex.Match(tag ?? "", @"\d+(?:\.\d+)+");
        return match.Success && Version.TryParse(match.Value, out var version) ? version : null;
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
        button.SetMinWidth(UI.Dp(Context, 96));
        button.SetMinHeight(UI.Dp(Context, 44));
        button.StateListAnimator = null;
        button.SetPadding(
            UI.Dp(Context, 18), 0,
            UI.Dp(Context, 18), 0);
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
