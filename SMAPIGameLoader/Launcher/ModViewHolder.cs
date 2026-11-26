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

    private readonly View _content;
    private readonly TextView _name;
    private readonly TextView _version;
    private readonly TextView _size;
    private readonly TextView _date;
    private readonly Action<ModInfo, View> _onClicked;
    private ModInfo? _mod;

    private ModViewHolder(View root, View content, TextView name, TextView version, TextView size,
                          TextView date, Action<ModInfo, View> onClicked) : base(root)
    {
        _content = content;
        _name = name;
        _version = version;
        _size = size;
        _date = date;
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

        // แถวข้อมูลเสริม: ขนาดไฟล์ | วันที่
        var metaRow = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        metaRow.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        { TopMargin = Ui.Dp(context, 4) };

        var size = CreateSecondaryText(context);
        var date = CreateSecondaryText(context);
        date.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent)
        { LeftMargin = Ui.Dp(context, 16) };

        metaRow.AddView(size);
        metaRow.AddView(date);

        column.AddView(name);
        column.AddView(version);
        column.AddView(metaRow);

        return new ModViewHolder(card, column, name, version, size, date, onClicked);
    }

    static TextView CreateSecondaryText(Context context)
    {
        var text = new TextView(context);
        text.SetTextSize(ComplexUnitType.Sp, 11);
        text.SetTextColor(Color.ParseColor("#9AA0A6"));
        return text;
    }

    public void Bind(ModInfo mod)
    {
        _mod = mod;
        _name.Text = mod.NameUIText;
        _version.Text = mod.VersionUIText;
        _size.Text = mod.SizeUIText;
        _date.Text = mod.DateUIText;

        // ต้อง dim ที่ content ข้างใน ไม่ใช่ ItemView เพราะ DefaultItemAnimator
        // จะ cross-fade แล้วเซ็ต itemView.Alpha กลับเป็น 1 ตอน NotifyItemChanged
        _content.Alpha = mod.Enabled ? 1f : DisabledAlpha;
    }
}
