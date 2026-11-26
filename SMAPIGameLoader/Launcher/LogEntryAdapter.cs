using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using System.Collections.Generic;

namespace SMAPIGameLoader.Launcher;

/// <summary>แถว log หนึ่งบรรทัด ใช้ RecyclerView เพื่อไม่ให้ต้องสร้าง TextView หลายพันตัวพร้อมกัน</summary>
internal sealed class LogEntryAdapter : RecyclerView.Adapter
{
    static readonly Color ColorError = LauncherTheme.Parse(LauncherTheme.StatusErrorTextHex);
    static readonly Color ColorWarn = LauncherTheme.Parse(LauncherTheme.WarningTextHex);
    static readonly Color ColorInfo = LauncherTheme.Parse(LauncherTheme.TextPrimaryHex);
    static readonly Color ColorMuted = LauncherTheme.Parse(LauncherTheme.TextSecondaryHex);
    static readonly Color ColorOther = LauncherTheme.Parse(LauncherTheme.LogContinuationHex);

    readonly List<LogEntry> _entries = new();

    public override int ItemCount => _entries.Count;

    public void Submit(IEnumerable<LogEntry> entries)
    {
        _entries.Clear();
        _entries.AddRange(entries);
        NotifyDataSetChanged();
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var context = parent.Context;

        var text = new TextView(context);
        text.SetTextSize(ComplexUnitType.Sp, 11);
        text.SetTypeface(Typeface.Monospace, TypefaceStyle.Normal);
        text.SetLineSpacing(0, 1.15f);
        text.SetTextIsSelectable(false);
        text.SetPadding(
            UI.Dp(context, 16), UI.Dp(context, 3),
            UI.Dp(context, 16), UI.Dp(context, 3));

        return new LineHolder(text);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        var line = _entries[position];
        var lineHolder = (LineHolder)holder;
        lineHolder.Text.Text = line.Text;
        lineHolder.Text.SetTextColor(ColorFor(line.Level));
    }

    static Color ColorFor(LogEntry.Kind level) => level switch
    {
        LogEntry.Kind.Error => ColorError,
        LogEntry.Kind.Warn => ColorWarn,
        LogEntry.Kind.Info => ColorInfo,
        LogEntry.Kind.Debug => ColorMuted,
        LogEntry.Kind.Trace => ColorMuted,
        _ => ColorOther,
    };

    sealed class LineHolder : RecyclerView.ViewHolder
    {
        public TextView Text { get; }

        public LineHolder(TextView text) : base(text) => Text = text;
    }
}
