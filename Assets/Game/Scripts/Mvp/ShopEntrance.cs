using System.Collections.Generic;
using UnityEngine;

// The entrance opens for both customers and the player. Only the purchased
// security shutter can block passage; the doorway remains an interaction target.
[DisallowMultipleComponent]
public sealed class ShopEntrance : MonoBehaviour
{
    private MvpShopController shop;
    private Transform player;
    private Transform root, hinge, signPivot;
    private float passageTime, angle;
    private readonly List<Material> materials = new List<Material>();
    public Vector3 Position => root.position;
    public bool IsOpen => angle > 5f;
    public BoxCollider PlayerBoundary { get; private set; }
    public Transform Sign => signPivot;
    public Transform DoorLeaf => hinge;

    public void Configure(MvpShopController owner)
    {
        shop = owner;
        player = FindAnyObjectByType<PlayerInteractor>().transform;
        var lintel = GameObject.Find("Door lintel").transform;
        float width = Mathf.Abs(lintel.lossyScale.x);
        float height = lintel.position.y - Mathf.Abs(lintel.lossyScale.y) * .5f;
        root = new GameObject("Shop Entrance").transform;
        root.SetParent(transform, false);
        root.position = new Vector3(lintel.position.x, 0, lintel.position.z);
        PlayerBoundary = root.gameObject.AddComponent<BoxCollider>();
        PlayerBoundary.center = new Vector3(0, height * .5f, 0);
        PlayerBoundary.size = new Vector3(width, height, .12f);
        PlayerBoundary.isTrigger = true;
        root.gameObject.AddComponent<ShopStation>().Configure(shop, ShopStationKind.Exit);
        var oldBoundary = GameObject.Find("Player Door Boundary");
        if (oldBoundary != null) oldBoundary.SetActive(false);

        hinge = new GameObject("NPC Door Hinge").transform;
        hinge.SetParent(root, false);
        hinge.localPosition = Vector3.left * (width * .5f - .04f);
        var paint = Material("Entrance paint", new Color(.14f, .23f, .21f));
        var handle = Material("Entrance handle", new Color(.63f, .64f, .60f));
        float leafWidth = width - .08f;
        Box("Door lower panel", new Vector3(leafWidth * .5f, .45f, 0), new Vector3(leafWidth, .86f, .065f), paint);
        foreach (float x in new[] { .055f, leafWidth - .055f })
            Box("Door stile", new Vector3(x, height * .5f, 0), new Vector3(.11f, height-.04f, .065f), paint);
        Box("Door top rail", new Vector3(leafWidth*.5f,height-.07f,0),new Vector3(leafWidth,.10f,.065f),paint);
        var glass = new Material(Shader.Find("Shop/PET")) { name="Entrance glass", color=new Color(.75f,.86f,.85f,.12f), renderQueue=3000 };
        materials.Add(glass);
        Box("Door glass",new Vector3(leafWidth*.5f,(height+.88f)*.5f,0),new Vector3(leafWidth-.22f,height-.98f,.009f),glass);
        Box("Door handle", new Vector3(leafWidth - .16f, 1.05f, .09f), new Vector3(.035f, .23f, .05f), handle);

        var sign = GameObject.Find("Open Close Sign");
        signPivot = new GameObject("Opening Sign Pivot").transform;
        signPivot.SetParent(hinge, false);
        signPivot.localPosition = new Vector3(leafWidth*.5f,1.5f,0);
        sign.transform.SetParent(signPivot, false);
        sign.transform.localPosition = Vector3.zero;
        sign.transform.localRotation = Quaternion.identity;
        // Reach the sign in front of the fixed player boundary on both sides.
        sign.GetComponent<BoxCollider>().size = new Vector3(1,1,3);
        var oldLabel = GameObject.Find("Nyitás felirat");
        if (oldLabel != null) oldLabel.SetActive(false);
        ProductVisuals.Text(signPivot, "Closed sign face", "ZÁRVA", new Vector3(0, 0, -.052f), Quaternion.identity, .065f, Color.white);
        ProductVisuals.Text(signPivot, "Open sign face", "NYITVA", new Vector3(0, 0, .052f), Quaternion.Euler(0,180,0), .065f, Color.white);
    }

    public void RequestPassage(Vector3 from)
    {
        if (shop.Security.Locked) return;
        var position = root.InverseTransformPoint(from);
        if (Mathf.Abs(position.x) < 1.2f && Mathf.Abs(position.z) < 1.6f)
            passageTime = .8f;
    }

    private void Update() => Tick(Time.deltaTime);
    public void Tick(float dt)
    {
        if (root == null || ShopMenu.Active || ShopInputGate.Blocked) return;
        passageTime = Mathf.Max(0, passageTime - dt);
        if (player != null) RequestPassage(player.position);
        angle = Mathf.MoveTowards(angle, passageTime > 0 && !shop.Security.Locked ? 100 : 0, 240 * dt);
        hinge.localRotation = Quaternion.Euler(0, angle, 0);
        signPivot.localRotation = Quaternion.RotateTowards(signPivot.localRotation,
            Quaternion.Euler(0, shop.Phase == ShopPhase.Open ? 180 : 0, 0), 360 * dt);
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
