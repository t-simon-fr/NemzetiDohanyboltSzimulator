using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CompactShopSetup
{
    private static Material wall,wood,trim,dark;
    private static Material[] products;
    [MenuItem("Nemzeti Dohánybolt/Build Compact Shop")]
    public static void BuildFromMenu()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(!EditorUtility.DisplayDialog("Kompakt bolt","Újraépíted a ShopPrototype jelenetet a kompakt elrendezéssel?","Újraépítés","Mégse"))return;
        Build();
    }
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Stop Play Mode before rebuilding.");
        wall=Mat("MAT_CompactWall",new Color(.75f,.72f,.63f));wood=Mat("MAT_CompactWood",new Color(.24f,.16f,.11f));
        trim=Mat("MAT_CompactTrim",new Color(.82f,.76f,.58f));dark=Mat("MAT_CompactDevice",new Color(.06f,.075f,.085f));
        products=new Material[ShopCatalog.Count];for(int i=0;i<products.Length;i++)products[i]=Mat("MAT_CompactProduct"+i,ProductVisuals.Colors[i]);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var room=new GameObject("Compact Layout").transform;
        Box(room,"Floor",new Vector3(0,-.1f,-1.25f),new Vector3(5,.2f,6.3f),Mat("MAT_CompactFloor",new Color(.27f,.29f,.28f)));
        Box(room,"Left wall",new Vector3(-2.5f,1.5f,-1.25f),new Vector3(.15f,3,6.3f),wall);
        Box(room,"Back wall",new Vector3(0,1.5f,1.9f),new Vector3(5,3,.15f),wall);
        Box(room,"Front wall left",new Vector3(-1.65f,1.5f,-4.4f),new Vector3(1.7f,3,.15f),wall);
        Box(room,"Front wall right",new Vector3(1.65f,1.5f,-4.4f),new Vector3(1.7f,3,.15f),wall);
        Box(room,"Door lintel",new Vector3(0,2.7f,-4.4f),new Vector3(1.6f,.6f,.15f),wood);
        Box(room,"Ceiling",new Vector3(0,3.05f,-1.25f),new Vector3(5,.1f,6.3f),wall);
        var boundary=new GameObject("Player Door Boundary");boundary.transform.SetParent(room,false);boundary.transform.localPosition=new Vector3(0,1.2f,-4.48f);boundary.AddComponent<BoxCollider>().size=new Vector3(1.6f,2.4f,.12f);
        var starterWall=Box(room,"Expansion partition",new Vector3(2.5f,1.5f,-3.2f),new Vector3(.15f,3,2.4f),wall);
        // This permanent section keeps the staff area separated from the new public aisle.
        Box(room,"Staff side wall",new Vector3(2.5f,1.5f,0),new Vector3(.15f,3,3.8f),wall);
        Box(room,"Service counter",new Vector3(0,.55f,-2.05f),new Vector3(3.4f,1.1f,.75f),wood);
        Box(room,"Counter top",new Vector3(0,1.13f,-2.05f),new Vector3(3.6f,.08f,.85f),trim);
        var extension=new GameObject("Self-service Extension");extension.transform.SetParent(room,false);
        Box(extension.transform,"Extension floor",new Vector3(4.5f,-.1f,-1.25f),new Vector3(4,.2f,6.3f),Mat("MAT_CompactFloor",new Color(.27f,.29f,.28f)));
        Box(extension.transform,"Extension ceiling",new Vector3(4.5f,3.05f,-1.25f),new Vector3(4,.1f,6.3f),wall);
        Box(extension.transform,"Extension outer wall",new Vector3(6.5f,1.5f,-1.25f),new Vector3(.15f,3,6.3f),wall);
        Box(extension.transform,"Extension back wall",new Vector3(4.5f,1.5f,1.9f),new Vector3(4,3,.15f),wall);
        Box(extension.transform,"Extension front wall",new Vector3(4.5f,1.5f,-4.4f),new Vector3(4,3,.15f),wall);
        ProductVisuals.Text(extension.transform,"Önkiszolgáló felirat","ÖNKISZOLGÁLÓ\nA türelmet nem kell lemérni.",new Vector3(4.4f,2.15f,1.8f),Quaternion.identity,.13f,Color.black);
        var slots=new List<ProductSlot>();var types=new List<int>();var publicSlots=new List<ProductSlot>();var publicTypes=new List<int>();
        var rack=new GameObject("Cigarette rack").transform;rack.SetParent(room,false);rack.position=new Vector3(0,0,1.5f);
        Box(rack,"Rack back",new Vector3(0,1, .16f),new Vector3(3.5f,1.9f,.08f),wood);
        for(int row=0;row<4;row++)
        {
            float y=.25f+row*.42f;Box(rack,"Shelf "+row,new Vector3(0,y,0),new Vector3(3.5f,.06f,.4f),trim);
            for(int col=0;col<6;col++)
            {
                var slot=Slot(rack,"Cigarette slot "+row+"-"+col,new Vector3((col-2.5f)*.52f,y+.03f,0),Quaternion.identity,new Vector3(.35f,.36f,.3f));
                slot.RestrictTo(ShopCatalog.Ids[col/2]);slots.Add(slot);types.Add(col/2);
            }
        }
        ProductVisuals.Text(room,"Tobacco sign","NEMZETI DOHÁNYBOLT\n18+  •  A dohányzás káros az egészségre",new Vector3(0,2.35f,1.8f),Quaternion.identity,.13f,Color.black);
        var publicRack=new GameObject("Public shelf").transform;publicRack.SetParent(extension.transform,false);publicRack.position=new Vector3(5.95f,0,-1f);
        Box(publicRack,"Shelf back",new Vector3(.22f,.9f,0),new Vector3(.08f,1.8f,3.3f),wood);
        for(int row=0;row<3;row++)
        {
            float y=.3f+row*.46f;Box(publicRack,"Shelf "+row,new Vector3(0,y,0),new Vector3(.55f,.06f,3.3f),trim);
            for(int col=0;col<6;col++)
            {
                var slot=Slot(publicRack,"Public slot "+row+"-"+col,new Vector3(0,y+.03f,(col-2.5f)*.5f),Quaternion.Euler(0,90,0),new Vector3(.4f,.38f,.4f));
                slot.RestrictTo(ShopCatalog.Ids[row+3]);slots.Add(slot);types.Add(row+3);publicSlots.Add(slot);publicTypes.Add(row+3);
            }
            ProductVisuals.Text(publicRack,"Árcímke",ShopCatalog.Names[row+3]+" • "+ShopCatalog.Money(ShopCatalog.Prices[row+3]),new Vector3(-.29f,y-.06f,0),Quaternion.Euler(0,90,0),.09f,Color.black);
        }
        var player=new GameObject("Player");player.transform.SetPositionAndRotation(new Vector3(0,.05f,-.7f),Quaternion.Euler(0,180,0));
        var cc=player.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.27f;cc.center=new Vector3(0,.9f,0);
        var cam=new GameObject("Player Camera");cam.tag="MainCamera";cam.transform.SetParent(player.transform,false);cam.transform.localPosition=new Vector3(0,1.62f,0);cam.AddComponent<Camera>();cam.AddComponent<AudioListener>();
        player.AddComponent<FirstPersonController>();var interactor=player.AddComponent<PlayerInteractor>();interactor.Configure(cam.GetComponent<Camera>());
        var manager=new GameObject("MVP Shop");var game=manager.AddComponent<MvpShopController>();
        var layout=manager.AddComponent<ShopLayout>();layout.Configure(starterWall,extension,publicSlots.ToArray(),publicTypes.ToArray());
        var crate=Box(room,"Supply Crate",new Vector3(1.9f,.35f,.8f),new Vector3(.7f,.7f,.7f),wood);Station(crate,game,ShopStationKind.Supplies);
        ProductVisuals.Text(crate.transform,"Doboz felirat","SZÁLLÍTMÁNY\nQ: típus  •  E: kivétel",new Vector3(0,.51f,0),Quaternion.Euler(90,0,0),.08f,Color.white);
        var sign=Box(room,"Open Close Sign",new Vector3(1.9f,1.5f,-4.25f),new Vector3(.6f,.45f,.08f),dark);Station(sign,game,ShopStationKind.Door);
        ProductVisuals.Text(room,"Nyitás felirat","NYITÁS / ZÁRÁS\nE vagy O",new Vector3(1.9f,1.5f,-4.19f),Quaternion.Euler(0,180,0),.06f,Color.white);
        var tray=Slot(room,"Counter Slot 2",new Vector3(.15f,1.18f,-2.05f),Quaternion.identity,new Vector3(.38f,.36f,.36f));
        Box(room,"Blue handover tray",new Vector3(.15f,1.173f,-2.05f),new Vector3(.4f,.012f,.38f),products[3]);
        ProductVisuals.Text(room,"Átadó felirat","ÁRU ÁTADÁSA",new Vector3(.15f,1.185f,-1.8f),Quaternion.Euler(90,180,0),.035f,Color.black);
        Slot(room,"Counter Slot 1",new Vector3(.72f,1.18f,-2.05f),Quaternion.identity,new Vector3(.35f,.36f,.36f));
        var till=Box(room,"Register blockout",new Vector3(-1.18f,1.23f,-2.05f),new Vector3(1.18f,.12f,.82f),dark);Station(till,game,ShopStationKind.Till);
        var drawer=new GameObject("Cash drawer").transform;drawer.SetParent(room,false);drawer.position=new Vector3(-1.18f,1.3f,-1.96f);
        Box(room,"Till display",new Vector3(-1.18f,1.64f,-2.47f),new Vector3(1.18f,.44f,.06f),dark);
        var display=new GameObject("Till screen").transform;display.SetParent(room,false);display.position=new Vector3(-1.18f,1.64f,-2.425f);
        var changeTray=new GameObject("Change tray").transform;changeTray.SetParent(room,false);changeTray.position=new Vector3(-.32f,1.2f,-1.9f);
        Box(changeTray,"Change plate",Vector3.zero,new Vector3(.4f,.035f,.4f),wood);
        var monitorBody=Box(room,"Order Monitor",new Vector3(1.35f,1.94f,-2.19f),new Vector3(1.27f,1.16f,.09f),dark);Station(monitorBody,game,ShopStationKind.Monitor);
        Box(room,"Monitor stand",new Vector3(1.35f,1.3f,-2.2f),new Vector3(.12f,.3f,.14f),dark);
        var monitor=new GameObject("Order screen").transform;monitor.SetParent(room,false);monitor.position=new Vector3(1.35f,1.97f,-2.13f);
        manager.AddComponent<ShopDevices>();
        var template=new GameObject("Customer Template");template.SetActive(false);
        var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name="Body";body.transform.SetParent(template.transform,false);body.transform.localPosition=new Vector3(0,.85f,0);body.transform.localScale=new Vector3(.42f,.55f,.42f);body.GetComponent<Renderer>().sharedMaterial=products[1];Object.DestroyImmediate(body.GetComponent<Collider>());
        var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="Head";head.transform.SetParent(template.transform,false);head.transform.localPosition=new Vector3(0,1.55f,0);head.transform.localScale=Vector3.one*.32f;head.GetComponent<Renderer>().sharedMaterial=Mat("MAT_CompactSkin",new Color(.73f,.54f,.39f));Object.DestroyImmediate(head.GetComponent<Collider>());
        game.Configure(interactor,tray,template,slots.ToArray(),types.ToArray(),products,crate.transform,till.transform);
        manager.AddComponent<MvpShopHud>().Configure(game);
        var light=new GameObject("Shop Light");light.transform.rotation=Quaternion.Euler(50,-30,0);var sun=light.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.2f;
        RenderSettings.ambientLight=new Color(.58f,.57f,.54f);
        var lamp=new GameObject("Interior light");lamp.transform.position=new Vector3(0,2.7f,-.7f);var point=lamp.AddComponent<Light>();point.type=LightType.Point;point.range=12;point.intensity=5;point.color=new Color(1,.9f,.72f);
        PlayerSettings.productName="Nemzeti Dohánybolt Szimulátor";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
        const string path="Assets/Game/Scenes/ShopPrototype.unity";EditorSceneManager.SaveScene(scene,path);CounterFixtureSetup.UpgradeCurrentScene();EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};AssetDatabase.SaveAssets();
        Debug.Log("Compact shop saved: 24 cigarette slots, 18 expansion slots, physical monitor and register. Counter remains at (0, .55, -2.05).");
    }
    private static Material Mat(string name,Color color)
    {
        string path="Assets/Game/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;EditorUtility.SetDirty(m);return m;
    }
    private static GameObject Box(Transform p,string n,Vector3 pos,Vector3 size,Material m)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=n;go.transform.SetParent(p,false);go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=m;return go;
    }
    private static ProductSlot Slot(Transform p,string n,Vector3 pos,Quaternion rotation,Vector3 size)
    {
        var go=new GameObject(n);go.transform.SetParent(p,false);go.transform.localPosition=pos;go.transform.localRotation=rotation;var s=go.AddComponent<ProductSlot>();s.Configure(size);return s;
    }
    private static void Station(GameObject target,MvpShopController game,ShopStationKind kind)=>target.AddComponent<ShopStation>().Configure(game,kind);
}
