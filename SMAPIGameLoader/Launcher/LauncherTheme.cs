using Android.Content;
using Android.Graphics;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher;

/// <summary>
/// Design tokens ของ launcher — สี/ฟอนต์/รัศมีที่ทุกหน้าใช้ร่วมกัน
/// สีเป็น property เพราะสลับธีมได้ตอนรัน (Settings page) — แก้ palette ที่ <see cref="Apply"/> ที่เดียว
/// </summary>
internal static class LauncherTheme
{
    public const string DarkName = "dark";
    public const string ChristmasName = "christmas";
    public const string PrefKey = "launcher_theme";

    public static string CurrentName { get; private set; } = DarkName;

    // พื้นผิว
    public static string BackgroundHex { get; private set; } = "#0D1116";
    public static string HeaderBackgroundHex { get; private set; } = "#141A21";
    public static string CardBackgroundHex { get; private set; } = "#191F29";
    public static string SurfaceVariantHex { get; private set; } = "#232B37";
    public static string NavBarBackgroundHex { get; private set; } = "#12171E";

    // การ์ดที่กำลังถูกเลือก/เปิด action อยู่
    public static string SelectedCardBackgroundHex { get; private set; } = "#1F3A26";

    // สีหลัก (เขียว Stardew) และสีเน้น
    public static string PrimaryHex { get; private set; } = "#2E7D32";
    public static string AccentHex { get; private set; } = "#8AD49A";

    // ตัวอักษร
    public static string TextPrimaryHex { get; private set; } = "#E9EDF2";
    public static string TextSecondaryHex { get; private set; } = "#94A0B0";
    public static string TextOnPrimaryHex { get; private set; } = "#FFFFFF";

    // เส้นคั่น
    public static string DividerHex { get; private set; } = "#1AFFFFFF";

    // สถานะ (pill อ่อนบนพื้นเข้ม)
    public static string StatusOkTextHex { get; private set; } = "#8CE0A0";
    public static string StatusOkBackgroundHex { get; private set; } = "#1F3A26";
    public static string StatusErrorTextHex { get; private set; } = "#F1A0A6";
    public static string StatusErrorBackgroundHex { get; private set; } = "#3A1F22";

    // ระดับของ log
    public static string WarningTextHex { get; private set; } = "#E8C07D";
    public static string LogContinuationHex { get; private set; } = "#C3CBD6";

    // scrollbar
    public static string ScrollThumbHex { get; private set; } = "#66707E";

    /// <summary>สีไอคอน/ข้อความของ bottom nav: selected = accent, ปกติ = เทากลาง</summary>
    public static string NavInactiveHex { get; private set; } = "#7D8797";

    // รัศมีมุม
    public const float CardRadiusDp = 18;
    public const float ButtonRadiusDp = 16;
    public const float ChipRadiusDp = 12;

    /// <summary>โหลดธีมที่บันทึกไว้ — ต้องเรียกก่อนสร้าง UI</summary>
    public static void Load() => Apply(Preferences.Get(PrefKey, DarkName));

    /// <summary>เปลี่ยนธีมแล้วบันทึกไว้ (UI ต้อง recreate เองเพื่อใช้สีใหม่)</summary>
    public static void Set(string name)
    {
        Preferences.Set(PrefKey, name);
        Apply(name);
    }

    // shortcut: dialog/popup ของระบบยังใช้สีจาก styles.xml (Theme.SMAPILauncher) จึงเป็นโทนเขียวเดิมทุกธีม
    public static void Apply(string name)
    {
        var christmas = name == ChristmasName;
        CurrentName = christmas ? ChristmasName : DarkName;

        BackgroundHex = christmas ? "#0B1410" : "#0D1116";
        HeaderBackgroundHex = christmas ? "#122019" : "#141A21";
        CardBackgroundHex = christmas ? "#172A1F" : "#191F29";
        SurfaceVariantHex = christmas ? "#22372A" : "#232B37";
        NavBarBackgroundHex = christmas ? "#0E1A14" : "#12171E";
        SelectedCardBackgroundHex = christmas ? "#3A2220" : "#1F3A26";
        PrimaryHex = christmas ? "#B3261E" : "#2E7D32";
        AccentHex = christmas ? "#E8C07D" : "#8AD49A";
        TextPrimaryHex = christmas ? "#F2F5F1" : "#E9EDF2";
        TextSecondaryHex = christmas ? "#9FAFA4" : "#94A0B0";
        TextOnPrimaryHex = "#FFFFFF";
        DividerHex = "#1AFFFFFF";
        StatusOkTextHex = christmas ? "#A8E6B4" : "#8CE0A0";
        StatusOkBackgroundHex = christmas ? "#1E3A2A" : "#1F3A26";
        StatusErrorTextHex = christmas ? "#F3A9A9" : "#F1A0A6";
        StatusErrorBackgroundHex = christmas ? "#3A1F22" : "#3A1F22";
        WarningTextHex = christmas ? "#F0A98C" : "#E8C07D";
        LogContinuationHex = christmas ? "#C8D3CA" : "#C3CBD6";
        ScrollThumbHex = christmas ? "#6C8074" : "#66707E";
        NavInactiveHex = christmas ? "#7E9083" : "#7D8797";
    }

    public static Color Parse(string hex) => Color.ParseColor(hex);

    public static Typeface TitleTypeface => Typeface.Create("sans-serif-medium", TypefaceStyle.Normal);

    public static Typeface BodyTypeface => Typeface.Create("sans-serif", TypefaceStyle.Normal);

    /// <summary>พื้นหลังกลมมีขอบจาง ใช้กับการ์ด/ชิปที่ต้องการเส้นแบ่งชัดขึ้น</summary>
    public static Android.Graphics.Drawables.GradientDrawable CreateSurfaceDrawable(
        Context context, string fillHex, float radiusDp, string strokeHex = null)
    {
        var drawable = new Android.Graphics.Drawables.GradientDrawable();
        drawable.SetShape(Android.Graphics.Drawables.ShapeType.Rectangle);
        drawable.SetCornerRadius(UI.Dp(context, radiusDp));
        drawable.SetColor(Parse(fillHex));
        if (strokeHex is not null)
            drawable.SetStroke(UI.Dp(context, 1), Parse(strokeHex));
        return drawable;
    }
}
