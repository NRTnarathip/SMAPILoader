using Android.App;
using Android.OS;
using Android.Widget;
using SMAPIGameLoader.Tool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Launcher;
[Activity(
    Label = "Mod Manager"
)]
internal class ModManagerActivity : Activity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        //setup base
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.ModManagerLayout);

        //setup my sdk
        ActivityTool.Init(this);//debug

        //ready
        ToastNotifyTool.Notify("OnCreate ModManager");

        SetupPage();
    }

    ModAdapter modAdapter;
    void SetupPage()
    {
        //setup bind
        var modsListView = FindViewById<ListView>(Resource.Id.modsListViews);
        modsListView.Adapter = modAdapter = new ModAdapter(this, new());
        modsListView.ItemClick += (sender, e) =>
        {
            OnClickModItemView(e);
        };

        //ready
        RefreshMods();
    }
    void RefreshMods()
    {
        //simulate test only
        List<ModItemView> mods = new();

        var folders = Directory.GetDirectories(ModInstaller.ModDir);
        foreach (var folderPath in folders)
        {
            var mod = new ModItemView();
            mod.FolderPath = folderPath;
            mod.Name = Path.GetDirectoryName(folderPath);
            mod.Version = "unknow";
        }

        modAdapter.RefreshMods(mods);
    }

    void OnClickModItemView(AdapterView.ItemClickEventArgs e)
    {
        var mod = modAdapter.GetModOnClick(e);
        ToastNotifyTool.Notify($"Clicked on {mod.Name} (Version: {mod.Version}");
    }
}

