using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using Google.Android.Material.Dialog;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using Orientation = Android.Widget.Orientation;

namespace SMAPIGameLoader.Launcher;

/// <summary>
/// ฟอร์มแก้ JSON แบบไม่ต้องเขียนโค้ด (object/array ซ้อนได้, เพิ่ม/ลบ field ได้)
/// ค่าถูกเขียนกลับเข้า JToken ทันทีที่ผู้ใช้แก้ — เปลี่ยนชื่อ key ยังไม่รองรับ
/// </summary>
internal sealed class JsonTreeEditor : LinearLayout
{
    private enum ValueKind { Text, Number, Bool, Object, Array }

    private static readonly string[] KindNames = { "Text", "Number", "True / False", "Object", "Array" };

    private sealed class NumberField
    {
        public EditText Input;
        public JValue Value;
        public string Path;
    }

    private readonly Context _context;
    private readonly JToken _root;
    private readonly List<NumberField> _numbers = new();

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
            if (!TryParseNumber(field.Input.Text, out _))
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
                AddObjectChildren(this, obj);
                break;
            case JArray array:
                AddArrayChildren(this, array);
                break;
            default:
                AddInfo("Root value is not an object or array. Switch to Raw mode to edit it.");
                break;
        }
    }

    void AddObjectChildren(LinearLayout parent, JObject obj)
    {
        foreach (var property in obj.Properties())
        {
            string name = property.Name;
            AddNode(parent, property.Value, name, name,
                () => { obj.Remove(name); OnStructureChanged(); });
        }

        AddAddButton(parent, "+ Add field", () => ShowAddDialog(obj, false));
    }

    void AddArrayChildren(LinearLayout parent, JArray array)
    {
        for (int i = 0; i < array.Count; i++)
        {
            int index = i;
            AddNode(parent, array[index], $"[{index}]", $"[{index}]",
                () => { array.RemoveAt(index); OnStructureChanged(); });
        }

        AddAddButton(parent, "+ Add item", () => ShowAddDialog(array, true));
    }

    void AddNode(LinearLayout parent, JToken token, string label, string path, Action delete)
    {
        switch (token)
        {
            case JObject obj:
                AddObjectChildren(AddSection(parent, label, delete), obj);
                break;
            case JArray array:
                AddArrayChildren(AddSection(parent, label, delete), array);
                break;
            default:
                AddScalarRow(parent, token, label, path, delete);
                break;
        }
    }

    LinearLayout AddSection(LinearLayout parent, string label, Action delete)
    {
        var section = new LinearLayout(_context) { Orientation = Orientation.Vertical };
        section.SetPadding(UI.Dp(_context, 12), 0, 0, 0);
        parent.AddView(section, new LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        var row = new LinearLayout(_context) { Orientation = Orientation.Horizontal };
        row.SetGravity(GravityFlags.CenterVertical);
        row.SetPadding(0, UI.Dp(_context, 6), 0, 0);

        var title = new TextView(_context) { Text = label };
        title.SetTextSize(ComplexUnitType.Sp, 14);
        title.SetTextColor(LauncherTheme.Parse(LauncherTheme.CardTitleHex));
        title.SetTypeface(LauncherTheme.TitleTypeface, TypefaceStyle.Normal);
        title.SetSingleLine(true);
        title.Ellipsize = TextUtils.TruncateAt.End;
        title.LayoutParameters = new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f);
        row.AddView(title);
        row.AddView(CreateDeleteButton(delete));

        section.AddView(row, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
        return section;
    }

    void AddScalarRow(LinearLayout parent, JToken token, string label, string path, Action delete)
    {
        var cell = new LinearLayout(_context) { Orientation = Orientation.Vertical };
        parent.AddView(cell, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        { BottomMargin = UI.Dp(_context, 10) });

        var labelRow = new LinearLayout(_context) { Orientation = Orientation.Horizontal };
        labelRow.SetGravity(GravityFlags.CenterVertical);

        var labelView = new TextView(_context) { Text = label };
        labelView.SetTextSize(ComplexUnitType.Sp, 12);
        labelView.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        labelView.SetTypeface(LauncherTheme.BodyTypeface, TypefaceStyle.Normal);
        labelView.SetSingleLine(true);
        labelView.Ellipsize = TextUtils.TruncateAt.End;
        labelView.LayoutParameters = new LinearLayout.LayoutParams(
            0, ViewGroup.LayoutParams.WrapContent, 1f);
        labelRow.AddView(labelView);
        labelRow.AddView(CreateDeleteButton(delete));

        cell.AddView(labelRow, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

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
                var input = CreateInput(InputTypes.ClassNumber
                    | InputTypes.NumberFlagDecimal | InputTypes.NumberFlagSigned);
                input.Text = token.ToString();
                var field = new NumberField { Input = input, Value = (JValue)token, Path = path };
                _numbers.Add(field);
                input.TextChanged += (s, e) =>
                {
                    if (TryParseNumber(e.Text?.ToString(), out var parsed))
                    {
                        field.Value.Value = parsed;
                        input.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
                    }
                    else
                    {
                        input.SetTextColor(LauncherTheme.Parse(LauncherTheme.StatusErrorTextHex));
                    }

                    RaiseChanged();
                };
                editor = input;
                break;
            }
            case JTokenType.String:
            {
                var input = CreateInput(InputTypes.ClassText);
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

        editor.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        { TopMargin = UI.Dp(_context, 2) };
        cell.AddView(editor);
    }

    EditText CreateInput(InputTypes inputType)
    {
        var input = new EditText(_context)
        {
            Background = LauncherTheme.CreateSurfaceDrawable(
                _context, LauncherTheme.SurfaceVariantHex, LauncherTheme.ChipRadiusDp)
        };
        input.SetTextSize(ComplexUnitType.Sp, 14);
        input.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextPrimaryHex));
        input.SetPadding(
            UI.Dp(_context, 12), UI.Dp(_context, 10),
            UI.Dp(_context, 12), UI.Dp(_context, 10));
        input.SetSingleLine(true);
        input.InputType = inputType;
        return input;
    }

    TextView CreateReadOnlyValue(string text)
    {
        var view = new TextView(_context) { Text = text };
        view.SetTextSize(ComplexUnitType.Sp, 14);
        view.SetTextColor(LauncherTheme.Parse(LauncherTheme.TextSecondaryHex));
        view.SetTypeface(Typeface.Monospace, TypefaceStyle.Normal);
        view.SetSingleLine(true);
        view.Ellipsize = TextUtils.TruncateAt.End;
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
        button.SetPadding(
            UI.Dp(_context, 8), UI.Dp(_context, 10),
            UI.Dp(_context, 8), UI.Dp(_context, 10));
        button.Background = UI.CreateRippleDrawable(_context, null, LauncherTheme.ChipRadiusDp);
        button.Click += (s, e) => action();

        parent.AddView(button, new LayoutParams(
            ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = UI.Dp(_context, 2),
            BottomMargin = UI.Dp(_context, 6)
        });
    }

    ImageButton CreateDeleteButton(Action delete)
    {
        var button = new ImageButton(_context);
        button.SetImageResource(Resource.Drawable.ic_delete);
        button.SetColorFilter(LauncherTheme.Parse(LauncherTheme.StatusErrorTextHex));
        button.Background = UI.CreateRippleDrawable(_context, null, LauncherTheme.ChipRadiusDp);
        button.ContentDescription = "Delete";
        button.SetPadding(
            UI.Dp(_context, 12), UI.Dp(_context, 12),
            UI.Dp(_context, 12), UI.Dp(_context, 12));
        button.Click += (s, e) => delete();

        int size = UI.Dp(_context, 48);
        button.LayoutParameters = new LinearLayout.LayoutParams(size, size);
        return button;
    }

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
            .SetTitle(isArray ? "Add item" : "Add field")
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

    static bool TryParseNumber(string text, out object value)
    {
        text = (text ?? "").Trim();
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long integer))
        {
            value = integer;
            return true;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
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
