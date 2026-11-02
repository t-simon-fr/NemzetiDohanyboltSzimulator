using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CounterFixtureSetup
{
    [MenuItem("Nemzeti Dohánybolt/Align Counter Equipment")]
    public static void UpgradeCurrentScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene=SceneManager.GetActiveScene();
        Transform body=Find(scene,"Service counter"), top=Find(scene,"Counter top");
        var game=Object.FindAnyObjectByType<MvpShopController>();
        if(body==null||top==null||game==null)throw new System.InvalidOperationException("Open the compact shop scene first.");
        // Keep the user's counter body, top, floor, walls, shelves and all unrelated scene edits.
        Transform assembly=Find(scene,"Counter Assembly");
        if(assembly==null)
        {
            assembly=Node(body.parent,"Counter Assembly",body.position,body.rotation,true);
            body.SetParent(assembly,true);top.SetParent(assembly,true);
        }
        ProductSlot tray=Find(scene,"Counter Slot 2").GetComponent<ProductSlot>();
        tray.transform.SetParent(assembly,true);
        Transform oldFixtures=assembly.Find("Counter Fixtures");
        if(oldFixtures!=null)Object.DestroyImmediate(oldFixtures.gameObject);
        string[] legacy={"Register blockout","Cash drawer","Till display","Till screen","Change tray","Order Monitor","Monitor stand","Order screen","Blue handover tray","Átadó felirat","Counter Slot 1"};
        foreach(string name in legacy){var t=Find(scene,name);if(t!=null)Object.DestroyImmediate(t.gameObject);}
        Transform surface=Node(assembly,"Counter Fixtures",top.position+top.up*(top.lossyScale.y*.5f),top.rotation,true);
        var layout=assembly.GetComponent<CounterLayout>();if(layout==null)layout=assembly.gameObject.AddComponent<CounterLayout>();
        Transform service=assembly.Find("Customer service point");
        if(service==null)service=Node(assembly,"Customer service point",Vector3.zero,Quaternion.identity);
        Vector3 serviceWorld=top.position-top.forward*(top.lossyScale.z*.5f+.48f);serviceWorld.y=0;service.position=serviceWorld;layout.Configure(service);
        tray.transform.SetParent(surface,false);tray.transform.localPosition=new Vector3(-.05f,.01f,0);tray.transform.localRotation=Quaternion.identity;
        tray.Configure(new Vector3(.34f,.34f,.32f));
        Material dark=Mat("MAT_RegisterCharcoal",new Color(.055f,.065f,.07f));
        Material ivory=Mat("MAT_RegisterIvory",new Color(.68f,.69f,.63f));
        Material metal=Mat("MAT_RegisterMetal",new Color(.45f,.48f,.48f));
        Material screen=Mat("MAT_DeviceScreen",new Color(.012f,.025f,.026f),true);
        Material blue=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/MAT_CompactProduct3.mat");
        Material paper=Mat("MAT_RegisterPaper",new Color(.93f,.92f,.84f));
        Box(surface,"Blue handover tray",new Vector3(-.05f,.003f,0),new Vector3(.36f,.008f,.34f),blue);
        ProductVisuals.Text(surface,"Átadó felirat","ÁRU ÁTADÁSA",new Vector3(-.05f,.013f,.205f),Quaternion.Euler(90,180,0),.027f,Color.black);
        var spare=Node(surface,"Counter Slot 1",new Vector3(.5f,.01f,0),Quaternion.identity).gameObject.AddComponent<ProductSlot>();
        spare.Configure(new Vector3(.32f,.34f,.32f));
        Transform change=Node(surface,"Change tray",new Vector3(-.55f,.012f,.04f),Quaternion.identity);
        Box(change,"Change plate",Vector3.zero,new Vector3(.28f,.018f,.29f),dark);
        ProductVisuals.Text(change,"Visszajáró felirat","VISSZAJÁRÓ",new Vector3(0,.015f,.17f),Quaternion.Euler(90,180,0),.021f,Color.black);
        Transform monitor=Node(surface,"Order Monitor Assembly",new Vector3(1.06f,0,-.04f),Quaternion.identity);
        Station(monitor.gameObject,game,ShopStationKind.Monitor);
        var monitorBody=Box(monitor,"Order Monitor",new Vector3(0,.36f,0),new Vector3(.62f,.37f,.035f),dark);
        Station(monitorBody,game,ShopStationKind.Monitor);
        Box(monitor,"Monitor stand",new Vector3(0,.11f,-.005f),new Vector3(.045f,.2f,.055f),metal);
        Box(monitor,"Monitor base",new Vector3(0,.015f,.025f),new Vector3(.23f,.03f,.17f),dark);
        Box(monitor,"Screen glass",new Vector3(0,.36f,.020f),new Vector3(.595f,.34f,.008f),screen);
        Transform canvas=Node(monitor,"Order screen",new Vector3(0,.36f,.027f),Quaternion.identity);canvas.localScale=Vector3.one*.33f;
        Transform till=Node(surface,"Cash Register Assembly",new Vector3(-1.04f,0,-.025f),Quaternion.identity);
        Station(till.gameObject,game,ShopStationKind.Till);
        // A drawer cabinet with a real opening: no solid cube blocks the sliding insert.
        Box(till,"Cabinet bottom",new Vector3(0,.015f,0),new Vector3(.58f,.03f,.46f),dark);
        Box(till,"Cabinet left",new Vector3(.282f,.075f,0),new Vector3(.016f,.12f,.46f),dark);
        Box(till,"Cabinet right",new Vector3(-.282f,.075f,0),new Vector3(.016f,.12f,.46f),dark);
        Box(till,"Cabinet back",new Vector3(0,.075f,-.224f),new Vector3(.55f,.12f,.012f),dark);
        Box(till,"Cabinet roof",new Vector3(0,.137f,0),new Vector3(.58f,.018f,.46f),ivory);
        var hood=new GameObject("Sloped register housing");hood.transform.SetParent(till,false);
        hood.AddComponent<MeshFilter>().sharedMesh=HousingMesh();hood.AddComponent<MeshRenderer>().sharedMaterial=ivory;
        // Follow the actual slope so the shell does not cover the keypad hit targets.
        var slopeCollider=Node(hood.transform,"Sloped housing collision",new Vector3(0,.217f,0),Quaternion.Euler(18,0,0)).gameObject.AddComponent<BoxCollider>();
        slopeCollider.size=new Vector3(.56f,.022f,.44f);
        Transform keys=Node(till,"Register keypad",new Vector3(-.015f,.24f,.025f),Quaternion.Euler(18,0,0));
        for(int row=0;row<3;row++)for(int col=0;col<3;col++)
        {
            Vector3 p=new Vector3((1-col)*.1f,.005f,.10f+row*.04f);
            Box(keys,"Numeric key "+(row*3+col+1),p,new Vector3(.085f,.012f,.031f),ivory);
            ProductVisuals.Text(keys,"Key digit",(row*3+col+1).ToString(),p+Vector3.up*.009f,Quaternion.Euler(90,180,0),.011f,Color.black);
        }
        Box(till,"Display neck",new Vector3(-.035f,.32f,-.15f),new Vector3(.055f,.10f,.04f),dark);
        Box(till,"Display bezel",new Vector3(-.035f,.385f,-.15f),new Vector3(.35f,.135f,.035f),dark);
        Box(till,"Display glass",new Vector3(-.035f,.385f,-.128f),new Vector3(.324f,.111f,.009f),screen);
        Transform display=Node(till,"Till screen",new Vector3(-.035f,.385f,-.117f),Quaternion.identity);
        Box(till,"Receipt printer",new Vector3(.224f,.296f,-.142f),new Vector3(.105f,.085f,.12f),ivory);
        Box(till,"Receipt slot",new Vector3(.224f,.341f,-.12f),new Vector3(.083f,.005f,.013f),dark);
        var receipt=Box(till,"Receipt paper",new Vector3(.224f,.385f,-.12f),new Vector3(.075f,.085f,.002f),paper);receipt.transform.localRotation=Quaternion.Euler(-12,0,0);
        ProductVisuals.Text(till,"Receipt print","TRAFIK\nNYUGTA",new Vector3(.224f,.388f,-.108f),Quaternion.Euler(0,180,0),.008f,Color.black);
        Transform moving=Node(till,"Sliding cash drawer",new Vector3(0,.041f,.015f),Quaternion.identity);
        Box(moving,"Drawer floor",Vector3.zero,new Vector3(.54f,.014f,.35f),dark,false);
        Box(moving,"Drawer left rim",new Vector3(.267f,.035f,0),new Vector3(.012f,.07f,.35f),dark,false);
        Box(moving,"Drawer right rim",new Vector3(-.267f,.035f,0),new Vector3(.012f,.07f,.35f),dark,false);
        Box(moving,"Drawer front",new Vector3(0,.026f,.179f),new Vector3(.56f,.09f,.02f),ivory,false);
        Box(moving,"Drawer handle",new Vector3(0,.035f,.196f),new Vector3(.23f,.019f,.018f),metal,false);
        Transform contents=Node(moving,"Money compartments",new Vector3(0,.009f,0),Quaternion.identity);
        var mechanism=till.gameObject.AddComponent<CashRegisterDrawer>();mechanism.Configure(moving,contents);
        var player=Object.FindAnyObjectByType<PlayerInteractor>();player.gameObject.layer=2;
        if(Vector3.Distance(player.transform.position,new Vector3(0,.05f,-.7f))<.01f)
        {
            Vector3 position=top.position+top.forward*1.18f;position.y=.05f;
            player.transform.SetPositionAndRotation(position,top.rotation*Quaternion.Euler(0,180,0));
        }
        var devices=game.GetComponent<ShopDevices>();devices.Configure(canvas,contents,change,display,monitor,till,keys,mechanism);
        game.ConfigureCounter(tray,till);
        Transform routes=Find(scene,"Customer Routes");if(routes==null)routes=Node(game.transform,"Customer Routes",Vector3.zero,Quaternion.identity);
        var shopLayout=game.GetComponent<ShopLayout>();
        shopLayout.ConfigureRoutes(Route(routes,"Entrance outside",new Vector3(.13f,0,-4.95f)),Route(routes,"Entrance inside",new Vector3(.13f,0,-3.65f)),Route(routes,"Public aisle gate",new Vector3(3.2f,0,-3.25f)),Route(routes,"Exit doorway",new Vector3(.13f,0,-4.05f)));
        foreach(var obj in new Object[]{layout,game,devices,shopLayout,mechanism})EditorUtility.SetDirty(obj);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("Counter equipment aligned to the edited countertop. Move Counter Assembly to move its equipment and customer service point together.");
    }
    private static Transform Route(Transform parent,string name,Vector3 position)
    {var t=parent.Find(name);return t==null?Node(parent,name,position,Quaternion.identity,true):t;}
    private static Transform Find(Scene scene,string name)=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name==name);
    private static Transform Node(Transform parent,string name,Vector3 position,Quaternion rotation,bool world=false)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);
        if(world)t.SetPositionAndRotation(position,rotation);else{t.localPosition=position;t.localRotation=rotation;}return t;
    }
    private static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool collider=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
        if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    private static Material Mat(string name,Color color,bool unlit=false)
    {
        string path="Assets/Game/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;EditorUtility.SetDirty(m);return m;
    }
    private static Mesh HousingMesh()
    {
        const string path="Assets/Game/Materials/MESH_RegisterHousing.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null)return existing;
        Vector3[] corners={new Vector3(-.28f,.145f,-.21f),new Vector3(.28f,.145f,-.21f),new Vector3(.28f,.145f,.21f),new Vector3(-.28f,.145f,.21f),new Vector3(-.28f,.29f,-.21f),new Vector3(.28f,.29f,-.21f),new Vector3(.28f,.16f,.21f),new Vector3(-.28f,.16f,.21f)};
        int[] faces={0,3,2,1,4,5,6,7,0,1,5,4,3,7,6,2,0,4,7,3,1,2,6,5};
        var vertices=new Vector3[24];var triangles=new int[36];
        for(int f=0;f<6;f++){for(int v=0;v<4;v++)vertices[f*4+v]=corners[faces[f*4+v]];int o=f*6,b=f*4;triangles[o]=b;triangles[o+1]=b+1;triangles[o+2]=b+2;triangles[o+3]=b;triangles[o+4]=b+2;triangles[o+5]=b+3;}
        var mesh=new Mesh{name="Register housing"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    private static void Station(GameObject target,MvpShopController shop,ShopStationKind kind)=>target.AddComponent<ShopStation>().Configure(shop,kind);
}
