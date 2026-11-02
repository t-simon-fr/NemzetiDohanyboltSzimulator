using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CigarettePackagingSetup
{
    const string Art = "Assets/Game/Art/CigarettePacks";
    const string Prefabs = "Assets/Game/Resources/CigarettePacks";
    static readonly string[] Keys = { "LuckyStroke", "Marlbroke", "Downhill", "Davidon" };

    [MenuItem("Nemzeti Dohánybolt/Apply Cigarette Packaging")]
    public static void UpgradeCurrentScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Exit Play mode first.");
        CreateAssets();
        var scene = SceneManager.GetActiveScene();
        var shop = Object.FindAnyObjectByType<MvpShopController>();
        if (shop == null) throw new System.InvalidOperationException("Open ShopPrototype first.");
        var serialized = new SerializedObject(shop);
        bool legacy = serialized.FindProperty("productMaterials").arraySize == 6;
        var oldSlots = ReadObjects<ProductSlot>(serialized.FindProperty("shelfSlots"));
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var rack = all.First(t => t.name == "Cigarette rack");
        var slots = oldSlots.Where(s => s != null && !s.transform.IsChildOf(rack) && !s.name.StartsWith("Extra cigarette slot ")).ToList();
        for (int row = 0; row < 4; row++)
        {
            var first = rack.Find("Cigarette slot " + row + "-0");
            float y = first != null ? first.localPosition.y : .28f + row * .42f;
            for (int col = 0; col < 8; col++)
            {
                string name = "Cigarette slot " + row + "-" + col;
                var cell = rack.Find(name);
                if (cell == null) { cell = new GameObject(name).transform; cell.SetParent(rack, false); }
                cell.localPosition = new Vector3((col - 3.5f) * .40f, y, 0);
                var slot = cell.GetComponent<ProductSlot>(); if (slot == null) slot = cell.gameObject.AddComponent<ProductSlot>();
                slot.Configure(new Vector3(.35f, .36f, .3f)); slot.RestrictTo(ShopCatalog.Ids[col / 2]); slots.Add(slot);
            }
        }
        // Existing public product IDs stay stable; only their catalog indices move by one.
        if (legacy)
            foreach (var station in all.Select(t => t.GetComponent<ShopStation>()).Where(s => s != null))
            {
                var so = new SerializedObject(station); var type = so.FindProperty("productType");
                if (type.intValue >= 3) { type.intValue++; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
        var previousGoods = ReadObjects<Material>(serialized.FindProperty("productMaterials"));
        var goods = new Material[ShopCatalog.Count];
        for (int i = 0; i < goods.Length; i++)
        {
            int oldIndex = legacy && i >= ShopCatalog.CigaretteCount ? i - 1 : i;
            goods[i] = i == 3 ? AssetDatabase.LoadAssetAtPath<Material>(Art + "/DavidonSide.mat") : (oldIndex < previousGoods.Length ? previousGoods[oldIndex] : AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/MAT_CompactProduct" + oldIndex + ".mat"));
        }
        int extraIndex = 0;
        foreach (var extra in all.Where(t => t.name.StartsWith("Extra cigarette slot ")).OrderBy(t => t.name))
        {
            var slot = extra.GetComponent<ProductSlot>(); slot.RestrictTo(ShopCatalog.Ids[extraIndex++ % ShopCatalog.CigaretteCount]);
            if (!slots.Contains(slot)) slots.Add(slot);
        }
        WriteObjects(serialized.FindProperty("shelfSlots"), slots.ToArray());
        var types = serialized.FindProperty("shelfTypes"); types.arraySize = slots.Count;
        for (int i = 0; i < slots.Count; i++)
        {
            var slotData = new SerializedObject(slots[i]);
            types.GetArrayElementAtIndex(i).intValue = ShopCatalog.Index(slotData.FindProperty("allowedProductId").stringValue);
        }
        WriteObjects(serialized.FindProperty("productMaterials"), goods); serialized.ApplyModifiedPropertiesWithoutUndo();
        var layout = new SerializedObject(shop.GetComponent<ShopLayout>());
        if (legacy)
        {
            var publicTypes = layout.FindProperty("publicTypes");
            for (int i = 0; i < publicTypes.arraySize; i++) publicTypes.GetArrayElementAtIndex(i).intValue++;
            layout.ApplyModifiedPropertiesWithoutUndo();
        }
        var warehouse = shop.GetComponent<WarehouseArea>();
        var storage = new SerializedObject(warehouse);
        var docks = ReadObjects<Transform>(storage.FindProperty("docks")).ToList();
        if (docks.Count == 6)
        {
            var dock = new GameObject("Brand Dock - Davidon").transform;
            dock.SetParent(docks[2].parent, false); dock.SetPositionAndRotation(docks[2].position + Vector3.up * .55f, docks[2].rotation);
            var selection = dock.gameObject.AddComponent<BoxCollider>(); selection.isTrigger = true; selection.size = new Vector3(.76f, .10f, .7f); selection.center = Vector3.down * .055f;
            ProductVisuals.Text(dock, "Brand dock label", "Davidon", new Vector3(0, -.055f, -.357f), Quaternion.identity, .042f, Color.black);
            docks.Insert(3, dock);
        }
        for (int i = 0; i < docks.Count; i++)
        {
            docks[i].name = "Brand Dock - " + ShopCatalog.Names[i];
            var station = docks[i].GetComponent<ShopStation>(); if (station == null) station = docks[i].gameObject.AddComponent<ShopStation>();
            station.ConfigureBrand(shop, i);
            var label = docks[i].Find("Brand dock label"); if (label != null) label.GetComponent<TextMesh>().text = ShopCatalog.Names[i];
        }
        WriteObjects(storage.FindProperty("docks"), docks.ToArray()); WriteObjects(storage.FindProperty("productMaterials"), goods); storage.ApplyModifiedPropertiesWithoutUndo();
        // Four labeled shelf sections; the counter and all shop/warehouse transforms stay in place.
        for (int brand = 0; brand < 4; brand++)
        {
            string name = "Brand price " + brand; var old = rack.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject);
            ProductVisuals.Text(rack, name, ShopCatalog.Names[brand] + "\n" + ShopCatalog.Money(ShopCatalog.Prices[brand]), new Vector3((brand - 1.5f) * .8f, 1.78f, -.055f), Quaternion.identity, .048f, Color.black);
        }
        if (rack.Find("Interior Rack Detail") != null) InteriorArtSetup.RefreshRackArt();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("CIGARETTE_PACKAGING_READY: 4 brands, 32 cigarette slots, 7 warehouse docks; existing layout preserved.");
    }

    public static void CreateAssets()
    {
        Directory.CreateDirectory(Prefabs); AssetDatabase.Refresh();
        string texturePath = Art + "/PackagingAtlas.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.None; importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Trilinear;
        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048; importer.anisoLevel = 2;
        importer.SaveAndReimport();
        var print = Material("PackagingPrint", Color.white); print.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath); EditorUtility.SetDirty(print);
        var seamMaterial = Material("LidSeam", new Color(.10f, .07f, .055f));
        var seam = SaveMesh("LidSeam", SeamMesh());
        for (int i = 0; i < Keys.Length; i++)
        {
            var mesh = SaveMesh(Keys[i], PackMesh(i));
            var side = Material(Keys[i] + "Side", ProductVisuals.Colors[i]);
            var go = new GameObject(ShopCatalog.Names[i]); go.transform.localScale = new Vector3(.16f, .26f, .08f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { print, side };
            var lid = new GameObject("Lid joint"); lid.transform.SetParent(go.transform, false);
            lid.AddComponent<MeshFilter>().sharedMesh = seam; lid.AddComponent<MeshRenderer>().sharedMaterial = seamMaterial;
            PrefabUtility.SaveAsPrefabAsset(go, Prefabs + "/" + Keys[i] + ".prefab"); Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();
    }
    static Material Material(string name, Color color)
    {
        string path = Art + "/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.color = color; m.SetFloat("_Smoothness", .12f); m.SetFloat("_Metallic", 0); EditorUtility.SetDirty(m); return m;
    }
    static Mesh SaveMesh(string name, Mesh mesh)
    {
        mesh.name = name; string path = Art + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(existing); return existing;
    }
    static T[] ReadObjects<T>(SerializedProperty property) where T : Object => Enumerable.Range(0, property.arraySize).Select(i => property.GetArrayElementAtIndex(i).objectReferenceValue as T).ToArray();
    static void WriteObjects<T>(SerializedProperty property, T[] objects) where T : Object
    { property.arraySize = objects.Length; for (int i = 0; i < objects.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i]; }

    static Vector3 Ring(int index, float y, float inset = 0)
    {
        const float b = .035f; float h = .5f - inset;
        var points = new[] { new Vector2(-h+b,-h),new Vector2(h-b,-h),new Vector2(h,-h+b),new Vector2(h,h-b),new Vector2(h-b,h),new Vector2(-h+b,h),new Vector2(-h,h-b),new Vector2(-h,-h+b) };
        return new Vector3(points[index % 8].x, y, points[index % 8].y);
    }
    static Mesh PackMesh(int brand)
    {
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var print = new List<int>(); var body = new List<int>();
        float[] levels = { -.5f, -.487f, .487f, .5f }; float[] insets = { .012f, 0, 0, .012f };
        Vector2 offset = new Vector2((brand % 2) * .5f, brand < 2 ? .5f : 0);
        for (int ring = 0; ring < 3; ring++) for (int edge = 0; edge < 8; edge++)
        {
            bool face = ring == 1 && (edge == 0 || edge == 4); int first = vertices.Count;
            vertices.AddRange(new[] { Ring(edge,levels[ring],insets[ring]), Ring(edge,levels[ring+1],insets[ring+1]), Ring(edge+1,levels[ring+1],insets[ring+1]), Ring(edge+1,levels[ring],insets[ring]) });
            const float gutter = .0025f;
            foreach (var corner in new[] { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) }) uv.Add(offset + Vector2.one * gutter + corner * (.5f - 2*gutter));
            (face ? print : body).AddRange(new[] { first,first+1,first+2,first,first+2,first+3 });
        }
        for (int cap = 0; cap < 2; cap++) for (int edge = 0; edge < 8; edge++)
        {
            int first = vertices.Count; float y = cap == 0 ? -.5f : .5f;
            vertices.Add(new Vector3(0,y,0)); vertices.Add(Ring(cap == 0 ? edge : edge+1,y,.012f)); vertices.Add(Ring(cap == 0 ? edge+1 : edge,y,.012f));
            uv.AddRange(new[] { Vector2.zero,Vector2.zero,Vector2.zero }); body.AddRange(new[] { first,first+1,first+2 });
        }
        var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.subMeshCount = 2;
        mesh.SetTriangles(print,0); mesh.SetTriangles(body,1); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
    static Mesh SeamMesh()
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        for (int edge = 0; edge < 8; edge++)
        {
            int first = vertices.Count;
            vertices.AddRange(new[] { Ring(edge,.388f,-.001f), Ring(edge,.392f,-.001f), Ring(edge+1,.392f,-.001f), Ring(edge+1,.388f,-.001f) });
            triangles.AddRange(new[] { first,first+1,first+2,first,first+2,first+3 });
        }
        var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
    [MenuItem("Nemzeti Dohánybolt/Preview Cigarette Packaging")]
    public static void CapturePreview()
    {
        var preview = new PreviewRenderUtility();
        try
        {
            preview.camera.fieldOfView = 34; preview.camera.nearClipPlane = .01f; preview.camera.farClipPlane = 10;
            preview.camera.transform.position = new Vector3(.28f,.28f,-1.15f); preview.camera.transform.LookAt(new Vector3(0,.12f,0));
            preview.camera.clearFlags = CameraClearFlags.SolidColor; preview.camera.backgroundColor = new Color(.73f,.70f,.65f);
            preview.lights[0].intensity = 1.2f; preview.lights[0].transform.rotation = Quaternion.Euler(35,-35,0);
            preview.lights[1].intensity = .55f; preview.ambientColor = new Color(.5f,.5f,.5f);
            for (int i = 0; i < 4; i++)
            {
                var item = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + Keys[i] + ".prefab"));
                item.transform.position = new Vector3((i-1.5f)*.23f,.13f,0); preview.AddSingleGO(item);
            }
            preview.BeginStaticPreview(new Rect(0,0,1600,900)); preview.Render(true);
            var image = preview.EndStaticPreview(); Directory.CreateDirectory("Builds/PackagingPreview"); File.WriteAllBytes("Builds/PackagingPreview/cigarette-packs.png", image.EncodeToPNG()); Object.DestroyImmediate(image);
        }
        finally { preview.Cleanup(); }
        Debug.Log("PACKAGING_PREVIEW: Builds/PackagingPreview/cigarette-packs.png");
    }
}
