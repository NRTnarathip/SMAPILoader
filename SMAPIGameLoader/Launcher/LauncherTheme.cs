using Android.Content;
using Android.Content.Res;
using Android.Graphics;

namespace SMAPIGameLoader.Launcher;

/// <summary>
/// Design tokens ของ launcher (dark-only) — สี/ฟอนต์/รัศมีที่ทุกหน้าใช้ร่วมกัน
/// แก้ค่าที่นี่ที่เดียวเพื่อเปลี่ยนโทนทั้งแอป
/// </summary>
internal static class LauncherTheme
{
    // พื้นผิว
    public const string BackgroundHex = "#0D1116";
    public const string HeaderBackgroundHex = "#141A21";
    public const string CardBackgroundHex = "#191F29";
    public const string SurfaceVariantHex = "#232B37";
    public const string NavBarBackgroundHex = "#12171E";

    // การ์ดที่กำลังถูกเลือก/เปิด action อยู่
    public const string SelectedCardBackgroundHex = "#1F3A26";

    // สีหลัก (เขียว Stardew) และสีเน้น
    public const string PrimaryHex = "#2E7D32";
    public const string AccentHex = "#8AD49A";

    // ตัวอักษร
    public const string TextPrimaryHex = "#E9EDF2";
    public const string TextSecondaryHex = "#94A0B0";
    public const string TextOnPrimaryHex = "#FFFFFF";

    // เส้นคั่น
    public const string DividerHex = "#1AFFFFFF";

    // สถานะ (pill อ่อนบนพื้นเข้ม)
    public const string StatusOkTextHex = "#8CE0A0";
    public const string StatusOkBackgroundHex = "#1F3A26";
    public const string StatusErrorTextHex = "#F1A0A6";
    public const string StatusErrorBackgroundHex = "#3A1F22";

    // ระดับของ log
    public const string WarningTextHex = "#E8C07D";
    public const string LogContinuationHex = "#C3CBD6";

    // รัศมีมุม
    public const float CardRadiusDp = 18;
    public const float ButtonRadiusDp = 16;
    public const float ChipRadiusDp = 12;

    public static Color Parse(string hex) => Color.ParseColor(hex);

    public static Typeface TitleTypeface => Typeface.Create("sans-serif-medium", TypefaceStyle.Normal);

    public static Typeface BodyTypeface => Typeface.Create("sans-serif", TypefaceStyle.Normal);

    /// <summary>สีไอคอน/ข้อความของ bottom nav: selected = accent, ปกติ = เทากลาง</summary>
    public static ColorStateList BottomNavItemColors()
    {
        return new ColorStateList(
            new[]
            {
                new[] { Android.Resource.Attribute.StateChecked },
                new int[0]
            },
            new[]
            {
                Parse(AccentHex).ToArgb(),
                Parse("#7D8797").ToArgb()
            });
    }

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
