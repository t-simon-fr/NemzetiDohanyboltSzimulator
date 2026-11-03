using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class AlcoholPackagingSetup
{
    const string Art="Assets/Game/Art/Drinks";
    static Vector2 V(float r,float y)=>new Vector2(r,y);
    static Mesh Shape(string name,params Vector2[] profile)=>DrinkPackagingSetup.Lathe(name,profile,64);
    static GameObject Part(GameObject root,string name,Mesh mesh,Material material)=>DrinkPackagingSetup.Part(root,name,mesh,material);
    static Material Mat(string name,Color color,float smooth=.3f,float metallic=0)
    {
        string path=Art+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.color=color;mat.SetFloat("_Smoothness",smooth);mat.SetFloat("_Metallic",metallic);EditorUtility.SetDirty(mat);return mat;
    }
    static Material Label(string key)
    {
        string path=Art+"/"+key+"Label.png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        if(importer==null)throw new InvalidOperationException("Missing alcohol label: "+path);
        importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.SaveAndReimport();
        var mat=Mat(key+"Print",Color.white,.35f);mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);EditorUtility.SetDirty(mat);return mat;
    }
    [MenuItem("Nemzeti Dohánybolt/Apply Alcohol Products")]
    public static void UpgradeCurrentScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play mode first.");
        CreateAssets();var shop=Object.FindAnyObjectByType<MvpShopController>();if(shop==null)throw new InvalidOperationException("Open ShopPrototype first.");
        var area=shop.GetComponent<WarehouseArea>();var areaData=new SerializedObject(area);var docks=areaData.FindProperty("docks");
        int previous=docks.arraySize;docks.arraySize=ShopCatalog.Count;
        for(int type=7;type<ShopCatalog.Count;type++)
        {
            if(type<previous&&docks.GetArrayElementAtIndex(type).objectReferenceValue!=null)continue;
            var below=area.Dock(type-3);var dock=new GameObject("Brand Dock - "+ShopCatalog.Names[type]).transform;
            dock.SetParent(below.parent,false);dock.SetPositionAndRotation(below.position+Vector3.up*.55f,below.rotation);
            dock.gameObject.AddComponent<ShopStation>().ConfigureBrand(shop,type);
            var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.name="Brand shelf label plate";plate.transform.SetParent(dock,false);
            plate.transform.localPosition=new Vector3(0,-.055f,-.35f);plate.transform.localScale=new Vector3(.76f,.11f,.008f);
            plate.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/MAT_CartonTape.mat");
            ProductVisuals.Text(dock,"Brand dock label",ShopCatalog.Names[type],new Vector3(0,-.055f,-.357f),Quaternion.identity,.042f,Color.black);
            docks.GetArrayElementAtIndex(type).objectReferenceValue=dock;
        }
        areaData.ApplyModifiedPropertiesWithoutUndo();
        foreach(var owner in new Object[]{shop,area})
        {
            var data=new SerializedObject(owner);var materials=data.FindProperty("productMaterials");materials.arraySize=ShopCatalog.Count;
            materials.GetArrayElementAtIndex(7).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>(Art+"/AmberLiquor.mat");
            materials.GetArrayElementAtIndex(8).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>(Art+"/BeerGreen.mat");data.ApplyModifiedPropertiesWithoutUndo();
        }
        var layout=shop.GetComponent<ShopLayout>();var dataLayout=new SerializedObject(layout);
        var publicSlots=dataLayout.FindProperty("publicSlots");var publicTypes=dataLayout.FindProperty("publicTypes");
        var scene=SceneManager.GetActiveScene();var rack=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Public shelf");
        for(int type=7;type<ShopCatalog.Count;type++)
        {
            if(Enumerable.Range(0,publicTypes.arraySize).Any(i=>publicTypes.GetArrayElementAtIndex(i).intValue==type))continue;
            float y=.3f+(type-4)*.46f;var board=GameObject.CreatePrimitive(PrimitiveType.Cube);board.name="Shelf "+(type-4);board.transform.SetParent(rack,false);
            board.transform.localPosition=new Vector3(0,y,0);board.transform.localScale=new Vector3(.55f,.06f,3.3f);
            board.GetComponent<Renderer>().sharedMaterial=rack.Find("Shelf 2").GetComponent<Renderer>().sharedMaterial;
            for(int col=0;col<6;col++)
            {
                var go=new GameObject("Public slot "+(type-4)+"-"+col);go.transform.SetParent(rack,false);go.transform.localPosition=new Vector3(0,y+.03f,(col-2.5f)*.5f);go.transform.localRotation=Quaternion.Euler(0,90,0);
                var slot=go.AddComponent<ProductSlot>();slot.Configure(new Vector3(.4f,.38f,.4f));slot.RestrictTo(ShopCatalog.Ids[type]);
                int index=publicSlots.arraySize;publicSlots.arraySize=index+1;publicTypes.arraySize=index+1;
                publicSlots.GetArrayElementAtIndex(index).objectReferenceValue=slot;publicTypes.GetArrayElementAtIndex(index).intValue=type;
            }
            ProductVisuals.Text(rack,"Árcímke",ShopCatalog.Names[type]+" • "+ShopCatalog.Money(ShopCatalog.Prices[type]),new Vector3(-.29f,y-.06f,0),Quaternion.Euler(0,90,0),.045f,Color.black);
        }
        foreach(var text in rack.GetComponentsInChildren<TextMesh>(true).Where(t=>t.name=="Árcímke"))text.characterSize=.045f*.18f;
        for(int type=4;type<ShopCatalog.Count;type++)
        {
            string name="Product price plate "+type;if(rack.Find(name)!=null)continue;
            var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.name=name;plate.transform.SetParent(rack,false);
            plate.transform.localPosition=new Vector3(-.282f,.24f+(type-4)*.46f,0);plate.transform.localScale=new Vector3(.008f,.10f,3.0f);
            plate.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/MAT_CartonTape.mat");
        }
        var back=rack.Find("Shelf back");back.localPosition=new Vector3(.22f,1.275f,0);back.localScale=new Vector3(.08f,2.55f,3.3f);
        dataLayout.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("ALCOHOL_READY: Kevert 50 ml and Kőfejtői 500 ml; five public shelf rows, nine warehouse brands.");
    }
    public static void CreateAssets()
    {
        var amber=Mat("AmberLiquor",new Color(.87f,.22f,.015f),.72f);var cap=Mat("BlackScrewCap",new Color(.025f,.024f,.022f),.36f);
        var glass=Mat("ClearGlass",new Color(.94f,.91f,.82f,.2f),.9f);glass.shader=Shader.Find("Shop/PET");glass.color=new Color(.94f,.91f,.82f,.2f);glass.renderQueue=(int)RenderQueue.Transparent;EditorUtility.SetDirty(glass);
        var bottle=new GameObject("Kevert 50 ml");
        Part(bottle,"Glass bottle",Shape("KevertGlass",V(0,-.5f),V(.35f,-.5f),V(.47f,-.48f),V(.5f,-.44f),V(.5f,.04f),V(.47f,.11f),V(.34f,.20f),V(.28f,.28f),V(.27f,.39f),V(0,.39f)),glass);
        Part(bottle,"Amber liquor",Shape("KevertLiquid",V(0,-.46f),V(.43f,-.46f),V(.462f,-.42f),V(.462f,.035f),V(.43f,.09f),V(.32f,.18f),V(0,.18f)),amber);
        Part(bottle,"Orange paper label",Shape("KevertWrap",V(.504f,-.43f),V(.504f,-.035f)),Label("Kevert"));
        var profile=new System.Collections.Generic.List<Vector2>{V(0,.365f),V(.29f,.365f),V(.31f,.39f)};
        for(int i=0;i<9;i++)profile.Add(V(i%2==0?.315f:.30f,.395f+i*.011f));profile.Add(V(.29f,.5f));profile.Add(V(0,.5f));
        Part(bottle,"Ribbed black screw cap",Shape("KevertCap",profile.ToArray()),cap);Save(bottle,"Kevert",7);
        var beer=new GameObject("Kőfejtői 500 ml");var green=Mat("BeerGreen",new Color(.05f,.28f,.10f),.38f,.35f);
        Part(beer,"Green aluminium can",AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/CanShell.asset"),green);
        var silver=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Aluminium.mat");
        foreach(var pair in new[]{new[]{"Rolled rim","CanRims"},new[]{"Metal lid","CanLid"},new[]{"Metal bottom","CanBottom"}})Part(beer,pair[0],AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/"+pair[1]+".asset"),silver);
        var hole=Part(beer,"Drinking opening",AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/Opening.asset"),AssetDatabase.LoadAssetAtPath<Material>(Art+"/CanOpening.mat"));hole.transform.localPosition=new Vector3(0,.482f,-.18f);hole.transform.localScale=new Vector3(1,1,1.15f);
        var tab=Part(beer,"Pull tab",AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/PullTab.asset"),silver);tab.transform.localPosition=new Vector3(0,.492f,.045f);tab.transform.localScale=new Vector3(.12f,1,.20f);
        Part(beer,"Kőfejtői printed wrap",Shape("KofejtoiWrap",V(.501f,-.38f),V(.501f,.35f)),Label("Kofejtoi"));Save(beer,"Kofejtoi",8);AssetDatabase.SaveAssets();
    }
    static void Save(GameObject root,string key,int type){root.transform.localScale=ProductVisuals.Size(type);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Game/Resources/Drinks/"+key+".prefab");Object.DestroyImmediate(root);}
    public static void CapturePreview()
    {
        var preview=new PreviewRenderUtility();
        try
        {
            preview.camera.fieldOfView=33;preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=10;
            preview.camera.transform.position=new Vector3(.02f,.22f,-.85f);preview.camera.transform.LookAt(new Vector3(0,.15f,0));
            preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.72f,.70f,.65f);
            preview.lights[0].intensity=1.25f;preview.lights[0].transform.rotation=Quaternion.Euler(35,-35,0);preview.lights[1].intensity=.7f;preview.ambientColor=new Color(.5f,.5f,.5f);
            for(int i=0;i<2;i++){var model=Object.Instantiate(ProductVisuals.LoadModel(7+i));model.transform.position=new Vector3(i==0?-.115f:.10f,ProductVisuals.Size(7+i).y*.5f,0);preview.AddSingleGO(model);}
            preview.BeginStaticPreview(new Rect(0,0,1200,1000));preview.Render(true);var image=preview.EndStaticPreview();Directory.CreateDirectory("Builds/KeyboardAlcoholRelease");File.WriteAllBytes("Builds/KeyboardAlcoholRelease/alcohol.png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }
        finally{preview.Cleanup();}
    }
}
