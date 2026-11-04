using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Original Blender model with live, world-space checkout labels.
public sealed class ShopRegisterVisuals : MonoBehaviour
{
    private Transform till;
    private CashRegisterDrawer drawer;
    private float framedAspect;
    public bool NeedsReframe(Camera camera) => Mathf.Abs(camera.aspect - framedAspect) > .01f;
    private TMP_FontAsset font;
    private Mesh keyMesh;
    private readonly List<Material> materials = new List<Material>();
    private TMP_Text dueTitle, due, received, change, selected;
    public string DisplayDue => due.text;
    public string DisplayReceived => received.text;
    public string DisplayChange => change.text;
    public string DisplaySelected => selected.text;
    public Transform Model { get; private set; }
    public const string Resource = "Equipment/ModernRegister/";

    public void Configure(Transform assembly, CashRegisterDrawer mechanism, Transform keyboard, Transform screen)
    {
        till = assembly;
        drawer = mechanism;
        font = Resources.Load<TMP_FontAsset>(Resource + "RegisterFont");
        keyMesh = Resources.Load<Mesh>(Resource + "Keycap_Template");
        var prefab = Resources.Load<GameObject>(Resource + "ModernRegister");
        if (font == null || prefab == null || keyMesh == null) throw new System.InvalidOperationException("Generate modern register assets before playing.");
        foreach (Transform child in till)
            if (child != keyboard && child != screen && child != mechanism.MovingDrawer) child.gameObject.SetActive(false);
        foreach (Transform child in keyboard) child.gameObject.SetActive(false);
        foreach (Transform child in mechanism.MovingDrawer)
            if (child.name != "Money compartments") child.gameObject.SetActive(false);

        Model = Instantiate(prefab, till, false).transform;
        var parts = new List<Transform>();
        foreach (Transform child in Model) parts.Add(child);
        foreach (var part in parts)
        {
            if (part.name.StartsWith("Drawer_"))
            {
                part.SetParent(mechanism.MovingDrawer, false);
                part.localPosition = -new Vector3(0, .041f, .015f);
            }
            else if (part.name == "Register_Shell" || part.name == "Display_Bezel" || part.name.StartsWith("Cabinet_"))
                part.gameObject.AddComponent<MeshCollider>().sharedMesh = part.GetComponent<MeshFilter>().sharedMesh;
        }
        keyboard.localPosition = new Vector3(0, .258f, .06f);
        keyboard.localRotation = Quaternion.Euler(18, 0, 0);
        screen.localRotation = Quaternion.Euler(-25, 0, 0);
        screen.localPosition = new Vector3(0, .49f, -.15f) + screen.localRotation * Vector3.forward * .034f;
        dueTitle = Label(screen, "Due title", "FIZETENDŐ", new Vector3(.145f, .074f, .002f), Quaternion.Euler(0, 180, 0), .020f, .185f, .035f, ShopUi.Ink);
        due = Label(screen, "Due amount", "", new Vector3(-.105f, .074f, .002f), Quaternion.Euler(0, 180, 0), .037f, .270f, .047f, ShopUi.Ink, true);
        Label(screen, "Received title", "KAPOTT", new Vector3(.145f, .024f, .002f), Quaternion.Euler(0, 180, 0), .020f, .185f, .035f, ShopUi.Ink);
        received = Label(screen, "Received amount", "", new Vector3(-.105f, .024f, .002f), Quaternion.Euler(0, 180, 0), .026f, .270f, .039f, ShopUi.Ink);
        Label(screen, "Change title", "VISSZAJÁRÓ", new Vector3(.145f, -.027f, .002f), Quaternion.Euler(0, 180, 0), .020f, .185f, .035f, ShopUi.Ink, true);
        change = Label(screen, "Change amount", "", new Vector3(-.105f, -.027f, .002f), Quaternion.Euler(0, 180, 0), .032f, .270f, .043f, ShopUi.Green, true);
        Label(screen, "Selected title", "KIVÁLASZTVA", new Vector3(.145f, -.078f, .002f), Quaternion.Euler(0, 180, 0), .020f, .185f, .035f, ShopUi.Ink);
        selected = Label(screen, "Selected amount", "", new Vector3(-.105f, -.078f, .002f), Quaternion.Euler(0, 180, 0), .026f, .270f, .039f, ShopUi.Ink);
    }

    public TMP_Text Label(Transform parent, string title, string text, Vector3 position, Quaternion rotation, float size, float width, float height, Color color, bool bold = false)
    {
        var item = new GameObject(title);
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localRotation = rotation;
        var label = item.AddComponent<TextMeshPro>();
        label.font = font;
        label.fontSharedMaterial = font.material;
        label.fontSize = size * 10;
        label.enableAutoSizing = true;
        label.fontSizeMax = label.fontSize;
        label.fontSizeMin = label.fontSize * .6f;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.rectTransform.sizeDelta = new Vector2(width, height);
        label.text = text;
        return label;
    }

    private Material Material(string title, Color color, bool unlit = false)
    {
        var material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")) { name = title, color = color };
        if (!unlit) material.SetFloat("_Smoothness", .2f);
        materials.Add(material);
        return material;
    }

    private GameObject KeyShape(Transform parent, string title, Vector3 position, Vector3 size, Material material)
    {
        var item = new GameObject(title);
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = size;
        item.AddComponent<MeshFilter>().sharedMesh = keyMesh;
        item.AddComponent<MeshRenderer>().sharedMaterial = material;
        item.AddComponent<BoxCollider>();
        return item;
    }

    public void BuildKeys(Transform keyboard)
    {
        var dark = Material("Register control keys", new Color(.12f, .16f, .145f));
        var green = Material("Register payment key", ShopUi.Green);
        string[] captions = { "JAVÍTÁS", "TÖRLÉS", "VÁLTÁS", "ÁTADÁS" };
        WorldControlKind[] kinds = { WorldControlKind.Undo, WorldControlKind.Clear, WorldControlKind.Exchange, WorldControlKind.Pay };
        for (int i = 0; i < kinds.Length; i++)
        {
            bool pay = i == 3;
            Vector3 position = pay ? new Vector3(0, .018f, .052f) : new Vector3((1 - i) * .135f, .018f, -.055f);
            var button = KeyShape(keyboard, "Register " + kinds[i], position, new Vector3(pay ? .39f : .12f, .027f, .072f), pay ? green : dark);
            button.AddComponent<ShopWorldControl>().Kind = kinds[i];
            Label(keyboard, "Key caption", captions[i], position + Vector3.up * .015f, Quaternion.Euler(90, 180, 0), pay ? .022f : .018f, pay ? .36f : .115f, .04f, ShopUi.Paper, true);
        }
    }

    public TMP_Text DenominationLabel(Transform drawer, int value, float x)
    {
        bool note = value >= 500;
        var rotation = Quaternion.Euler(70, 180, 0);
        var position = new Vector3(x, .073f, note ? -.145f : .151f);
        var plate = KeyShape(drawer, "Denomination label " + value, position, new Vector3(.078f, .037f, .004f), Material("Denomination paper", ShopUi.Paper, true));
        plate.transform.localRotation = rotation;
        var control = plate.AddComponent<ShopWorldControl>(); control.Kind = WorldControlKind.Denomination; control.Value = value;
        return Label(drawer, "Denomination " + value, "", position + rotation * Vector3.back * .003f, rotation, .022f, .075f, .036f, ShopUi.Ink, true);
    }

    public void Refresh(MvpShopController shop)
    {
        bool paying = shop.Stage == ServiceStage.Payment && shop.ActiveCustomer != null;
        dueTitle.text = paying ? "FIZETENDŐ" : "KASSZAEGYENLEG";
        due.text = ShopCatalog.Money(paying ? shop.ActiveCustomer.TotalPrice : shop.Ledger.Cash);
        received.text = paying && !shop.CardPayment ? ShopCatalog.Money(shop.ActiveCustomer.Tender) : "–";
        change.text = paying && !shop.CardPayment ? ShopCatalog.Money(shop.ActiveCustomer.Tender - shop.ActiveCustomer.TotalPrice) : "–";
        selected.text = paying && !shop.CardPayment ? ShopCatalog.Money(shop.ChangeSelected) : "–";
    }

    public void Focus(Camera camera)
    {
        framedAspect = camera.aspect;
        camera.fieldOfView = 42;
        Vector3 center = till.TransformPoint(new Vector3(0, .30f, .27f));
        Vector3 back = till.TransformDirection(new Vector3(0, .76f, .65f).normalized);
        camera.transform.rotation = Quaternion.LookRotation(-back, till.up);
        float vertical = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f);
        float horizontal = vertical * camera.aspect;
        float distance = 0;
        var renderers = new List<Renderer>(Model.GetComponentsInChildren<Renderer>());
        renderers.AddRange(drawer.MovingDrawer.GetComponentsInChildren<Renderer>());
        foreach (var renderer in renderers)
        {
            if (!renderer.gameObject.activeInHierarchy || renderer.GetComponent<MeshFilter>() == null) continue;
            var bounds = renderer.bounds;
            Vector3 offset = renderer.transform.IsChildOf(drawer.MovingDrawer) ? drawer.OpenPosition - drawer.MovingDrawer.position : Vector3.zero;
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)) + offset - center;
                float depth = Vector3.Dot(point, back);
                distance = Mathf.Max(distance, depth + Mathf.Abs(Vector3.Dot(point, camera.transform.right)) / horizontal,
                    depth + Mathf.Abs(Vector3.Dot(point, camera.transform.up)) / vertical);
            }
        }
        camera.transform.position = center + back * (distance * 1.06f);
    }

    private void OnDestroy()
    {
        foreach (var material in materials) if (material != null) Destroy(material);
    }
}
