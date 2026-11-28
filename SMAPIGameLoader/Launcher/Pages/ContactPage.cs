using Android.Content;
using Android.Graphics;
using Android.Net;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.CardView.Widget;
using System;
using Orientation = Android.Widget.Orientation;

namespace SMAPIGameLoader.Launcher.Pages;

/// <summary>
/// หน้า Contact: ลิงก์ชุมชน (Discord/GitHub/Nexus) + คำขอบคุณ community
/// </summary>
internal sealed class ContactPage : LinearLayout
{
    const string ThanksText =
        "Made with love for the Stardew Valley Android modding community.\n\n" +
        "Huge thanks to Pathoschild and the SMAPI team, and to every mod author, " +
        "translator, playtester and helper who keeps this project alive. " +
        "You are the reason Stardew Valley still feels like home.";

    public ContactPage(Context context) : base(context)
    {
        Orientation = Orientation.Vertical;
        SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.BackgroundHex));
        LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        AddView(new PageHeader(context, "Social"), new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

        var body = new LinearLayout(context) { Orientation = Orientation.Vertical };
        body.SetPadding(UI.Dp(context, 16), UI.Dp(context, 12),
                        UI.Dp(context, 16), UI.Dp(context, 24));

        var scroll = new ScrollView(context) { FillViewport = false };
        scroll.AddView(body, new ScrollView.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));
        AddView(scroll, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 0, 1f));

        var cardParams = new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        { BottomMargin = UI.Dp(context, 12) };

        body.AddView(CreateThanksCard(context), cardParams);
        body.AddView(CreateLinksCard(context), cardParams);
    }

    static CardView CreateThanksCard(Context context)
    {
        var card = CreateCard(context);
        var content = CreateCardContent(context);

        var title = new TextView(context) { Text = "Thanks for being such a great community!" };
        title.SetTextSize(ComplexUnitType.Sp, 17);
        title.SetTextColor(LauncherTheme.Parse(LauncherTheme.AccentHex));
        title.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        content.AddView(title);

        var text = new TextView(context) { Text = ThanksText };
        text.SetTextSize(ComplexUnitType.Sp, 13);
        text.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        text.SetLineSpacing(0, 1.3f);
        text.SetPadding(0, UI.Dp(context, 8), 0, 0);
        content.AddView(text);

        card.AddView(content);
        return card;
    }

    /// <summary>ไอคอน GitHub เป็นสีขาว/ดำตามความสว่างของธีม</summary>
    static int GithubIcon => LauncherTheme.IsLightTheme
        ? Resource.Drawable.ic_contact_github_ink
        : Resource.Drawable.ic_contact_github;

    static CardView CreateLinksCard(Context context)
    {
        var card = CreateCard(context);
        var content = CreateCardContent(context);
        content.SetPadding(0, UI.Dp(context, 4), 0, UI.Dp(context, 4));

        var links = new (int Icon, string Title, string Subtitle, string Url)[]
        {
            (Resource.Drawable.ic_contact_discord, "SMAPI Thailand & Global", "discord.gg/cHPWYe2RYb",
                "https://discord.gg/cHPWYe2RYb"),
            (GithubIcon, "GitHub SMAPI Launcher",
                "github.com/NRTnarathip/SMAPI-Android-Launcher",
                "https://github.com/NRTnarathip/SMAPI-Android-Launcher"),
            (GithubIcon, "GitHub SMAPI Zip",
                "github.com/NRTnarathip/SMAPI-Android-1.6",
                "https://github.com/NRTnarathip/SMAPI-Android-1.6"),
            (Resource.Drawable.ic_contact_nexus, "Nexus SMAPI Zip",
                "nexusmods.com/stardewvalley/mods/44436",
                "https://www.nexusmods.com/stardewvalley/mods/44436"),
            (Resource.Drawable.ic_contact_cinderbox, "Eky-Cinderbox",
                "discord.gg/ntrCvacg49", "https://discord.gg/ntrCvacg49"),
        };

        for (int i = 0; i < links.Length; i++)
        {
            if (i > 0)
                content.AddView(CreateDivider(context));
            content.AddView(CreateLinkRow(context, links[i].Icon, links[i].Title,
                links[i].Subtitle, links[i].Url));
        }

        card.AddView(content);
        return card;
    }

    static View CreateLinkRow(Context context, int iconRes, string title,
                              string subtitle, string url)
    {
        var row = new LinearLayout(context)
        {
            Orientation = Orientation.Horizontal,
            Clickable = true,
            Focusable = true
        };
        row.SetGravity(GravityFlags.CenterVertical);
        row.SetPadding(UI.Dp(context, 16), UI.Dp(context, 12),
                       UI.Dp(context, 16), UI.Dp(context, 12));
        row.Background = UI.CreateRippleDrawable(context, null, LauncherTheme.ChipRadiusDp);
        row.ContentDescription = $"Open {title}";

        var icon = new ImageView(context);
        icon.SetImageResource(iconRes);
        icon.SetScaleType(ImageView.ScaleType.CenterInside);
        row.AddView(icon, new LinearLayout.LayoutParams(
            UI.Dp(context, 26), UI.Dp(context, 26))
        { RightMargin = UI.Dp(context, 14) });

        var column = new LinearLayout(context) { Orientation = Orientation.Vertical };
        row.AddView(column, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f));

        var titleView = new TextView(context) { Text = title };
        titleView.SetTextSize(ComplexUnitType.Sp, 15);
        titleView.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        titleView.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        titleView.SetSingleLine(true);
        column.AddView(titleView);

        var subtitleView = new TextView(context) { Text = subtitle };
        subtitleView.SetTextSize(ComplexUnitType.Sp, 12);
        subtitleView.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        subtitleView.SetSingleLine(true);
        subtitleView.Ellipsize = Android.Text.TextUtils.TruncateAt.End;
        subtitleView.SetPadding(0, UI.Dp(context, 2), 0, 0);
        column.AddView(subtitleView);

        row.Click += (s, e) => OpenUrl(context, url);
        return row;
    }

    static void OpenUrl(Context context, string url)
    {
        try
        {
            context.StartActivity(new Intent(
                Intent.ActionView, Android.Net.Uri.Parse(url)));
        }
        catch (Exception)
        {
            Toast.MakeText(context, "No app can open this link", ToastLength.Short)?.Show();
        }
    }

    static CardView CreateCard(Context context)
    {
        var card = new CardView(context)
        {
            Radius = UI.Dp(context, LauncherTheme.CardRadiusDp),
            CardElevation = 0,
            UseCompatPadding = false
        };
        card.SetCardBackgroundColor(LauncherTheme.Parse(LauncherTheme.CardBackgroundHex));
        return card;
    }

    static LinearLayout CreateCardContent(Context context)
    {
        var content = new LinearLayout(context) { Orientation = Orientation.Vertical };
        content.SetPadding(UI.Dp(context, 18), UI.Dp(context, 16),
                           UI.Dp(context, 18), UI.Dp(context, 16));
        return content;
    }

    static View CreateDivider(Context context)
    {
        var divider = new View(context);
        divider.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.DividerHex));
        divider.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, UI.Dp(context, 1))
        {
            LeftMargin = UI.Dp(context, 56),
            RightMargin = UI.Dp(context, 16)
        };
        return divider;
    }
}
