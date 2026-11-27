using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.CardView.Widget;
using AndroidX.RecyclerView.Widget;
using System;
using Orientation = Android.Widget.Orientation;

namespace SMAPIGameLoader.Launcher;

internal class ModViewHolder : RecyclerView.ViewHolder
{
    const float DisabledAlpha = 0.45f;

    private readonly View _content;
    private readonly TextView _name;
    private readonly TextView _detail;
    private readonly SwitchCompat _toggle;
    private readonly Action<ModInfo, View> _onClicked;
    private readonly Action<ModInfo> _onToggled;
    private ModInfo? _mod;

    private ModViewHolder(View root, View content, TextView name, TextView detail,
                          SwitchCompat toggle, Action<ModInfo, View> onClicked,
                          Action<ModInfo> onToggled) : base(root)
    {
        _content = content;
        _name = name;
        _detail = detail;
        _toggle = toggle;
        _onClicked = onClicked;
        _onToggled = onToggled;

        root.Click += (s, e) =>
        {
            if (_mod != null) _onClicked(_mod, root);
        };

        // Bind ตั้ง Checked ให้ตรงกับ state จริงอยู่แล้ว ค่าที่ต่างจึงเป็นการกดของผู้ใช้เท่านั้น
        toggle.CheckedChange += (s, e) =>
        {
            if (_mod != null && e.IsChecked != _mod.Enabled)
                _onToggled(_mod);
        };
    }

    public static ModViewHolder Create(Context context, Action<ModInfo, View> onClicked,
                                       Action<ModInfo> onToggled)
    {
        var card = new CardView(context)
        {
            Radius = UI.Dp(context, 14),
            CardElevation = 0,
            UseCompatPadding = true
        };
        card.SetCardBackgroundColor(LauncherTheme.Parse(LauncherTheme.CardBackgroundHex));
        int sideMargin = UI.Dp(context, 8);
        card.LayoutParameters = new RecyclerView.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            LeftMargin = sideMargin,
            RightMargin = sideMargin
        };

        // สองบรรทัดต่อการ์ด (ชื่อ / version · size · date) + switch เปิด/ปิด mod ที่ขวาสุด
        var row = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        row.SetGravity(GravityFlags.CenterVertical);
        row.SetPadding(UI.Dp(context, 16), UI.Dp(context, 6),
                       UI.Dp(context, 12), UI.Dp(context, 6));
        card.AddView(row);

        var column = new LinearLayout(context) { Orientation = Orientation.Vertical };
        row.AddView(column, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f));

        var name = new TextView(context);
        name.SetTextSize(ComplexUnitType.Sp, 15);
        name.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        name.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        name.SetSingleLine(true);
        name.Ellipsize = TextUtils.TruncateAt.End;

        var detail = new TextView(context);
        detail.SetTextSize(ComplexUnitType.Sp, 12);
        detail.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        detail.SetSingleLine(true);
        detail.Ellipsize = TextUtils.TruncateAt.End;
        detail.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        { TopMargin = UI.Dp(context, 3) };

        column.AddView(name);
        column.AddView(detail);

        var toggle = new SwitchCompat(context);
        toggle.ThumbTintList = TintStates(LauncherTheme.AccentHex, LauncherTheme.TextSecondaryHex);
        toggle.TrackTintList = TintStates(LauncherTheme.StatusOkBackgroundHex,
                                          LauncherTheme.SurfaceVariantHex);
        toggle.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent)
        { LeftMargin = UI.Dp(context, 8) };
        row.AddView(toggle);

        return new ModViewHolder(card, column, name, detail, toggle, onClicked, onToggled);
    }

    static ColorStateList TintStates(string checkedHex, string normalHex) =>
        new(
            new[] { new[] { Android.Resource.Attribute.StateChecked }, new int[0] },
            new[]
            {
                LauncherTheme.Parse(checkedHex).ToArgb(),
                LauncherTheme.Parse(normalHex).ToArgb(),
            });

    public void Bind(ModInfo mod)
    {
        _mod = mod;
        _name.Text = mod.NameUIText;
        _detail.Text = $"{mod.VersionUIText} · {mod.SizeUIText} · {mod.DateUIText}";
        _toggle.ContentDescription = $"Toggle {mod.NameUIText}";

        // ต้อง dim ที่ content ข้างใน ไม่ใช่ ItemView เพราะ DefaultItemAnimator
        // จะ cross-fade แล้วเซ็ต itemView.Alpha กลับเป็น 1 ตอน NotifyItemChanged
        _content.Alpha = mod.Enabled ? 1f : DisabledAlpha;
        _toggle.Checked = mod.Enabled;
    }
}
