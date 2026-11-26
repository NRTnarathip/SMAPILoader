using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.CardView.Widget;

namespace SMAPIGameLoader.Launcher;

/// <summary>
/// Shared CardView scaffold used by the launcher info cards:
/// dark card background, rounded corners, header with a status badge, divider and key-value rows.
/// </summary>
public abstract class InfoCard : CardView
{
    protected LinearLayout Content { get; }

    protected InfoCard(Context context) : base(context)
    {
        SetCardBackgroundColor(LauncherTheme.Parse(LauncherTheme.CardBackgroundHex));
        Radius = Ui.Dp(context, LauncherTheme.CardRadiusDp);
        CardElevation = 0;
        UseCompatPadding = false;
        LayoutParameters = new ViewGroup.MarginLayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);

        Content = new LinearLayout(context) { Orientation = Orientation.Vertical };
        Content.SetPadding(
            Ui.Dp(context, 18), Ui.Dp(context, 16),
            Ui.Dp(context, 18), Ui.Dp(context, 16));
        AddView(Content);
    }

    protected TextView AddHeader(string titleText, int titleSizeSp = 17)
    {
        var header = new LinearLayout(Context) { Orientation = Orientation.Horizontal };
        header.SetGravity(GravityFlags.CenterVertical);

        var title = new TextView(Context) { Text = titleText };
        title.SetTextSize(ComplexUnitType.Sp, titleSizeSp);
        title.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        title.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        title.LetterSpacing = 0.01f;
        header.AddView(title, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f)
        {
            RightMargin = Ui.Dp(Context, 12)
        });

        var status = CreateStatusBadge(Context);
        header.AddView(status);

        Content.AddView(header);
        return status;
    }

    protected static void SetStatus(
        TextView status, bool isActive, string activeText, string inactiveText)
    {
        status.Text = isActive ? activeText : inactiveText;
        status.SetTextColor(LauncherTheme.Parse(
            isActive ? LauncherTheme.StatusOkTextHex : LauncherTheme.StatusErrorTextHex));
        if (status.Background is GradientDrawable background)
            background.SetColor(LauncherTheme.Parse(
                isActive ? LauncherTheme.StatusOkBackgroundHex : LauncherTheme.StatusErrorBackgroundHex));
    }

    protected void AddDivider()
    {
        var divider = new View(Context);
        divider.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.DividerHex));
        Content.AddView(divider, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, Ui.Dp(Context, 1))
        {
            TopMargin = Ui.Dp(Context, 14),
            BottomMargin = Ui.Dp(Context, 8)
        });
    }

    protected TextView AddRow(string label)
    {
        var row = new LinearLayout(Context) { Orientation = Orientation.Horizontal };
        row.SetGravity(GravityFlags.CenterVertical);
        row.SetPadding(0, Ui.Dp(Context, 7), 0, Ui.Dp(Context, 7));

        var key = new TextView(Context) { Text = label };
        key.SetTextSize(ComplexUnitType.Sp, 13);
        key.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        row.AddView(key, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f));

        var value = new TextView(Context) { Text = "-" };
        value.SetTextSize(ComplexUnitType.Sp, 14);
        value.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        value.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        row.AddView(value);

        Content.AddView(row);
        return value;
    }

    protected TextView AddTextLine(string text, int textSizeSp = 13, bool emphasized = false)
    {
        var textView = new TextView(Context) { Text = text };
        textView.SetTextSize(ComplexUnitType.Sp, textSizeSp);
        textView.SetTextColor(LauncherTheme.Parse(
            emphasized ? LauncherTheme.TextPrimaryHex : LauncherTheme.TextSecondaryHex));
        textView.SetTypeface(
            emphasized ? LauncherTheme.TitleTypeface : LauncherTheme.BodyTypeface,
            TypefaceStyle.Normal);
        textView.SetLineSpacing(0, 1.25f);
        Content.AddView(textView, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Ui.Dp(Context, emphasized ? 2 : 6)
        });
        return textView;
    }

    static TextView CreateStatusBadge(Context context)
    {
        var status = new TextView(context);
        status.SetTextSize(ComplexUnitType.Sp, 11);
        status.SetTextColor(LauncherTheme.Parse(LauncherTheme.StatusErrorTextHex));
        status.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        status.LetterSpacing = 0.03f;
        status.SetIncludeFontPadding(false);
        status.Gravity = GravityFlags.Center;
        status.SetPadding(
            Ui.Dp(context, 10), Ui.Dp(context, 5),
            Ui.Dp(context, 10), Ui.Dp(context, 5));
        status.Background = CreateStatusBadgeBackground(context, LauncherTheme.StatusErrorBackgroundHex);
        return status;
    }

    static GradientDrawable CreateStatusBadgeBackground(Context context, string colorHex)
    {
        var background = new GradientDrawable();
        background.SetShape(ShapeType.Rectangle);
        background.SetCornerRadius(Ui.Dp(context, 100));
        background.SetColor(LauncherTheme.Parse(colorHex));
        return background;
    }
}
