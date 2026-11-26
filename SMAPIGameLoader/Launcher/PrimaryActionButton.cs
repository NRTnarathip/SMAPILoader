using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace SMAPIGameLoader.Launcher;

/// <summary>
/// ปุ่มหลักของหน้า Home — ไอคอนปักซ้าย ข้อความ center จริง
/// (Button + compound drawable จะ center ไอคอนและข้อความรวมกัน ทำให้ข้อความเยื้องไปทางขวา)
/// </summary>
internal sealed class PrimaryActionButton : FrameLayout
{
    const int IconSizeDp = 24;
    const int IconInsetDp = 20;

    readonly TextView _label;

    public PrimaryActionButton(Context context, string text, int iconResId) : base(context)
    {
        Clickable = true;
        Focusable = true;
        SetMinimumHeight(Ui.Dp(context, 56));
        Background = Ui.CreateRippleDrawable(
            context, LauncherTheme.PrimaryHex, LauncherTheme.ButtonRadiusDp);

        var icon = new ImageView(context);
        icon.SetImageResource(iconResId);
        icon.ImportantForAccessibility = ImportantForAccessibility.No;
        var iconParams = new LayoutParams(
            Ui.Dp(context, IconSizeDp), Ui.Dp(context, IconSizeDp))
        {
            Gravity = GravityFlags.Start | GravityFlags.CenterVertical,
            LeftMargin = Ui.Dp(context, IconInsetDp)
        };
        AddView(icon, iconParams);

        _label = new TextView(context) { Text = text };
        _label.SetTextSize(ComplexUnitType.Sp, 17);
        _label.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextOnPrimaryHex));
        _label.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        _label.LetterSpacing = 0.01f;
        _label.Gravity = GravityFlags.Center;
        _label.SetSingleLine(true);
        _label.Ellipsize = TextUtils.TruncateAt.End;
        // padding ซ้าย/ขวาเท่ากัน เพื่อให้ข้อความ center ของปุ่มจริง
        // (ค่าที่เผื่อไว้ต้องกว้างกว่าไอคอน ไม่งั้นข้อความยาวจะทับไอคอน)
        int sidePadding = Ui.Dp(context, IconInsetDp + IconSizeDp + 8);
        _label.SetPadding(sidePadding, Ui.Dp(context, 14),
            sidePadding, Ui.Dp(context, 14));
        AddView(_label);

        UpdateContentDescription();
    }

    public string Text
    {
        get => _label.Text;
        set
        {
            _label.Text = value;
            UpdateContentDescription();
        }
    }

    void UpdateContentDescription() => ContentDescription = _label.Text;
}
