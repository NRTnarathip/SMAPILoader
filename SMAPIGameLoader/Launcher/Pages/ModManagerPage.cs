using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.FloatingActionButton;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Path = System.IO.Path;

namespace SMAPIGameLoader.Launcher.Pages;


internal sealed class ModManagerPage : LinearLayout
{

    private readonly Context _context;
    private readonly List<ModInfo> _mods = new();
    private readonly ModAdapter _adapter;
    private readonly TextView _emptyText;
    private readonly TextView _foundModsText;
    private readonly RecyclerView _list;

    public FloatingActionButton InstallFab { get; }

    public ModManagerPage(Context context) : base(context)
    {
        _context = context;
        Orientation = Orientation.Vertical;
        LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        // ===== Header =====
        var headerBox = new LinearLayout(context) { Orientation = Orientation.Vertical };
        headerBox.SetBackgroundColor(Color.ParseColor("#1A1D24"));
        headerBox.Elevation = Ui.Dp(context, 6);
        headerBox.SetPadding(Ui.Dp(context, 20), Ui.Dp(context, 48), Ui.Dp(context, 20), Ui.Dp(context, 16));

        var header = new TextView(context) { Text = "Mod Manager" };
        header.SetTextSize(ComplexUnitType.Sp, 22);
        header.SetTextColor(Color.White);
        header.SetTypeface(null, TypefaceStyle.Bold);
        headerBox.AddView(header);

        _foundModsText = new TextView(context) { Text = "...." };
        _foundModsText.SetTextSize(ComplexUnitType.Sp, 14);
        _foundModsText.SetTextColor(Color.ParseColor("#9AA0A6"));
        headerBox.AddView(_foundModsText, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        { TopMargin = Ui.Dp(context, 4) });

        AddView(headerBox, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

        // ---- Body: list + empty state + FAB ซ้อนกันใน FrameLayout ----
        var body = new FrameLayout(context);
        AddView(body, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 0, 1f));

        _adapter = new ModAdapter(context, _mods, OnToggledModItem, OnClickedModItem);
        _list = new RecyclerView(context);
        _list.SetLayoutManager(new LinearLayoutManager(context));
        _list.SetAdapter(_adapter);
        body.AddView(_list, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));

        _emptyText = new TextView(context)
        {
            Text = "No mods found.",
            Gravity = GravityFlags.Center
        };
        _emptyText.SetTextSize(ComplexUnitType.Sp, 16);
        body.AddView(_emptyText, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));

        InstallFab = new FloatingActionButton(context);
        InstallFab.SetImageResource(Android.Resource.Drawable.IcInputAdd);
        var fabLp = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = GravityFlags.End | GravityFlags.Bottom
        };
        InstallFab.Click += InstallFab_Click;
        fabLp.SetMargins(
            Ui.Dp(context, 16), Ui.Dp(context, 16),
            Ui.Dp(context, 16), Ui.Dp(context, 16));
        body.AddView(InstallFab, fabLp);

        RefreshMods();
    }

    void RefreshMods()
    {
        //clear first
        _mods.Clear();

        try
        {
            var manifestFiles = new List<string>();
            Console.WriteLine("Start Refresh Mods..");
            ModTool.FindManifestFile(ModTool.ModsDir, manifestFiles);
            for (int i = 0; i < manifestFiles.Count; i++)
            {
                var mod = new ModInfo(manifestFiles[i], i);
                _mods.Add(mod);
            }
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }

        //refresh
        _adapter.NotifyDataSetChanged();

        bool empty = _mods.Count == 0;
        _emptyText.Visibility = empty ? ViewStates.Visible : ViewStates.Gone;
        _list.Visibility = empty ? ViewStates.Gone : ViewStates.Visible;

        RefreshTitleHeader();
    }

    void RefreshTitleHeader()
    {
        var sb = new SpannableStringBuilder();
        sb.Append($"Total Mods: {_mods.Count}");
        sb.Append("\n");
        sb.Append($"Enabled Mods: {EnabledModsCount}");
        sb.Append("\n");
        sb.Append($"Disabled Mods: {DisabldModsCount}");
        _foundModsText.SetText(sb, TextView.BufferType.Spannable);
    }


    int EnabledModsCount => _mods.Count(m => m.Enabled);
    int DisabldModsCount => _mods.Count(m => !m.Enabled);

    private void OnClickedModItem(ModInfo mod, View anchor)
    {
        var popup = new Android.Widget.PopupMenu(_context, anchor);
        popup.Menu!.Add(0, 1, 0, "Open Folder");
        popup.Menu.Add(0, 2, 1, "Delete");

        popup.MenuItemClick += (s, e) =>
        {
            switch (e.Item!.ItemId)
            {
                case 1: OpenModFolder(mod); break;
                case 2: OnClickDeleteMod(mod); break;
            }
        };

        popup.Show();
    }

    private void OpenModFolder(ModInfo mod)
    {
        try
        {
            FileTool.OpenAppFiles(mod.ModFolderPath);
        }
        catch (Exception)
        {
            Toast.MakeText(_context, "Failed to open mod folder", ToastLength.Short)!.Show();
        }
    }

    private void OnClickDeleteMod(ModInfo mod)
    {
        new AlertDialog.Builder(_context)
            .SetTitle("Delete Mod!")
            .SetMessage($"Do you want delete \"{mod.NameUIText}\"?")
            .SetPositiveButton("Yes", (s, e) =>
            {
                if (ModInstaller.TryDeleteMod(mod.ModFolderPath))
                {
                    RefreshMods();
                }
            })
            .SetNegativeButton("No", (s, e) => { })
            .Show();
    }

    private void InstallFab_Click(object sender, System.EventArgs e)
    {
        ModInstaller.OnClickInstallMod(() =>
        {
            RefreshMods();
        });
    }

    private void OnToggledModItem(ModInfo mod)
    {
        RefreshTitleHeader();
    }
}
