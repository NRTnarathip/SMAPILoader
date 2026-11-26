using Android.Content;
using Android.Views;
using Android.Widget;
using System;

namespace SMAPIGameLoader.Launcher;

public class SMAPIInfoCard : InfoCard
{
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
        actions.SetPadding(0, Ui.Dp(Context, 12), 0, 0);

        _uninstallButton = CreateActionButton("Uninstall", OnClickUninstall);
        actions.AddView(_uninstallButton, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));

        _installButton = CreateActionButton("Install", OnClickInstall);
        actions.AddView(_installButton, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        {
            RightMargin = Ui.Dp(Context, 8)
        });

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

        _installButton.Text = installed ? "Update SMAPI" : "Install SMAPI";
        _uninstallButton.Enabled = installed;
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

    Button CreateActionButton(string text, EventHandler onClick)
    {
        var button = new Button(Context) { Text = text };
        button.SetAllCaps(false);
        button.Click += onClick;
        return button;
    }

    static string FormatBuildDate(long buildCode)
    {
        var localTime = DateTimeOffset.FromUnixTimeSeconds(buildCode).ToLocalTime();
        return $"{localTime:HH:mm:ss dd/MM/yyyy} (d/m/y)";
    }
}
