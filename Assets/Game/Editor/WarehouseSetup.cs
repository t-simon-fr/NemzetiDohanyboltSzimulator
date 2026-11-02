using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarehouseSetup
{
    [MenuItem("Nemzeti Dohánybolt/Add Warehouse And Rear Yard")]
    public static void AddToCurrentScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var scene=SceneManager.GetActiveScene();
        if(scene.GetRootGameObjects().Any(r=>r.name=="Warehouse And Rear Yard"))return;
        var shop=Object.FindAnyObjectByType<MvpShopController>();if(shop==null)throw new System.InvalidOperationException("Open the shop scene first.");
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var oldWall=all.First(t=>t.name=="Left wall");oldWall.gameObject.SetActive(false);
        var oldCrate=all.FirstOrDefault(t=>t.name=="Supply Crate");if(oldCrate!=null)Object.DestroyImmediate(oldCrate.gameObject);
        Material wall=Mat("MAT_WarehouseWall",new Color(.65f,.66f,.61f));
        Material floor=Mat("MAT_WarehouseFloor",new Color(.30f,.32f,.30f));
        Material cardboard=Mat("MAT_Cardboard",new Color(.58f,.39f,.22f));
        Material tape=Mat("MAT_CartonTape",new Color(.80f,.73f,.48f));
        Material green=Mat("MAT_RecyclingBin",new Color(.10f,.29f,.19f));
        Material dark=Mat("MAT_WarehouseMetal",new Color(.10f,.12f,.12f));
        var root=new GameObject("Warehouse And Rear Yard").transform;
        // Split the existing cashier-side wall, leaving a 1.3 m doorway into the staff store.
        Box(root,"Shop side wall front",new Vector3(-2.5f,1.5f,-2.525f),new Vector3(.15f,3,3.75f),wall);
        Box(root,"Shop side wall rear",new Vector3(-2.5f,1.5f,1.275f),new Vector3(.15f,3,1.25f),wall);
        Box(root,"Warehouse entrance lintel",new Vector3(-2.5f,2.65f,0),new Vector3(.15f,.7f,1.3f),wall);
        Box(root,"Warehouse floor",new Vector3(-4.25f,-.1f,0),new Vector3(3.5f,.2f,4),floor);
        Box(root,"Warehouse ceiling",new Vector3(-4.25f,3.05f,0),new Vector3(3.5f,.1f,4),wall);
        Box(root,"Warehouse outer wall",new Vector3(-6,1.5f,0),new Vector3(.15f,3,4),wall);
        Box(root,"Warehouse front wall",new Vector3(-4.25f,1.5f,-2),new Vector3(3.5f,3,.15f),wall);
        Box(root,"Rear wall left",new Vector3(-5.45f,1.5f,2),new Vector3(1.1f,3,.15f),wall);
        Box(root,"Rear wall right",new Vector3(-3.05f,1.5f,2),new Vector3(1.1f,3,.15f),wall);
        Box(root,"Rear door lintel",new Vector3(-4.25f,2.65f,2),new Vector3(1.3f,.7f,.15f),wall);
        Box(root,"Yard floor",new Vector3(-4.25f,-.1f,3.8f),new Vector3(3.5f,.2f,3.6f),floor);
        Box(root,"Yard fence left",new Vector3(-6, .95f,3.8f),new Vector3(.12f,1.9f,3.6f),dark);
        Box(root,"Yard fence right",new Vector3(-2.5f,.95f,3.8f),new Vector3(.12f,1.9f,3.6f),dark);
        Box(root,"Yard fence rear",new Vector3(-4.25f,.95f,5.6f),new Vector3(3.5f,1.9f,.12f),dark);
        ProductVisuals.Text(root,"Warehouse entrance sign","RAKTÁR",new Vector3(-2.40f,2.62f,0),Quaternion.Euler(0,-90,0),.12f,Color.black);
        ProductVisuals.Text(root,"Rear door sign","HÁTSÓ UDVAR\nPAPÍRGYŰJTŐ →",new Vector3(-4.25f,2.58f,1.89f),Quaternion.identity,.08f,Color.black);
        ProductVisuals.Text(root,"Warehouse wall joke","A doboz nem párolog el.\nSajnos a főnök sem.",new Vector3(-3.4f,1.9f,-1.91f),Quaternion.Euler(0,180,0),.07f,Color.black);
        var area=shop.gameObject.AddComponent<WarehouseArea>();var docks=new Transform[ShopCatalog.Count];
        for(int i=0;i<docks.Length;i++)
        {
            var dock=new GameObject("Brand Dock - "+ShopCatalog.Names[i]).transform;dock.SetParent(root,false);
            if(i<3){dock.localPosition=new Vector3(-5.45f+i*1.2f,.75f,-1.45f);dock.localRotation=Quaternion.Euler(0,180,0);}
            else{dock.localPosition=new Vector3(-5.55f,.75f,-.35f+(i-3)*.9f);dock.localRotation=Quaternion.Euler(0,-90,0);}
            BuildRack(dock,cardboard,dark,i);
            dock.gameObject.AddComponent<ShopStation>().ConfigureBrand(shop,i);docks[i]=dock;
        }
        var loose=new GameObject("Loose empty cartons").transform;loose.SetParent(root,false);loose.position=new Vector3(-3.15f,0,1.25f);
        Material[] goods=Enumerable.Range(0,ShopCatalog.Count).Select(i=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/MAT_CompactProduct"+i+".mat")).ToArray();
        area.Configure(docks,cardboard,tape,goods,loose);shop.ConfigureWarehouse(area);
        var binRoot=new GameObject("Rear Paper Dumpster").transform;binRoot.SetParent(root,false);binRoot.position=new Vector3(-4.25f,0,4.9f);
        var bin=binRoot.gameObject.AddComponent<WarehouseBin>();
        Box(binRoot,"Bin bottom",new Vector3(0,.17f,0),new Vector3(1.25f,.10f,.85f),green);
        Box(binRoot,"Bin front",new Vector3(0,.68f,-.4f),new Vector3(1.25f,1.0f,.06f),green);
        Box(binRoot,"Bin back",new Vector3(0,.68f,.4f),new Vector3(1.25f,1.0f,.06f),green);
        Box(binRoot,"Bin left",new Vector3(-.6f,.68f,0),new Vector3(.05f,1,.8f),green);
        Box(binRoot,"Bin right",new Vector3(.6f,.68f,0),new Vector3(.05f,1,.8f),green);
        var lid=Box(binRoot,"Open dumpster lid",new Vector3(0,1.38f,.57f),new Vector3(1.30f,.07f,.85f),dark);lid.transform.localRotation=Quaternion.Euler(-65,0,0);
        Box(binRoot,"Left wheel",new Vector3(-.50f,.10f,.25f),new Vector3(.13f,.20f,.20f),dark);
        Box(binRoot,"Right wheel",new Vector3(.50f,.10f,.25f),new Vector3(.13f,.20f,.20f),dark);
        var text=ProductVisuals.Text(binRoot,"Paper bin label","PAPÍRGYŰJTŐ",new Vector3(0,.75f,-.435f),Quaternion.identity,.055f,Color.white);bin.Configure(area,text);
        var lamp=new GameObject("Warehouse ceiling lamp");lamp.transform.SetParent(root,false);lamp.transform.position=new Vector3(-4.25f,2.65f,0);var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=6;light.intensity=3.5f;light.color=new Color(1,.91f,.75f);
        EditorUtility.SetDirty(shop);EditorUtility.SetDirty(area);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("Added cashier-side warehouse, six brand docks, rear yard and paper dumpster; existing counter and shelves retained.");
    }
    private static void BuildRack(Transform dock,Material shelf,Material metal,int type)
    {
        for(int row=0;row<3;row++)Box(dock,"Warehouse shelf "+row,new Vector3(0,-.55f+row*.55f,0),new Vector3(.82f,.06f,.70f),shelf);
        // Highest board is above the delivery carton, leaving enough room to lift it.
        Box(dock,"Warehouse top shelf",new Vector3(0,1.05f,0),new Vector3(.82f,.06f,.70f),shelf);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            Box(dock,"Rack upright",new Vector3(x*.38f,.2f,z*.31f),new Vector3(.035f,1.9f,.035f),metal);
        Box(dock,"Rack back brace",new Vector3(0,.2f,.33f),new Vector3(.76f,1.9f,.025f),metal);
        Box(dock,"Brand shelf label plate",new Vector3(0,-.055f,-.35f),new Vector3(.76f,.11f,.008f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/MAT_CartonTape.mat"));
        ProductVisuals.Text(dock,"Brand dock label",ShopCatalog.Names[type],new Vector3(0,-.055f,-.357f),Quaternion.identity,.042f,Color.black);
    }
    [MenuItem("Nemzeti Dohánybolt/Upgrade Warehouse Shelves")]
    public static void UpgradeShelves()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var area=Object.FindAnyObjectByType<WarehouseArea>();if(area==null)return;
        var shelf=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/MAT_Cardboard.mat");
        var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/MAT_WarehouseMetal.mat");
        for(int i=0;i<ShopCatalog.Count;i++)
        {
            var dock=area.Dock(i);
            if(dock.Find("Warehouse shelf 0")!=null)
            {
                if(dock.Find("Brand shelf label plate")==null)Box(dock,"Brand shelf label plate",new Vector3(0,-.055f,-.35f),new Vector3(.76f,.11f,.008f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/MAT_CartonTape.mat"));
                continue;
            }
            for(int c=dock.childCount-1;c>=0;c--)Object.DestroyImmediate(dock.GetChild(c).gameObject);
            BuildRack(dock,shelf,metal,i);
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
    private static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;return go;
    }
    private static Material Mat(string name,Color color)
    {
        string path="Assets/Game/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;EditorUtility.SetDirty(m);return m;
    }
}
