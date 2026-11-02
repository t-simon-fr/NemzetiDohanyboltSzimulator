using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class DrinkPackagingSetup
{
    const string Art = "Assets/Game/Art/Drinks";
    const string Prefabs = "Assets/Game/Resources/Drinks";
    static readonly string[] Keys = { "ReturAqua", "LocaCola", "HamsterEnergy" };
    [MenuItem("Nemzeti Dohánybolt/Apply Drink Packaging")]
    public static void UpgradeCurrentScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        CreateAssets(); var scene = SceneManager.GetActiveScene();
        var shop = UnityEngine.Object.FindAnyObjectByType<MvpShopController>();
        if (shop == null) throw new InvalidOperationException("Open ShopPrototype first.");
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (var slot in all.Select(t => t.GetComponent<ProductSlot>()).Where(s => s != null))
        {
            var data = new SerializedObject(slot); var id = data.FindProperty("allowedProductId");
            if (id.stringValue == "gum") id.stringValue = "cola";
            if (id.stringValue == "beer") id.stringValue = "energy";
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (var text in all.Select(t => t.GetComponent<TextMesh>()).Where(t => t != null))
        {
            text.text = text.text.Replace("Buborék ásványvíz","ReturAqua 500 ml").Replace("Menta rágó","Loca Cola 330 ml").Replace("Pirkadat sör","Hamster Energy 500 ml");
            if (text.name == "Árcímke")
            {
                text.characterSize = .045f * .18f;
                for (int type = 4; type < ShopCatalog.Count; type++)
                    if (text.text.Contains(ShopCatalog.Names[type])) text.text = ShopCatalog.Names[type] + " • " + ShopCatalog.Money(ShopCatalog.Prices[type]);
            }
        }
        var fallback = new[] { AssetDatabase.LoadAssetAtPath<Material>(Art + "/WaterCore.mat"), AssetDatabase.LoadAssetAtPath<Material>(Art + "/ColaPaint.mat"), AssetDatabase.LoadAssetAtPath<Material>(Art + "/EnergyPaint.mat") };
        foreach (var owner in new UnityEngine.Object[] { shop, shop.GetComponent<WarehouseArea>() })
        {
            var data = new SerializedObject(owner); var materials = data.FindProperty("productMaterials");
            for (int type = 4; type < ShopCatalog.Count; type++) materials.GetArrayElementAtIndex(type).objectReferenceValue = fallback[type-4];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        var area = shop.GetComponent<WarehouseArea>();
        for (int type = 4; type < ShopCatalog.Count; type++)
        {
            var dock = area.Dock(type); dock.name = "Brand Dock - " + ShopCatalog.Names[type];
            var label = dock.Find("Brand dock label"); if (label != null) label.GetComponent<TextMesh>().text = ShopCatalog.Names[type];
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("DRINK_PACKAGING_READY: ReturAqua 500 ml, Loca Cola 330 ml, Hamster Energy 500 ml.");
    }
    public static void CreateAssets()
    {
        Directory.CreateDirectory(Prefabs); AssetDatabase.Refresh();
        var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "/DrinkLabels.png");
        importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true; importer.alphaSource = TextureImporterAlphaSource.None;
        importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Trilinear;
        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048; importer.anisoLevel = 2; importer.SaveAndReimport();
        var label = Material("DrinkLabels", Color.white, .25f, .15f); label.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/DrinkLabels.png"); EditorUtility.SetDirty(label);
        var silver = Material("Aluminium", new Color(.65f,.68f,.70f), .65f, .75f);
        var dark = Material("CanOpening", new Color(.025f,.028f,.03f), .1f);
        var red = Material("ColaPaint", new Color(.65f,.015f,.025f), .4f, .4f);
        var black = Material("EnergyPaint", new Color(.016f,.019f,.018f), .25f, .3f);
        var can = Lathe("CanShell", new[] { V(0,-.49f),V(.36f,-.49f),V(.43f,-.47f),V(.49f,-.42f),V(.5f,-.38f),V(.5f,.35f),V(.45f,.43f),V(.43f,.46f) }, 32);
        var rims = Lathe("CanRims", new[] { V(.43f,.46f),V(.46f,.47f),V(.46f,.49f),V(.43f,.50f),V(.41f,.49f) },32);
        var lid = Lathe("CanLid", new[] { V(.435f,.478f),V(0,.478f) },32);
        var bottom = Lathe("CanBottom", new[] { V(0,-.485f),V(.43f,-.485f) },32);
        var opening = Lathe("Opening",new[] { V(.15f,0),V(0,0) },24);
        var tab = Lathe("PullTab",new[] { V(1,.006f),V(.55f,.006f),V(.55f,-.006f),V(1,-.006f),V(1,.006f) },24);
        for (int index = 1; index <= 2; index++)
        {
            var root = new GameObject(ShopCatalog.Names[index+4]);
            Part(root,"Painted can",can,index == 1 ? red : black);
            Part(root,"Rolled aluminium rim",rims,silver); Part(root,"Metal lid",lid,silver); Part(root,"Metal bottom",bottom,silver);
            var hole = Part(root,"Drinking opening",opening,dark); hole.transform.localPosition = new Vector3(0,.482f,-.18f); hole.transform.localScale = new Vector3(1,1,1.15f);
            var pull = Part(root,"Pull tab",tab,silver); pull.transform.localPosition = new Vector3(0,.492f,.045f); pull.transform.localScale = new Vector3(.12f,1,.20f);
            Part(root,"Printed wrap",Lathe(Keys[index]+"Label",new[] { V(.501f,-.38f),V(.501f,.35f) },64,index-1),label);
            SavePrefab(root,index);
        }
        var blue = Material("BottleCapBlue", new Color(.025f,.20f,.60f), .3f);
        var water = Material("WaterCore",new Color(.57f,.76f,.87f), .6f);
        var plastic = Material("ClearPET",new Color(.68f,.85f,.96f,.18f), .8f);
        plastic.shader = Shader.Find("Shop/PET"); plastic.color = new Color(.68f,.85f,.96f,.18f);
        plastic.shaderKeywords = new string[0]; plastic.renderQueue = (int)RenderQueue.Transparent; EditorUtility.SetDirty(plastic);
        var bottle = new GameObject(ShopCatalog.Names[4]);
        Part(bottle,"Faceted clear PET",Lathe("BottleShell",new[] { V(0,-.5f),V(.32f,-.5f),V(.43f,-.47f),V(.49f,-.42f),V(.48f,-.34f),V(.5f,-.29f),V(.5f,.14f),V(.47f,.20f),V(.35f,.29f),V(.22f,.36f),V(.22f,.41f),V(0,.41f) },24,-1,true),plastic);
        Part(bottle,"Water inside",Lathe("WaterVolume",new[] { V(0,-.46f),V(.36f,-.46f),V(.455f,-.40f),V(.455f,.16f),V(.40f,.21f),V(0,.21f) },24),water);
        Part(bottle,"Mineral water label",Lathe("ReturAquaLabel",new[] { V(.503f,-.23f),V(.503f,.10f) },48,2),label);
        Part(bottle,"Ribbed blue screw cap",Lathe("BottleCap",new[] { V(0,.385f),V(.25f,.385f),V(.27f,.40f),V(.258f,.414f),V(.27f,.428f),V(.258f,.442f),V(.27f,.456f),V(.258f,.47f),V(.27f,.484f),V(.25f,.5f),V(0,.5f) },24,-1,true),blue);
        SavePrefab(bottle,0); AssetDatabase.SaveAssets();
    }
    static Vector2 V(float radius,float y) => new Vector2(radius,y);
    static Material Material(string name,Color color,float smoothness,float metallic = 0)
    {
        string path = Art + "/" + name + ".mat"; var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
        mat.color = color; mat.SetFloat("_Smoothness",smoothness); mat.SetFloat("_Metallic",metallic); EditorUtility.SetDirty(mat); return mat;
    }
    static GameObject Part(GameObject root,string name,Mesh mesh,Material material)
    {
        var go = new GameObject(name); go.transform.SetParent(root.transform,false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = material; return go;
    }
    static void SavePrefab(GameObject root,int index)
    {
        root.transform.localScale = ProductVisuals.Size(index+4);
        PrefabUtility.SaveAsPrefabAsset(root,Prefabs + "/" + Keys[index] + ".prefab"); UnityEngine.Object.DestroyImmediate(root);
    }
    static Mesh Lathe(string name,Vector2[] profile,int segments,int labelRow = -1,bool faceted = false)
    {
        var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
        float min = profile.Min(p=>p.y),height = Mathf.Max(.001f,profile.Max(p=>p.y)-min);
        for (int row = 0; row < profile.Length-1; row++) for (int segment = 0; segment < segments; segment++)
        {
            int first = vertices.Count; float u0 = (float)segment/segments,u1 = (float)(segment+1)/segments;
            foreach (var corner in new[] { new Vector2(u0,row),new Vector2(u0,row+1),new Vector2(u1,row+1),new Vector2(u1,row) })
            {
                var p = profile[(int)corner.y]; float angle = 2*Mathf.PI*(corner.x-.5f);
                vertices.Add(new Vector3(Mathf.Sin(angle)*p.x,p.y,-Mathf.Cos(angle)*p.x));
                float dy = profile[row+1].y-profile[row].y,dr = profile[row+1].x-profile[row].x;
                normals.Add(new Vector3(Mathf.Sin(angle)*dy,-dr,-Mathf.Cos(angle)*dy).normalized);
                float v = (p.y-min)/height;
                uv.Add(labelRow < 0 ? new Vector2(corner.x,v) : new Vector2(Mathf.Lerp(.002f,.998f,corner.x),(2-labelRow)/3f + Mathf.Lerp(.002f,1f/3f-.002f,v)));
            }
            triangles.AddRange(new[] {first,first+1,first+2,first,first+2,first+3});
        }
        var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0);
        if (faceted) mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = Art + "/" + name + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { AssetDatabase.CreateAsset(mesh,path); return mesh; }
        EditorUtility.CopySerialized(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(existing); return existing;
    }
    [MenuItem("Nemzeti Dohánybolt/Preview Drink Packaging")]
    public static void CapturePreview()
    {
        var preview = new PreviewRenderUtility();
        try
        {
            preview.camera.fieldOfView = 34; preview.camera.nearClipPlane = .01f; preview.camera.farClipPlane = 10;
            preview.camera.transform.position = new Vector3(.06f,.28f,-1.02f); preview.camera.transform.LookAt(new Vector3(0,.16f,0));
            preview.camera.clearFlags = CameraClearFlags.SolidColor; preview.camera.backgroundColor = new Color(.73f,.70f,.65f);
            preview.lights[0].intensity = 1.2f; preview.lights[0].transform.rotation = Quaternion.Euler(35,-35,0); preview.lights[1].intensity = .6f; preview.ambientColor = new Color(.5f,.5f,.5f);
            int[] order = { 1,2,0 };
            for (int i = 0; i < order.Length; i++)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + Keys[order[i]] + ".prefab"));
                model.transform.position = new Vector3((i-1)*.23f,ProductVisuals.Size(order[i]+4).y/2,0);
                model.transform.rotation = Quaternion.LookRotation(new Vector3(model.transform.position.x-preview.camera.transform.position.x,0,model.transform.position.z-preview.camera.transform.position.z));
                preview.AddSingleGO(model);
            }
            preview.BeginStaticPreview(new Rect(0,0,1500,1000)); preview.Render(true); var image = preview.EndStaticPreview();
            Directory.CreateDirectory("Builds/DrinkPreview"); File.WriteAllBytes("Builds/DrinkPreview/drinks.png",image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
        }
        finally { preview.Cleanup(); }
    }
}
