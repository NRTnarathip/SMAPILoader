using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;

namespace SMAPIGameLoader.Launcher;

/// <summary>
/// Popup แบบ anchored ที่ใช้ร่วมกัน: โผล่ใต้ view ที่กด ชิดขอบซ้าย
/// ถ้าด้านล่างไม่พอจะพลิกขึ้นด้านบนของ anchor แทน (ไม่ให้ล้นขอบจอ)
/// </summary>
internal sealed class AnchoredMenu
{
    const int WidthDp = 224;

    private readonly Context _context;
    private readonly LinearLayout _content;
    private readonly PopupWindow _popup;
    private Action _onDismiss;

    public AnchoredMenu(Context context)
    {
        _context = context;
        _content = new LinearLayout(context) { Orientation = Orientation.Vertical };
        _content.SetPadding(0, UI.Dp(context, 6), 0, UI.Dp(context, 6));

        _popup = new PopupWindow(_content, UI.Dp(context, WidthDp),
            ViewGroup.LayoutParams.WrapContent, true);
        _popup.SetBackgroundDrawable(LauncherTheme.CreateSurfaceDrawable(
            context, LauncherTheme.CardBackgroundHex,
            LauncherTheme.ChipRadiusDp + 2, LauncherTheme.DividerHex));
        _popup.Elevation = UI.Dp(context, 8);
        _popup.OutsideTouchable = true;

        _popup.DismissEvent += (s, e) => _onDismiss?.Invoke();
    }

    public AnchoredMenu AddItem(int iconResId, string label, string tintHex, Action onClick)
    {
        var row = new LinearLayout(_context)
        {
            Orientation = Orientation.Horizontal,
            Clickable = true,
            Focusable = true
        };
        row.SetGravity(GravityFlags.CenterVertical);
        row.SetMinimumHeight(UI.Dp(_context, 46));
        row.SetPadding(UI.Dp(_context, 16), 0, UI.Dp(_context, 16), 0);
        row.Background = UI.CreateRippleDrawable(_context, null, LauncherTheme.ChipRadiusDp);

        _content.AddView(row, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        {
            LeftMargin = UI.Dp(_context, 4),
            RightMargin = UI.Dp(_context, 4)
        });

        var color = LauncherTheme.Parse(tintHex ?? LauncherTheme.TextPrimaryHex);

        var icon = new ImageView(_context);
        icon.SetImageResource(iconResId);
        icon.SetColorFilter(color);
        icon.LayoutParameters = new LinearLayout.LayoutParams(
            UI.Dp(_context, 20), UI.Dp(_context, 20));
        row.AddView(icon);

        var text = new TextView(_context) { Text = label };
        text.SetTextSize(ComplexUnitType.Sp, 14);
        text.SetTextColor(color);
        text.SetTypeface(LauncherTheme.BodyTypeface, TypefaceStyle.Normal);
        text.LayoutParameters = new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f)
        { LeftMargin = UI.Dp(_context, 14) };
        row.AddView(text);

        row.Click += (s, e) =>
        {
            _popup.Dismiss();
            onClick?.Invoke();
        };

        return this;
    }

    public AnchoredMenu AddDivider()
    {
        var divider = new View(_context);
        divider.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.DividerHex));
        _content.AddView(divider, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, UI.Dp(_context, 1))
        {
            LeftMargin = UI.Dp(_context, 12),
            RightMargin = UI.Dp(_context, 12),
            TopMargin = UI.Dp(_context, 4),
            BottomMargin = UI.Dp(_context, 4)
        });

        return this;
    }

    public AnchoredMenu OnDismiss(Action onDismiss)
    {
        _onDismiss = onDismiss;
        return this;
    }

    public void Show(View anchor)
    {
        // วัดความสูงจริงก่อน show เพื่อเลือกทิศวางได้แน่นอน
        // (PopupWindow ที่ปล่อยเป็น WrapContent จะคำนวณตำแหน่งตอน layout แล้วล้นจอได้)
        int popupWidth = UI.Dp(_context, WidthDp);
        _content.Measure(
            View.MeasureSpec.MakeMeasureSpec(popupWidth, MeasureSpecMode.AtMost),
            View.MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified));
        _popup.Height = _content.MeasuredHeight;

        int gap = UI.Dp(_context, 4);
        var location = new int[2];
        anchor.GetLocationInWindow(location);
        int spaceBelow = anchor.RootView.Height - (location[1] + anchor.Height) - gap;
        int offsetY = _popup.Height <= spaceBelow
            ? gap
            : -(anchor.Height + _popup.Height + gap);

        _popup.ShowAsDropDown(anchor, 0, offsetY);
    }
}
