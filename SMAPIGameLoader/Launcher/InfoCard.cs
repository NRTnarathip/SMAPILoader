using Android.Content;
using Android.Graphics;
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

    protected LinearLayout Content { get; }

    protected InfoCard(Context context) : base(context)
    {
        SetCardBackgroundColor(Color.ParseColor(CardBackgroundColorHex));
        Radius = Ui.Dp(context, 16);
        CardElevation = Ui.Dp(context, 2);
        UseCompatPadding = true;
        LayoutParameters = new ViewGroup.MarginLayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);

        Content = new LinearLayout(context) { Orientation = Orientation.Vertical };
        Content.SetPadding(
            Ui.Dp(context, 20), Ui.Dp(context, 16),
            Ui.Dp(context, 20), Ui.Dp(context, 16));
        AddView(Content);
    }

    protected TextView AddHeader(string titleText, int titleSizeSp = 18)
    {
        var header = new LinearLayout(Context) { Orientation = Orientation.Horizontal };
        header.SetGravity(GravityFlags.CenterVertical);

        var title = new TextView(Context) { Text = titleText };
        title.SetTextSize(ComplexUnitType.Sp, titleSizeSp);
        title.SetTextColor(Color.White);
        title.SetTypeface(null, TypefaceStyle.Bold);
        header.AddView(title, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f));

        var status = CreateStatusBadge(Context);
        header.AddView(status);

        Content.AddView(header);
        return status;
    }

    protected static void SetStatus(
        TextView status, bool isActive, string activeText, string inactiveText)
    {
        status.Text = isActive ? activeText : inactiveText;
        status.SetBackgroundColor(
            Color.ParseColor(isActive ? StatusOkColorHex : StatusErrorColorHex));
    }

    protected void AddDivider()
    {
        var divider = new View(Context);
        divider.SetBackgroundColor(Color.ParseColor(DividerColorHex));
        Content.AddView(divider, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, Ui.Dp(Context, 1))
        {
            TopMargin = Ui.Dp(Context, 12),
            BottomMargin = Ui.Dp(Context, 12)
        });
    }

    protected TextView AddRow(string label)
    {
        var row = new LinearLayout(Context) { Orientation = Orientation.Horizontal };
        row.SetPadding(0, Ui.Dp(Context, 4), 0, Ui.Dp(Context, 4));

        var key = new TextView(Context) { Text = label };
        key.SetTextSize(ComplexUnitType.Sp, 14);
        key.Alpha = 0.6f;
        row.AddView(key, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f));

        var value = new TextView(Context) { Text = "-" };
        value.SetTextSize(ComplexUnitType.Sp, 14);
        value.SetTypeface(null, TypefaceStyle.Bold);
        row.AddView(value);

        Content.AddView(row);
        return value;
    }

    protected TextView AddTextLine(string text)
    {
        var textView = new TextView(Context) { Text = text };
        textView.SetTextSize(ComplexUnitType.Sp, 14);
        textView.SetTextColor(Color.ParseColor(SecondaryTextColorHex));
        Content.AddView(textView, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Ui.Dp(Context, 4)
        });
        return textView;
    }

    static TextView CreateStatusBadge(Context context)
    {
        var status = new TextView(context);
        status.SetTextSize(ComplexUnitType.Sp, 12);
        status.SetTextColor(Color.White);
        status.SetPadding(
            Ui.Dp(context, 10), Ui.Dp(context, 4),
            Ui.Dp(context, 10), Ui.Dp(context, 4));
        return status;
    }
}
