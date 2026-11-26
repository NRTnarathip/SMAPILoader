using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;

namespace SMAPIGameLoader.Launcher;

public class SMAPIInfoCard : InfoCard
{
    const string PrimaryButtonColorHex = "#1565C0";
    const string OutlineColorHex = "#33FFFFFF";

    readonly TextView _version;
    readonly TextView _buildCode;
    readonly TextView _buildDate;
    readonly TextView _status;
    readonly Button _installButton;
    readonly Button _uninstallButton;

    public SMAPIInfoCard(Context context) : base(context)
    {
        _status = AddHeader("SMAPI");
        AddDivider();

        _version = AddRow("Version");
        _buildCode = AddRow("Build Code");
        _buildDate = AddRow("Build Date");

        var actions = new LinearLayout(Context) { Orientation = Orientation.Horizontal };
        actions.SetGravity(GravityFlags.CenterHorizontal);
        actions.SetPadding(0, Ui.Dp(Context, 16), 0, 0);

        _uninstallButton = CreateActionButton("Uninstall", OnClickUninstall, primary: false);
        actions.AddView(_uninstallButton, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        {
            RightMargin = Ui.Dp(Context, 10)
        });

        _installButton = CreateActionButton("Install", OnClickInstall, primary: true);
        actions.AddView(_installButton, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));

        Content.AddView(actions);

        Refresh();
    }

    public void Refresh()
    {
        var installed = SMAPIInstaller.IsInstalled;
        var buildCode = SMAPIInstaller.GetBuildCode();

        _version.Text = SMAPIInstaller.GetCurrentVersion().ToString();
        _buildCode.Text = buildCode.ToString();
        _buildDate.Text = FormatBuildDate(buildCode);
        SetStatus(_status, installed, "Installed", "Not installed");

        _installButton.Text = installed ? "Reinstall" : "Install";
        _uninstallButton.Enabled = installed;
        _uninstallButton.Alpha = installed ? 1f : 0.4f;
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
        button.SetTypeface(null, TypefaceStyle.Bold);
        button.SetTextColor(Color.White);
        button.SetMinWidth(Ui.Dp(Context, 96));
        button.SetMinHeight(Ui.Dp(Context, 44));
        button.SetPadding(
            Ui.Dp(Context, 18), 0,
            Ui.Dp(Context, 18), 0);
        button.Background = primary
            ? Ui.CreateRippleDrawable(Context, PrimaryButtonColorHex, 10)
            : Ui.CreateRippleDrawable(Context, null, 10, OutlineColorHex);
        button.Click += onClick;
        return button;
    }

    static string FormatBuildDate(long buildCode)
    {
        var localTime = DateTimeOffset.FromUnixTimeSeconds(buildCode).ToLocalTime();
        return $"{localTime:HH:mm:ss dd/MM/yyyy} (d/m/y)";
    }
}
