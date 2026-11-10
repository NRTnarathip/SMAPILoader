
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
    public Button PlayButton { get; }
    public SMAPIInfoCard SmapiCard { get; }
    public GameInfoCard GameInfoCard { get; }


    public HomePage(Context context) : base(context)
    {
        Orientation = Orientation.Vertical;
        SetPadding(0, 0, 0, Ui.Dp(context, 16));
        LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        // ===== Header =====
        var header = new TextView(context) { Text = "SMAPI Launcher" };
        header.SetTextSize(ComplexUnitType.Sp, 20);
        header.SetTextColor(Color.White);
        header.SetTypeface(null, TypefaceStyle.Bold);
        header.SetPadding(Ui.Dp(context, 20), Ui.Dp(context, 48), Ui.Dp(context, 20), Ui.Dp(context, 16));
        header.Elevation = Ui.Dp(context, 6);
        AddView(header, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

        // ===== Body =====
        var body = new LinearLayout(context) { Orientation = Orientation.Vertical };
        body.SetPadding(Ui.Dp(context, 16), Ui.Dp(context, 16), Ui.Dp(context, 16), 0);
        var scroll = new ScrollView(context);
        scroll.AddView(body, new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));
        AddView(scroll, new LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1f));

        // game is ready!


        var cardLayoutParam = new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            BottomMargin = Ui.Dp(context, 16)
        };

        GameInfoCard = new(context);
        body.AddView(GameInfoCard, cardLayoutParam);

        SmapiCard = new SMAPIInfoCard(context);
        body.AddView(SmapiCard, cardLayoutParam);

        PlayButton = CreateButton(context, "Launch Game", "#1565C0");
        PlayButton.Click += PlayButton_Click;
        body.AddView(PlayButton);

        var exportSavesButton = CreateButton(context, "Export Saves", "#455A64");
        exportSavesButton.Click += SaveManager.OnClickExportSaves;
        body.AddView(exportSavesButton);

        var importSavesButton = CreateButton(context, "Import Saves", "#455A64");
        importSavesButton.Click += SaveManager.OnClickImportSaveZip;
        body.AddView(importSavesButton);
    }

    // ===== Helper =====
    static Button CreateButton(Context context, string text, string colorHex)
    {
        var btn = new Button(context) { Text = text };
        btn.SetAllCaps(false);
        btn.SetTextColor(Color.White);
        btn.SetTextSize(ComplexUnitType.Sp, 16);
        btn.SetTypeface(null, TypefaceStyle.Bold);

        var bg = new GradientDrawable();
        bg.SetShape(ShapeType.Rectangle);
        bg.SetCornerRadius(Ui.Dp(context, 12));
        bg.SetColor(Color.ParseColor(colorHex));
        btn.Background = bg;

        btn.SetPadding(0, Ui.Dp(context, 14), 0, Ui.Dp(context, 14));
        var lp = new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            BottomMargin = Ui.Dp(context, 12)
        };
        btn.LayoutParameters = lp;
        return btn;
    }



    private void InstallSMAPIButton_Click(object sender, EventArgs e)
    {
        SMAPIInstaller.OnInstalledSMAPI = () =>
        {
            SmapiCard.Refresh();
        };
        SMAPIInstaller.OnClickInstallSMAPIZip(sender, e);
    }

    private void PlayButton_Click(object sender, System.EventArgs e)
    {
        LauncherActivity.Instance.OnClickStartGame();
    }
}
