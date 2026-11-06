using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ShopNextUpgrades : MonoBehaviour
{
    public const int FridgePrice=18000;
    public bool FridgeOwned{get;private set;}
    private MvpShopController shop;
    private GameObject fridge;
    private Transform coffee;
    private readonly List<Material> materials=new List<Material>();
    private bool developer;private Vector2 developerScroll;
    public void Configure(MvpShopController owner)
    {
        shop=owner;BuildExterior();BuildCounter();BuildFridge();
    }
    Material Mat(Color c){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=c};materials.Add(m);return m;}
    GameObject Box(Transform parent,string name,Vector3 pos,Vector3 scale,Material material)
    {var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;return o;}
    void BuildExterior()
    {
        var root=new GameObject("Shop Exterior").transform;root.SetParent(transform,false);
        var pavement=Mat(new Color(.42f,.43f,.41f));var road=Mat(new Color(.12f,.14f,.15f));var paint=Mat(new Color(.88f,.84f,.64f));var green=Mat(new Color(.15f,.25f,.13f));
        Box(root,"Outdoor ground",new Vector3(0,-.17f,-9),new Vector3(36,.25f,24),green);
        Box(root,"Entrance pavement",new Vector3(0,-.025f,-5.9f),new Vector3(14,.10f,3),pavement);
        Box(root,"Street",new Vector3(0,-.035f,-10),new Vector3(36,.08f,5),road);
        for(int i=-8;i<=8;i++)Box(root,"Street marking",new Vector3(i*2, .01f,-10),new Vector3(.9f,.008f,.08f),paint);
        for(int i=-6;i<=6;i++){Box(root,"Paving joint",new Vector3(i,.028f,-5.9f),new Vector3(.012f,.005f,3),road).GetComponent<Collider>().enabled=false;}
        foreach(int side in new[]{-1,1})
        {
            Box(root,"Street lamp post",new Vector3(side*5,1.6f,-6.7f),new Vector3(.09f,3.2f,.09f),road);
            Box(root,"Street lamp",new Vector3(side*5,3.2f,-6.7f),new Vector3(.45f,.08f,.35f),paint);
            var lamp=new GameObject("Street light").AddComponent<Light>();lamp.transform.SetParent(root,false);lamp.transform.position=new Vector3(side*5,3,-6.7f);lamp.type=LightType.Point;lamp.range=6;lamp.intensity=1.2f;
            Box(root,"Planter",new Vector3(side*3.5f,.22f,-4.9f),new Vector3(.65f,.44f,.65f),pavement);
            var shrub=GameObject.CreatePrimitive(PrimitiveType.Sphere);shrub.name="Shrub";shrub.transform.SetParent(root,false);shrub.transform.position=new Vector3(side*3.5f,.6f,-4.9f);shrub.transform.localScale=new Vector3(.7f,.65f,.7f);shrub.GetComponent<Renderer>().sharedMaterial=green;
        }
        var sign=Mat(new Color(.035f,.12f,.10f));Box(root,"Exterior shop sign",new Vector3(0,2.55f,-4.52f),new Vector3(3.3f,.70f,.05f),sign);
        ProductVisuals.Text(root,"Exterior shop title","NEMZETI DOHÁNYBOLT",new Vector3(0,2.63f,-4.552f),Quaternion.identity,.12f,Color.white);
        ProductVisuals.Text(root,"Exterior shop subtitle","TRAFIK PRESSZÓ • 18+",new Vector3(0,2.38f,-4.552f),Quaternion.identity,.06f,new Color(.8f,.75f,.5f));
        var boundary=GameObject.Find("Player Door Boundary");if(boundary!=null)boundary.SetActive(false);
        gameObject.AddComponent<ShopDroppedItemRecovery>();
    }
    void BuildCounter()
    {
        coffee=GetComponentsInChildren<Transform>(true).First(t=>t.name=="Purchased Coffee Machine");
        coffee.SetPositionAndRotation(new Vector3(-1.85f,0,1.15f),Quaternion.identity);
    }
    void BuildFridge()
    {
        fridge=new GameObject("Purchased Drinks Fridge");fridge.transform.SetParent(transform,false);fridge.transform.position=new Vector3(3.35f,0,1.35f);
        var white=Mat(new Color(.78f,.83f,.82f));var dark=Mat(new Color(.12f,.17f,.18f));var shelves=new List<ProductSlot>();
        Box(fridge.transform,"Fridge back",new Vector3(0,1,.30f),new Vector3(1.55f,2,.08f),white);
        foreach(int side in new[]{-1,1})Box(fridge.transform,"Fridge side",new Vector3(side*.77f,1,0),new Vector3(.07f,2,.65f),white);
        Box(fridge.transform,"Fridge roof",new Vector3(0,2,0),new Vector3(1.55f,.08f,.65f),white);
        ProductVisuals.Text(fridge.transform,"Fridge sign","HŰTÖTT ITALOK",new Vector3(0,1.88f,-.34f),Quaternion.identity,.055f,Color.white);
        for(int row=0;row<4;row++)
        {
            float y=.12f+row*.43f;Box(fridge.transform,"Fridge shelf",new Vector3(0,y,0),new Vector3(1.45f,.035f,.6f),dark);
            for(int col=0;col<4;col++){var s=new GameObject("Fridge slot "+row+"-"+col).AddComponent<ProductSlot>();s.transform.SetParent(fridge.transform,false);s.transform.localPosition=new Vector3((col-1.5f)*.35f,y+.02f,0);s.Configure(new Vector3(.32f,.39f,.4f));s.RestrictTo("water");shelves.Add(s);}
        }
        Box(fridge.transform,"Compressor base",new Vector3(0,.065f,0),new Vector3(1.55f,.13f,.65f),white);
        Box(fridge.transform,"Compressor grille",new Vector3(0,.065f,-.335f),new Vector3(1.35f,.09f,.012f),dark);
        for(int i=0;i<23;i++)Box(fridge.transform,"Vent fin",new Vector3((i-11)*.055f,.065f,-.345f),new Vector3(.008f,.08f,.008f),white);
        Box(fridge.transform,"Temperature display",new Vector3(.55f,1.88f,-.345f),new Vector3(.27f,.11f,.015f),dark);
        ProductVisuals.Text(fridge.transform,"Temperature","4 °C",new Vector3(.55f,1.88f,-.356f),Quaternion.identity,.035f,new Color(.3f,1,.7f));
        var glass=Mat(new Color(.72f,.87f,.9f,.09f));glass.shader=Shader.Find("Shop/PET");glass.color=new Color(.72f,.87f,.9f,.09f);glass.renderQueue=3000;
        var pane=Box(fridge.transform,"Fridge glazing",new Vector3(0,.95f,-.32f),new Vector3(1.43f,1.65f,.009f),glass);pane.GetComponent<Collider>().enabled=false;
        shop.Layout.RegisterPublicSlots(shelves.ToArray());fridge.SetActive(false);
    }
    public bool BuyFridge()
    {
        if(FridgeOwned||!shop.Layout.Expanded||!shop.CanDevelopShop){shop.Notify("Hűtőt a bővített boltba, nyitás előtt vagy zárás után, üres boltban vásárolhatsz.");return false;}
        var furniture = fridge.GetComponent<ShopFurniture>();
        if (furniture != null && !shop.Arrangement.CanActivate(furniture)) return false;
        if(!shop.Ledger.TryImprove(FridgePrice)){shop.Notify("Nincs elég pénz a hűtőre.");return false;}
        shop.SyncCashDrawer();FridgeOwned=true;fridge.SetActive(true);shop.Navigation?.Rebuild();shop.Notify("Hűtő megvásárolva. A polcait kézzel töltheted fel.");return true;
    }
    public void ResetUpgrades(){FridgeOwned=false;if(fridge!=null)fridge.SetActive(false);}
    public void RefreshLayout()
    {
        // Expansion changes available floor area, never existing furniture poses.
        shop.GetComponent<ShopTrafikIdentity>()?.RefreshLayout(shop.Layout.Expanded);
    }
    void Update()
    {
        if(shop==null||shop.Ledger==null||shop.Arrangement != null && shop.Arrangement.Active)return;RefreshLayout();
        if(Application.isFocused&&(Application.isEditor||Debug.isDebugBuild)&&Keyboard.current!=null&&ShopBindings.Pressed(UnityEngine.InputSystem.Key.F1))
        HandleDeveloperKey(Key.F1);
    }
    public bool DeveloperVisible=>developer;
    public bool HandleDeveloperKey(Key key)
    {
        if(key!=Key.F1||!(Application.isEditor||Debug.isDebugBuild))return false;
        developer=!developer;if(developer){shop.ClosePanel();ShopInputGate.Blocked=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}else shop.ClosePanel();return true;
    }
    void OnGUI()
    {
        if(shop==null||shop.Ledger==null||shop.Arrangement != null && shop.Arrangement.Active)return;
        if(!developer)return;
        GUI.depth=-200;GUI.Box(new Rect(12,12,380,Mathf.Min(Screen.height-24,680)),"DEVELOPER MODE • F1");GUILayout.BeginArea(new Rect(25,45,352,Mathf.Min(Screen.height-65,615)));developerScroll=GUILayout.BeginScrollView(developerScroll);
        GUILayout.Label("Nap "+shop.Day+" | "+shop.Phase+" | "+shop.Stage);GUILayout.Label("Kassza "+shop.Ledger.Cash+" / címletek "+shop.Drawer.Total);
        if(GUILayout.Button("+50 000 Ft")){typeof(ShopLedger).GetProperty("Cash").SetValue(shop.Ledger,shop.Ledger.Cash+50000);typeof(ShopLedger).GetProperty("OpeningCash").SetValue(shop.Ledger,shop.Ledger.OpeningCash+50000);shop.Drawer.Settle(50000,new int[ShopCatalog.Denominations.Length]);}
        if(GUILayout.Button("Bővítés"))shop.TryExpand();if(GUILayout.Button("Kávégép"))shop.Activities.BuyCoffeeMachine();if(GUILayout.Button("Hűtő"))BuyFridge();
        if(GUILayout.Button("Extra polc"))shop.BuyExtraShelf();if(GUILayout.Button("Vészgomb"))shop.BuyPanicButton();
        if(GUILayout.Button("Árurendelés és polcfeltöltés"))shop.DebugRestock();if(GUILayout.Button("Új vásárló"))shop.DebugSpawnVisit(false);if(GUILayout.Button("Tolvaj"))shop.DebugSpawnVisit(true);
        for(int type=0;type<shop.AvailableProducts;type++)GUILayout.Label(ShopCatalog.ShortNames[type]+": "+shop.StockCount(type)+" polc / "+shop.WarehouseCount(type)+" raktár");
        if(GUILayout.Button("Koszfolt"))shop.Activities.SpawnSpill();if(GUILayout.Button("Következő vásárló"))shop.SkipCurrent();
        if(GUILayout.Button("10 másodperc szimuláció")){bool previous=shop.ManualTick;shop.ManualTick=true;shop.Tick(10);shop.ManualTick=previous;}
        if(GUILayout.Button("Új játék")){shop.ResetDemo();FridgeOwned=false;fridge.SetActive(false);}
        if(GUILayout.Button("Bezárás")){developer=false;shop.ClosePanel();}GUILayout.EndScrollView();GUILayout.EndArea();
    }
    void OnDestroy(){foreach(var m in materials)if(m!=null)Destroy(m);}
}
public sealed class ShopDroppedItemRecovery:MonoBehaviour
{
    float timer;
    void Update(){timer-=Time.unscaledDeltaTime;if(timer>0)return;timer=1;RecoverNow();}
    public void RecoverNow(){
        foreach(var body in FindObjectsByType<Rigidbody>())if(!body.isKinematic&&body.transform.position.y< -4&&(body.GetComponent<ShopProduct>()!=null||body.GetComponent<ShopHandTool>()!=null||body.GetComponent<WarehouseCarton>()!=null))
        {var destination=new Vector3(UnityEngine.Random.Range(-.6f,.6f),.5f,-5.5f);body.transform.position=destination;body.position=destination;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
    }
}
