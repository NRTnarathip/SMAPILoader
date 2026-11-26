using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;

namespace SMAPIGameLoader.Launcher.Pages;

public class HomePage : LinearLayout
{
    const string LaunchButtonColorHex = "#1565C0";
    const string HeaderBackgroundColorHex = "#1A1D24";
    const string LaunchGameText = "Launch Game";
    const string GameNotInstalledText = "Game Not Installed\nDownload Now!";

    readonly Button _playButton;

    public HomePage(Context context) : base(context)
    {
        Orientation = Orientation.Vertical;
        LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        AddView(CreateHeader(context), new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

        var body = CreateBody(context);
        var scroll = new ScrollView(context) { FillViewport = false };
        scroll.AddView(body, new ScrollView.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));
        AddView(scroll, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 0, 1f));

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

    static LinearLayout CreateHeader(Context context)
    {
        var headerBox = new LinearLayout(context) { Orientation = Orientation.Vertical };
        headerBox.SetBackgroundColor(Color.ParseColor(HeaderBackgroundColorHex));
        headerBox.Elevation = Ui.Dp(context, 4);
        headerBox.SetPadding(
            Ui.Dp(context, 20), Ui.Dp(context, 48),
            Ui.Dp(context, 20), Ui.Dp(context, 18));

        var header = new TextView(context) { Text = "SMAPI Launcher" };
        header.SetTextSize(ComplexUnitType.Sp, 22);
        header.SetTextColor(Color.White);
        header.SetTypeface(null, TypefaceStyle.Bold);
        header.LetterSpacing = 0.01f;
        headerBox.AddView(header);
        return headerBox;
    }

    static LinearLayout CreateBody(Context context)
    {
        var body = new LinearLayout(context) { Orientation = Orientation.Vertical };
        body.SetPadding(
            Ui.Dp(context, 20), Ui.Dp(context, 20),
            Ui.Dp(context, 20), Ui.Dp(context, 20));
        return body;
    }

    static Button CreatePrimaryButton(Context context, string text, string colorHex)
    {
        var button = new Button(context) { Text = text };
        button.SetAllCaps(false);
        button.SetTextColor(Color.White);
        button.SetTextSize(ComplexUnitType.Sp, 17);
        button.SetTypeface(null, TypefaceStyle.Bold);
        button.SetMinHeight(Ui.Dp(context, 56));
        button.SetPadding(
            Ui.Dp(context, 24), Ui.Dp(context, 14),
            Ui.Dp(context, 24), Ui.Dp(context, 14));
        button.Background = Ui.CreateRippleDrawable(context, colorHex, 14);
        button.LayoutParameters = new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Ui.Dp(context, 4),
            BottomMargin = Ui.Dp(context, 8)
        };
        return button;
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
