using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Builds visual assets only. Never modifies or saves the authored shop scene.
public static class IngredientPackagingSetup
{
    private const string Art = "Assets/Game/Art/Ingredients";
    private const string Prefabs = "Assets/Game/Resources/Ingredients";

    [MenuItem("Nemzeti Dohánybolt/Build Ingredient Packaging Assets")]
    public static void CreateAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before building packaging assets.");
        Directory.CreateDirectory(Prefabs);
        AssetDatabase.Refresh();
        var paper = Material("AlbroPaper", new Color(.96f, .935f, .86f));
        var cap = Material("AlbroCap", new Color(.97f, .96f, .91f));
        var green = Material("AlbroGreen", new Color(.28f, .52f, .08f));
        var navy = Material("LaVazzeFoil", new Color(.035f, .065f, .16f), .26f);
        BuildMilk(paper, cap, green, Print("Albro"));
        BuildCoffee(navy, Print("LaVazze"));
        AssetDatabase.SaveAssets();
    }

    private static Material Material(string key, Color color, float smoothness = .15f)
    {
        string path = Art + "/" + key + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Print(string key)
    {
        string path = Art + "/" + key + "Front.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing ingredient artwork: " + path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.anisoLevel = 4;
        importer.SaveAndReimport();
        var material = Material(key + "Print", Color.white);
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void BuildMilk(Material paper, Material cap, Material green, Material print)
    {
        var root = new GameObject("Albro 1 L");
        try
        {
            Box(root, "Folded carton body", new Vector3(0, .14f, 0), new Vector3(.16f, .28f, .12f), paper);
            var roof = new MeshData();
            roof.Quad(new Vector3(-.08f,.28f,-.06f), new Vector3(.08f,.28f,-.06f), new Vector3(.08f,.33f,.015f), new Vector3(-.08f,.33f,.015f));
            roof.Quad(new Vector3(.08f,.28f,.06f), new Vector3(-.08f,.28f,.06f), new Vector3(-.08f,.33f,.015f), new Vector3(.08f,.33f,.015f));
            roof.Triangle(new Vector3(-.08f,.28f,.06f), new Vector3(-.08f,.28f,-.06f), new Vector3(-.08f,.33f,.015f));
            roof.Triangle(new Vector3(.08f,.28f,-.06f), new Vector3(.08f,.28f,.06f), new Vector3(.08f,.33f,.015f));
            Part(root, "Folded shoulders", roof.Save("AlbroShoulders"), paper);
            Box(root, "Sealed top fold", new Vector3(0,.338f,.015f), new Vector3(.16f,.018f,.005f), paper);
            var lid = Primitive(root, "Screw cap", PrimitiveType.Cylinder, new Vector3(.027f,.315f,-.021f), new Vector3(.047f,.007f,.047f), cap);
            lid.transform.localRotation = Quaternion.Euler(-34, 0, 0);
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2 / 16;
                var rib = Box(lid, "Cap rib", new Vector3(Mathf.Cos(angle)*.48f,0,Mathf.Sin(angle)*.48f), new Vector3(.025f,1.4f,.025f), cap);
                rib.transform.localRotation = Quaternion.Euler(0, -i*22.5f, 0);
            }
            foreach (float x in new[] { -.0801f, .0801f })
                Box(root, "Green side band", new Vector3(x,.025f,0), new Vector3(.001f,.05f,.12f), green);
            var labels = new MeshData();
            labels.Quad(new Vector3(-.08f,0,-.0602f),new Vector3(.08f,0,-.0602f),new Vector3(.08f,.28f,-.0602f),new Vector3(-.08f,.28f,-.0602f));
            labels.Quad(new Vector3(.08f,0,.0602f),new Vector3(-.08f,0,.0602f),new Vector3(-.08f,.28f,.0602f),new Vector3(.08f,.28f,.0602f));
            Part(root, "Albro printed panels", labels.Save("AlbroPanels"), print);
            Save(root, "Albro");
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void BuildCoffee(Material foil, Material print)
    {
        var root = new GameObject("La Vazze 250 g");
        try
        {
            // Tapered rings make a sealed, softly folded brick rather than a plain box.
            var widths = new[] { .17f, .194f, .20f, .196f, .182f, .194f };
            var depths = new[] { .075f, .094f, .10f, .094f, .074f, .018f };
            var heights = new[] { 0f, .012f, .06f, .20f, .224f, .24f };
            var body = new MeshData();
            for (int ring = 0; ring < heights.Length-1; ring++)
            {
                var lower = Ring(widths[ring], depths[ring], heights[ring]);
                var upper = Ring(widths[ring+1], depths[ring+1], heights[ring+1]);
                for (int side = 0; side < 4; side++)
                {
                    // The printed panels ARE the front/back surfaces, not overlays.
                    if (ring >= 1 && ring <= 3 && (side == 0 || side == 2)) continue;
                    body.Quad(lower[side], lower[(side+1)%4], upper[(side+1)%4], upper[side]);
                }
            }
            var bottom = Ring(widths[0], depths[0], 0);
            var top = Ring(widths[5], depths[5], .24f);
            body.Quad(bottom[3],bottom[2],bottom[1],bottom[0]);
            body.Quad(top[0],top[1],top[2],top[3]);
            Part(root, "Sealed coffee brick", body.Save("LaVazzeBody"), foil);
            Box(root, "Top crimp", new Vector3(0,.237f,0), new Vector3(.194f,.006f,.020f), foil);
            var labels = new MeshData();
            // Follow each foil fold: a single flat print would sink inside the bulging brick.
            for (int ring = 1; ring < 4; ring++)
            {
                float x0 = widths[ring]/2, x1 = widths[ring+1]/2;
                float z0 = depths[ring]/2, z1 = depths[ring+1]/2;
                float y0 = heights[ring], y1 = heights[ring+1];
                float v0 = Mathf.InverseLerp(.012f,.224f,y0), v1 = Mathf.InverseLerp(.012f,.224f,y1);
                labels.Quad(new Vector3(-x0,y0,-z0),new Vector3(x0,y0,-z0),new Vector3(x1,y1,-z1),new Vector3(-x1,y1,-z1),v0,v1);
                labels.Quad(new Vector3(x0,y0,z0),new Vector3(-x0,y0,z0),new Vector3(-x1,y1,z1),new Vector3(x1,y1,z1),v0,v1);
            }
            Part(root, "La Vazze printed panels", labels.Save("LaVazzePanels"), print);
            Save(root, "LaVazze");
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static Vector3[] Ring(float width, float depth, float y) => new[] {
        new Vector3(-width/2,y,-depth/2), new Vector3(width/2,y,-depth/2),
        new Vector3(width/2,y,depth/2), new Vector3(-width/2,y,depth/2)
    };

    private static void Save(GameObject root, string key)
        => PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/" + key + ".prefab");

    private static GameObject Primitive(GameObject root, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(root.transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }

    private static GameObject Box(GameObject root, string name, Vector3 position, Vector3 scale, Material material)
        => Primitive(root, name, PrimitiveType.Cube, position, scale, material);

    private static void Part(GameObject root, string name, Mesh mesh, Material material)
    {
        var part = new GameObject(name);
        part.transform.SetParent(root.transform, false);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        part.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private sealed class MeshData
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector2> uv = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float v0 = 0, float v1 = 1)
        {
            int start = vertices.Count;
            vertices.AddRange(new[] { a,b,c,d });
            uv.AddRange(new[] { new Vector2(0,v0),new Vector2(1,v0),new Vector2(1,v1),new Vector2(0,v1) });
            // Front-facing surfaces are viewed from -Z, matching product conventions.
            triangles.AddRange(new[] { start,start+2,start+1,start,start+3,start+2 });
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            int start = vertices.Count;
            vertices.AddRange(new[] { a,b,c });
            uv.AddRange(new[] { Vector2.zero,Vector2.right,Vector2.up });
            triangles.AddRange(new[] { start,start+2,start+1 });
        }

        public Mesh Save(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            string path = Art + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}
