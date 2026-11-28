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

    /// <summary>ธีมพื้นสว่าง — ใช้เลือกไอคอนแบบสีเข้ม (ไอคอนขาวจะมองไม่เห็น)</summary>
    public static bool IsLightTheme => CurrentName == ChristmasName;

    /// <summary>ไอคอน status bar เป็นสีเข้ม (พื้นสว่าง) หรือสีขาว (พื้นเข้ม)</summary>
    public static bool StatusBarDarkIcons { get; private set; }

    /// <summary>ไอคอน system navigation bar เป็นสีเข้ม (พื้นสว่าง)</summary>
    public static bool NavBarDarkIcons { get; private set; }

    // พื้นผิว
    public static string BackgroundHex { get; private set; } = "#0D1116";
    public static string HeaderBackgroundHex { get; private set; } = "#141A21";
    public static string CardBackgroundHex { get; private set; } = "#191F29";
    public static string SurfaceVariantHex { get; private set; } = "#232B37";
    public static string NavBarBackgroundHex { get; private set; } = "#12171E";

    /// <summary>สีข้อความ/ไอคอนบน header (ต้องอ่านออกบน HeaderBackgroundHex)</summary>
    public static string HeaderTextHex { get; private set; } = "#E9EDF2";

    /// <summary>พื้นปุ่มใน header (เช่น install/sort/reload)</summary>
    public static string HeaderPillHex { get; private set; } = "#232B37";

    /// <summary>สีหัวข้อการ์ด</summary>
    public static string CardTitleHex { get; private set; } = "#E9EDF2";

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

    /// <summary>สี ripple ของการแตะ (สว่างบนพื้นเข้ม, เข้มบนพื้นสว่าง)</summary>
    public static string RippleHex { get; private set; } = "#33FFFFFF";

    // รัศมีมุม
    public const float CardRadiusDp = 18;
    public const float ButtonRadiusDp = 16;
    public const float ChipRadiusDp = 12;

    // scrollbar — ใช้ร่วมกันทุกหน้าผ่าน RecyclerScrollBar (สี = ScrollThumbHex, ตอนลาก = AccentHex)
    public const float ScrollThumbWidthDp = 12;
    public const float ScrollThumbMinHeightDp = 24;
    public const float ScrollBarTouchSlopDp = 12;
    public const float ScrollThumbInsetDp = 1;

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

        BackgroundHex = christmas ? "#FAF6EF" : "#0D1116";
        HeaderBackgroundHex = christmas ? "#1B7A3E" : "#141A21";
        HeaderTextHex = christmas ? "#FFFFFF" : "#E9EDF2";
        HeaderPillHex = christmas ? "#26FFFFFF" : "#232B37";
        CardBackgroundHex = christmas ? "#FFFFFF" : "#191F29";
        CardTitleHex = christmas ? "#1B7A3E" : "#E9EDF2";
        SurfaceVariantHex = christmas ? "#EDF4EC" : "#232B37";
        NavBarBackgroundHex = christmas ? "#EDF4EC" : "#12171E";
        SelectedCardBackgroundHex = christmas ? "#FFF3D1" : "#1F3A26";
        PrimaryHex = christmas ? "#B3261E" : "#2E7D32";
        AccentHex = christmas ? "#1B7A3E" : "#8AD49A";
        TextPrimaryHex = christmas ? "#1C1B18" : "#E9EDF2";
        TextSecondaryHex = christmas ? "#666B62" : "#94A0B0";
        TextOnPrimaryHex = "#FFFFFF";
        DividerHex = christmas ? "#1A000000" : "#1AFFFFFF";
        StatusOkTextHex = christmas ? "#1B7A3E" : "#8CE0A0";
        StatusOkBackgroundHex = christmas ? "#E3F3E6" : "#1F3A26";
        StatusErrorTextHex = christmas ? "#B3261E" : "#F1A0A6";
        StatusErrorBackgroundHex = christmas ? "#FBE6E3" : "#3A1F22";
        WarningTextHex = christmas ? "#8A6100" : "#E8C07D";
        LogContinuationHex = christmas ? "#3A3933" : "#C3CBD6";
        ScrollThumbHex = christmas ? "#9A968C" : "#66707E";
        NavInactiveHex = christmas ? "#6F7A6C" : "#7D8797";
        RippleHex = christmas ? "#1A000000" : "#33FFFFFF";

        // status bar = สี header (christmas เป็นเขียวเข้ม จึงต้องใช้ไอคอนขาว)
        StatusBarDarkIcons = false;
        NavBarDarkIcons = christmas;
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
