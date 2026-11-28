using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using System;

namespace SMAPIGameLoader.Launcher;

/// <summary>
/// Scrollbar แนวตั้งแบบลาก thumb ได้สำหรับ RecyclerView
/// (RecyclerView ไม่มี fast-scroll thumb ให้ใช้เหมือน ScrollView)
/// รับ touch เฉพาะตอนกดโดน thumb — จุดอื่นปล่อยผ่านให้ list เลื่อนปกติ
/// </summary>
internal sealed class RecyclerScrollBar : View
{
    /// <summary>ความกว้างของ view ที่รับ touch (thumb อยู่ในนี้ ชิดขวา)</summary>
    const float BarWidthDp = 16;

    readonly RecyclerView _list;
    readonly Paint _paint = new() { AntiAlias = true };

    bool _dragging;
    bool _thumbSizeValid;
    float _thumbTop;
    float _thumbHeight;
    float _dragOffset;
    int _dragRange;
    int _dragExtent;
    int _lastTrackHeight;

    RecyclerScrollBar(Context context, RecyclerView list) : base(context)
    {
        _list = list;
    }

    /// <summary>
    /// แนบ scrollbar ให้ list: ชิดขอบขวาของ container (ใช้ร่วมกันทุกหน้า)
    /// แล้วผูกให้วาดใหม่ทุกครั้งที่ list เลื่อน
    /// </summary>
    public static RecyclerScrollBar Attach(FrameLayout container, RecyclerView list)
    {
        var bar = new RecyclerScrollBar(container.Context!, list);
        container.AddView(bar, new FrameLayout.LayoutParams(
            UI.Dp(container.Context!, BarWidthDp), ViewGroup.LayoutParams.MatchParent)
        {
            Gravity = GravityFlags.Right
        });
        list.AddOnScrollListener(new Invalidator(bar));
        return bar;
    }

    /// <summary>เรียกเมื่อเนื้อหา list เปลี่ยน — ให้คำนวณความสูง thumb ใหม่ครั้งเดียว</summary>
    public void ResetThumb()
    {
        _thumbSizeValid = false;
        Invalidate();
    }

    bool TryGetMetrics(out int range, out int extent, out int offset)
    {
        range = _list.ComputeVerticalScrollRange();
        extent = _list.ComputeVerticalScrollExtent();
        offset = _list.ComputeVerticalScrollOffset();
        return range > extent && Height > 0;
    }

    /// <summary>
    /// ความสูง thumb ตรึงไว้ต่อหน้า: range ของ RecyclerView เป็นค่าประมาณที่ขยับตามความสูงแถว
    /// ที่มองเห็น ถ้าคำนวณทุก draw thumb จะยืด/หดระหว่างเลื่อน
    /// </summary>
    void EnsureThumbHeight(int range, int extent)
    {
        if (Height != _lastTrackHeight)
        {
            _lastTrackHeight = Height;
            _thumbSizeValid = false;
        }

        if (_thumbSizeValid)
            return;

        _thumbHeight = Math.Max(UI.Dp(Context, LauncherTheme.ScrollThumbMinHeightDp), Height * extent / (float)range);
        _thumbSizeValid = true;
    }

    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);
        if (!TryGetMetrics(out int range, out int extent, out int offset))
            return;

        EnsureThumbHeight(range, extent);
        _thumbTop = (Height - _thumbHeight) * offset / Math.Max(1, range - extent);

        float thumbWidth = UI.Dp(Context, LauncherTheme.ScrollThumbWidthDp);
        // ชิดขวาของ view เพื่อให้ bar อยู่ติดขอบจอ (เว้น 1dp ไม่ให้ชนมุมโค้ง)
        float left = Width - thumbWidth - UI.Dp(Context, LauncherTheme.ScrollThumbInsetDp);

        _paint.Color = LauncherTheme.Parse(_dragging
            ? LauncherTheme.AccentHex
            : LauncherTheme.ScrollThumbHex);

        canvas.DrawRoundRect(
            left, _thumbTop, left + thumbWidth, _thumbTop + _thumbHeight,
            thumbWidth / 2f, thumbWidth / 2f, _paint);
    }

    public override bool OnTouchEvent(MotionEvent e)
    {
        if (!TryGetMetrics(out int range, out int extent, out int offset))
            return false;

        if (!_dragging)
        {
            EnsureThumbHeight(range, extent);
            _thumbTop = (Height - _thumbHeight) * offset / Math.Max(1, range - extent);
        }

        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                float slop = UI.Dp(Context, LauncherTheme.ScrollBarTouchSlopDp);
                float y = e.GetY();
                if (y < _thumbTop - slop || y > _thumbTop + _thumbHeight + slop)
                    return false;

                _dragging = true;
                _dragOffset = y - _thumbTop;
                _dragRange = range;
                _dragExtent = extent;
                Parent?.RequestDisallowInterceptTouchEvent(true);
                Invalidate();
                return true;

            case MotionEventActions.Move:
                if (!_dragging)
                    return false;
                Drag(e.GetY());
                return true;

            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                if (!_dragging)
                    return false;
                _dragging = false;
                Parent?.RequestDisallowInterceptTouchEvent(false);
                Invalidate();
                return true;
        }

        return false;
    }

    /// <summary>บอกให้ scrollbar วาดใหม่ทุกครั้งที่ list เลื่อน</summary>
    internal sealed class Invalidator : RecyclerView.OnScrollListener
    {
        readonly View _bar;

        public Invalidator(View bar) => _bar = bar;

        public override void OnScrolled(RecyclerView recyclerView, int dx, int dy) =>
            _bar.Invalidate();
    }

    void Drag(float y)
    {
        float maxTravel = Height - _thumbHeight;
        if (maxTravel <= 0)
            return;

        // ใช้ range/extent ที่จำไว้ตอนกด กัน mapping ขยับระหว่างลาก
        int maxScroll = Math.Max(1, _dragRange - _dragExtent);
        int offset = _list.ComputeVerticalScrollOffset();

        float fraction = Math.Clamp((y - _dragOffset) / maxTravel, 0f, 1f);
        int target = (int)(fraction * maxScroll);
        int delta = target - offset;
        if (delta != 0)
            _list.ScrollBy(0, delta);

        Invalidate();
    }
}
