using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;

namespace SMAPIGameLoader.Launcher.Pages;

public class HomePage : LinearLayout
{
    const string LaunchButtonColorHex = "#1565C0";
    const string LaunchGameText = "Launch Game";
    const string GameNotInstalledText = "Game Not Installed\nDownload Now!";

    readonly Button _playButton;

    public HomePage(Context context) : base(context)
    {
        Orientation = Orientation.Vertical;
        SetPadding(0, 0, 0, Ui.Dp(context, 16));
        LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        AddView(CreateHeader(context), new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

        var body = CreateBody(context);
        AddView(body, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

        var cardLayoutParams = new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            BottomMargin = Ui.Dp(context, 16)
        };

        body.AddView(new GameInfoCard(context), cardLayoutParams);
        body.AddView(new SMAPIInfoCard(context), cardLayoutParams);

        _playButton = CreatePrimaryButton(context, LaunchGameText, LaunchButtonColorHex);
        _playButton.Click += OnPlayButtonClick;
        body.AddView(_playButton);

        UpdatePlayButton();
    }

    static TextView CreateHeader(Context context)
    {
        var header = new TextView(context) { Text = "SMAPI Launcher" };
        header.SetTextSize(ComplexUnitType.Sp, 20);
        header.SetTextColor(Color.White);
        header.SetTypeface(null, TypefaceStyle.Bold);
        header.SetPadding(
            Ui.Dp(context, 20), Ui.Dp(context, 48),
            Ui.Dp(context, 20), Ui.Dp(context, 16));
        header.Elevation = Ui.Dp(context, 6);
        return header;
    }

    static LinearLayout CreateBody(Context context)
    {
        var body = new LinearLayout(context) { Orientation = Orientation.Vertical };
        body.SetPadding(
            Ui.Dp(context, 16), Ui.Dp(context, 16),
            Ui.Dp(context, 16), 0);
        return body;
    }

    static Button CreatePrimaryButton(Context context, string text, string colorHex)
    {
        var button = new Button(context) { Text = text };
        button.SetAllCaps(false);
        button.SetTextColor(Color.White);
        button.SetTextSize(ComplexUnitType.Sp, 16);
        button.SetTypeface(null, TypefaceStyle.Bold);
        button.Background = CreateButtonBackground(context, colorHex);
        button.SetPadding(0, Ui.Dp(context, 14), 0, Ui.Dp(context, 14));
        button.LayoutParameters = new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            BottomMargin = Ui.Dp(context, 12)
        };
        return button;
    }

    static GradientDrawable CreateButtonBackground(Context context, string colorHex)
    {
        var background = new GradientDrawable();
        background.SetShape(ShapeType.Rectangle);
        background.SetCornerRadius(Ui.Dp(context, 12));
        background.SetColor(Color.ParseColor(colorHex));
        return background;
    }

    void UpdatePlayButton()
    {
        _playButton.Text = GameApkTool.IsInstalled ? LaunchGameText : GameNotInstalledText;
    }

    void OnPlayButtonClick(object sender, EventArgs e)
    {
        if (GameApkTool.IsInstalled is false)
        {
            OpenPlayStore(Context, GameApkTool.GamePlayStorePackageName);
            return;
        }

        LauncherActivity.Instance.OnClickStartGame();
    }

    public static void OpenPlayStore(Context context, string packageName)
    {
        var uri = Android.Net.Uri.Parse($"https://play.google.com/store/apps/details?id={packageName}");
        var intent = new Intent(Intent.ActionView, uri);
        intent.AddFlags(ActivityFlags.NewTask);
        try
        {
            context.StartActivity(intent);
        }
        catch (ActivityNotFoundException e)
        {
            ErrorDialogTool.Show(e);
        }
    }
}
