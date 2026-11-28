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
        Radius = UI.Dp(context, LauncherTheme.CardRadiusDp);
        CardElevation = 0;
        UseCompatPadding = false;
        LayoutParameters = new ViewGroup.MarginLayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);

        Content = new LinearLayout(context) { Orientation = Orientation.Vertical };
        Content.SetPadding(
            UI.Dp(context, 18), UI.Dp(context, 16),
            UI.Dp(context, 18), UI.Dp(context, 16));
        AddView(Content);
    }

    protected TextView AddHeader(string titleText, int titleSizeSp = 17)
    {
        var header = new LinearLayout(Context) { Orientation = Orientation.Horizontal };
        header.SetGravity(GravityFlags.CenterVertical);

        var title = new TextView(Context) { Text = titleText };
        title.SetTextSize(ComplexUnitType.Sp, titleSizeSp);
        title.SetTextColor(LauncherTheme.Parse(LauncherTheme.CardTitleHex));
        title.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        title.LetterSpacing = 0.01f;
        header.AddView(title, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f)
        {
            RightMargin = UI.Dp(Context, 12)
        });

        var status = CreateStatusBadge(Context);
        header.AddView(status);

        Content.AddView(header);
        return status;
    }

    /// <summary>
    /// เพิ่ม badge เสริมใน header ทางซ้ายของ status badge (เช่น "New Version")
    /// คืน TextView มาให้หน้าที่เรียกตั้งสี/ข้อความ/visibility เอง
    /// </summary>
    protected TextView AddHeaderBadge(TextView status)
    {
        var badge = CreateStatusBadge(Context);
        if (status.Parent is LinearLayout header)
        {
            header.AddView(badge, header.IndexOfChild(status), new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent)
            { RightMargin = UI.Dp(Context, 8) });
        }
        return badge;
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
            ViewGroup.LayoutParams.MatchParent, UI.Dp(Context, 1))
        {
            TopMargin = UI.Dp(Context, 8),
            BottomMargin = UI.Dp(Context, 2)
        });
    }

    protected TextView AddRow(string label)
    {
        var row = new LinearLayout(Context) { Orientation = Orientation.Horizontal };
        row.SetGravity(GravityFlags.CenterVertical);
        row.SetPadding(0, UI.Dp(Context, 3), 0, UI.Dp(Context, 3));

        // ไม่เอา font padding ของ TextView ไม่งั้นแต่ละแถวจะดูลอยห่างกันเป็นช่อง
        var key = new TextView(Context) { Text = label };
        key.SetIncludeFontPadding(false);
        key.SetTextSize(ComplexUnitType.Sp, 13);
        key.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        row.AddView(key, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f));

        var value = new TextView(Context) { Text = "-" };
        value.SetTextSize(ComplexUnitType.Sp, 14);
        value.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        value.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        value.Gravity = GravityFlags.End | GravityFlags.CenterVertical;
        value.SetMaxLines(3);
        value.SetIncludeFontPadding(false);
        value.SetLineSpacing(0, 1.15f);
        value.SetPadding(UI.Dp(Context, 16), 0, 0, 0);
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
            TopMargin = UI.Dp(Context, emphasized ? 0 : 4)
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
            UI.Dp(context, 10), UI.Dp(context, 5),
            UI.Dp(context, 10), UI.Dp(context, 5));
        status.Background = CreateStatusBadgeBackground(context, LauncherTheme.StatusErrorBackgroundHex);
        return status;
    }

    static GradientDrawable CreateStatusBadgeBackground(Context context, string colorHex)
    {
        var background = new GradientDrawable();
        background.SetShape(ShapeType.Rectangle);
        background.SetCornerRadius(UI.Dp(context, 100));
        background.SetColor(LauncherTheme.Parse(colorHex));
        return background;
    }
}
