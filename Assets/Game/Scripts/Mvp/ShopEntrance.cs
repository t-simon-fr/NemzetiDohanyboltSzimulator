using System.Collections.Generic;
using UnityEngine;

// NPCs control the leaf. A separate fixed collider keeps the player inside,
// even while a customer passes through the open entrance.
[DisallowMultipleComponent]
public sealed class ShopEntrance : MonoBehaviour
{
    private MvpShopController shop;
    private Transform root, hinge, signPivot;
    private float passageTime, angle;
    private readonly List<Material> materials = new List<Material>();
    public Vector3 Position => root.position;
    public bool IsOpen => angle > 5f;
    public BoxCollider PlayerBoundary { get; private set; }

    public void Configure(MvpShopController owner)
    {
        shop = owner;
        var lintel = GameObject.Find("Door lintel").transform;
        float width = Mathf.Abs(lintel.lossyScale.x);
        float height = lintel.position.y - Mathf.Abs(lintel.lossyScale.y) * .5f;
        root = new GameObject("Shop Entrance").transform;
        root.SetParent(transform, false);
        root.position = new Vector3(lintel.position.x, 0, lintel.position.z);
        PlayerBoundary = root.gameObject.AddComponent<BoxCollider>();
        PlayerBoundary.center = new Vector3(0, height * .5f, 0);
        PlayerBoundary.size = new Vector3(width, height, .12f);
        root.gameObject.AddComponent<ShopStation>().Configure(shop, ShopStationKind.Exit);
        var oldBoundary = GameObject.Find("Player Door Boundary");
        if (oldBoundary != null) oldBoundary.SetActive(false);

        hinge = new GameObject("NPC Door Hinge").transform;
        hinge.SetParent(root, false);
        hinge.localPosition = Vector3.left * (width * .5f - .04f);
        var paint = Material("Entrance paint", new Color(.14f, .23f, .21f));
        var panel = Material("Entrance panel", new Color(.38f, .44f, .40f));
        var handle = Material("Entrance handle", new Color(.63f, .64f, .60f));
        float leafWidth = width - .08f;
        Box("Door panel", new Vector3(leafWidth * .5f, height * .5f, 0), new Vector3(leafWidth, height - .04f, .065f), paint);
        Box("Door inset", new Vector3(leafWidth * .5f, height * .63f, .039f), new Vector3(leafWidth - .22f, height * .54f, .018f), panel);
        Box("Door handle", new Vector3(leafWidth - .16f, 1.05f, .09f), new Vector3(.035f, .23f, .05f), handle);

        var sign = GameObject.Find("Open Close Sign");
        signPivot = new GameObject("Opening Sign Pivot").transform;
        signPivot.SetParent(transform, false);
        signPivot.position = sign.transform.position;
        sign.transform.SetParent(signPivot, true);
        var oldLabel = GameObject.Find("Nyitás felirat");
        if (oldLabel != null) oldLabel.SetActive(false);
        ProductVisuals.Text(signPivot, "Closed sign face", "ZÁRVA\nE – Nyitás", new Vector3(0, 0, .052f), Quaternion.Euler(0, 180, 0), .065f, Color.white);
        ProductVisuals.Text(signPivot, "Open sign face", "NYITVA\nE – Zárás", new Vector3(0, 0, -.052f), Quaternion.identity, .065f, Color.white);
    }

    public void RequestPassage(Vector3 from)
    {
        if (shop.Security.Locked) return;
        var position = root.InverseTransformPoint(from);
        if (Mathf.Abs(position.x) < 1.2f && Mathf.Abs(position.z) < 1.6f)
            passageTime = .8f;
    }

    private void Update()
    {
        if (root == null || ShopMenu.Active || ShopInputGate.Blocked) return;
        passageTime = Mathf.Max(0, passageTime - Time.deltaTime);
        angle = Mathf.MoveTowards(angle, passageTime > 0 && !shop.Security.Locked ? 100 : 0, 240 * Time.deltaTime);
        hinge.localRotation = Quaternion.Euler(0, angle, 0);
        signPivot.localRotation = Quaternion.RotateTowards(signPivot.localRotation,
            Quaternion.Euler(0, shop.Phase == ShopPhase.Open ? 180 : 0, 0), 360 * Time.deltaTime);
    }

    private Material Material(string title, Color color)
    {
        var value = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = title, color = color };
        materials.Add(value);
        return value;
    }

    private void Box(string title, Vector3 position, Vector3 size, Material material)
    {
        var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = title;
        item.transform.SetParent(hinge, false);
        item.transform.localPosition = position;
        item.transform.localScale = size;
        item.GetComponent<Renderer>().sharedMaterial = material;
        item.GetComponent<Collider>().enabled = false;
        Destroy(item.GetComponent<Collider>());
    }

    private void OnDestroy()
    {
        foreach (var material in materials) if (material != null) Destroy(material);
    }
}
