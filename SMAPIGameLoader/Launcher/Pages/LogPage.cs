using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using SMAPIGameLoader.Tool;
using System;
using System.Collections.Generic;
using System.IO;

namespace SMAPIGameLoader.Launcher.Pages;

/// <summary>
/// หน้าแสดง SMAPI log ล่าสุด (อ่านจาก SmapiLogTool.GetLogFilePath()) พร้อมปุ่มแชร์ขึ้น smapi.io
/// </summary>
internal sealed class LogPage : LinearLayout
{
    /// <summary>กัน log ยาวมาก ๆ กินหน่วยความจำ — เก็บเฉพาะบรรทัดท้าย ๆ</summary>
    const int MaxLines = 5000;

    readonly TextView _metaText;
    readonly RecyclerView _list;
    readonly LogEntryAdapter _adapter;
    readonly LinearLayout _emptyView;
    readonly TextView _emptyPath;

    public LogPage(Context context) : base(context)
    {
        Orientation = Orientation.Vertical;
        SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.BackgroundHex));
        LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        // ===== Header =====
        var header = new PageHeader(context, "SMAPI Log");

        var reloadButton = CreateReloadButton(context);
        reloadButton.Click += (s, e) => Reload();
        header.Actions.AddView(reloadButton);

        var shareButton = CreateShareButton(context);
        shareButton.Click += (s, e) => SmapiLogTool.ShareLog();
        header.Actions.AddView(shareButton);

        AddView(header, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

        // ===== Meta: จำนวนบรรทัด / ขนาด / เวลาแก้ไขล่าสุด =====
        _metaText = new TextView(context);
        _metaText.SetTextSize(ComplexUnitType.Sp, 12);
        _metaText.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        _metaText.SetSingleLine(true);
        _metaText.Ellipsize = TextUtils.TruncateAt.End;
        _metaText.SetPadding(
            Ui.Dp(context, 16), Ui.Dp(context, 10),
            Ui.Dp(context, 16), Ui.Dp(context, 6));
        AddView(_metaText);

        // ===== Body =====
        var body = new FrameLayout(context);
        AddView(body, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 0, 1f));

        _adapter = new LogEntryAdapter();
        _list = new RecyclerView(context);
        _list.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.BackgroundHex));
        _list.SetPadding(0, Ui.Dp(context, 6), 0, Ui.Dp(context, 16));
        _list.SetClipToPadding(false);
        _list.SetLayoutManager(new LinearLayoutManager(context));
        _list.SetItemAnimator(null);
        _list.SetAdapter(_adapter);
        body.AddView(_list, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));

        _emptyView = CreateEmptyView(context, out _emptyPath);
        body.AddView(_emptyView, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));

        Reload();
    }

    static ImageButton CreateReloadButton(Context context)
    {
        var button = new ImageButton(context);
        button.SetImageResource(Resource.Drawable.ic_refresh);
        button.SetColorFilter(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        button.Background = Ui.CreateRippleDrawable(
            context, LauncherTheme.SurfaceVariantHex, 20);
        button.ContentDescription = "Reload log";
        button.SetPadding(
            Ui.Dp(context, 10), Ui.Dp(context, 10),
            Ui.Dp(context, 10), Ui.Dp(context, 10));

        int size = Ui.Dp(context, 40);
        button.LayoutParameters = new LinearLayout.LayoutParams(size, size)
        { RightMargin = Ui.Dp(context, 8) };
        return button;
    }

    static ImageButton CreateShareButton(Context context)
    {
        var button = new ImageButton(context);
        button.SetImageResource(Resource.Drawable.ic_share);
        button.SetColorFilter(LauncherTheme.Parse(LauncherTheme.AccentHex));
        button.Background = Ui.CreateRippleDrawable(
            context, LauncherTheme.SurfaceVariantHex, 20);
        button.ContentDescription = "Share log";
        button.SetPadding(
            Ui.Dp(context, 10), Ui.Dp(context, 10),
            Ui.Dp(context, 10), Ui.Dp(context, 10));

        int size = Ui.Dp(context, 40);
        button.LayoutParameters = new LinearLayout.LayoutParams(size, size);
        return button;
    }

    static LinearLayout CreateEmptyView(Context context, out TextView pathText)
    {
        var empty = new LinearLayout(context) { Orientation = Orientation.Vertical };
        empty.SetGravity(GravityFlags.Center);
        empty.SetPadding(
            Ui.Dp(context, 24), 0, Ui.Dp(context, 24), 0);

        var title = new TextView(context) { Text = "No log file yet." };
        title.SetTextSize(ComplexUnitType.Sp, 16);
        title.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        title.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        title.Gravity = GravityFlags.Center;
        empty.AddView(title);

        var hint = new TextView(context) { Text = "Launch the game once to generate a log." };
        hint.SetTextSize(ComplexUnitType.Sp, 13);
        hint.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        hint.Gravity = GravityFlags.Center;
        hint.SetPadding(0, Ui.Dp(context, 6), 0, 0);
        empty.AddView(hint);

        pathText = new TextView(context);
        pathText.SetTextSize(ComplexUnitType.Sp, 11);
        pathText.SetTypeface(Typeface.Monospace, TypefaceStyle.Normal);
        pathText.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        pathText.Gravity = GravityFlags.Center;
        pathText.SetLineSpacing(0, 1.15f);
        pathText.SetPadding(0, Ui.Dp(context, 14), 0, 0);
        empty.AddView(pathText);

        return empty;
    }

    /// <summary>อ่าน log ใหม่จากดิสก์ (เรียกซ้ำได้ทุกครั้งที่เปิดหน้านี้)</summary>
    public void Reload()
    {
        string path = SmapiLogTool.GetLogFilePath();

        if (File.Exists(path) is false)
        {
            _adapter.Submit(Array.Empty<LogEntry>());
            _metaText.Text = "No SMAPI log found";
            _emptyPath.Text = path;
            _emptyView.Visibility = ViewStates.Visible;
            _list.Visibility = ViewStates.Gone;
            return;
        }

        List<LogEntry> entries;
        try
        {
            string[] rawLines = File.ReadAllLines(path);
            int start = Math.Max(0, rawLines.Length - MaxLines);
            entries = new List<LogEntry>(rawLines.Length - start);
            for (int i = start; i < rawLines.Length; i++)
                entries.Add(LogEntry.Parse(rawLines[i]));

            var info = new FileInfo(path);
            string meta = $"{rawLines.Length:N0} lines · {info.Length / 1024f:F1} KB · {info.LastWriteTime:dd/MM/yyyy HH:mm}";
            if (start > 0)
                meta += $" · showing last {MaxLines:N0}";
            _metaText.Text = meta;
        }
        catch (Exception ex)
        {
            _adapter.Submit(Array.Empty<LogEntry>());
            _metaText.Text = "Failed to read log";
            _emptyPath.Text = ex.Message;
            _emptyView.Visibility = ViewStates.Visible;
            _list.Visibility = ViewStates.Gone;
            return;
        }

        _adapter.Submit(entries);
        _emptyView.Visibility = entries.Count == 0 ? ViewStates.Visible : ViewStates.Gone;
        _list.Visibility = entries.Count == 0 ? ViewStates.Gone : ViewStates.Visible;
    }
}
