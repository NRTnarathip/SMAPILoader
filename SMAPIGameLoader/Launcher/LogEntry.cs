using Android.Graphics;

namespace SMAPIGameLoader.Launcher;

/// <summary>หนึ่งบรรทัดของ SMAPI log พร้อมสีที่ใช้แสดงตามระดับความรุนแรง</summary>
internal readonly struct LogEntry
{
    public string Text { get; }
    public Color Color { get; }

    LogEntry(string text, Color color)
    {
        Text = text;
        Color = color;
    }

    /// <summary>
    /// บรรทัด SMAPI หน้าตาเป็น "[09:25:12 ERROR SMAPI] ข้อความ" จึงอ่านระดับจากส่วนหัวของบรรทัด
    /// บรรทัดต่อเนื่องที่ไม่มีหัวจะถูกทำให้จางลงเพื่อไม่ให้แย่งสายตากับบรรทัดหลัก
    /// </summary>
    public static LogEntry Parse(string line)
    {
        // ครอบหัวบรรทัด "[HH:MM:SS LEVEL SOURCE]" — 22 ตัวอักษรพอสำหรับอ่าน LEVEL
        string head = line.Length > 24 ? line.Substring(0, 24) : line;

        if (head.Contains("ERROR") || head.Contains("ALERT"))
            return new LogEntry(line, LauncherTheme.Parse(LauncherTheme.StatusErrorTextHex));

        if (head.Contains("WARN"))
            return new LogEntry(line, LauncherTheme.Parse(LauncherTheme.WarningTextHex));

        if (head.Contains("INFO"))
            return new LogEntry(line, LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));

        if (head.Contains("TRACE") || head.Contains("DEBUG"))
            return new LogEntry(line, LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));

        return new LogEntry(line, LauncherTheme.Parse(LauncherTheme.LogContinuationHex));
    }
}
