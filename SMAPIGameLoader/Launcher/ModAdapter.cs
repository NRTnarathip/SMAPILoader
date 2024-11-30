using Android.App;
using Android.Widget;
using System.Collections.Generic;

namespace SMAPIGameLoader.Launcher;

public class ModAdapter : BaseAdapter<ModItemView>
{
    private readonly Activity context;
    private readonly List<ModItemView> items;

    public ModAdapter(Activity context, List<ModItemView> items)
    {
        this.context = context;
        this.items = items;
    }

    public override ModItemView this[int position] => items[position];

    public override int Count => items.Count;

    public override long GetItemId(int position) => position;

    public override Android.Views.View GetView(int position, Android.Views.View convertView, Android.Views.ViewGroup parent)
    {
        var item = items[position];
        var view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.ModItemViewLayout, null);

        view.FindViewById<TextView>(Resource.Id.modName).Text = item.NameText;
        view.FindViewById<TextView>(Resource.Id.version).Text = item.VersionText;
        view.FindViewById<TextView>(Resource.Id.folderPath).Text = item.FolderPathText;

        return view;
    }

    public void RefreshMods(List<ModItemView> newMods)
    {
        this.items.Clear();
        this.items.AddRange(newMods);
        NotifyDataSetChanged();
    }
    public ModItemView GetModOnClick(AdapterView.ItemClickEventArgs click)
    {
        if (items.Count == 0)
            return null;
        if (click.Position >= items.Count)
            return null;

        return items[click.Position];
    }
}
