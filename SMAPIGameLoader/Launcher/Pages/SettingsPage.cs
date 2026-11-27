using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.CardView.Widget;
using System;
using Orientation = Android.Widget.Orientation;

namespace SMAPIGameLoader.Launcher.Pages;

/// <summary>
/// หน้า Settings: เลือกธีมของ launcher (dark / christmas)
/// เปลี่ยนธีมแล้ว recreate activity เพื่อให้ทุกหน้าใช้สีใหม่
/// </summary>
internal sealed class SettingsPage : LinearLayout
{
    public SettingsPage(Context context) : base(context)
    {
        Orientation = Orientation.Vertical;
        SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.BackgroundHex));
        LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        AddView(new PageHeader(context, "Settings"), new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

        var body = new LinearLayout(context) { Orientation = Orientation.Vertical };
        body.SetPadding(UI.Dp(context, 16), UI.Dp(context, 16),
                        UI.Dp(context, 16), UI.Dp(context, 24));

        var scroll = new ScrollView(context) { FillViewport = false };
        scroll.AddView(body, new ScrollView.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));
        AddView(scroll, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 0, 1f));

        body.AddView(CreateThemeCard(context), new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));
    }

    static CardView CreateThemeCard(Context context)
    {
        var card = new CardView(context)
        {
            Radius = UI.Dp(context, LauncherTheme.CardRadiusDp),
            CardElevation = 0,
            UseCompatPadding = false
        };
        card.SetCardBackgroundColor(LauncherTheme.Parse(LauncherTheme.CardBackgroundHex));

        var content = new LinearLayout(context) { Orientation = Orientation.Vertical };
        content.SetPadding(0, UI.Dp(context, 14), 0, UI.Dp(context, 6));

        var title = new TextView(context) { Text = "Theme" };
        title.SetTextSize(ComplexUnitType.Sp, 17);
        title.SetTextColor(LauncherTheme.Parse(LauncherTheme.CardTitleHex));
        title.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        title.SetPadding(UI.Dp(context, 16), 0, UI.Dp(context, 16), UI.Dp(context, 6));
        content.AddView(title);

        content.AddView(CreateThemeRow(context, LauncherTheme.DarkName, "Dark",
            "Stardew green on dark surfaces"));
        content.AddView(CreateThemeRow(context, LauncherTheme.ChristmasName, "Christmas",
            "Festive red, green and gold"));

        card.AddView(content);
        return card;
    }

    static View CreateThemeRow(Context context, string themeName, string title, string subtitle)
    {
        bool selected = LauncherTheme.CurrentName == themeName;

        var row = new LinearLayout(context)
        {
            Orientation = Orientation.Horizontal,
            Clickable = true,
            Focusable = true
        };
        row.SetGravity(GravityFlags.CenterVertical);
        row.SetPadding(UI.Dp(context, 12), UI.Dp(context, 12),
                       UI.Dp(context, 12), UI.Dp(context, 12));
        row.Background = UI.CreateRippleDrawable(context,
            selected ? LauncherTheme.SelectedCardBackgroundHex : null,
            LauncherTheme.ChipRadiusDp);
        row.LayoutParameters = new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        { LeftMargin = UI.Dp(context, 4), RightMargin = UI.Dp(context, 4), BottomMargin = UI.Dp(context, 4) };

        var column = new LinearLayout(context) { Orientation = Orientation.Vertical };
        row.AddView(column, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f));

        var titleView = new TextView(context) { Text = title };
        titleView.SetTextSize(ComplexUnitType.Sp, 15);
        titleView.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        titleView.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        column.AddView(titleView);

        var subtitleView = new TextView(context) { Text = subtitle };
        subtitleView.SetTextSize(ComplexUnitType.Sp, 12);
        subtitleView.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        subtitleView.SetPadding(0, UI.Dp(context, 2), 0, 0);
        column.AddView(subtitleView);

        var check = new TextView(context) { Text = "✓" };
        check.SetTextSize(ComplexUnitType.Sp, 18);
        check.SetTextColor(LauncherTheme.Parse(selected
            ? LauncherTheme.AccentHex
            : LauncherTheme.TextSecondaryHex));
        check.Visibility = selected ? ViewStates.Visible : ViewStates.Invisible;
        check.SetPadding(UI.Dp(context, 8), 0, UI.Dp(context, 4), 0);
        row.AddView(check);

        row.Click += (s, e) => ApplyTheme(context, themeName, selected);
        return row;
    }

    static void ApplyTheme(Context context, string themeName, bool alreadySelected)
    {
        if (alreadySelected)
            return;

        LauncherTheme.Set(themeName);
        if (context is Activity activity)
            activity.Recreate();
    }
}
