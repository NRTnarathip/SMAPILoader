using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;

namespace SMAPIGameLoader.Launcher.Pages;

public class HomePage : LinearLayout
{
    enum PlayGameState
    {
        GameNotInstalled,
        GameInstalled,
        GameInstalledButNotSupport,
    }

    readonly PrimaryActionButton _playGameButton;

    public HomePage(Context context) : base(context)
    {
        Orientation = Orientation.Vertical;
        SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.BackgroundHex));
        LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        string headerTitle = "Home";
        if (ApkTool.IsDebugMode) headerTitle += " (Debug Mode)";
        AddView(new PageHeader(context, headerTitle), new LayoutParams(
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
            BottomMargin = UI.Dp(context, 12)
        };

        body.AddView(new GameInfoCard(context), cardLayoutParams);
        body.AddView(new SMAPIInfoCard(context), cardLayoutParams);

        _playGameButton = CreatePrimaryButton(context, GetPlayGameButtonText());
        _playGameButton.Click += OnPlayButtonClick;
        body.AddView(_playGameButton);
    }

    PlayGameState GetPlayGameState()
    {
        if (GameApkTool.IsInstalled is false)
            return PlayGameState.GameNotInstalled;
        if (GameApkTool.IsGameVersionSupport is false)
            return PlayGameState.GameInstalledButNotSupport;
        return PlayGameState.GameInstalled;
    }

    string GetPlayGameButtonText()
    {
        switch (GetPlayGameState())
        {
            case PlayGameState.GameNotInstalled:
                return "Download Game From Store";
            case PlayGameState.GameInstalledButNotSupport:
                return "Please Update Game";
            case PlayGameState.GameInstalled:
                return "Play Game";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    static LinearLayout CreateBody(Context context)
    {
        var body = new LinearLayout(context) { Orientation = Orientation.Vertical };
        body.SetPadding(
            UI.Dp(context, 16), UI.Dp(context, 16),
            UI.Dp(context, 16), UI.Dp(context, 20));
        return body;
    }

    static PrimaryActionButton CreatePrimaryButton(Context context, string text)
    {
        var button = new PrimaryActionButton(context, text, Resource.Drawable.ic_play);
        button.LayoutParameters = new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = UI.Dp(context, 8),
            BottomMargin = UI.Dp(context, 8)
        };
        return button;
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
