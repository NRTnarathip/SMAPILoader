using Android.App;
using Android.OS;
using Android.Widget;
using Newtonsoft.Json.Linq;
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

            var directoryInfo = new DirectoryInfo(folderPath);
            //find manfiest single
            var files = Directory.GetFiles(folderPath);
            var manifestFiles = files.Where(file => file.Contains("manifest.json")).ToArray();
            if (manifestFiles.Length == 1)
            {
                var manifestText = File.ReadAllText(manifestFiles[0]);
                var manifest = JObject.Parse(manifestText);
                var mod = new ModItemView(manifest, folderPath);
                mods.Add(mod);
            }
        }

        modAdapter.RefreshMods(mods);
    }

    void OnClickModItemView(AdapterView.ItemClickEventArgs e)
    {
        var mod = modAdapter.GetModOnClick(e);
        var text = new StringBuilder();
        text.AppendLine($"Mod: {mod.NameText}");
        text.AppendLine($"{mod.VersionText}");
        text.AppendLine();
        text.AppendLine("Are you sure to delete this mod?");
        DialogTool.Show(
            "❌Delete: " + mod.NameText,
            text.ToString(),
            buttonOKName: "Yes Delete It!",
            onClickYes: () =>
            {
                DeleteMod(mod);
            }
        );
    }
    void DeleteMod(ModItemView mod)
    {
        Console.WriteLine("try delete mod: " + mod.modName);
        if (ModInstaller.TryDeleteMod(mod.modFolderPath))
        {
            ToastNotifyTool.Notify("Done delete mod: " + mod.modName);
            RefreshMods();
        }
    }
}

