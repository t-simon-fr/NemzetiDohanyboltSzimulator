using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Runtime art pass: authored scene assets, stock and collision stay owned by their systems.
public sealed class ShopTrafikIdentity : MonoBehaviour
{
    private readonly List<Material> owned = new List<Material>();
    private readonly List<Mesh> meshes = new List<Mesh>();
    private Transform root;
    private Mesh roofMesh;
    private Transform rightGutter,rightDrain,chimney,chimneyCap;
    private readonly List<Transform> courses=new List<Transform>();
    private Material brown, cream, red, green, charcoal, plaster, clay;
    public Transform Facade { get; private set; }

    public void Configure(MvpShopController shop)
    {
        root = new GameObject("Trafik visual identity").transform;
        root.SetParent(transform, false);
        brown = Make("Trafik tobacco brown", ShopComputerTheme.Brown);
        cream = Make("Trafik warm cream", ShopComputerTheme.Paper);
        red = Make("Trafik brick red", ShopComputerTheme.Red);
        green = Make("Trafik deep green", ShopComputerTheme.Green);
        charcoal = Make("Trafik countertop", new Color(.17f, .18f, .17f));
        plaster = Make("Trafik plaster", new Color(.77f, .74f, .65f));
        clay = Make("Trafik clay roof", new Color(.43f, .22f, .14f));
        var authored = gameObject.scene.GetRootGameObjects().SelectMany(t => t.GetComponentsInChildren<Renderer>(true)).ToArray();
        foreach (var renderer in authored)
        {
            string n = renderer.name;
            if (n == "Exterior shop sign" || n == "Exterior shop title" || n == "Exterior shop subtitle" ||
                n == "Shop sign frame" || n == "Shop sign face") renderer.enabled = false;
            if (n == "Tobacco sign") renderer.enabled = false;
            if (n == "Counter body" || n == "Counter front" || n == "Rack upright" || n == "Rack back brace" ||
                n.StartsWith("Shelf ") && !n.StartsWith("Shelf price") || n == "Interior Wall Trim" || n == "Painted lower panel" || n == "Skirting board") renderer.sharedMaterial = brown;
            if (n == "Counter top") renderer.sharedMaterial = charcoal;
            if (n.Contains("wall") && !n.Contains("joke") && !(renderer is SpriteRenderer) && renderer.GetComponent<TextMesh>() == null)
                renderer.sharedMaterial = plaster;
            if (n == "Door lower panel" || n == "Door stile" || n == "Door top rail") renderer.sharedMaterial = brown;
            if (n == "Fridge back" || n == "Fridge side" || n == "Fridge roof" || n == "Compressor base") renderer.sharedMaterial = charcoal;
            if (n == "Closed sign face" || n == "Open sign face") renderer.GetComponent<TextMesh>().color = ShopComputerTheme.Paper;
        }
        var sign = GameObject.Find("Open Close Sign");
        if (sign != null) sign.GetComponent<Renderer>().sharedMaterial = brown;
        BuildFacade();
        BuildRoof();
        var leanRoof=Box(root,"Trafik stockroom lean roof",new Vector3(-4.6f,3.06f,-1.25f),new Vector3(3.95f,.12f,6.7f),clay);
        leanRoof.transform.localRotation=Quaternion.Euler(0,0,4);
        BuildInterior(shop);
        BuildVillageEdges();
        RefreshLayout(shop.Layout.Expanded);
    }

    private Material Make(string title, Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = title, color = color };
        material.SetFloat("_Smoothness", .12f);
        owned.Add(material);
        return material;
    }

    private GameObject Box(Transform parent, string name, Vector3 p, Vector3 size, Material material)
    {
        var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = p;
        item.transform.localScale = size;
        item.GetComponent<Renderer>().sharedMaterial = material;
        item.GetComponent<Collider>().enabled = false;
        Destroy(item.GetComponent<Collider>());
        return item;
    }

    private void Text(Transform parent, string name, string text, Vector3 p, float size, Quaternion rotation)
        => ProductVisuals.Text(parent, name, text, p, rotation, size, ShopComputerTheme.Paper);

    private void Stripe(Transform parent, Vector3 p, float width)
    {
        Box(parent, "Trafik red stripe", p + Vector3.up * .048f, new Vector3(width, .032f, .014f), red);
        Box(parent, "Trafik cream stripe", p + Vector3.up * .016f, new Vector3(width, .032f, .014f), cream);
        Box(parent, "Trafik green stripe", p - Vector3.up * .016f, new Vector3(width, .032f, .014f), green);
    }

    private void Roundel(Transform parent, Vector3 p, float radius, Quaternion rotation)
    {
        var badge = new GameObject("Trafik T roundel").transform;
        badge.SetParent(parent, false);
        badge.localPosition = p;
        badge.localRotation = rotation;
        Material[] layers = { brown, cream, red, cream, green };
        float[] sizes = { 1, .94f, .85f, .69f, .58f };
        for (int i = 0; i < layers.Length; i++)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Roundel ring";
            disc.transform.SetParent(badge, false);
            disc.transform.localPosition = new Vector3(0, 0, -.014f * i);
            disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
            disc.transform.localScale = new Vector3(radius * sizes[i] * 2, .009f, radius * sizes[i] * 2);
            disc.GetComponent<Renderer>().sharedMaterial = layers[i];
            disc.GetComponent<Collider>().enabled = false;
            Destroy(disc.GetComponent<Collider>());
        }
        Text(badge, "Trafik T", "T", new Vector3(0, 0, -.08f), radius * .80f, Quaternion.identity);
    }

    private void BuildFacade()
    {
        Facade = new GameObject("Trafik facade").transform;
        Facade.SetParent(root, false);
        Box(Facade, "Trafik fascia", new Vector3(0, 2.64f, -4.54f), new Vector3(5.12f, .64f, .10f), brown);
        Text(Facade, "Trafik main name", "NEMZETI DOHÁNYBOLT", new Vector3(0, 2.70f, -4.60f), .26f, Quaternion.identity);
        Stripe(Facade, new Vector3(0, 2.36f, -4.605f), 5.12f);
        // A shallow window treatment sits outside the existing solid lower facade.
        // The entrance and all player/NPC colliders remain unchanged.
        Box(Facade, "Trafik window frame", new Vector3(-1.06f, 1.20f, -4.50f), new Vector3(2.64f, 2.03f, .08f), brown);
        Box(Facade, "Trafik upper window", new Vector3(-1.06f, 1.77f, -4.55f), new Vector3(2.44f, .72f, .015f), charcoal);
        Box(Facade, "Trafik opaque window foil", new Vector3(-1.06f, .83f, -4.55f), new Vector3(2.44f, 1.04f, .016f), cream);
        Box(Facade, "Trafik sign bracket", new Vector3(-2.67f, 2.61f, -4.84f), new Vector3(.16f, .12f, .66f), charcoal);
        Roundel(Facade, new Vector3(-2.67f, 2.61f, -5.18f), .43f, Quaternion.identity);
        Box(Facade, "Trafik age plaque", new Vector3(.45f, .66f, -4.53f), new Vector3(.30f, .30f, .015f), brown);
        Text(Facade, "Trafik age", "18+", new Vector3(.45f, .66f, -4.55f), .075f, Quaternion.identity);
    }

    private void BuildRoof()
    {
        // A compact pitched roof over the full existing starter/extension shell.
        var vertices = new[] {
            new Vector3(-2.75f,3.12f,-4.70f),new Vector3(2,4.50f,-4.70f),new Vector3(6.75f,3.12f,-4.70f),
            new Vector3(-2.75f,3.12f,2.15f),new Vector3(2,4.50f,2.15f),new Vector3(6.75f,3.12f,2.15f)
        };
        var mesh = new Mesh { name = "Trafik pitched roof" };
        mesh.vertices = vertices;
        mesh.triangles = new[] { 0,3,1, 1,3,4, 1,4,2, 2,4,5, 0,1,2, 3,5,4 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshes.Add(mesh);roofMesh=mesh;
        var roof = new GameObject("Trafik pitched roof");
        roof.transform.SetParent(root, false);
        roof.AddComponent<MeshFilter>().sharedMesh = mesh;
        roof.AddComponent<MeshRenderer>().sharedMaterial = clay;
        chimney=Box(root, "Trafik chimney", new Vector3(4.9f, 4.05f, .7f), new Vector3(.5f, 1.4f, .6f), clay).transform;
        chimneyCap=Box(root, "Trafik chimney cap", new Vector3(4.9f, 4.77f, .7f), new Vector3(.63f, .12f, .73f), charcoal).transform;
        foreach (float x in new[] { -2.77f, 6.77f })
        {
            var gutter=Box(root, "Trafik gutter", new Vector3(x, 3.10f, -1.25f), new Vector3(.10f, .10f, 6.95f), charcoal);
            var drain=Box(root, "Trafik drainpipe", new Vector3(x, 1.55f, -4.35f), new Vector3(.08f, 3.10f, .08f), charcoal);
            if(x>0){rightGutter=gutter.transform;rightDrain=drain.transform;}
        }
        // Broad tile courses read at game distance without hundreds of tiny tiles.
        for (int row = 1; row < 9; row++)
        {
            float t = row / 9f;
            foreach (int side in new[] { -1, 1 })
                courses.Add(Box(root, "Trafik tile course", new Vector3(2 + side * 4.75f * t, 4.51f - 1.38f * t, -1.275f), new Vector3(.045f, .022f, 6.85f), clay).transform);
        }
    }

    public void RefreshLayout(bool expanded)
    {
        if(roofMesh==null)return;
        float right=expanded?6.75f:2.75f;
        float middle=(-2.75f+right)*.5f;
        float half=(right+2.75f)*.5f;
        var v=roofMesh.vertices;
        v[1].x=v[4].x=middle;v[2].x=v[5].x=right;
        roofMesh.vertices=v;roofMesh.RecalculateNormals();roofMesh.RecalculateBounds();
        rightGutter.localPosition=new Vector3(right+.02f,3.10f,-1.25f);
        rightDrain.localPosition=new Vector3(right+.02f,1.55f,-4.35f);
        float chimneyX=expanded?4.9f:1.4f;
        chimney.localPosition=new Vector3(chimneyX,4.05f,.7f);
        chimneyCap.localPosition=new Vector3(chimneyX,4.77f,.7f);
        for(int i=0;i<courses.Count;i++){
            float t=(i/2+1)/9f;int side=i%2==0?-1:1;
            courses[i].localPosition=new Vector3(middle+side*half*t,4.51f-1.38f*t,-1.275f);
        }
    }

    private void BuildInterior(MvpShopController shop)
    {
        // Follow the split authored wall, leaving the stockroom doorway unobstructed.
        foreach (string wallName in new[] { "Shop side wall front", "Shop side wall rear" })
        {
            var wall = shop.FloorPlan.Find(wallName).GetComponent<Renderer>().bounds;
            Box(root, "Trafik left wainscot " + wallName, new Vector3(wall.max.x + .018f, .48f, wall.center.z),
                new Vector3(.035f, .94f, wall.size.z), brown);
        }
        var counter = shop.Counter.transform;
        Box(counter, "Trafik counter trim", new Vector3(0, .22f, -.435f), new Vector3(3.30f, .025f, .012f), cream);
        foreach(var label in gameObject.scene.GetRootGameObjects().SelectMany(t=>t.GetComponentsInChildren<TextMesh>(true))) {
            if(label.text.Contains("türelmet"))label.text="ÖNKISZOLGÁLÓ";
            if(label.text.ToUpperInvariant().Contains("KIS BOLT. NAGY"))label.text="";
        }
        var hours = GameObject.Find("Opening hours");
        if (hours != null) hours.GetComponent<TextMesh>().text = "NYITÁS 08:00";
        var joke = GameObject.Find("Warehouse wall joke");
        if (joke != null) joke.SetActive(false);
    }

    private void BuildVillageEdges()
    {
        var timber = Make("Trafik fence wood", new Color(.27f, .23f, .16f));
        foreach (int side in new[] { -1, 1 })
        {
            float start = side == -1 ? -8.2f : 7.4f;
            for (int i = 0; i < 8; i++)
                Box(root, "Village fence slat", new Vector3(start + i * .20f, .58f, -4.72f), new Vector3(.12f, 1.12f, .045f), timber);
            Box(root, "Village fence rail", new Vector3(start + .7f, .40f, -4.70f), new Vector3(1.6f, .07f, .05f), timber);
        }
        Box(root, "Village bin", new Vector3(-3.15f, .55f, -5.12f), new Vector3(.34f, .65f, .26f), green);
        Box(root, "Village bin opening", new Vector3(-3.15f, .72f, -5.26f), new Vector3(.23f, .11f, .012f), charcoal);
    }

    private void OnDestroy()
    {
        foreach (var material in owned) if (material != null) Destroy(material);
        foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
    }
}
