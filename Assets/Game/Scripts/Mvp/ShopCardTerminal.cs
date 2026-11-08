using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ShopCardTerminal : MonoBehaviour
{
    private MvpShopController shop;
    private TextMesh due, entered, status;
    private readonly List<Material> materials = new List<Material>();
    private readonly List<ShopWorldControl> controls = new List<ShopWorldControl>();
    public IReadOnlyList<ShopWorldControl> Controls => controls;
    public string DisplayAmount => entered.text;
    public string DisplayStatus => status.text;
    public string DisplayDue => due.text;

    public void Configure(MvpShopController owner, Transform till)
    {
        shop = owner;
        transform.SetParent(till, false);
        transform.localPosition = new Vector3(-.48f, .11f, .04f);
        transform.localRotation = Quaternion.Euler(-55, 0, 0);
        transform.localScale = Vector3.one * .6f;
        var body = Material("Terminal casing", new Color(.065f, .085f, .075f));
        var key = Material("Terminal keypad", new Color(.72f, .73f, .65f));
        var green = Material("Terminal approval key", ShopUi.Green);
        var red = Material("Terminal clear key", ShopUi.Red);
        var display = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Terminal LCD", color = new Color(.71f, .80f, .58f) };
        materials.Add(display);
        Box("Card terminal casing", Vector3.zero, new Vector3(.44f, .60f, .075f), body);
        var screen = Box("Card terminal display", new Vector3(0, .185f, .042f), new Vector3(.395f, .225f, .008f), display);
        screen.GetComponent<Collider>().enabled = false;
        due = Text("Terminal due", new Vector3(0, .26f, .051f), .016f, Color.black);
        entered = Text("Terminal entered amount", new Vector3(0, .185f, .051f), .036f, Color.black);
        entered.fontStyle = FontStyle.Bold;
        status = Text("Terminal status", new Vector3(0, .105f, .051f), .015f, Color.black);
        status.fontStyle = FontStyle.Bold;
        for (int digit = 1; digit <= 9; digit++)
        {
            int index = digit - 1;
            Key(digit.ToString(), new Vector3(.15f - index % 3 * .095f, .035f - index / 3 * .075f, .052f), WorldControlKind.CardDigit, digit, key);
        }
        Key("0", new Vector3(.055f, -.19f, .052f), WorldControlKind.CardDigit, 0, key);
        Key("JAV", new Vector3(-.145f, .035f, .052f), WorldControlKind.CardUndo, 0, key);
        Key("TÖR", new Vector3(-.145f, -.04f, .052f), WorldControlKind.CardClear, 0, red, true);
        Key("OK", new Vector3(-.145f, -.115f, .052f), WorldControlKind.CardConfirm, 0, green, true);
        Key("ESC", new Vector3(-.145f, -.19f, .052f), WorldControlKind.Exit, 0, key);
        gameObject.AddComponent<ShopStation>().Configure(shop, ShopStationKind.Terminal);
        Refresh();
    }

    private Material Material(string title, Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = title, color = color };
        materials.Add(material);
        return material;
    }
    private GameObject Box(string title, Vector3 position, Vector3 size, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = title;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }
    private TextMesh Text(string title, Vector3 position, float size, Color color)
        => ProductVisuals.Text(transform, title, "", position, Quaternion.Euler(0, 180, 0), size, color);

    private void Key(string caption, Vector3 position, WorldControlKind kind, int value, Material material, bool light = false)
    {
        var part = Box("Terminal key " + caption, position, new Vector3(.078f, .056f, .022f), material);
        var control = part.AddComponent<ShopWorldControl>();
        control.Kind = kind;
        control.Value = value;
        controls.Add(control);
        Text("Terminal key label", position + Vector3.forward * .014f, .018f, light ? ShopUi.Paper : Color.black).text = caption;
    }

    public void Focus(Camera view)
    {
        float distance = .78f * transform.lossyScale.y / (2 * Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f));
        view.transform.position = transform.position + transform.forward * distance;
        view.transform.LookAt(transform.position, transform.up);
    }
    public ShopWorldControl Find(WorldControlKind kind, int value = 0) => controls.First(c => c.Kind == kind && c.Value == value);
    public Rect Bounds(Camera view, WorldControlKind kind, int value = 0)
    {
        var collider = Find(kind, value).GetComponent<Collider>();
        Vector2 min = Vector2.one * float.MaxValue, max = Vector2.one * float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var b = collider.bounds;
            var p = view.WorldToScreenPoint(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
            if (p.z <= .01f) return Rect.zero;
            var point = new Vector2(p.x, Screen.height - p.y);
            min = Vector2.Min(min, point); max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    public void Refresh()
    {
        if (shop.Mobile != null && shop.Mobile.IsCurrent && shop.Stage != ServiceStage.Payment)
        {
            due.text = "TELEFONFELTÖLTÉS";
            entered.text = ShopCatalog.Money(shop.Mobile.Request.Amount);
            status.text = shop.Stage == ServiceStage.Thanks ? "SIKERES FELTÖLTÉS" : "E: ADATOK MEGADÁSA";
            return;
        }
        bool paying = shop.Stage == ServiceStage.Payment && shop.CardPayment;
        due.text = paying ? "FIZETENDŐ " + ShopCatalog.Money(shop.ActiveCustomer.TotalPrice) : "TRAFIK / BANKKÁRTYA";
        entered.text = shop.CardStatus == CardTerminalStatus.Approved ? ShopCatalog.Money(shop.LastApprovedCardAmount) : (string.IsNullOrEmpty(shop.CardAmount) ? "0" : shop.CardAmount) + " Ft";
        status.text = shop.CardStatus == CardTerminalStatus.Approved ? "SIKERES FIZETÉS" : shop.CardStatus == CardTerminalStatus.Error ? "HIBÁS ÖSSZEG" : paying ? "ÍRD BE AZ ÖSSZEGET" : "KÉSZENLÉT";
    }
    private void OnDestroy()
    {
        foreach (var material in materials) if (material != null) Destroy(material);
    }
}
