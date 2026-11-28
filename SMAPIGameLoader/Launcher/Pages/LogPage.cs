using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using SMAPIGameLoader.Tool;
using System;
using System.Collections.Generic;
using System.IO;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher.Pages;

/// <summary>
/// หน้าแสดง SMAPI log ล่าสุด (อ่านจาก SmapiLogTool.GetLogFilePath())
/// มี filter ตามระดับ log, แบ่งหน้า ๆ ละ 100 บรรทัด และแชร์ขึ้น smapi.io
/// </summary>
internal sealed class LogPage : LinearLayout
{
    /// <summary>กัน log ยาวมาก ๆ กินหน่วยความจำ — เก็บเฉพาะบรรทัดท้าย ๆ</summary>
    const int MaxLines = 5000;

    const int PageSize = 100;
    const string FilterPrefKey = "log_filter_mask";

    static readonly LogEntry.Kind[] FilterKinds =
    {
        LogEntry.Kind.Info,
        LogEntry.Kind.Debug,
        LogEntry.Kind.Trace,
        LogEntry.Kind.Warn,
        LogEntry.Kind.Error,
    };

    readonly List<LogEntry> _allLines = new();
    readonly List<LogEntry> _filtered = new();
    readonly Dictionary<LogEntry.Kind, TextView> _chips = new();

    readonly TextView _metaText;
    readonly TextView _pageText;
    readonly RecyclerView _list;
    readonly LogEntryAdapter _adapter;
    readonly LinearLayout _emptyView;
    readonly TextView _emptyTitle;
    readonly TextView _emptyHint;
    readonly TextView _emptyDetail;
    readonly ImageButton _firstPageButton;
    readonly ImageButton _prevButton;
    readonly ImageButton _nextButton;
    readonly ImageButton _lastPageButton;
    readonly RecyclerScrollBar _scrollBar;

    int _filterMask;
    int _page;
    string _emptyDetailText = "";

    public LogPage(Context context) : base(context)
    {
        Orientation = Orientation.Vertical;
        SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.BackgroundHex));
        LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        _filterMask = Preferences.Get(FilterPrefKey, DefaultFilterMask());

        // ===== Header =====
        var header = new PageHeader(context, "SMAPI Log");

        var reloadButton = CreateHeaderButton(context, Resource.Drawable.ic_refresh,
            LauncherTheme.HeaderTextHex, "Reload log");
        reloadButton.Click += (s, e) => Reload();
        header.Actions.AddView(reloadButton);

        var shareButton = CreateHeaderButton(context, Resource.Drawable.ic_share,
            LauncherTheme.HeaderTextHex, "Share log");
        shareButton.Click += (s, e) => SmapiLogTool.ShareLog();
        header.Actions.AddView(shareButton);

        AddView(header, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

        // ===== Filter chips =====
        var chipsScroll = new HorizontalScrollView(context)
        {
            HorizontalScrollBarEnabled = false
        };
        var chipsRow = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        chipsRow.SetGravity(GravityFlags.CenterVertical);
        chipsRow.SetPadding(
            UI.Dp(context, 16), UI.Dp(context, 10),
            UI.Dp(context, 16), UI.Dp(context, 2));

        foreach (var kind in FilterKinds)
        {
            var chip = CreateFilterChip(context, kind);
            _chips[kind] = chip;
            chipsRow.AddView(chip);
        }

        chipsScroll.AddView(chipsRow);
        AddView(chipsScroll, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

        // ===== Meta =====
        _metaText = new TextView(context);
        _metaText.SetTextSize(ComplexUnitType.Sp, 12);
        _metaText.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        _metaText.SetSingleLine(true);
        _metaText.Ellipsize = TextUtils.TruncateAt.End;
        _metaText.SetPadding(
            UI.Dp(context, 16), UI.Dp(context, 2),
            UI.Dp(context, 16), UI.Dp(context, 6));
        AddView(_metaText);

        // ===== Body =====
        var body = new FrameLayout(context);
        AddView(body, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 0, 1f));

        _adapter = new LogEntryAdapter();
        _list = new RecyclerView(context);
        _list.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.BackgroundHex));
        _list.SetPadding(0, UI.Dp(context, 4), 0, UI.Dp(context, 12));
        _list.SetClipToPadding(false);
        _list.SetLayoutManager(new LinearLayoutManager(context));
        _list.SetItemAnimator(null);
        _list.SetAdapter(_adapter);
        body.AddView(_list, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));

        _emptyView = CreateEmptyView(context, out _emptyTitle, out _emptyHint, out _emptyDetail);
        body.AddView(_emptyView, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));

        // scrollbar ลากได้ ชิดขอบขวาจอ (geometry/size มาจาก RecyclerScrollBar ร่วมกันทุกหน้า)
        _scrollBar = RecyclerScrollBar.Attach(body, _list);

        // ===== Pager bar =====
        var pagerDivider = new View(context);
        pagerDivider.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.DividerHex));
        AddView(pagerDivider, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, UI.Dp(context, 1)));

        var pager = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        pager.SetGravity(GravityFlags.CenterVertical);
        pager.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.NavBarBackgroundHex));
        pager.SetPadding(
            UI.Dp(context, 8), UI.Dp(context, 6),
            UI.Dp(context, 8), UI.Dp(context, 6));

        _firstPageButton = CreatePagerButton(context, Resource.Drawable.ic_first_page, "First page");
        _firstPageButton.Click += (s, e) => GoToPage(0);
        pager.AddView(_firstPageButton);

        _prevButton = CreatePagerButton(context, Resource.Drawable.ic_chevron_left, "Previous page");
        _prevButton.Click += (s, e) => GoToPage(_page - 1);
        pager.AddView(_prevButton, PagerGap());

        _pageText = new TextView(context) { Gravity = GravityFlags.Center };
        _pageText.SetTextSize(ComplexUnitType.Sp, 12);
        _pageText.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        _pageText.SetSingleLine(true);
        _pageText.Ellipsize = TextUtils.TruncateAt.End;
        pager.AddView(_pageText, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f)
        {
            LeftMargin = UI.Dp(context, 6),
            RightMargin = UI.Dp(context, 6)
        });

        _nextButton = CreatePagerButton(context, Resource.Drawable.ic_chevron_right, "Next page");
        _nextButton.Click += (s, e) => GoToPage(_page + 1);
        pager.AddView(_nextButton, PagerGap());

        _lastPageButton = CreatePagerButton(context, Resource.Drawable.ic_last_page, "Last page");
        _lastPageButton.Click += (s, e) => GoToLastLine(); // หน้าสุดท้าย + เลื่อนถึงบรรทัดสุดท้ายเลย
        pager.AddView(_lastPageButton, PagerGap());

        var pagerSeparator = new View(context);
        pagerSeparator.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.DividerHex));
        pager.AddView(pagerSeparator, new LinearLayout.LayoutParams(
            UI.Dp(context, 1), UI.Dp(context, 20))
        {
            LeftMargin = UI.Dp(context, 6),
            RightMargin = UI.Dp(context, 6)
        });

        // เลื่อนบนสุด/ล่างสุดของหน้าที่กำลังดูอยู่เท่านั้น (ไม่ข้ามหน้า)
        var topButton = CreatePagerButton(context, Resource.Drawable.ic_to_top, "Scroll to top of page");
        topButton.Click += (s, e) => _list.ScrollToPosition(0);
        pager.AddView(topButton);

        var bottomButton = CreatePagerButton(context, Resource.Drawable.ic_to_bottom, "Scroll to bottom of page");
        bottomButton.Click += (s, e) =>
        {
            if (_adapter.ItemCount > 0)
                _list.ScrollToPosition(_adapter.ItemCount - 1);
        };
        pager.AddView(bottomButton, PagerGap());

        AddView(pager, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

        Reload();
    }

    // ===== Filter =====

    static int DefaultFilterMask()
    {
        int mask = 0;
        foreach (var kind in FilterKinds)
            mask |= 1 << (int)kind;
        return mask;
    }

    bool IsFilterEnabled(LogEntry.Kind kind) => (_filterMask & (1 << (int)kind)) != 0;

    TextView CreateFilterChip(Context context, LogEntry.Kind kind)
    {
        var chip = new TextView(context)
        {
            Text = kind.ToString().ToUpperInvariant(),
            Clickable = true,
            Focusable = true,
            Gravity = GravityFlags.Center
        };
        chip.SetTextSize(ComplexUnitType.Sp, 11);
        chip.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        chip.SetIncludeFontPadding(false);
        chip.SetPadding(
            UI.Dp(context, 10), UI.Dp(context, 5),
            UI.Dp(context, 10), UI.Dp(context, 5));
        chip.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent)
        { RightMargin = UI.Dp(context, 6) };

        ApplyChipStyle(chip, IsFilterEnabled(kind));

        chip.Click += (s, e) =>
        {
            _filterMask ^= 1 << (int)kind;
            Preferences.Set(FilterPrefKey, _filterMask);
            ApplyChipStyle(chip, IsFilterEnabled(kind));
            _page = 0;
            RefreshView(scrollToTop: true);
        };

        return chip;
    }

    void ApplyChipStyle(TextView chip, bool selected)
    {
        chip.SetTextColor(LauncherTheme.Parse(
            selected ? LauncherTheme.AccentHex : LauncherTheme.TextSecondaryHex));

        var background = new GradientDrawable();
        background.SetShape(ShapeType.Rectangle);
        background.SetCornerRadius(UI.Dp(Context, LauncherTheme.ChipRadiusDp));
        background.SetColor(LauncherTheme.Parse(
            selected ? LauncherTheme.StatusOkBackgroundHex : LauncherTheme.SurfaceVariantHex));
        if (selected)
            background.SetStroke(UI.Dp(Context, 1),
                LauncherTheme.Parse(LauncherTheme.StatusOkTextHex));

        chip.Background = background;
    }

    // ===== Paging =====

    void GoToPage(int page)
    {
        int pageCount = PageCount;
        if (page < 0 || page >= pageCount || page == _page)
            return;

        _page = page;
        RefreshView(scrollToTop: true);
    }

    int PageCount => Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)PageSize));

    /// <summary>ไปที่บรรทัดสุดท้ายของ log: สลับไปหน้าสุดท้ายแล้วเลื่อนลงล่างสุด</summary>
    void GoToLastLine()
    {
        int lastPage = PageCount - 1;
        if (_page != lastPage)
        {
            _page = lastPage;
            RefreshView(scrollToTop: false);
        }

        if (_adapter.ItemCount > 0)
            _list.ScrollToPosition(_adapter.ItemCount - 1);
    }

    // ===== UI helpers =====

    static ImageButton CreateHeaderButton(
        Context context, int iconResId, string tintHex, string description)
    {
        var button = new ImageButton(context);
        button.SetImageResource(iconResId);
        button.SetColorFilter(LauncherTheme.Parse(tintHex));
        button.Background = UI.CreateRippleDrawable(
            context, LauncherTheme.HeaderPillHex, 20);
        button.ContentDescription = description;
        button.SetPadding(
            UI.Dp(context, 10), UI.Dp(context, 10),
            UI.Dp(context, 10), UI.Dp(context, 10));

        int size = UI.Dp(context, 40);
        button.LayoutParameters = new LinearLayout.LayoutParams(size, size)
        { RightMargin = UI.Dp(context, 8) };
        return button;
    }

    LinearLayout.LayoutParams PagerGap() => new(
        ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
    { LeftMargin = UI.Dp(Context, 2) };

    static ImageButton CreatePagerButton(Context context, int iconResId, string description)
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

    static LinearLayout CreateEmptyView(
        Context context, out TextView title, out TextView hint, out TextView detail)
    {
        var empty = new LinearLayout(context) { Orientation = Orientation.Vertical };
        empty.SetGravity(GravityFlags.Center);
        empty.SetPadding(UI.Dp(context, 24), 0, UI.Dp(context, 24), 0);

        title = new TextView(context);
        title.SetTextSize(ComplexUnitType.Sp, 16);
        title.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        title.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        title.Gravity = GravityFlags.Center;
        empty.AddView(title);

        hint = new TextView(context);
        hint.SetTextSize(ComplexUnitType.Sp, 13);
        hint.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        hint.Gravity = GravityFlags.Center;
        hint.SetPadding(0, UI.Dp(context, 6), 0, 0);
        empty.AddView(hint);

        detail = new TextView(context);
        detail.SetTextSize(ComplexUnitType.Sp, 11);
        detail.SetTypeface(Typeface.Monospace, TypefaceStyle.Normal);
        detail.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        detail.Gravity = GravityFlags.Center;
        detail.SetLineSpacing(0, 1.15f);
        detail.SetPadding(0, UI.Dp(context, 14), 0, 0);
        empty.AddView(detail);

        return empty;
    }

    // ===== Load / render =====

    /// <summary>อ่าน log ใหม่จากดิสก์ (เรียกซ้ำได้ทุกครั้งที่เปิดหน้านี้)</summary>
    public void Reload()
    {
        string path = SmapiLogTool.GetLogFilePath();
        _allLines.Clear();
        _emptyDetailText = "";
        _page = 0;

        if (File.Exists(path) is false)
        {
            _metaText.Text = "No SMAPI log found";
            _emptyDetailText = path;
            RefreshView(scrollToTop: true);
            return;
        }

        try
        {
            string[] rawLines = File.ReadAllLines(path);
            int start = Math.Max(0, rawLines.Length - MaxLines);
            for (int i = start; i < rawLines.Length; i++)
                _allLines.Add(LogEntry.Parse(rawLines[i]));

            var info = new FileInfo(path);
            string meta = $"{rawLines.Length:N0} lines · {info.Length / 1024f:F1} KB · {info.LastWriteTime:dd/MM/yyyy HH:mm}";
            if (start > 0)
                meta += $" · last {MaxLines:N0}";
            _metaText.Text = meta;
        }
        catch (Exception ex)
        {
            _metaText.Text = "Failed to read log";
            _emptyDetailText = ex.Message;
        }

        RefreshView(scrollToTop: true);
    }

    void BuildFilteredLines()
    {
        _filtered.Clear();

        // บรรทัดต่อเนื่อง (ไม่มีหัวระดับ) จะแสดงเฉพาะเมื่อบรรทัดหลักก่อนหน้าถูกแสดง
        bool lastShown = false;
        foreach (var line in _allLines)
        {
            bool show;
            if (line.Level == LogEntry.Kind.Other)
            {
                show = lastShown;
            }
            else
            {
                show = IsFilterEnabled(line.Level);
                lastShown = show;
            }

            if (show)
                _filtered.Add(line);
        }
    }

    void RefreshView(bool scrollToTop)
    {
        BuildFilteredLines();

        int pageCount = PageCount;
        if (_page >= pageCount)
            _page = pageCount - 1;
        if (_page < 0)
            _page = 0;

        int start = _page * PageSize;
        int count = Math.Min(PageSize, Math.Max(0, _filtered.Count - start));

        var pageLines = new List<LogEntry>(count);
        for (int i = 0; i < count; i++)
            pageLines.Add(_filtered[start + i]);
        _adapter.Submit(pageLines);
        _scrollBar.ResetThumb();

        bool hasLines = _filtered.Count > 0;
        _list.Visibility = hasLines ? ViewStates.Visible : ViewStates.Gone;
        _emptyView.Visibility = hasLines ? ViewStates.Gone : ViewStates.Visible;

        if (hasLines)
        {
            _pageText.Text = $"{_page + 1}/{pageCount} · {start + 1}-{start + count}";
        }
        else
        {
            _pageText.Text = "0/0";
            SetEmptyState();
        }

        bool hasPrev = _page > 0;
        bool hasNext = _page < pageCount - 1;
        SetPagerButtonEnabled(_firstPageButton, hasPrev);
        SetPagerButtonEnabled(_prevButton, hasPrev);
        SetPagerButtonEnabled(_nextButton, hasNext);
        SetPagerButtonEnabled(_lastPageButton, hasNext);

        if (scrollToTop)
            _list.ScrollToPosition(0);
    }

    void SetEmptyState()
    {
        if (_allLines.Count == 0)
        {
            _emptyTitle.Text = _emptyDetailText.StartsWith("/")
                ? "No log file yet."
                : "Failed to read log";
            _emptyHint.Text = "Launch the game once to generate a log.";
            _emptyDetail.Text = _emptyDetailText;
            return;
        }

        _emptyTitle.Text = "No lines match the filters.";
        _emptyHint.Text = "Enable more log levels above.";
        _emptyDetail.Text = "";
    }

    static void SetPagerButtonEnabled(ImageButton button, bool enabled)
    {
        button.Enabled = enabled;
        button.Alpha = enabled ? 1f : 0.35f;
    }
}
