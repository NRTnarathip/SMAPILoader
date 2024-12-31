using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using Newtonsoft.Json.Linq;
using SMAPIGameLoader.Tool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher;
[Activity(
    Label = "Mod Manager",
    Theme = "@style/AppTheme"
)]
internal class ModManagerActivity : AppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        //setup base
        base.OnCreate(savedInstanceState);
        Platform.Init(this, savedInstanceState);
        SetContentView(Resource.Layout.ModManagerLayout);

        //setup my sdk
        ActivityTool.Init(this);//debug

        //ready
        SetupPage();
    }

    ModAdapter modAdapter;
    List<ModItemView> mods = new();
    void SetupPage()
    {
        //setup bind
        var modsListView = FindViewById<ListView>(Resource.Id.modsListViews);
        modsListView.Adapter = modAdapter = new ModAdapter(this, mods);
        modsListView.ItemClick += (sender, e) =>
        {
            OnClickModItemView(e);
        };
        var installModBtn = FindViewById<Button>(Resource.Id.InstallModBtn);
        installModBtn.Click += async (sender, e) =>
        {
            ModInstaller.OnClickInstallMod(OnInstalledCallback: () =>
            {
                RefreshMods();
            });
        };

        FindViewById<Button>(Resource.Id.OpenFolderModsBtn).Click += OnClick_OpenFolderMods;

        //ready
        RefreshMods();
    }

    void OnClick_OpenFolderMods(object sender, EventArgs e)
    {
        FileTool.OpenAppFilesExternalFilesDir("Mods");
    }

    // mod folder path with manifest.json inside
    void RefreshMods()
    {
        //clear first
        mods.Clear();

        try
        {
            var manifestFiles = new List<string>();
            Console.WriteLine("Start Refresh Mods..");
            ModTool.FindManifestFile(ModTool.ModsDir, manifestFiles);
            for (int i = 0; i < manifestFiles.Count; i++)
            {
                var mod = new ModItemView(manifestFiles[i], i);
                mods.Add(mod);
            }

        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }

        //refresh
        modAdapter.RefreshMods();
        var foundModsText = FindViewById<TextView>(Resource.Id.foundModsText);
        foundModsText.Text = "Found Mods: " + mods.Count;
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
                OnClickDeleteMod(mod);
            }
        );
    }
    void OnClickDeleteMod(ModItemView mod)
    {
        Console.WriteLine("try delete mod: " + mod.modName);
        if (ModInstaller.TryDeleteMod(mod.modFolderPath, true))
        {
            ToastNotifyTool.Notify("Done delete mod: " + mod.modName);
            RefreshMods();
        }
    }
}

