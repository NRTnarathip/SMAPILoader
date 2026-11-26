using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.FloatingActionButton;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher.Pages;


internal sealed class ModManagerPage : LinearLayout
{
    const string HeaderBackgroundColorHex = "#1A1D24";
    const string SecondaryTextColorHex = "#9AA0A6";
    const string SortButtonBackgroundHex = "#262A33";

    const int MenuToggleMod = 1;
    const int MenuOpenFolder = 2;
    const int MenuShowManifest = 3;
    const int MenuDeleteMod = 4;

    const int MenuSortDate = 11;
    const int MenuSortName = 12;
    const int MenuSortSize = 13;

    private enum SortMode { Date, Name, Size }

    private readonly Context _context;
    private readonly List<ModInfo> _mods = new();
    private readonly ModAdapter _adapter;
    private readonly TextView _emptyText;
    private readonly TextView _foundModsText;
    private readonly TextView _sortButton;
    private readonly RecyclerView _list;
    private SortMode _sortMode = SortMode.Date;

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
        headerBox.SetBackgroundColor(Color.ParseColor(HeaderBackgroundColorHex));
        headerBox.Elevation = Ui.Dp(context, 6);
        headerBox.SetPadding(Ui.Dp(context, 20), Ui.Dp(context, 48), Ui.Dp(context, 20), Ui.Dp(context, 16));

        var titleRow = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        titleRow.SetGravity(GravityFlags.CenterVertical);

        var header = new TextView(context) { Text = "Mod Manager" };
        header.SetTextSize(ComplexUnitType.Sp, 22);
        header.SetTextColor(Color.White);
        header.SetTypeface(null, TypefaceStyle.Bold);

        _sortButton = CreateSortButton(context);

        titleRow.AddView(header, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f));
        titleRow.AddView(_sortButton);
        headerBox.AddView(titleRow);

        _foundModsText = new TextView(context) { Text = "...." };
        _foundModsText.SetTextSize(ComplexUnitType.Sp, 14);
        _foundModsText.SetTextColor(Color.ParseColor(SecondaryTextColorHex));
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

        _adapter = new ModAdapter(context, _mods, OnClickedModItem);
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

    private TextView CreateSortButton(Context context)
    {
        var button = new TextView(context)
        {
            Gravity = GravityFlags.Center,
            Clickable = true,
            Focusable = true
        };
        button.SetTextSize(ComplexUnitType.Sp, 13);
        button.SetTextColor(Color.White);
        button.SetPadding(
            Ui.Dp(context, 14), Ui.Dp(context, 8),
            Ui.Dp(context, 14), Ui.Dp(context, 8));
        button.Background = Ui.CreateRippleDrawable(context, SortButtonBackgroundHex, 16);
        button.Click += SortButton_Click;
        return button;
    }

    void SortButton_Click(object sender, EventArgs e)
    {
        var popup = new Android.Widget.PopupMenu(_context, _sortButton);
        popup.Menu!.Add(0, MenuSortDate, 0, "Date");
        popup.Menu.Add(0, MenuSortName, 1, "Name");
        popup.Menu.Add(0, MenuSortSize, 2, "Size");
        popup.Menu.SetGroupCheckable(0, true, true);
        popup.Menu.FindItem(CurrentSortMenuItemId)?.SetChecked(true);

        popup.MenuItemClick += (s, args) =>
        {
            switch (args.Item!.ItemId)
            {
                case MenuSortDate: _sortMode = SortMode.Date; break;
                case MenuSortName: _sortMode = SortMode.Name; break;
                case MenuSortSize: _sortMode = SortMode.Size; break;
                default: return;
            }
            ApplySort();
        };

        popup.Show();
    }

    int CurrentSortMenuItemId => _sortMode switch
    {
        SortMode.Name => MenuSortName,
        SortMode.Size => MenuSortSize,
        _ => MenuSortDate,
    };

    void ApplySort()
    {
        Comparison<ModInfo> comparison = _sortMode switch
        {
            SortMode.Name => (a, b) => string.Compare(
                a.NameUIText, b.NameUIText, StringComparison.OrdinalIgnoreCase),
            SortMode.Size => (a, b) => b.SizeBytes.CompareTo(a.SizeBytes),
            _ => (a, b) => b.LastModified.CompareTo(a.LastModified),
        };

        _mods.Sort(comparison);
        _adapter.NotifyDataSetChanged();
        _sortButton.Text = $"Sort: {_sortMode} ▾";
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
                var mod = new ModInfo(manifestFiles[i]);
                _mods.Add(mod);
            }
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }

        //refresh
        ApplySort();
        RecomputeModSizes();

        bool empty = _mods.Count == 0;
        _emptyText.Visibility = empty ? ViewStates.Visible : ViewStates.Gone;
        _list.Visibility = empty ? ViewStates.Gone : ViewStates.Visible;

        RefreshTitleHeader();
    }

    /// <summary>
    /// คำนวณขนาดโฟลเดอร์ของทุก mod ใน background thread (ใช้ตอน sort by size)
    /// แล้ว sort ใหม่บน main thread เมื่อเสร็จ
    /// </summary>
    void RecomputeModSizes()
    {
        if (_mods.Count == 0)
            return;

        var snapshot = _mods.ToArray();
        Task.Run(() =>
        {
            foreach (var mod in snapshot)
                mod.ComputeSize();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_sortMode == SortMode.Size)
                    ApplySort();
            });
        });
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
        popup.Menu!.Add(0, MenuToggleMod, 0, mod.Enabled ? "Toggle Off" : "Toggle On");
        popup.Menu.Add(0, MenuOpenFolder, 1, "Open Folder");
        popup.Menu.Add(0, MenuShowManifest, 2, "Show manifest.json");
        popup.Menu.Add(0, MenuDeleteMod, 3, "Delete");

        popup.MenuItemClick += (s, e) =>
        {
            switch (e.Item!.ItemId)
            {
                case MenuToggleMod: ToggleMod(mod); break;
                case MenuOpenFolder: OpenModFolder(mod); break;
                case MenuShowManifest: ShowManifest(mod); break;
                case MenuDeleteMod: OnClickDeleteMod(mod); break;
            }
        };

        popup.Show();
    }

    private void ToggleMod(ModInfo mod)
    {
        mod.Enabled = !mod.Enabled;

        int position = _mods.IndexOf(mod);
        if (position >= 0)
            _adapter.NotifyItemChanged(position);

        RefreshTitleHeader();
    }

    private void ShowManifest(ModInfo mod)
    {
        string manifestText;
        try
        {
            manifestText = File.ReadAllText(mod.ManifestPath);
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex, "Failed to read manifest.json");
            return;
        }

        var textView = new TextView(_context);
        textView.SetTextSize(ComplexUnitType.Sp, 12);
        textView.SetTextColor(Color.White);
        textView.SetTypeface(Typeface.Monospace, TypefaceStyle.Normal);
        textView.SetTextIsSelectable(true);
        textView.Text = manifestText;

        int padding = Ui.Dp(_context, 16);
        textView.SetPadding(padding, padding, padding, padding);

        var scroll = new ScrollView(_context);
        scroll.AddView(textView);

        new AlertDialog.Builder(_context)
            .SetTitle("manifest.json")
            .SetView(scroll)
            .SetPositiveButton("Close", (s, e) => { })
            .Show();
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
}
