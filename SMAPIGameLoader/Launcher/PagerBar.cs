using System;
using Android.Content;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace SMAPIGameLoader.Launcher;

/// <summary>
/// แถบ pager มาตรฐานของหน้า list (ใช้ร่วมกันระหว่าง LogPage และ ModManagerPage):
/// first/prev/next/last + ข้อความหน้าที่กำลังดู + ปุ่มเลื่อนบน/ล่างของหน้านั้น
/// </summary>
internal sealed class PagerBar : LinearLayout
{
    readonly ImageButton _first;
    readonly ImageButton _prev;
    readonly ImageButton _next;
    readonly ImageButton _last;

    public TextView PageText { get; }

    public PagerBar(Context context, RecyclerView list,
        Action onFirst, Action onPrev, Action onNext, Action onLast) : base(context)
    {
        Orientation = Orientation.Horizontal;
        SetGravity(GravityFlags.CenterVertical);
        SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.NavBarBackgroundHex));
        SetPadding(
            UI.Dp(context, 8), UI.Dp(context, 6),
            UI.Dp(context, 8), UI.Dp(context, 6));

        _first = CreateButton(context, Resource.Drawable.ic_first_page, "First page");
        _first.Click += (s, e) => onFirst();
        AddView(_first);

        _prev = CreateButton(context, Resource.Drawable.ic_chevron_left, "Previous page");
        _prev.Click += (s, e) => onPrev();
        AddView(_prev, Gap(context));

        PageText = new TextView(context) { Gravity = GravityFlags.Center };
        PageText.SetTextSize(ComplexUnitType.Sp, 12);
        PageText.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        PageText.SetSingleLine(true);
        PageText.Ellipsize = TextUtils.TruncateAt.End;
        AddView(PageText, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f)
        {
            LeftMargin = UI.Dp(context, 6),
            RightMargin = UI.Dp(context, 6)
        });

        _next = CreateButton(context, Resource.Drawable.ic_chevron_right, "Next page");
        _next.Click += (s, e) => onNext();
        AddView(_next, Gap(context));

        _last = CreateButton(context, Resource.Drawable.ic_last_page, "Last page");
        _last.Click += (s, e) => onLast();
        AddView(_last, Gap(context));

        var separator = new View(context);
        separator.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.DividerHex));
        AddView(separator, new LinearLayout.LayoutParams(
            UI.Dp(context, 1), UI.Dp(context, 20))
        {
            LeftMargin = UI.Dp(context, 6),
            RightMargin = UI.Dp(context, 6)
        });

        // เลื่อนบนสุด/ล่างสุดของหน้าที่กำลังดูอยู่เท่านั้น (ไม่ข้ามหน้า)
        var top = CreateButton(context, Resource.Drawable.ic_to_top, "Scroll to top of page");
        top.Click += (s, e) => list.ScrollToPosition(0);
        AddView(top);

        var bottom = CreateButton(context, Resource.Drawable.ic_to_bottom, "Scroll to bottom of page");
        bottom.Click += (s, e) =>
        {
            int count = list.GetAdapter()?.ItemCount ?? 0;
            if (count > 0)
                list.ScrollToPosition(count - 1);
        };
        AddView(bottom, Gap(context));
    }

    /// <summary>อัปเดตข้อความหน้า + เปิด/ปิดปุ่มเลื่อนหน้า</summary>
    public void SetPage(string text, bool hasPrev, bool hasNext)
    {
        PageText.Text = text;
        SetButtonEnabled(_first, hasPrev);
        SetButtonEnabled(_prev, hasPrev);
        SetButtonEnabled(_next, hasNext);
        SetButtonEnabled(_last, hasNext);
    }

    public static View CreateDivider(Context context)
    {
        var divider = new View(context);
        divider.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.DividerHex));
        divider.LayoutParameters = new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, UI.Dp(context, 1));
        return divider;
    }

    static ImageButton CreateButton(Context context, int iconResId, string description)
    {
        var button = new ImageButton(context);
        button.SetImageResource(iconResId);
        button.SetColorFilter(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        button.Background = UI.CreateRippleDrawable(
            context, LauncherTheme.SurfaceVariantHex, 20);
        button.ContentDescription = description;
        button.SetPadding(
            UI.Dp(context, 6), UI.Dp(context, 6),
            UI.Dp(context, 6), UI.Dp(context, 6));

        int size = UI.Dp(context, 36);
        button.LayoutParameters = new LinearLayout.LayoutParams(size, size);
        return button;
    }

    static LinearLayout.LayoutParams Gap(Context context) => new(
        ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
    { LeftMargin = UI.Dp(context, 2) };

    static void SetButtonEnabled(ImageButton button, bool enabled)
    {
        button.Enabled = enabled;
        button.Alpha = enabled ? 1f : 0.35f;
    }
}
