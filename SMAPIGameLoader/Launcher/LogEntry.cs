namespace SMAPIGameLoader.Launcher;

/// <summary>หนึ่งบรรทัดของ SMAPI log พร้อมระดับความรุนแรงที่ใช้ทั้ง filter และสี</summary>
internal readonly struct LogEntry
{
    internal enum Kind
    {
        Info = 0,
        Debug = 1,
        Trace = 2,
        Warn = 3,
        Error = 4,
        /// <summary>บรรทัดต่อเนื่อง/บรรทัดว่าง ที่ไม่มีหัว "[เวลา LEVEL source]"</summary>
        Other = 5,
    }

    public string Text { get; }
    public Kind Level { get; }

    LogEntry(string text, Kind level)
    {
        Text = text;
        Level = level;
    }

    /// <summary>
    /// บรรทัด SMAPI หน้าตาเป็น "[09:25:12 ERROR SMAPI] ข้อความ" จึงอ่านระดับจากส่วนหัวของบรรทัด
    /// </summary>
    public static LogEntry Parse(string line)
    {
        // ครอบหัวบรรทัด "[HH:MM:SS LEVEL SOURCE]" — 24 ตัวอักษรพอสำหรับอ่าน LEVEL
        string head = line.Length > 24 ? line.Substring(0, 24) : line;

        if (head.Contains("ERROR") || head.Contains("ALERT"))
            return new LogEntry(line, Kind.Error);
        if (head.Contains("WARN"))
            return new LogEntry(line, Kind.Warn);
        if (head.Contains("INFO"))
            return new LogEntry(line, Kind.Info);
        if (head.Contains("TRACE"))
            return new LogEntry(line, Kind.Trace);
        if (head.Contains("DEBUG"))
            return new LogEntry(line, Kind.Debug);

        return new LogEntry(line, Kind.Other);
    }
}
