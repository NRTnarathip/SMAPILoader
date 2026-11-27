using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;

namespace SMAPIGameLoader.Launcher;

public static class UI
{
    public static int Dp(Context context, float dp) =>
        (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, context.Resources!.DisplayMetrics!);

    /// <summary>
    /// Rounded drawable wrapped in a ripple for touch feedback.
    /// Pass a null fill or stroke color to leave that layer transparent.
    /// </summary>
    public static Drawable CreateRippleDrawable(
        Context context, string fillColorHex, float cornerRadiusDp, string strokeColorHex = null)
    {
        var fill = CreateRoundedDrawable(context, fillColorHex, cornerRadiusDp, strokeColorHex);
        var mask = CreateRoundedDrawable(context, "#FFFFFF", cornerRadiusDp, null);
        return new RippleDrawable(
            ColorStateList.ValueOf(Color.ParseColor(LauncherTheme.RippleHex)), fill, mask);
    }

    static GradientDrawable CreateRoundedDrawable(
        Context context, string fillColorHex, float cornerRadiusDp, string strokeColorHex)
    {
        var drawable = new GradientDrawable();
        drawable.SetShape(ShapeType.Rectangle);
        drawable.SetCornerRadius(Dp(context, cornerRadiusDp));
        drawable.SetColor(fillColorHex is null
            ? Color.Transparent
            : Color.ParseColor(fillColorHex));

        if (strokeColorHex is not null)
            drawable.SetStroke(Dp(context, 1), Color.ParseColor(strokeColorHex));

        return drawable;
    }
}
