using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;

namespace SMAPIGameLoader.Launcher;

internal class ModAdapter : RecyclerView.Adapter
{
    private readonly Context _context;
    private readonly List<ModInfo> _mods;
    private readonly Action<ModInfo, View> _onClicked;
    private readonly Action<ModInfo> _onToggled;

    public ModAdapter(Context context, List<ModInfo> mods, Action<ModInfo, View> onClicked,
                      Action<ModInfo> onToggled)
    {
        _context = context;
        _mods = mods;
        _onClicked = onClicked;
        _onToggled = onToggled;
    }

    public override int ItemCount => _mods.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) =>
        ModViewHolder.Create(_context, _onClicked, _onToggled);

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position) =>
        ((ModViewHolder)holder).Bind(_mods[position]);
}
