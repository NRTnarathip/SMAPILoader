using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Dialog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Launcher.Pages;


internal sealed class ModManagerPage : LinearLayout
{
    const string SortModePrefKey = "mod_manager_sort_mode";
    const string SortDescendingPrefKey = "mod_manager_sort_descending";

    const int MenuSortDate = 11;
    const int MenuSortName = 12;
    const int MenuSortSize = 13;

    private enum SortMode { Date, Name, Size }

    private readonly Context _context;
    private readonly List<ModInfo> _mods = new();
    private readonly ModAdapter _adapter;
    private readonly LinearLayout _emptyView;
    private readonly TextView _sortButton;
    private readonly ImageButton _installButton;
    private readonly RecyclerView _list;
    private SortMode _sortMode = SortMode.Date;
    private bool _sortDescending = true;

    public ModManagerPage(Context context) : base(context)
    {
        _context = context;
        _sortMode = LoadSortMode();
        _sortDescending = Preferences.Get(SortDescendingPrefKey, true);
        Orientation = Orientation.Vertical;
        SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.BackgroundHex));
        LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        // ===== Header (ใช้ PageHeader ร่วมกับ HomePage เพื่อให้ title ตรงกัน) =====
        var pageHeader = new PageHeader(context, "Mod Manager");
        _installButton = CreateInstallButton(context);
        _sortButton = CreateSortButton(context);
        pageHeader.Actions.AddView(_installButton);
        pageHeader.Actions.AddView(_sortButton);

        AddView(pageHeader, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

        // ---- Body: list + empty state ซ้อนกันใน FrameLayout ----
        var body = new FrameLayout(context);
        AddView(body, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 0, 1f));

        _adapter = new ModAdapter(context, _mods, OnClickedModItem);
        _list = new RecyclerView(context);
        _list.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.BackgroundHex));
        _list.SetPadding(0, UI.Dp(context, 12), 0, UI.Dp(context, 16));
        _list.SetClipToPadding(false);
        _list.SetLayoutManager(new LinearLayoutManager(context));
        _list.SetAdapter(_adapter);

        // ปิด change animation เพื่อให้ NotifyItemChanged (toggle on/off) อัปเดตสีทันที
        if (_list.GetItemAnimator() is SimpleItemAnimator animator)
            animator.SupportsChangeAnimations = false;
        body.AddView(_list, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));

        var emptyView = new LinearLayout(context) { Orientation = Orientation.Vertical };
        emptyView.SetGravity(GravityFlags.Center);
        _emptyView = emptyView;

        var emptyText = new TextView(context) { Text = "No mods found." };
        emptyText.SetTextSize(ComplexUnitType.Sp, 16);
        emptyText.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        emptyText.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        emptyText.Gravity = GravityFlags.Center;
        emptyView.AddView(emptyText);

        var emptyHint = new TextView(context) { Text = "Tap + to install your first mod." };
        emptyHint.SetTextSize(ComplexUnitType.Sp, 13);
        emptyHint.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        emptyHint.Gravity = GravityFlags.Center;
        emptyHint.SetPadding(0, UI.Dp(context, 6), 0, 0);
        emptyView.AddView(emptyHint);

        body.AddView(emptyView, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));

        RefreshMods();
    }

    private ImageButton CreateInstallButton(Context context)
    {
        var button = new ImageButton(context);
        button.SetImageResource(Android.Resource.Drawable.IcInputAdd);
        button.SetColorFilter(LauncherTheme.Parse(LauncherTheme.AccentHex));
        button.Background = UI.CreateRippleDrawable(
            context, LauncherTheme.SurfaceVariantHex, 20);
        button.ContentDescription = "Install mod";
        button.SetPadding(
            UI.Dp(context, 10), UI.Dp(context, 10),
            UI.Dp(context, 10), UI.Dp(context, 10));

        int size = UI.Dp(context, 40);
        button.LayoutParameters = new LinearLayout.LayoutParams(size, size)
        { RightMargin = UI.Dp(context, 8) };
        button.Click += InstallButton_Click;
        return button;
    }

    private TextView CreateSortButton(Context context)
    {
        var button = new TextView(context)
        {
            Gravity = GravityFlags.Center,
            Clickable = true,
            Focusable = true
        };
        button.SetTextSize(ComplexUnitType.Sp, 12);
        button.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        button.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        button.SetPadding(
            UI.Dp(context, 12), UI.Dp(context, 9),
            UI.Dp(context, 12), UI.Dp(context, 9));
        button.Background = UI.CreateRippleDrawable(
            context, LauncherTheme.SurfaceVariantHex, LauncherTheme.ChipRadiusDp);
        button.Click += SortButton_Click;
        return button;
    }

    void SortButton_Click(object sender, EventArgs e)
    {
        var popup = new Android.Widget.PopupMenu(_context, _sortButton);
        popup.Menu!.Add(0, MenuSortDate, 0, SortMenuTitle(SortMode.Date));
        popup.Menu.Add(0, MenuSortName, 1, SortMenuTitle(SortMode.Name));
        popup.Menu.Add(0, MenuSortSize, 2, SortMenuTitle(SortMode.Size));
        popup.Menu.SetGroupCheckable(0, true, true);
        popup.Menu.FindItem(CurrentSortMenuItemId)?.SetChecked(true);

        popup.MenuItemClick += (s, args) =>
        {
            SortMode selected;
            switch (args.Item!.ItemId)
            {
                case MenuSortDate: selected = SortMode.Date; break;
                case MenuSortName: selected = SortMode.Name; break;
                case MenuSortSize: selected = SortMode.Size; break;
                default: return;
            }

            if (selected == _sortMode)
            {
                // กดโหมดเดิมซ้ำ = สลับทิศทาง
                _sortDescending = !_sortDescending;
            }
            else
            {
                _sortMode = selected;
                _sortDescending = DefaultDescending(selected);
            }

            SaveSortState();
            ApplySort();
        };

        popup.Show();
    }

    // ค่าเริ่มต้นของแต่ละโหมด: Date/Size ใหม่สุด-ใหญ่สุดก่อน, Name ก่อนตามตัวอักษร
    static bool DefaultDescending(SortMode mode) => mode != SortMode.Name;

    string SortMenuTitle(SortMode mode) =>
        mode == _sortMode ? $"{mode} {(_sortDescending ? "↓" : "↑")}" : mode.ToString();

    static SortMode LoadSortMode()
    {
        int stored = Preferences.Get(SortModePrefKey, (int)SortMode.Date);
        return Enum.IsDefined(typeof(SortMode), stored) ? (SortMode)stored : SortMode.Date;
    }

    void SaveSortState()
    {
        Preferences.Set(SortModePrefKey, (int)_sortMode);
        Preferences.Set(SortDescendingPrefKey, _sortDescending);
    }

    int CurrentSortMenuItemId => _sortMode switch
    {
        SortMode.Name => MenuSortName,
        SortMode.Size => MenuSortSize,
        _ => MenuSortDate,
    };

    void ApplySort()
    {
        Comparison<ModInfo> ascending = _sortMode switch
        {
            SortMode.Name => (a, b) => string.Compare(
                a.NameUIText, b.NameUIText, StringComparison.OrdinalIgnoreCase),
            SortMode.Size => (a, b) => a.SizeBytes.CompareTo(b.SizeBytes),
            _ => (a, b) => a.LastModified.CompareTo(b.LastModified),
        };

        _mods.Sort((a, b) => _sortDescending ? ascending(b, a) : ascending(a, b));
        _adapter.NotifyDataSetChanged();
        _sortButton.Text = $"Sort: {_sortMode} {(_sortDescending ? "↓" : "↑")}";
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
        _emptyView.Visibility = empty ? ViewStates.Visible : ViewStates.Gone;
        _list.Visibility = empty ? ViewStates.Gone : ViewStates.Visible;
    }

    /// <summary>
    /// คำนวณขนาดโฟลเดอร์ของทุก mod ใน background thread (ใช้แสดงผลและ sort by size)
    /// แล้ว sort + rebind ใหม่บน main thread เมื่อเสร็จ
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

            MainThread.BeginInvokeOnMainThread(ApplySort);
        });
    }

    private void OnClickedModItem(ModInfo mod, View anchor) => ShowModActions(mod, anchor);

    /// <summary>
    /// Popup แบบ anchored: โผล่ใต้กล่อง mod ที่กด ชิดขอบซ้าย
    /// ถ้าด้านล่างไม่พอจะพลิกขึ้นด้านบนของ item แทน (ไม่ให้ล้นขอบจอ)
    /// </summary>
    private void ShowModActions(ModInfo mod, View anchor)
    {
        var content = new LinearLayout(_context) { Orientation = Orientation.Vertical };
        content.SetPadding(0, UI.Dp(_context, 6), 0, UI.Dp(_context, 6));

        var popup = new PopupWindow(
            content, UI.Dp(_context, 224), ViewGroup.LayoutParams.WrapContent, true);
        popup.SetBackgroundDrawable(LauncherTheme.CreateSurfaceDrawable(
            _context, LauncherTheme.CardBackgroundHex,
            LauncherTheme.ChipRadiusDp + 2, LauncherTheme.DividerHex));
        popup.Elevation = UI.Dp(_context, 8);
        popup.OutsideTouchable = true;

        AddActionRow(content, popup, Resource.Drawable.ic_power,
            mod.Enabled ? "Toggle Off" : "Toggle On", null, () => ToggleMod(mod));
        AddActionRow(content, popup, Resource.Drawable.ic_folder,
            "Open Folder", null, () => OpenModFolder(mod));
        AddActionRow(content, popup, Resource.Drawable.ic_code,
            "Show manifest.json", null, () => ShowManifest(mod));

        var divider = new View(_context);
        divider.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.DividerHex));
        content.AddView(divider, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, UI.Dp(_context, 1))
        {
            LeftMargin = UI.Dp(_context, 12),
            RightMargin = UI.Dp(_context, 12),
            TopMargin = UI.Dp(_context, 4),
            BottomMargin = UI.Dp(_context, 4)
        });

        AddActionRow(content, popup, Resource.Drawable.ic_delete,
            "Delete", LauncherTheme.StatusErrorTextHex, () => OnClickDeleteMod(mod));

        // วัดความสูงจริงก่อน show เพื่อเลือกทิศวางได้แน่นอน
        // (PopupWindow ที่ปล่อยเป็น WrapContent จะคำนวณตำแหน่งตอน layout แล้วล้นจอได้)
        int popupWidth = UI.Dp(_context, 224);
        content.Measure(
            MeasureSpec.MakeMeasureSpec(popupWidth, MeasureSpecMode.AtMost),
            MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified));
        popup.Height = content.MeasuredHeight;

        int gap = UI.Dp(_context, 4);
        var location = new int[2];
        anchor.GetLocationInWindow(location);
        int spaceBelow = anchor.RootView.Height - (location[1] + anchor.Height) - gap;
        int offsetY = popup.Height <= spaceBelow
            ? gap
            : -(anchor.Height + popup.Height + gap);

        SetModHighlight(anchor, true);
        popup.DismissEvent += (s, e) => SetModHighlight(anchor, false);
        popup.ShowAsDropDown(anchor, 0, offsetY);
    }

    /// <summary>ไฮไลต์การ์ด mod ที่กำลังเปิด popup อยู่ (พื้นเขียวเข้ม)</summary>
    private void SetModHighlight(View view, bool on)
    {
        if (view is not AndroidX.CardView.Widget.CardView card)
            return;

        card.SetCardBackgroundColor(LauncherTheme.Parse(on
            ? LauncherTheme.SelectedCardBackgroundHex
            : LauncherTheme.CardBackgroundHex));
    }

    private void AddActionRow(LinearLayout parent, PopupWindow popup, int iconRes,
                              string label, string tintHex, Action action)
    {
        var row = new LinearLayout(_context)
        {
            Orientation = Orientation.Horizontal,
            Clickable = true,
            Focusable = true
        };
        row.SetGravity(GravityFlags.CenterVertical);
        row.SetMinimumHeight(UI.Dp(_context, 46));
        row.SetPadding(UI.Dp(_context, 16), 0, UI.Dp(_context, 16), 0);
        row.Background = UI.CreateRippleDrawable(_context, null, LauncherTheme.ChipRadiusDp);

        var rowParams = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        {
            LeftMargin = UI.Dp(_context, 4),
            RightMargin = UI.Dp(_context, 4)
        };
        parent.AddView(row, rowParams);

        var color = LauncherTheme.Parse(tintHex ?? LauncherTheme.TextPrimaryHex);

        var icon = new ImageView(_context);
        icon.SetImageResource(iconRes);
        icon.SetColorFilter(color);
        icon.LayoutParameters = new LinearLayout.LayoutParams(
            UI.Dp(_context, 20), UI.Dp(_context, 20));
        row.AddView(icon);

        var text = new TextView(_context) { Text = label };
        text.SetTextSize(ComplexUnitType.Sp, 14);
        text.SetTextColor(color);
        text.SetTypeface(LauncherTheme.BodyTypeface, TypefaceStyle.Normal);
        text.LayoutParameters = new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f)
        { LeftMargin = UI.Dp(_context, 14) };
        row.AddView(text);

        row.Click += (s, e) =>
        {
            popup.Dismiss();
            action();
        };
    }

    private void ToggleMod(ModInfo mod)
    {
        mod.Enabled = !mod.Enabled;

        int position = _mods.IndexOf(mod);
        if (position >= 0)
            _adapter.NotifyItemChanged(position);
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

        int padding = UI.Dp(_context, 16);
        textView.SetPadding(padding, padding, padding, padding);

        var scroll = new ScrollView(_context);
        scroll.AddView(textView);

        new MaterialAlertDialogBuilder(_context)
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
        new MaterialAlertDialogBuilder(_context)
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

    private void InstallButton_Click(object sender, System.EventArgs e)
    {
        ModInstaller.OnClickInstallMod(() =>
        {
            RefreshMods();
        });
    }
}
