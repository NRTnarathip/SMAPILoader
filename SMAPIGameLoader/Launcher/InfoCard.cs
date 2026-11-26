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
    protected const string CardBackgroundColorHex = "#1A1D24";
    protected const string StatusOkColorHex = "#2E7D32";
    protected const string StatusErrorColorHex = "#C62828";
    protected const string DividerColorHex = "#22FFFFFF";
    protected const string SecondaryTextColorHex = "#9AA0A6";
    protected const string PrimaryTextColorHex = "#E8EAED";

    protected LinearLayout Content { get; }

    protected InfoCard(Context context) : base(context)
    {
        SetCardBackgroundColor(Color.ParseColor(CardBackgroundColorHex));
        Radius = Ui.Dp(context, 16);
        CardElevation = Ui.Dp(context, 3);
        UseCompatPadding = false;
        LayoutParameters = new ViewGroup.MarginLayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);

        Content = new LinearLayout(context) { Orientation = Orientation.Vertical };
        Content.SetPadding(
            Ui.Dp(context, 20), Ui.Dp(context, 18),
            Ui.Dp(context, 20), Ui.Dp(context, 18));
        AddView(Content);
    }

    protected TextView AddHeader(string titleText, int titleSizeSp = 17)
    {
        var header = new LinearLayout(Context) { Orientation = Orientation.Horizontal };
        header.SetGravity(GravityFlags.CenterVertical);

        var title = new TextView(Context) { Text = titleText };
        title.SetTextSize(ComplexUnitType.Sp, titleSizeSp);
        title.SetTextColor(Color.White);
        title.SetTypeface(null, TypefaceStyle.Bold);
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
        if (status.Background is GradientDrawable background)
            background.SetColor(Color.ParseColor(isActive ? StatusOkColorHex : StatusErrorColorHex));
    }

    protected void AddDivider()
    {
        var divider = new View(Context);
        divider.SetBackgroundColor(Color.ParseColor(DividerColorHex));
        Content.AddView(divider, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, Ui.Dp(Context, 1))
        {
            TopMargin = Ui.Dp(Context, 14),
            BottomMargin = Ui.Dp(Context, 10)
        });
    }

    protected TextView AddRow(string label)
    {
        var row = new LinearLayout(Context) { Orientation = Orientation.Horizontal };
        row.SetGravity(GravityFlags.CenterVertical);
        row.SetPadding(0, Ui.Dp(Context, 6), 0, Ui.Dp(Context, 6));

        var key = new TextView(Context) { Text = label };
        key.SetTextSize(ComplexUnitType.Sp, 14);
        key.SetTextColor(Color.ParseColor(SecondaryTextColorHex));
        row.AddView(key, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f));

        var value = new TextView(Context) { Text = "-" };
        value.SetTextSize(ComplexUnitType.Sp, 14);
        value.SetTextColor(Color.White);
        value.SetTypeface(null, TypefaceStyle.Bold);
        row.AddView(value);

        Content.AddView(row);
        return value;
    }

    protected TextView AddTextLine(string text, int textSizeSp = 13, bool emphasized = false)
    {
        var textView = new TextView(Context) { Text = text };
        textView.SetTextSize(ComplexUnitType.Sp, textSizeSp);
        textView.SetTextColor(Color.ParseColor(
            emphasized ? PrimaryTextColorHex : SecondaryTextColorHex));
        textView.SetTypeface(null, emphasized ? TypefaceStyle.Bold : TypefaceStyle.Normal);
        textView.SetLineSpacing(0, 1.2f);
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
        status.SetTextColor(Color.White);
        status.SetTypeface(null, TypefaceStyle.Bold);
        status.LetterSpacing = 0.03f;
        status.SetIncludeFontPadding(false);
        status.Gravity = GravityFlags.Center;
        status.SetPadding(
            Ui.Dp(context, 10), Ui.Dp(context, 5),
            Ui.Dp(context, 10), Ui.Dp(context, 5));
        status.Background = CreateStatusBadgeBackground(context, StatusErrorColorHex);
        return status;
    }

    static GradientDrawable CreateStatusBadgeBackground(Context context, string colorHex)
    {
        var background = new GradientDrawable();
        background.SetShape(ShapeType.Rectangle);
        background.SetCornerRadius(Ui.Dp(context, 100));
        background.SetColor(Color.ParseColor(colorHex));
        return background;
    }
}
