using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using Google.Android.Material.Dialog;
using Google.Android.Material.Snackbar;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Orientation = Android.Widget.Orientation;

namespace SMAPIGameLoader.Launcher;

/// <summary>
/// ฟอร์มแก้ JSON แบบ compact: แถวเดียวต่อ field (label 40% / value 60%),
/// section ซ้อนพับได้, ลบด้วย long-press + Undo — ค่าถูกเขียนกลับเข้า JToken ทันที
/// </summary>
internal sealed class JsonTreeEditor : LinearLayout
{
    private const float RowHeightDp = 40;
    private const int AutoCollapseThreshold = 5;

    private enum ValueKind { Text, Number, Bool, Object, Array }

    private static readonly string[] KindNames = { "Text", "Number", "True / False", "Object", "Array" };

    private sealed class NumberField
    {
        public EditText Input;
        public JValue Value;
        public string Path;
        public bool IsInteger;
    }

    /// <summary>กันพิมพ์/วางค่าที่ไม่ใช่ตัวเลข (ปล่อย partial อย่าง "-" หรือ "1." ไว้ให้ validate ตอน save)</summary>
    private sealed class NumberInputFilter : Java.Lang.Object, IInputFilter
    {
        private readonly Regex _pattern;

        public NumberInputFilter(bool allowDecimal) =>
            _pattern = new Regex(allowDecimal ? @"^-?\d*\.?\d*$" : @"^-?\d*$");

        public Java.Lang.ICharSequence FilterFormatted(Java.Lang.ICharSequence source, int start, int end,
            ISpanned dest, int dstart, int dend)
        {
            string current = dest?.ToString() ?? "";
            if (dstart < 0 || dend > current.Length || dstart > dend)
                return null;

            string inserted = source?.SubSequenceFormatted(start, end)?.ToString() ?? "";
            string result = current.Substring(0, dstart) + inserted + current.Substring(dend);
            return _pattern.IsMatch(result) ? null : new Java.Lang.String("");
        }
    }

    private readonly Context _context;
    private readonly JToken _root;
    private readonly List<NumberField> _numbers = new();
    private readonly Dictionary<string, bool> _collapsed = new();

    /// <summary>ยิงเมื่อผู้ใช้เปลี่ยนค่าหรือโครงสร้าง (ใช้ทำ dirty flag)</summary>
    public event Action Changed;

    public JsonTreeEditor(Context context, JToken root) : base(context)
    {
        _context = context;
        _root = root;
        Orientation = Orientation.Vertical;
        Render();
    }

    /// <summary>number ที่พิมพ์ค้างไว้แบบ parse ไม่ได้จะไม่ถูกบันทึก — เรียกก่อน save/สลับไป Raw</summary>
    public bool TryValidateNumbers(out string error)
    {
        foreach (var field in _numbers)
        {
            if (!TryParseNumber(field.Input.Text, field.IsInteger, out _))
            {
                error = $"Invalid number for \"{field.Path}\"";
                return false;
            }
        }

        error = null;
        return true;
    }

    public void Render()
    {
        RemoveAllViews();
        _numbers.Clear();

        switch (_root)
        {
            case JObject obj:
                AddObjectChildren(this, obj, "");
                break;
            case JArray array:
                AddArrayChildren(this, array, "");
                break;
            default:
                AddInfo("Root value is not an object or array. Switch to Raw mode to edit it.");
                break;
        }
    }

    // ===== object / array =====

    void AddObjectChildren(LinearLayout parent, JObject obj, string path)
    {
        foreach (var property in obj.Properties().ToList())
        {
            string key = property.Name;
            AddNode(parent, property.Value, key, ChildPath(path, key),
                () => DeleteProperty(obj, key),
                () => ShowRenameDialog(obj, key));
        }

        AddAddButton(parent, "+ Add key", () => ShowAddDialog(obj, false));
    }

    void AddArrayChildren(LinearLayout parent, JArray array, string path)
    {
        for (int i = 0; i < array.Count; i++)
        {
            int index = i;
            string itemPath = $"{path}[{index}]";
            AddNode(parent, array[index], $"[{index}]", itemPath,
                () => DeleteArrayItem(array, index), null);   // array item ไม่มี key ให้ rename
        }

        AddAddButton(parent, "+ Add item", () => ShowAddDialog(array, true));
    }

    void AddNode(LinearLayout parent, JToken token, string label, string path,
                 Action delete, Action rename)
    {
        switch (token)
        {
            case JObject obj:
                AddSection(parent, label, obj.Count, path, delete, rename,
                    content => AddObjectChildren(content, obj, path));
                break;
            case JArray array:
                AddSection(parent, label, array.Count, path, delete, null,
                    content => AddArrayChildren(content, array, path));
                break;
            default:
                AddScalarRow(parent, token, label, path, delete, rename);
                break;
        }
    }

    static string ChildPath(string path, string key) =>
        string.IsNullOrEmpty(path) ? key : $"{path}.{key}";

    // ===== collapsible section =====

    void AddSection(LinearLayout parent, string label, int count, string path,
                    Action delete, Action rename, Action<LinearLayout> fill)
    {
        bool collapsed = _collapsed.TryGetValue(path, out bool stored)
            ? stored
            : count > AutoCollapseThreshold;

        var section = new LinearLayout(_context) { Orientation = Orientation.Vertical };
        parent.AddView(section, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        var header = new LinearLayout(_context)
        {
            Orientation = Orientation.Horizontal,
            Clickable = true,
            Focusable = true
        };
        header.SetGravity(GravityFlags.CenterVertical);
        header.SetMinimumHeight(UI.Dp(_context, RowHeightDp));
        header.SetPadding(UI.Dp(_context, 4), 0, UI.Dp(_context, 4), 0);
        header.Background = UI.CreateRippleDrawable(_context, null, LauncherTheme.ChipRadiusDp);

        var chevron = new ImageView(_context);
        chevron.SetImageResource(Resource.Drawable.ic_chevron_right);
        chevron.SetColorFilter(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        chevron.Rotation = collapsed ? 0 : 90;
        header.AddView(chevron, new LinearLayout.LayoutParams(
            UI.Dp(_context, 16), UI.Dp(_context, 16))
        { RightMargin = UI.Dp(_context, 6) });

        var title = new TextView(_context) { Text = $"{label} {{{count}}}" };
        title.SetTextSize(ComplexUnitType.Sp, 13);
        title.SetTextColor(LauncherTheme.Parse(LauncherTheme.CardTitleHex));
        title.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        title.SetSingleLine(true);
        title.Ellipsize = TextUtils.TruncateAt.End;
        title.SetIncludeFontPadding(false);
        header.AddView(title, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f));

        section.AddView(header, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
        AttachRowMenu(header, delete, rename);

        var body = new LinearLayout(_context) { Orientation = Orientation.Horizontal };
        body.Visibility = collapsed ? ViewStates.Gone : ViewStates.Visible;

        var indentLine = new View(_context);
        indentLine.SetBackgroundColor(LauncherTheme.Parse(LauncherTheme.DividerHex));
        body.AddView(indentLine, new LinearLayout.LayoutParams(
            UI.Dp(_context, 1), ViewGroup.LayoutParams.MatchParent)
        {
            LeftMargin = UI.Dp(_context, 12),
            RightMargin = UI.Dp(_context, 8)
        });

        var content = new LinearLayout(_context) { Orientation = Orientation.Vertical };
        body.AddView(content, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f));

        section.AddView(body, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        fill(content);

        header.Click += (s, e) =>
        {
            collapsed = !collapsed;
            _collapsed[path] = collapsed;
            body.Visibility = collapsed ? ViewStates.Gone : ViewStates.Visible;
            chevron.Rotation = collapsed ? 0 : 90;
        };
    }

    // ===== scalar row (label 40% / value 60%) =====

    void AddScalarRow(LinearLayout parent, JToken token, string label, string path,
                      Action delete, Action rename)
    {
        var row = new LinearLayout(_context) { Orientation = Orientation.Horizontal };
        row.SetGravity(GravityFlags.CenterVertical);
        row.SetMinimumHeight(UI.Dp(_context, RowHeightDp));
        row.SetPadding(0, UI.Dp(_context, 2), 0, UI.Dp(_context, 2));
        parent.AddView(row, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
        AttachRowMenu(row, delete, rename);

        var labelView = new TextView(_context) { Text = label };
        labelView.SetTextSize(ComplexUnitType.Sp, 13);
        labelView.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        labelView.SetTypeface(LauncherTheme.BodyTypeface, TypefaceStyle.Normal);
        labelView.SetSingleLine(true);
        labelView.Ellipsize = TextUtils.TruncateAt.End;
        labelView.SetIncludeFontPadding(false);
        row.AddView(labelView, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 0.4f)
        { RightMargin = UI.Dp(_context, 8) });

        View editor;
        switch (token.Type)
        {
            case JTokenType.Boolean:
            {
                var toggle = new SwitchCompat(_context);
                toggle.ThumbTintList = TintStates(
                    LauncherTheme.AccentHex, LauncherTheme.TextSecondaryHex);
                toggle.TrackTintList = TintStates(
                    LauncherTheme.StatusOkBackgroundHex, LauncherTheme.SurfaceVariantHex);
                toggle.Checked = token.Value<bool>();
                toggle.CheckedChange += (s, e) =>
                {
                    ((JValue)token).Value = e.IsChecked;
                    RaiseChanged();
                };
                editor = toggle;
                break;
            }
            case JTokenType.Integer:
            case JTokenType.Float:
            {
                bool isInteger = token.Type == JTokenType.Integer;
                var input = CreateEntry(isInteger
                    ? InputTypes.ClassNumber | InputTypes.NumberFlagSigned
                    : InputTypes.ClassNumber | InputTypes.NumberFlagSigned | InputTypes.NumberFlagDecimal);
                input.Text = token.ToString();
                input.SetFilters(new IInputFilter[] { new NumberInputFilter(!isInteger) });

                var field = new NumberField
                {
                    Input = input,
                    Value = (JValue)token,
                    Path = path,
                    IsInteger = isInteger
                };
                _numbers.Add(field);

                input.TextChanged += (s, e) =>
                {
                    if (TryParseNumber(e.Text?.ToString(), field.IsInteger, out var parsed))
                        field.Value.Value = parsed;

                    RaiseChanged();
                };
                editor = input;
                break;
            }
            case JTokenType.String:
            {
                var input = CreateEntry(InputTypes.ClassText);
                input.Text = token.Value<string>();
                input.TextChanged += (s, e) =>
                {
                    ((JValue)token).Value = e.Text?.ToString();
                    RaiseChanged();
                };
                editor = input;
                break;
            }
            case JTokenType.Null:
                editor = CreateReadOnlyValue("(null)");
                break;
            default:
                editor = CreateReadOnlyValue(token.ToString());
                break;
        }

        row.AddView(editor, new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 0.6f));
    }

    EditText CreateEntry(InputTypes inputType)
    {
        var input = new EditText(_context) { Background = CreateFieldDrawable() };
        input.SetTextSize(ComplexUnitType.Sp, 13);
        input.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        input.SetPadding(
            UI.Dp(_context, 8), UI.Dp(_context, 4),
            UI.Dp(_context, 8), UI.Dp(_context, 4));
        input.SetIncludeFontPadding(false);
        input.SetMinimumHeight(UI.Dp(_context, 32));
        input.SetSingleLine(true);
        input.InputType = inputType;
        return input;
    }

    /// <summary>พื้นกลมขอบบาง 1px (ไดอะล็อก 1dp จะหนาเกินไปในธีมมืด)</summary>
    Android.Graphics.Drawables.Drawable CreateFieldDrawable()
    {
        var drawable = LauncherTheme.CreateSurfaceDrawable(
            _context, LauncherTheme.SurfaceVariantHex, LauncherTheme.ChipRadiusDp);
        drawable.SetStroke(1, LauncherTheme.Parse(LauncherTheme.DividerHex));
        return drawable;
    }

    TextView CreateReadOnlyValue(string text)
    {
        var view = new TextView(_context) { Text = text };
        view.SetTextSize(ComplexUnitType.Sp, 13);
        view.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        view.SetTypeface(Typeface.Monospace, TypefaceStyle.Normal);
        view.SetSingleLine(true);
        view.Ellipsize = TextUtils.TruncateAt.End;
        view.SetIncludeFontPadding(false);
        return view;
    }

    void AddInfo(string message)
    {
        var view = new TextView(_context) { Text = message };
        view.SetTextSize(ComplexUnitType.Sp, 13);
        view.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        AddView(view, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
    }

    void AddAddButton(LinearLayout parent, string text, Action action)
    {
        var button = new TextView(_context)
        {
            Text = text,
            Clickable = true,
            Focusable = true
        };
        button.SetTextSize(ComplexUnitType.Sp, 13);
        button.SetTextColor(LauncherTheme.Parse(LauncherTheme.AccentHex));
        button.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        button.SetIncludeFontPadding(false);
        button.SetPadding(
            UI.Dp(_context, 4), UI.Dp(_context, 10),
            UI.Dp(_context, 4), UI.Dp(_context, 10));
        button.Background = UI.CreateRippleDrawable(_context, null, LauncherTheme.ChipRadiusDp);
        button.Click += (s, e) => action();

        parent.AddView(button, new LayoutParams(
            ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        { TopMargin = UI.Dp(_context, 2), BottomMargin = UI.Dp(_context, 2) });
    }

    // ===== row menu (rename / delete) + undo =====

    /// <summary>กดค้างที่แถว/หัว section: ไฮไลต์แถวระหว่างเปิด popup แล้วโชว์เมนู anchored</summary>
    void AttachRowMenu(View anchor, Action delete, Action rename)
    {
        anchor.LongClickable = true;
        anchor.LongClick += (s, e) =>
        {
            var previousBackground = anchor.Background;
            anchor.Background = UI.CreateRippleDrawable(
                _context, LauncherTheme.SelectedCardBackgroundHex, LauncherTheme.ChipRadiusDp);

            var menu = new AnchoredMenu(_context);
            if (rename is not null)
            {
                menu.AddItem(Resource.Drawable.ic_edit, "Rename", null, rename);
                menu.AddDivider();
            }

            menu.AddItem(Resource.Drawable.ic_delete, "Delete",
                    LauncherTheme.StatusErrorTextHex, delete)
                .OnDismiss(() => anchor.Background = previousBackground)
                .Show(anchor);

            e.Handled = true;
        };
    }

    void ShowRenameDialog(JObject owner, string key)
    {
        var box = new LinearLayout(_context) { Orientation = Orientation.Vertical };
        box.SetPadding(
            UI.Dp(_context, 20), UI.Dp(_context, 4),
            UI.Dp(_context, 20), 0);

        var input = new EditText(_context) { Text = key };
        input.SetSingleLine(true);
        input.SetTextSize(ComplexUnitType.Sp, 14);
        input.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        input.SetSelection(key.Length);
        box.AddView(input, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        new MaterialAlertDialogBuilder(_context)
            .SetTitle("Rename key")
            .SetView(box)
            .SetPositiveButton("Rename", (s, e) =>
            {
                string newKey = (input.Text ?? "").Trim();
                if (newKey.Length == 0)
                {
                    Toast.MakeText(_context, "Key cannot be empty", ToastLength.Short).Show();
                    return;
                }

                if (newKey == key)
                    return;

                if (owner.ContainsKey(newKey))
                {
                    Toast.MakeText(_context, "Key already exists", ToastLength.Short).Show();
                    return;
                }

                RenameProperty(owner, key, newKey);
            })
            .SetNegativeButton("Cancel", (s, e) => { })
            .Show();
    }

    /// <summary>เปลี่ยนชื่อ key โดยคงลำดับ field เดิมของ object</summary>
    void RenameProperty(JObject owner, string oldKey, string newKey)
    {
        var items = owner.Properties().Select(p => (p.Name, p.Value)).ToList();
        owner.RemoveAll();
        foreach (var (name, value) in items)
            owner.Add(name == oldKey ? newKey : name, value);

        OnStructureChanged();
    }

    void DeleteProperty(JObject obj, string key)
    {
        var token = obj[key];
        obj.Remove(key);
        OnStructureChanged();
        ShowUndo($"Deleted \"{key}\"", () =>
        {
            if (!obj.ContainsKey(key))
                obj.Add(key, token);
            OnStructureChanged();
        });
    }

    void DeleteArrayItem(JArray array, int index)
    {
        var token = array[index];
        array.RemoveAt(index);
        OnStructureChanged();
        ShowUndo($"Deleted \"[{index}]\"", () =>
        {
            if (index <= array.Count)
                array.Insert(index, token);
            else
                array.Add(token);
            OnStructureChanged();
        });
    }

    void ShowUndo(string message, Action undo)
    {
        var snackbar = Snackbar.Make(this, message, Snackbar.LengthLong);
        snackbar.SetBackgroundTint(
            LauncherTheme.Parse(LauncherTheme.SurfaceVariantHex).ToArgb());
        snackbar.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        snackbar.SetActionTextColor(
            LauncherTheme.Parse(LauncherTheme.AccentHex).ToArgb());
        snackbar.SetAction("Undo", v => undo());
        snackbar.Show();
    }

    // ===== add key / item =====

    void ShowAddDialog(JContainer container, bool isArray)
    {
        var box = new LinearLayout(_context) { Orientation = Orientation.Vertical };
        box.SetPadding(
            UI.Dp(_context, 20), UI.Dp(_context, 4),
            UI.Dp(_context, 20), 0);

        EditText keyInput = null;
        if (!isArray)
        {
            keyInput = new EditText(_context) { Hint = "Key" };
            keyInput.SetSingleLine(true);
            keyInput.SetTextSize(ComplexUnitType.Sp, 14);
            keyInput.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
            box.AddView(keyInput, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
        }

        var spinner = new Spinner(_context);
        var adapter = new ArrayAdapter<string>(
            _context, Android.Resource.Layout.SimpleSpinnerItem, KindNames);
        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        spinner.Adapter = adapter;
        box.AddView(spinner, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        { TopMargin = UI.Dp(_context, 8) });

        new MaterialAlertDialogBuilder(_context)
            .SetTitle(isArray ? "Add item" : "Add key")
            .SetView(box)
            .SetPositiveButton("Add", (s, e) =>
            {
                var kind = (ValueKind)spinner.SelectedItemPosition;

                if (isArray)
                {
                    ((JArray)container).Add(CreateDefaultValue(kind));
                    OnStructureChanged();
                    return;
                }

                string key = (keyInput.Text ?? "").Trim();
                if (key.Length == 0)
                {
                    Toast.MakeText(_context, "Key cannot be empty", ToastLength.Short).Show();
                    return;
                }

                var obj = (JObject)container;
                if (obj.ContainsKey(key))
                {
                    Toast.MakeText(_context, "Key already exists", ToastLength.Short).Show();
                    return;
                }

                obj[key] = CreateDefaultValue(kind);
                OnStructureChanged();
            })
            .SetNegativeButton("Cancel", (s, e) => { })
            .Show();
    }

    static JToken CreateDefaultValue(ValueKind kind) => kind switch
    {
        ValueKind.Text => new JValue(""),
        ValueKind.Number => new JValue(0),
        ValueKind.Bool => new JValue(false),
        ValueKind.Object => new JObject(),
        _ => new JArray(),
    };

    static bool TryParseNumber(string text, bool isInteger, out object value)
    {
        text = (text ?? "").Trim();

        if (isInteger)
        {
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long integer))
            {
                value = integer;
                return true;
            }
        }
        else if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
        {
            value = number;
            return true;
        }

        value = null;
        return false;
    }

    static ColorStateList TintStates(string checkedHex, string normalHex) =>
        new(
            new[] { new[] { Android.Resource.Attribute.StateChecked }, new int[0] },
            new[]
            {
                LauncherTheme.Parse(checkedHex).ToArgb(),
                LauncherTheme.Parse(normalHex).ToArgb(),
            });

    void OnStructureChanged()
    {
        Render();
        RaiseChanged();
    }

    void RaiseChanged() => Changed?.Invoke();
}
