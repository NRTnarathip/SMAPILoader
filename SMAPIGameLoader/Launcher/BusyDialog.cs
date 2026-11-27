using Android.App;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using Orientation = Android.Widget.Orientation;

namespace SMAPIGameLoader.Launcher;

/// <summary>
/// Overlay แสดงความคืบหน้าของงานยาว (กด Play Game / อัปโหลด log)
/// สีทั้งหมดมาจาก <see cref="LauncherTheme"/> จึงตามธีมที่ผู้ใช้เลือก
/// </summary>
internal sealed class BusyDialog : Dialog
{
    const int MaxLogHeightDp = 144;

    readonly FrameLayout _root;
    readonly LinearLayout _card;
    readonly LinearLayout.LayoutParams _scrollParams;
    readonly TextView _title;
    readonly ProgressBar _bar;
    readonly ScrollView _scroll;
    readonly TextView _log;
    readonly TextView _error;
    readonly TextView _hint;

    public BusyDialog(Activity activity, string title) : base(activity)
    {
        RequestWindowFeature((int)WindowFeatures.NoTitle);
        Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));

        _root = new FrameLayout(activity);
        _root.SetPadding(UI.Dp(activity, 24), UI.Dp(activity, 24),
                         UI.Dp(activity, 24), UI.Dp(activity, 24));

        _card = new LinearLayout(activity) { Orientation = Orientation.Vertical };
        _card.SetGravity(GravityFlags.CenterHorizontal);
        _card.Background = LauncherTheme.CreateSurfaceDrawable(
            activity, LauncherTheme.CardBackgroundHex, 20, LauncherTheme.DividerHex);
        _card.SetPadding(UI.Dp(activity, 20), UI.Dp(activity, 18),
                         UI.Dp(activity, 20), UI.Dp(activity, 18));

        _title = new TextView(activity) { Text = title };
        _title.SetTextSize(ComplexUnitType.Sp, 18);
        _title.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        _title.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        _card.AddView(_title, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        _bar = new ProgressBar(activity, null,
            Android.Resource.Attribute.ProgressBarStyleHorizontal) { Indeterminate = true };
        _bar.IndeterminateTintList = ColorStateList.ValueOf(
            LauncherTheme.Parse(LauncherTheme.AccentHex));
        _card.AddView(_bar, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, UI.Dp(activity, 4))
        { TopMargin = UI.Dp(activity, 14) });

        _log = new TextView(activity);
        _log.SetTextSize(ComplexUnitType.Sp, 11);
        _log.SetTypeface(Typeface.Monospace, TypefaceStyle.Normal);
        _log.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        _log.SetLineSpacing(0, 1.25f);

        _scroll = new ScrollView(activity) { FillViewport = false };
        _scroll.AddView(_log, new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
        _scrollParams = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        _scrollParams.TopMargin = UI.Dp(activity, 10);
        _card.AddView(_scroll, _scrollParams);

        _error = new TextView(activity);
        _error.SetTextSize(ComplexUnitType.Sp, 13);
        _error.SetTextColor(LauncherTheme.Parse(LauncherTheme.StatusErrorTextHex));
        _error.SetLineSpacing(0, 1.2f);
        _error.SetPadding(0, UI.Dp(activity, 10), 0, 0);
        _error.Visibility = ViewStates.Gone;
        _card.AddView(_error);

        _hint = new TextView(activity) { Text = "Tap anywhere to close" };
        _hint.SetTextSize(ComplexUnitType.Sp, 12);
        _hint.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        _hint.SetPadding(0, UI.Dp(activity, 10), 0, 0);
        _hint.Visibility = ViewStates.Gone;
        _card.AddView(_hint);

        _root.AddView(_card, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        SetContentView(_root);
        SetCancelable(false);
        SetCanceledOnTouchOutside(false);
    }

    public override void Show()
    {
        base.Show();
        Window?.SetLayout(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        Window?.SetDimAmount(0.6f);
    }

    public void SetTitleText(string text) => _title.Text = text;

    public void AppendLine(string message)
    {
        var text = _log.Text;
        _log.Text = text.Length == 0 ? message : text + "\n" + message;

        // ปรับความสูงตามเนื้อหาจริง (ไม่เกิน MaxLogHeightDp) แล้วเลื่อนไปบรรทัดล่าสุด
        _scroll.Post(() =>
        {
            int target = Math.Min(UI.Dp(Context, MaxLogHeightDp), _log.Height);
            if (target > 0 && _scrollParams.Height != target)
            {
                _scrollParams.Height = target;
                _scroll.LayoutParameters = _scrollParams;
            }

            _scroll.FullScroll(FocusSearchDirection.Down);
        });
    }

    /// <summary>สถานะล้มเหลว: ค้างไว้จนกว่าผู้ใช้จะแตะจอ</summary>
    public void ShowFailed(string message)
    {
        _title.Text = "Failed";
        _title.SetTextColor(LauncherTheme.Parse(LauncherTheme.StatusErrorTextHex));
        _bar.Visibility = ViewStates.Gone;

        _error.Text = message;
        _error.Visibility = ViewStates.Visible;
        _scroll.Post(() => _scroll.FullScroll(FocusSearchDirection.Down));
        _failed = true;

        _hint.Visibility = ViewStates.Visible;
        SetCancelable(true);
        SetCanceledOnTouchOutside(true);
    }

    bool _failed;

    // แตะที่ไหนก็ได้บนจอ = ปิด (children อย่าง ScrollView จะกิน touch ถ้าใช้ click listener)
    public override bool DispatchTouchEvent(MotionEvent e)
    {
        if (_failed && e.ActionMasked == MotionEventActions.Down)
        {
            Dismiss();
            return true;
        }

        return base.DispatchTouchEvent(e);
    }
}
