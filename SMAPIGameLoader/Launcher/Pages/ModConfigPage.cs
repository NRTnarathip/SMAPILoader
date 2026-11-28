using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Dialog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Path = System.IO.Path;

namespace SMAPIGameLoader.Launcher.Pages;

/// <summary>
/// หน้าจัดการ JSON ในโฟลเดอร์ mod: list ไฟล์ → editor (ฟอร์ม หรือ Raw) → save กลับไฟล์เดิม
/// ใช้เป็น sub-page ของแท็บ Mods (ดู LauncherActivity.ShowModConfig)
/// </summary>
internal sealed class ModConfigPage : LinearLayout
{
    private readonly Context _context;
    private readonly PageHeader _header;
    private readonly FrameLayout _content;

    private ModInfo _mod;
    private string _modFolder;
    private string _filePath;
    private JToken _root;
    private JsonTreeEditor _tree;
    private EditText _raw;
    private bool _rawMode;
    private bool _editorOpen;
    private bool _dirty;
    // เปิดไฟล์ตรง ๆ ด้วย path (ไม่ผ่าน list) — ใช้โดย McpDebugReceiver
    private bool _direct;

    public ModConfigPage(Context context) : base(context)
    {
        _context = context;
        Orientation = Orientation.Vertical;
        SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.BackgroundHex));
        LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        _header = new PageHeader(context, "", OnHeaderBack);
        AddView(_header, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        _content = new FrameLayout(context);
        AddView(_content, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 0, 1f));
    }

    public void Open(ModInfo mod)
    {
        _mod = mod;
        _modFolder = mod.ModFolderPath;
        _direct = false;
        ShowList();
    }

    /// <summary>
    /// เปิด editor ของไฟล์ JSON ตาม absolute path ตรง ๆ (ข้าม list)
    /// คืน false ถ้าอ่าน/parse ไม่ได้ (มี dialog แจ้งแล้ว)
    /// </summary>
    public bool OpenFile(string filePath)
    {
        _mod = null;
        _modFolder = Path.GetDirectoryName(filePath);
        _direct = true;
        _editorOpen = false;
        _dirty = false;
        return OpenEditor(filePath);
    }

    /// <summary>true = จบในหน้านี้แล้ว, false = อยู่ในโหมด list (ให้ activity กลับไปแท็บ Mods)</summary>
    public bool HandleBack()
    {
        if (!_editorOpen)
            return false;

        if (_dirty)
        {
            ConfirmDiscard(ExitEditor);
            return true;
        }

        ExitEditor();
        return true;
    }

    void ExitEditor()
    {
        if (_direct)
            ExitToMods();
        else
            ShowList();
    }

    void OnHeaderBack()
    {
        if (_editorOpen)
        {
            HandleBack();
            return;
        }

        ExitToMods();
    }

    void ExitToMods()
    {
        if (_context is LauncherActivity launcher)
            launcher.NavigateTo(LauncherActivity.ModManagerId);
    }

    // ===== โหมด list ไฟล์ =====

    void ShowList()
    {
        _editorOpen = false;
        _dirty = false;
        _filePath = null;
        _root = null;
        _tree = null;
        _raw = null;

        _header.Title.Text = "Config · " + _mod.NameUIText;
        _header.Actions.RemoveAllViews();

        var body = CreateBody(out var scroll);
        _content.RemoveAllViews();
        _content.AddView(scroll, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

        var files = GetJsonFiles();
        if (files.Count == 0)
        {
            body.AddView(CreateText("No JSON files found.", 15, LauncherTheme.TextPrimaryHex));
            return;
        }

        foreach (var file in files)
        {
            body.AddView(CreateFileRow(file), new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
            { BottomMargin = UI.Dp(_context, 10) });
        }
    }

    List<string> GetJsonFiles()
    {
        try
        {
            return Directory.EnumerateFiles(_modFolder, "*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .OrderBy(Rank)
                .ThenBy(RelativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex, "Failed to list JSON files");
            return new List<string>();
        }
    }

    int Rank(string path)
    {
        string relative = RelativePath(path);
        if (relative.Equals("config.json", StringComparison.OrdinalIgnoreCase))
            return 0;
        if (relative.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
            return 2;
        return 1;
    }

    string RelativePath(string path) =>
        Path.GetRelativePath(_modFolder, path).Replace('\\', '/');

    View CreateFileRow(string path)
    {
        var row = new LinearLayout(_context)
        {
            Orientation = Orientation.Horizontal,
            Clickable = true,
            Focusable = true
        };
        row.SetGravity(GravityFlags.CenterVertical);
        row.SetMinimumHeight(UI.Dp(_context, 56));
        row.SetPadding(
            UI.Dp(_context, 14), UI.Dp(_context, 12),
            UI.Dp(_context, 14), UI.Dp(_context, 12));
        row.Background = UI.CreateRippleDrawable(
            _context, LauncherTheme.CardBackgroundHex, LauncherTheme.CardRadiusDp);

        var icon = new ImageView(_context);
        icon.SetImageResource(Resource.Drawable.ic_code);
        icon.SetColorFilter(LauncherTheme.Parse(LauncherTheme.AccentHex));
        row.AddView(icon, new LinearLayout.LayoutParams(
            UI.Dp(_context, 20), UI.Dp(_context, 20)));

        var column = new LinearLayout(_context) { Orientation = Orientation.Vertical };
        column.LayoutParameters = new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f)
        { LeftMargin = UI.Dp(_context, 14) };

        var name = new TextView(_context) { Text = Path.GetFileName(path) };
        name.SetTextSize(ComplexUnitType.Sp, 15);
        name.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        name.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        name.SetSingleLine(true);
        name.Ellipsize = TextUtils.TruncateAt.End;

        var detail = new TextView(_context)
        {
            Text = $"{RelativePath(path)} · {SizeText(path)}"
        };
        detail.SetTextSize(ComplexUnitType.Sp, 12);
        detail.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        detail.SetSingleLine(true);
        detail.Ellipsize = TextUtils.TruncateAt.End;
        detail.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        { TopMargin = UI.Dp(_context, 3) };

        column.AddView(name);
        column.AddView(detail);
        row.AddView(column);

        row.Click += (s, e) => OpenEditor(path);
        return row;
    }

    static string SizeText(string path)
    {
        try
        {
            long bytes = new FileInfo(path).Length;
            if (bytes < 1024)
                return $"{bytes} B";

            double kb = bytes / 1024d;
            return kb < 1024d ? $"{kb:0.#} KB" : $"{kb / 1024d:0.0} MB";
        }
        catch (Exception)
        {
            return "—";
        }
    }

    // ===== โหมด editor =====

    bool OpenEditor(string path)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex, "Failed to read " + Path.GetFileName(path));
            return false;
        }

        JToken root;
        try
        {
            root = JToken.Parse(text);
        }
        catch (Exception ex)
        {
            ShowError("Invalid JSON", ex.Message);
            return false;
        }

        _filePath = path;
        _root = root;
        _editorOpen = true;
        _rawMode = false;
        _dirty = false;
        BuildEditor();
        return true;
    }

    void BuildEditor()
    {
        _header.Title.Text = RelativePath(_filePath);
        _header.Actions.RemoveAllViews();
        _header.Actions.AddView(CreatePill(_rawMode ? "Form" : "Raw", ToggleMode),
            new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
            { RightMargin = UI.Dp(_context, 8) });
        _header.Actions.AddView(CreatePill("Save", Save));

        var body = CreateBody(out var scroll);
        _content.RemoveAllViews();
        _content.AddView(scroll, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

        if (_rawMode)
        {
            AddRawEditor(body);
            return;
        }

        if (_root is JObject or JArray)
        {
            var card = new LinearLayout(_context) { Orientation = Orientation.Vertical };
            card.SetPadding(
                UI.Dp(_context, 14), UI.Dp(_context, 6),
                UI.Dp(_context, 14), UI.Dp(_context, 10));
            card.Background = LauncherTheme.CreateSurfaceDrawable(
                _context, LauncherTheme.CardBackgroundHex, LauncherTheme.CardRadiusDp);

            _tree = new JsonTreeEditor(_context, _root);
            _tree.Changed += () => _dirty = true;
            card.AddView(_tree, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

            body.AddView(card, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
            return;
        }

        body.AddView(CreateText(
            "Root value is not an object or array. Switch to Raw mode to edit it.",
            13, LauncherTheme.TextSecondaryHex));
    }

    void AddRawEditor(LinearLayout body)
    {
        _raw = new EditText(_context)
        {
            Background = LauncherTheme.CreateSurfaceDrawable(
                _context, LauncherTheme.SurfaceVariantHex, LauncherTheme.CardRadiusDp)
        };
        _raw.SetSingleLine(false);
        _raw.SetTextSize(ComplexUnitType.Sp, 12);
        _raw.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        _raw.SetTypeface(Typeface.Monospace, TypefaceStyle.Normal);
        _raw.SetPadding(
            UI.Dp(_context, 14), UI.Dp(_context, 12),
            UI.Dp(_context, 14), UI.Dp(_context, 12));
        _raw.InputType = InputTypes.ClassText
            | InputTypes.TextFlagMultiLine | InputTypes.TextFlagNoSuggestions;
        _raw.Text = _root.ToString(Formatting.Indented);
        _raw.TextChanged += (s, e) => _dirty = true;

        body.AddView(_raw, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
    }

    void ToggleMode()
    {
        if (!_rawMode)
        {
            if (_tree is not null && !_tree.TryValidateNumbers(out string error))
            {
                ShowError("Invalid number", error);
                return;
            }

            _rawMode = true;
        }
        else
        {
            JToken parsed;
            try
            {
                parsed = JToken.Parse(_raw.Text ?? "");
            }
            catch (Exception ex)
            {
                ShowError("Invalid JSON", ex.Message);
                return;
            }

            _root = parsed;
            _rawMode = false;
        }

        BuildEditor();
    }

    void Save()
    {
        string text;
        if (_rawMode)
        {
            try
            {
                JToken.Parse(_raw.Text ?? "");
            }
            catch (Exception ex)
            {
                ShowError("Invalid JSON", ex.Message);
                return;
            }

            text = _raw.Text ?? "";
        }
        else
        {
            if (_tree is not null && !_tree.TryValidateNumbers(out string error))
            {
                ShowError("Invalid number", error);
                return;
            }

            text = _root.ToString(Formatting.Indented);
        }

        var tempPath = _filePath + ".tmp";
        try
        {
            File.WriteAllText(tempPath, text);
            File.Move(tempPath, _filePath, true);
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex, "Failed to save " + Path.GetFileName(_filePath));
            return;
        }

        _dirty = false;
        Toast.MakeText(_context, "Saved", ToastLength.Short).Show();
    }

    void ShowError(string title, string message)
    {
        new MaterialAlertDialogBuilder(_context)
            .SetTitle(title)
            .SetMessage(message)
            .SetPositiveButton("OK", (s, e) => { })
            .Show();
    }

    void ConfirmDiscard(Action onDiscard)
    {
        new MaterialAlertDialogBuilder(_context)
            .SetTitle("Discard changes?")
            .SetMessage("This file has unsaved changes.")
            .SetPositiveButton("Discard", (s, e) => onDiscard())
            .SetNegativeButton("Cancel", (s, e) => { })
            .Show();
    }

    // ===== helpers =====

    LinearLayout CreateBody(out ScrollView scroll)
    {
        scroll = new ScrollView(_context);
        var body = new LinearLayout(_context) { Orientation = Orientation.Vertical };
        body.SetPadding(
            UI.Dp(_context, 16), UI.Dp(_context, 16),
            UI.Dp(_context, 16), UI.Dp(_context, 24));
        scroll.AddView(body, new ScrollView.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
        return body;
    }

    TextView CreateText(string text, float sizeSp, string colorHex)
    {
        var view = new TextView(_context) { Text = text };
        view.SetTextSize(ComplexUnitType.Sp, sizeSp);
        view.SetTextColor(LauncherTheme.Parse(colorHex));
        view.SetTypeface(LauncherTheme.BodyTypeface, TypefaceStyle.Normal);
        view.SetLineSpacing(0, 1.2f);
        return view;
    }

    TextView CreatePill(string text, Action onClick)
    {
        var button = new TextView(_context)
        {
            Text = text,
            Gravity = GravityFlags.Center,
            Clickable = true,
            Focusable = true
        };
        button.SetTextSize(ComplexUnitType.Sp, 12);
        button.SetTextColor(LauncherTheme.Parse(LauncherTheme.HeaderTextHex));
        button.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        button.SetPadding(
            UI.Dp(_context, 12), UI.Dp(_context, 9),
            UI.Dp(_context, 12), UI.Dp(_context, 9));
        button.Background = UI.CreateRippleDrawable(
            _context, LauncherTheme.HeaderPillHex, LauncherTheme.ChipRadiusDp);
        button.Click += (s, e) => onClick();
        return button;
    }
}
