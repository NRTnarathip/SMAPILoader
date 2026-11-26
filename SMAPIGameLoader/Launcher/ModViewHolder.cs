using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.CardView.Widget;
using AndroidX.RecyclerView.Widget;
using System;

namespace SMAPIGameLoader.Launcher;

internal class ModViewHolder : RecyclerView.ViewHolder
{
    const string CardBackgroundColorHex = "#1A1D24";
    const float DisabledAlpha = 0.45f;

    private readonly TextView _name;
    private readonly TextView _version;
    private readonly Action<ModInfo, View> _onClicked;
    private ModInfo? _mod;

    private ModViewHolder(View root, TextView name, TextView version,
                          Action<ModInfo, View> onClicked) : base(root)
    {
        _name = name;
        _version = version;
        _onClicked = onClicked;

        root.Click += (s, e) =>
        {
            if (_mod != null) _onClicked(_mod, root);
        };
    }

    public static ModViewHolder Create(Context context, Action<ModInfo, View> onClicked)
    {
        var card = new CardView(context)
        {
            Radius = Ui.Dp(context, 12),
            CardElevation = Ui.Dp(context, 4),
            UseCompatPadding = true
        };
        card.SetCardBackgroundColor(Color.ParseColor(CardBackgroundColorHex));
        card.LayoutParameters = new RecyclerView.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            BottomMargin = Ui.Dp(context, 12)
        };

        var column = new LinearLayout(context) { Orientation = Orientation.Vertical };
        column.SetPadding(Ui.Dp(context, 16), Ui.Dp(context, 14),
                          Ui.Dp(context, 16), Ui.Dp(context, 14));
        card.AddView(column);

        var name = new TextView(context);
        name.SetTextSize(ComplexUnitType.Sp, 16);
        name.SetTextColor(Color.White);
        name.SetTypeface(null, TypefaceStyle.Bold);

        var version = new TextView(context);
        version.SetTextSize(ComplexUnitType.Sp, 12);
        version.SetTextColor(Color.ParseColor("#9AA0A6"));
        version.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        { TopMargin = Ui.Dp(context, 2) };

        column.AddView(name);
        column.AddView(version);

        return new ModViewHolder(card, name, version, onClicked);
    }

    public void Bind(ModInfo mod)
    {
        _mod = mod;
        _name.Text = mod.NameUIText;
        _version.Text = mod.VersionUIText;
        ItemView.Alpha = mod.Enabled ? 1f : DisabledAlpha;
    }
}
