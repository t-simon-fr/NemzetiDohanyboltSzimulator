using System;
using System.Collections.Generic;
using UnityEngine;

// Shared by the finite drawer, selected change and the customer's offered money.
// Original MNB image files stay intact; UVs select each face of the banknote sheet.
public sealed class ShopMoneyVisuals : MonoBehaviour
{
    public const string ResourcePath = "Currency/HUF/";
    private readonly Dictionary<string, Material> faces = new Dictionary<string, Material>();
    private readonly List<Mesh> meshes = new List<Mesh>();
    private Material paper, silver, gold;
    private Mesh noteTop, noteBottom, coinTop, coinBottom;

    public GameObject Create(Transform parent, int value, Vector3 position, float scale = 1)
    {
        if (Array.IndexOf(ShopCatalog.Denominations, value) < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        EnsureGeometry();
        bool banknote = value >= 500;
        var root = new GameObject((banknote ? "Bankjegy " : "Aprópénz ") + value);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = position;
        root.transform.localScale = Vector3.one * scale;
        Vector3 size = banknote ? new Vector3(.065f, .003f, .143f) : new Vector3(.043f, .003f, .043f);
        var collider = root.AddComponent<BoxCollider>();
        collider.size = size;

        var body = GameObject.CreatePrimitive(banknote ? PrimitiveType.Cube : PrimitiveType.Cylinder);
        body.name = "Edge";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = banknote ? size : new Vector3(size.x, size.y / 2, size.z);
        body.GetComponent<Renderer>().sharedMaterial = banknote ? paper : value == 5 || value == 20 || value == 200 ? gold : silver;
        var bodyCollider = body.GetComponent<Collider>();
        bodyCollider.enabled = false;
        Destroy(bodyCollider);
        Face(root.transform, "Front", banknote ? noteTop : coinTop, FaceMaterial(value, true));
        Face(root.transform, "Back", banknote ? noteBottom : coinBottom, FaceMaterial(value, false));
        return root;
    }

    private Material FaceMaterial(int value, bool front)
    {
        bool banknote = value >= 500;
        string resource = "Banknote_" + value;
        if (!banknote) resource = "Coin_" + value + (front ? "_Reverse" : "_Obverse");
        string key = resource + (front ? "/front" : "/back");
        if (faces.TryGetValue(key, out var material)) return material;
        var texture = Resources.Load<Texture2D>(ResourcePath + resource);
        if (texture == null) throw new InvalidOperationException("Missing MNB money image: " + resource);
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) {
            name = "MNB " + key, color = Color.white, mainTexture = texture
        };
        if (banknote)
        {
            material.mainTextureScale = new Vector2(1, .465f);
            material.mainTextureOffset = new Vector2(0, front ? .535f : 0);
        }
        faces.Add(key, material);
        return material;
    }

    private static void Face(Transform parent, string name, Mesh mesh, Material material)
    {
        var face = new GameObject(name);
        face.transform.SetParent(parent, false);
        face.AddComponent<MeshFilter>().sharedMesh = mesh;
        face.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private void EnsureGeometry()
    {
        if (noteTop != null) return;
        paper = EdgeMaterial("Paper edge", new Color(.83f, .81f, .72f));
        silver = EdgeMaterial("Silver edge", new Color(.57f, .59f, .58f));
        gold = EdgeMaterial("Brass edge", new Color(.65f, .53f, .26f));
        noteTop = NoteMesh(true); noteBottom = NoteMesh(false);
        coinTop = CoinMesh(true); coinBottom = CoinMesh(false);
    }

    private static Material EdgeMaterial(string name, Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
        material.SetFloat("_Smoothness", .25f);
        return material;
    }

    private Mesh NoteMesh(bool top)
    {
        float y = top ? .0016f : -.0016f;
        return Mesh(top ? "Banknote front" : "Banknote back", new[] {
            new Vector3(-.0325f, y, -.0715f), new Vector3(-.0325f, y, .0715f),
            new Vector3(.0325f, y, .0715f), new Vector3(.0325f, y, -.0715f)
        }, top ? new[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0) }
               : new[] { new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(1, 0) },
            top ? new[] { 0, 1, 2, 0, 2, 3 } : new[] { 0, 2, 1, 0, 3, 2 });
    }

    private Mesh CoinMesh(bool top)
    {
        const int segments = 64;
        var vertices = new Vector3[segments + 1];
        var uv = new Vector2[segments + 1];
        var triangles = new int[segments * 3];
        float y = top ? .0016f : -.0016f;
        vertices[0] = Vector3.up * y; uv[0] = Vector2.one * .5f;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2 / segments;
            float x = Mathf.Sin(a), z = Mathf.Cos(a);
            vertices[i + 1] = new Vector3(x * .0215f, y, z * .0215f);
            uv[i + 1] = new Vector2(.5f - x * .495f * (top ? 1 : -1), .5f - z * .495f);
            int next = (i + 1) % segments + 1;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = top ? i + 1 : next;
            triangles[i * 3 + 2] = top ? next : i + 1;
        }
        return Mesh(top ? "Coin reverse" : "Coin obverse", vertices, uv, triangles);
    }

    private Mesh Mesh(string name, Vector3[] vertices, Vector2[] uv, int[] triangles)
    {
        var mesh = new Mesh { name = name, vertices = vertices, uv = uv, triangles = triangles };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        meshes.Add(mesh);
        return mesh;
    }

    private void OnDestroy()
    {
        foreach (var material in faces.Values) Destroy(material);
        foreach (var mesh in meshes) Destroy(mesh);
        if (paper != null) Destroy(paper);
        if (silver != null) Destroy(silver);
        if (gold != null) Destroy(gold);
    }
}
