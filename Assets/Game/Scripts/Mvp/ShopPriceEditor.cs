using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ShopPriceCategory { Tobacco, SoftDrinks, Alcohol, Coffee }

public sealed partial class ShopPricing
{
    private const string PriceField = "TobaccOS.Price";
    private string draft = "", error = "";
    private bool editing, clearFocus, focusEdit;
    private int cancelledFrame=-1;
    private Vector2 productScroll;
    public bool Editing => editing;
    public string Draft => draft;
    public string Error => error;
    public ShopPriceCategory Category { get; private set; }
    public static readonly string[] CategoryNames = { "Dohánytermékek", "Üdítők és víz", "Alkoholos italok", "Meleg italok" };
    public static ShopPriceCategory CategoryOf(int type) => ShopCatalog.IsCigarette(type) ? ShopPriceCategory.Tobacco :
        ShopCatalog.IsCoffee(type) ? ShopPriceCategory.Coffee : type == 7 || type == 8 || ShopPartnerCatalog.IsSpirit(type) ? ShopPriceCategory.Alcohol : ShopPriceCategory.SoftDrinks;
    public int[] VisibleTypes => AvailableTypes.Where(t => CategoryOf(t) == Category).ToArray();

    private void ResetEditor()
    {
        Category = ShopPriceCategory.Tobacco;
        cancelledFrame=-1;
        editing = false; clearFocus = true; focusEdit = false;
        error = "";
        draft = Price(SelectedType).ToString();
        productScroll = Vector2.zero;
    }

    public void BeginEdit() { editing = true; error = ""; focusEdit = true; clearFocus = false; }
    public void SetDraft(string value) { draft = value ?? ""; editing = true; }
    public bool CommitEdit()
    {
        if (!editing) return true;
        int type = SelectedType;
        if (!int.TryParse(draft, NumberStyles.None, CultureInfo.InvariantCulture, out int value) ||
            value % 5 != 0 || value < Minimum(type) || value > Maximum(type))
        {
            error = $"{ShopCatalog.Money(Minimum(type))}–{ShopCatalog.Money(Maximum(type))}, 5 Ft-os lépésekben";
            return false;
        }
        if (value != Price(type) && !TrySetPrice(type, value))
        {
            error = "Most nem módosítható az ár.";
            return false;
        }
        editing = false;
        error = "";
        draft = Price(type).ToString();
        clearFocus = true;
        return true;
    }

    public bool CancelEdit()
    {
        if (!editing) return cancelledFrame==Time.frameCount;
        cancelledFrame=Time.frameCount;
        editing = false; clearFocus = true; focusEdit = false;
        error = "";
        draft = Price(SelectedType).ToString();
        clearFocus = true;
        return true;
    }

    public void ChooseCategory(ShopPriceCategory category)
    {
        if (!Enum.IsDefined(typeof(ShopPriceCategory), category) || !CommitEdit()) return;
        Category = category;
        var visible = VisibleTypes;
        if (visible.Length > 0 && !visible.Contains(SelectedType)) selected = visible[0];
        draft = Price(SelectedType).ToString();
        productScroll = Vector2.zero;
    }

    public void ChooseProduct(int type)
    {
        if (!AvailableTypes.Contains(type) || !CommitEdit()) return;
        selected = type;
        Category = CategoryOf(type);
        draft = Price(type).ToString();
    }

    public void StepEditor(int direction, bool fine)
    {
        if(VisibleTypes.Length==0)return;
        if (!CommitEdit()) return;
        AdjustSelected(Math.Sign(direction) * (fine ? 5 : 100));
        draft = Price(SelectedType).ToString();
    }

    public bool HandleEditorKey(Key key, bool shift)
    {
        if (key == Key.Escape) return CancelEdit();
        if (editing)
        {
            if (key == Key.Enter || key == Key.NumpadEnter || key == Key.Tab) { CommitEdit(); return true; }
            return true; // TextField owns typing, Backspace and selection, not app shortcuts.
        }
        if (key == Key.Equals || key == Key.NumpadPlus || key == Key.Minus || key == Key.NumpadMinus)
        {
            StepEditor(key == Key.Minus || key == Key.NumpadMinus ? -1 : 1, shift);
            return true;
        }
        return false;
    }

    public void DrawEditor(Rect area)
    {
        GUI.BeginGroup(area);
        try
        {
            var e=Event.current;
            // WebGL's native text editor can consume Escape before Input System.
            // Own the IMGUI path too, and consume cancellation once per frame.
            if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape&&(editing||cancelledFrame==Time.frameCount)) {
                CancelEdit();GUI.FocusControl(null);e.Use();
            }
            if(e.type==EventType.KeyDown&&(e.keyCode==KeyCode.Return||e.keyCode==KeyCode.KeypadEnter)&&editing) {
                CommitEdit();e.Use();
            }
            if(clearFocus){GUI.FocusControl(null);clearFocus=false;}
            float w = area.width, h = area.height, left = w * .32f, x = left + 32;
            for (int i = 0; i < CategoryNames.Length; i++)
                if (GUI.Button(new Rect(i * w / 4, 0, w / 4 - 8, 44), CategoryNames[i],
                    ShopComputerTheme.Button((int)Category == i, 20))) ChooseCategory((ShopPriceCategory)i);
            var visible = VisibleTypes;
            var list = new Rect(0, 60, left, h - 60);
            productScroll = GUI.BeginScrollView(list, productScroll, new Rect(0, 0, left - 22, Mathf.Max(list.height, visible.Length * 68)));
            for (int i = 0; i < visible.Length; i++)
            {
                int type = visible[i];
                if (GUI.Button(new Rect(0, i * 68, left - 28, 60), ShopCatalog.ShortNames[type] + "\n" + ShopCatalog.Money(Price(type)),
                    ShopComputerTheme.Button(type == SelectedType, 20))) ChooseProduct(type);
            }
            GUI.EndScrollView();
            if (visible.Length == 0)
            {
                ShopComputerTheme.Label(new Rect(x, 110, w - x, 60), "Még nincs elérhető termék ebben a kategóriában.");
                return;
            }
            if (!visible.Contains(SelectedType)) ChooseProduct(visible[0]);
            int current = SelectedType;
            ShopComputerTheme.Label(new Rect(x, 60, w - x, 38), ShopCatalog.Names[current], 28, true);
            ShopComputerTheme.Label(new Rect(x, 104, w - x, 32), "Beszerzés " + ShopCatalog.Money(ShopCatalog.Costs[current]) + "     Alapár " + ShopCatalog.Money(ShopCatalog.Prices[current]), 20);
            float fieldWidth = Mathf.Min(300, w - x - 200);
            var field = new Rect(x + 74, 168, fieldWidth, 62);

            if (e.type == EventType.MouseDown)
            {
                if (field.Contains(e.mousePosition)) BeginEdit();
                else if (editing && !CommitEdit()) clearFocus = true;
            }
            if (!editing && error.Length == 0) draft = Price(current).ToString();
            ShopComputerTheme.Label(new Rect(x, 140, w - x, 26), "Eladási ár", 19);
            bool fine = e.shift || Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            if (GUI.Button(new Rect(x, 168, 60, 62), "-", ShopComputerTheme.Button(false, 30))) StepEditor(-1, fine);
            GUI.SetNextControlName(PriceField);
            string value = GUI.TextField(field, draft, 9, ShopComputerTheme.Field());
            if (value != draft && cancelledFrame!=Time.frameCount) SetDraft(value);
            ShopComputerTheme.Label(new Rect(field.xMax + 8, 168, 40, 62), "Ft", 24);
            if (GUI.Button(new Rect(field.xMax + 58, 168, 60, 62), "+", ShopComputerTheme.Button(false, 30))) StepEditor(1, fine);
            if (focusEdit) { GUI.FocusControl(PriceField); focusEdit = false; }
            if (clearFocus) { GUI.FocusControl(null); clearFocus = false; }
            if (error.Length > 0) ShopComputerTheme.Label(new Rect(x, 238, w - x, 38), error, 18);
            ShopComputerTheme.Label(new Rect(x, 282, w - x, 34), "Becsült ár-elfogadás: " + DemandPercent(current) + "%", 22);
            if (GUI.Button(new Rect(x, 330, 260, 44), "Alapár visszaállítása", ShopComputerTheme.Button()))
            {
                CancelEdit(); TrySetPrice(current, ShopCatalog.Prices[current]);
            }
            ShopComputerTheme.Label(new Rect(x, h - 40, w - x, 40), "Az új ár az újonnan érkezőkre érvényes.", 18);
        }
        finally { GUI.EndGroup(); }
    }
}
