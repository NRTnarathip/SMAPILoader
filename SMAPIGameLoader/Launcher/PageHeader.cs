using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace SMAPIGameLoader.Launcher;

/// <summary>
/// Header เดียวที่ทุกหน้าใช้ร่วมกัน เพื่อให้ตำแหน่ง/ขนาด title ตรงกันเสมอ
/// ความสูงแถวคงที่ 40dp ทำให้หน้าที่มี/ไม่มีปุ่ม action มี baseline เท่ากัน
/// </summary>
internal sealed class PageHeader : LinearLayout
{
    const int RowHeightDp = 40;

    public TextView Title { get; }

    /// <summary>ช่องวางปุ่มฝั่งขวา (ถ้าไม่ใช้จะไม่กินที่)</summary>
    public LinearLayout Actions { get; }

    public PageHeader(Context context, string titleText) : base(context)
    {
        Orientation = Orientation.Horizontal;
        SetGravity(GravityFlags.CenterVertical);
        SetMinimumHeight(UI.Dp(context, RowHeightDp)
            + UI.Dp(context, 18) + UI.Dp(context, 14));
        SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.HeaderBackgroundHex));
        Elevation = UI.Dp(context, 3);
        SetPadding(
            UI.Dp(context, 16), UI.Dp(context, 18),
            UI.Dp(context, 16), UI.Dp(context, 14));

        Title = new TextView(context) { Text = titleText };
        Title.SetTextSize(ComplexUnitType.Sp, 22);
        Title.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        Title.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        Title.LetterSpacing = 0.01f;
        Title.SetSingleLine(true);
        Title.Ellipsize = TextUtils.TruncateAt.End;
        Title.SetMinimumHeight(UI.Dp(context, RowHeightDp));
        Title.Gravity = GravityFlags.CenterVertical;

        Actions = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        Actions.SetGravity(GravityFlags.CenterVertical);

        AddView(Title, new LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f)
        {
            RightMargin = UI.Dp(context, 12)
        });
        AddView(Actions, new LayoutParams(
            ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));
    }
}
