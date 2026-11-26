using Android.Content;
using Android.Graphics;
using Android.Text;
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
    private readonly TextView _detail;
    private readonly Action<ModInfo, View> _onClicked;
    private ModInfo? _mod;

    private ModViewHolder(View root, View content, TextView name, TextView detail,
                          Action<ModInfo, View> onClicked) : base(root)
    {
        _content = content;
        _name = name;
        _detail = detail;
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
            Radius = Ui.Dp(context, 10),
            CardElevation = Ui.Dp(context, 2),
            UseCompatPadding = true
        };
        card.SetCardBackgroundColor(Color.ParseColor(CardBackgroundColorHex));
        int sideMargin = Ui.Dp(context, 12);
        card.LayoutParameters = new RecyclerView.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            BottomMargin = Ui.Dp(context, 8),
            LeftMargin = sideMargin,
            RightMargin = sideMargin
        };

        // สองบรรทัดต่อการ์ด (ชื่อ / version · size · date) เพื่อให้ list ยาว ๆ ดูแน่นและอ่านง่าย
        var column = new LinearLayout(context) { Orientation = Orientation.Vertical };
        column.SetPadding(Ui.Dp(context, 14), Ui.Dp(context, 10),
                          Ui.Dp(context, 14), Ui.Dp(context, 10));
        card.AddView(column);

        var name = new TextView(context);
        name.SetTextSize(ComplexUnitType.Sp, 15);
        name.SetTextColor(Color.White);
        name.SetTypeface(null, TypefaceStyle.Bold);
        name.SetSingleLine(true);
        name.Ellipsize = TextUtils.TruncateAt.End;

        var detail = new TextView(context);
        detail.SetTextSize(ComplexUnitType.Sp, 12);
        detail.SetTextColor(Color.ParseColor("#9AA0A6"));
        detail.SetSingleLine(true);
        detail.Ellipsize = TextUtils.TruncateAt.End;
        detail.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        { TopMargin = Ui.Dp(context, 3) };

        column.AddView(name);
        column.AddView(detail);

        return new ModViewHolder(card, column, name, detail, onClicked);
    }

    public void Bind(ModInfo mod)
    {
        _mod = mod;
        _name.Text = mod.NameUIText;
        _detail.Text = $"{mod.VersionUIText} · {mod.SizeUIText} · {mod.DateUIText}";

        // ต้อง dim ที่ content ข้างใน ไม่ใช่ ItemView เพราะ DefaultItemAnimator
        // จะ cross-fade แล้วเซ็ต itemView.Alpha กลับเป็น 1 ตอน NotifyItemChanged
        _content.Alpha = mod.Enabled ? 1f : DisabledAlpha;
    }
}
