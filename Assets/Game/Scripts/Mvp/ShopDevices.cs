using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ShopDeviceKind { None, Monitor, Till }
public enum ShopComputerApp { Desktop, Orders, Furnishing }
public sealed class ShopDevices : MonoBehaviour
{
    [SerializeField] private Transform monitor, drawer, changeTray, tillDisplay;
    [SerializeField] private Transform monitorAssembly, tillAssembly, keypad;
    [SerializeField] private CashRegisterDrawer registerDrawer;
    private MvpShopController shop;
    private Camera view;
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private TextMesh monitorText, cashText, appTitle, furnishingStatus;
    private TextMesh shopStatusText, orderQuantityText, monitorNotice;
    private Transform catalogHelp;
    private readonly List<GameObject> orderCards = new List<GameObject>();
    private readonly List<TextMesh> orderPrices = new List<TextMesh>();
    private readonly List<Material> screenMaterials = new List<Material>();
    private Material screenBackground, screenCard, screenAccent, screenBar;
    private int orderQuantity = 3, lastOrderType = -1;
    private bool lastOrderSucceeded;
    private string orderFeedback = "";
    public int OrderQuantity => orderQuantity;
    private Transform desktopPage, orderPage, furnishingPage, buildingPage;
    public ShopComputerApp ComputerApp { get; private set; }
    private readonly List<TextMesh> stockLabels = new List<TextMesh>();
    private readonly List<TextMesh> moneyLabels = new List<TextMesh>();
    private readonly List<GameObject> selectedVisuals = new List<GameObject>();
    private readonly List<GameObject> cashStacks = new List<GameObject>();
    private string lastSelection = "";
    private int refreshMonitorFrame=-1;
    private Material bodyMaterial, paperMaterial, coinMaterial;
    public ShopDeviceKind FocusKind { get; private set; }
    public bool Focused => FocusKind != ShopDeviceKind.None;
    public Vector3 MonitorPosition => monitor.position;
    public CashRegisterDrawer RegisterDrawer => registerDrawer;
    public Transform TillAssembly => tillAssembly;
    public void Configure(Transform screen, Transform contents, Transform tray, Transform display,
        Transform monitorRoot, Transform tillRoot, Transform keyboard, CashRegisterDrawer mechanism)
    {
        monitor = screen; drawer = contents; changeTray = tray; tillDisplay = display;
        monitorAssembly = monitorRoot; tillAssembly = tillRoot; keypad = keyboard; registerDrawer = mechanism;
    }
    private void Start()
    {
        shop = GetComponent<MvpShopController>(); view = Camera.main;
        bodyMaterial = Material(new Color(.045f,.06f,.07f));
        paperMaterial = Material(new Color(.65f,.73f,.6f));
        coinMaterial = Material(new Color(.72f,.62f,.34f));
        BuildMonitor(); BuildDrawer(); BuildKeypad();
        Font.textureRebuilt+=OnFontTextureRebuilt;
    }
    private static Material Material(Color color) => new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
    private GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material; return go;
    }
    private void Control(GameObject go, WorldControlKind kind, int value = 0)
    { var c = go.AddComponent<ShopWorldControl>(); c.Kind = kind; c.Value = value; }
    private TextMesh Label(Transform parent, string text, Vector3 position, float size = .035f)
        => ProductVisuals.Text(parent, "Felirat", text, position, Quaternion.Euler(0,180,0), size, Color.white);
    private Transform Page(string name)
    {
        var page=new GameObject(name).transform;page.SetParent(monitor,false);return page;
    }
    private Material ScreenMaterial(string name,Color color)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name=name,color=color};screenMaterials.Add(m);return m;
    }
    private GameObject ScreenPanel(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        var go=Box(parent,name,position,size,material);var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);return go;
    }
    private GameObject MonitorButton(Transform page,string text,Vector3 position,WorldControlKind kind,int value=0,float width=.84f,float height=.105f,Material material=null)
    {
        var group=new GameObject(text);group.transform.SetParent(page,false);group.transform.localPosition=position;
        var body=Box(group.transform,"Button",Vector3.zero,new Vector3(width,height,.018f),material??screenCard);
        Control(body,kind,value);Label(group.transform,text,Vector3.forward*.018f,.025f);return group;
    }
    private void DesktopIcon(Transform page,string name,float x,ShopComputerApp app,string symbol)
    {
        var group=MonitorButton(page,"",new Vector3(x,-.015f,.02f),WorldControlKind.OpenApp,(int)app,.76f,.37f);
        group.name=name;
        ScreenPanel(group.transform,"Application icon",new Vector3(0,.065f,.020f),new Vector3(.14f,.11f,.008f),screenAccent);
        Label(group.transform,symbol,new Vector3(0,.065f,.03f),.037f);
        Label(group.transform,name,new Vector3(0,-.105f,.028f),.031f);
    }
    private void BuildMonitor()
    {
        screenBackground=ScreenMaterial("TobaccOS background",new Color(.025f,.055f,.07f));
        screenCard=ScreenMaterial("TobaccOS card",new Color(.075f,.14f,.16f));
        screenAccent=ScreenMaterial("TobaccOS accent",new Color(.025f,.40f,.39f));
        screenBar=ScreenMaterial("TobaccOS header",new Color(.035f,.10f,.12f));
        ScreenPanel(monitor,"TobaccOS screen",new Vector3(0,0,.001f),new Vector3(1.80f,1.02f,.006f),screenBackground);
        ScreenPanel(monitor,"TobaccOS title bar",new Vector3(0,.445f,.010f),new Vector3(1.80f,.12f,.006f),screenBar);
        Label(monitor,"TobaccOS",new Vector3(.61f,.445f,.024f),.044f);
        appTitle=Label(monitor,"ASZTAL",new Vector3(-.23f,.445f,.024f),.032f);
        monitorText=Label(monitor,"",new Vector3(.45f,.326f,.02f),.023f);
        shopStatusText=Label(monitor,"",new Vector3(-.45f,.326f,.02f),.021f);
        desktopPage=Page("Computer Desktop");orderPage=Page("Orders App");furnishingPage=Page("Furnishing App");
        Label(desktopPage,"Minden műszak itt kezdődik.",new Vector3(0,.24f,.025f),.029f);
        DesktopIcon(desktopPage,"ÁRURENDELÉS",.45f,ShopComputerApp.Orders,"ÁRU");
        DesktopIcon(desktopPage,"ÜZLETFEJLESZTÉS",-.45f,ShopComputerApp.Furnishing,"BOLT");
        Label(desktopPage,"A kávé külön fut.",new Vector3(0,-.29f,.025f),.024f);
        orderQuantityText=Label(orderPage,"",new Vector3(.46f,.24f,.027f),.024f);
        MonitorButton(orderPage,"-",new Vector3(.02f,.24f,.020f),WorldControlKind.ChangeOrderQuantity,-1,.14f,.06f,screenAccent);
        MonitorButton(orderPage,"+",new Vector3(-.16f,.24f,.020f),WorldControlKind.ChangeOrderQuantity,1,.14f,.06f,screenAccent);
        Label(orderPage,"SZÁLLÍTÁS: 8 MP",new Vector3(-.57f,.24f,.027f),.019f);
        for(int type=0;type<ShopCatalog.Count;type++) BuildProductCard(type);
        catalogHelp=new GameObject("Delivery help").transform;catalogHelp.SetParent(orderPage,false);
        ScreenPanel(catalogHelp,"Delivery help background",new Vector3(0,-.225f,.01f),new Vector3(1.72f,.23f,.006f),screenBar);
        Label(catalogHelp,"A rendelés a raktárba érkezik.\nA dobozból egyesével töltheted fel a polcokat.",new Vector3(0,-.225f,.024f),.025f);
        Label(furnishingPage,"ÜZLETFEJLESZTÉS",new Vector3(0,.22f,.025f),.037f);
        furnishingStatus=Label(furnishingPage,"",new Vector3(0,.105f,.025f),.024f);
        MonitorButton(furnishingPage,"ÖNKISZOLGÁLÓ BŐVÍTÉS • "+ShopCatalog.Money(ShopLayout.ExpansionPrice),new Vector3(0,-.04f,.02f),WorldControlKind.Expand,0,1.72f,.105f,screenAccent);
        MonitorButton(furnishingPage,"CIGARETTAPOLC • 9 HELY • "+ShopCatalog.Money(ShopLayout.ExtraShelfPrice),new Vector3(0,-.19f,.02f),WorldControlKind.ExtraShelf,0,1.72f);
        Label(furnishingPage,"Fejlesztések vásárlása nyitás előtt.",new Vector3(0,-.30f,.025f),.023f);
        ScreenPanel(monitor,"TobaccOS status bar",new Vector3(0,-.391f,.010f),new Vector3(1.80f,.048f,.006f),screenBar);
        monitorNotice=Label(monitor,"",new Vector3(0,-.39f,.023f),.023f);
        buildingPage=Page("Computer Navigation");
        MonitorButton(buildingPage,"ASZTAL",new Vector3(.45f,-.465f,.02f),WorldControlKind.Desktop,0,.84f,.065f);
        MonitorButton(monitor,"KILÉPÉS • Esc",new Vector3(-.45f,-.465f,.02f),WorldControlKind.Exit,0,.84f,.065f);
        OpenApp(ShopComputerApp.Desktop);
    }
    private void BuildProductCard(int type)
    {
        var root=new GameObject("Product card - "+ShopCatalog.Ids[type]);root.transform.SetParent(orderPage,false);
        root.transform.localPosition=new Vector3(type%2==0?.45f:-.45f,.13f-(type/2)*.145f,.020f);
        var body=Box(root.transform,"Order "+ShopCatalog.ShortNames[type],Vector3.zero,new Vector3(.84f,.13f,.016f),screenCard);Control(body,WorldControlKind.Order,type);
        var prefab=ProductVisuals.LoadModel(type);
        if(prefab!=null)
        {
            var thumbnail=Instantiate(prefab,root.transform,false);thumbnail.name="Product preview";
            thumbnail.transform.localPosition=new Vector3(.30f,.003f,.044f);thumbnail.transform.localRotation=Quaternion.Euler(0,180,0);
            thumbnail.transform.localScale=ProductVisuals.Size(type)*(.095f/ProductVisuals.Size(type).y);
            foreach(var collider in thumbnail.GetComponentsInChildren<Collider>()){collider.enabled=false;Destroy(collider);}
            foreach(var renderer in thumbnail.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        Label(root.transform,ShopCatalog.Names[type],new Vector3(-.075f,.039f,.031f),.026f);
        orderPrices.Add(Label(root.transform,"",new Vector3(-.075f,.002f,.031f),.023f));
        stockLabels.Add(Label(root.transform,"",new Vector3(-.075f,-.041f,.031f),.021f));
        orderCards.Add(root);
        root.SetActive(type<shop.AvailableProducts);
    }
    public void OpenApp(ShopComputerApp app)
    {
        ComputerApp=app;if(desktopPage==null)return;
        desktopPage.gameObject.SetActive(app==ShopComputerApp.Desktop);orderPage.gameObject.SetActive(app==ShopComputerApp.Orders);furnishingPage.gameObject.SetActive(app==ShopComputerApp.Furnishing);
        buildingPage.gameObject.SetActive(app!=ShopComputerApp.Desktop);
        appTitle.text=app==ShopComputerApp.Desktop?"ASZTAL":app==ShopComputerApp.Orders?"TERMÉKRENDELÉS":"ÜZLETFEJLESZTÉS";
        RefreshMonitor();refreshMonitorFrame=Time.frameCount+1;
    }
    public void ChangeOrderQuantity(int delta)
    {
        orderQuantity=Mathf.Clamp(orderQuantity+Mathf.Clamp(delta,-ShopCatalog.MaxOrderQuantity,ShopCatalog.MaxOrderQuantity),1,ShopCatalog.MaxOrderQuantity);
        RefreshMonitor();
    }
    public bool OrderProduct(int type)
    {
        bool success=shop.OrderSupply(type,orderQuantity);lastOrderType=type;lastOrderSucceeded=success;
        orderFeedback=success?"Rendelés elküldve. Átvétel a raktárban.":shop.Message;RefreshMonitor();return success;
    }
    public void ResetComputer()
    {
        orderQuantity=3;lastOrderType=-1;lastOrderSucceeded=false;orderFeedback="";OpenApp(ShopComputerApp.Desktop);
    }
    public void RefreshMonitor()
    {
        if(shop==null||shop.Ledger==null||monitorText==null)return;
        monitorText.text="Kassza: "+ShopCatalog.Money(shop.Ledger.Cash);
        shopStatusText.text=shop.Day+". műszak • "+(shop.Layout.Expanded?"Bővített bolt":"Kezdőbolt");
        orderQuantityText.text="Mennyiség: "+orderQuantity+" db";
        furnishingStatus.text=(shop.Layout.Expanded?"Önkiszolgáló rész: megnyitva":"Önkiszolgáló rész: zárva")+"\n"+(shop.Layout.ExtraShelfOwned?"Kiegészítő polc: megvásárolva":"Kiegészítő polc: nincs");
        for(int type=0;type<orderCards.Count;type++)
        {
            orderCards[type].SetActive(type<shop.AvailableProducts);
            if(type>=shop.AvailableProducts)continue;
            int cost=ShopCatalog.Costs[type]*orderQuantity;
            orderPrices[type].text=orderQuantity+" db • "+ShopCatalog.Money(cost);
            orderPrices[type].color=cost<=shop.Ledger.Cash?new Color(.55f,.93f,.81f):new Color(1f,.66f,.51f);
            stockLabels[type].text="Polc "+shop.StockCount(type)+" • Raktár "+shop.WarehouseCount(type)+" • Úton "+shop.PendingQuantity(type);
        }
        catalogHelp.gameObject.SetActive(shop.AvailableProducts==ShopCatalog.CigaretteCount);
        if(ComputerApp==ShopComputerApp.Orders)
        {
            string notice=orderFeedback;
            if(lastOrderSucceeded&&lastOrderType>=0&&shop.PendingQuantity(lastOrderType)==0)notice="Megérkezett: "+ShopCatalog.ShortNames[lastOrderType]+" • a raktárban átvehető.";
            if(string.IsNullOrEmpty(notice))notice="Válassz mennyiséget, majd kattints a termékre a rendeléshez.";
            monitorNotice.text=notice.Length>105?notice.Substring(0,102)+"...":notice;
        }
        else monitorNotice.text=ComputerApp==ShopComputerApp.Desktop?"TobaccOS • Kis bolt. Nagy történetek.":"A fejlesztések a meglévő boltodat bővítik.";
    }
    private void OnFontTextureRebuilt(Font font)
    {
        if(FocusKind==ShopDeviceKind.Monitor)refreshMonitorFrame=Time.frameCount+1;
    }
    private void LateUpdate()
    {
        if(FocusKind!=ShopDeviceKind.Monitor||refreshMonitorFrame<0||Time.frameCount<refreshMonitorFrame)return;
        refreshMonitorFrame=-1;
        foreach(var label in monitor.GetComponentsInChildren<TextMesh>())
        { string text=label.text;label.text="";label.text=text; }
    }
    private void BuildDrawer()
    {
        cashText = Label(tillDisplay,"KASSZA",Vector3.forward*.018f,.0135f);
        cashText.color = new Color(.68f,1f,.60f);
        for (int i = 0; i < ShopCatalog.Denominations.Length; i++)
        {
            int value = ShopCatalog.Denominations[i];
            bool banknote = value >= 500;
            int column = banknote ? i : i-6;
            Vector3 p = new Vector3((2.5f-column)*.084f,.014f,banknote?-.062f:.095f);
            var compartment = Box(drawer,ShopCatalog.Money(value),p,new Vector3(.077f,.012f,banknote?.16f:.104f),bodyMaterial);
            Control(compartment,WorldControlKind.Denomination,value);
            // Raised dividers distinguish a fitted cash insert from a flat button grid.
            Box(drawer,"Rekeszfal",p+new Vector3(.04f,.015f,0),new Vector3(.005f,.04f,banknote?.16f:.104f),bodyMaterial);
            var money = Money(drawer,value,p+Vector3.up*.013f,1f);
            Control(money,WorldControlKind.Denomination,value); cashStacks.Add(money);
            Vector3 label = p+new Vector3(0,.043f,banknote?-.062f:.032f);
            moneyLabels.Add(ProductVisuals.Text(drawer,"Címlet és darabszám","",label,Quaternion.Euler(90,180,0),.011f,Color.white));
        }
    }
    private void BuildKeypad()
    {
        Key("VISSZA",new Vector3(.12f,.008f,-.045f),WorldControlKind.Undo);
        Key("TÖRLÉS",new Vector3(0,.008f,-.045f),WorldControlKind.Clear);
        Key("VÁLTÁS",new Vector3(-.12f,.008f,-.045f),WorldControlKind.Exchange);
        Key("ÁTADÁS",new Vector3(0,.008f,.045f),WorldControlKind.Pay,.34f);
    }
    private void Key(string label,Vector3 p,WorldControlKind kind,float width=.108f)
    {
        var go=Box(keypad,label,p,new Vector3(width,.018f,.065f),bodyMaterial);Control(go,kind);
        ProductVisuals.Text(keypad,label,label,p+Vector3.up*.013f,Quaternion.Euler(90,180,0),.014f,Color.white);
    }
    private GameObject Money(Transform parent,int value,Vector3 p,float scale)
    {
        var root=new GameObject((value>=500?"Bankjegy ":"Érme ")+value);
        root.transform.SetParent(parent,false);root.transform.localPosition=p;
        root.transform.localScale=Vector3.one*scale;
        GameObject mesh;
        if(value>=500)mesh=Box(root.transform,"Pénz",Vector3.zero,new Vector3(.065f,.005f,.135f),paperMaterial);
        else
        {
            mesh=GameObject.CreatePrimitive(PrimitiveType.Cylinder);mesh.name="Pénz";mesh.transform.SetParent(root.transform,false);
            mesh.transform.localScale=new Vector3(.043f,.003f,.043f);mesh.GetComponent<Renderer>().sharedMaterial=coinMaterial;
        }
        var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",MoneyColor(value));mesh.GetComponent<Renderer>().SetPropertyBlock(block);
        ProductVisuals.Text(root.transform,"Pénzérték",value.ToString(),Vector3.up*.006f,Quaternion.Euler(90,180,0),.013f,Color.black);
        return root;
    }
    private static Color MoneyColor(int value)
    {
        switch(value)
        {
            case 20000:return new Color(.64f,.76f,.66f);case 10000:return new Color(.65f,.69f,.83f);
            case 5000:return new Color(.85f,.76f,.55f);case 2000:return new Color(.8f,.62f,.57f);
            case 1000:return new Color(.65f,.77f,.83f);case 500:return new Color(.78f,.58f,.63f);
            case 100:case 200:return new Color(.76f,.69f,.42f);default:return new Color(.75f,.75f,.7f);
        }
    }
    public void Focus(ShopDeviceKind kind)
    {
        if(view==null||shop==null||shop.HasHeldProduct)return;
        if(Focused)Release();
        cameraPosition=view.transform.localPosition;cameraRotation=view.transform.localRotation;
        FocusKind=kind;ShopInputGate.Blocked=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        if(kind==ShopDeviceKind.Monitor)
        {
            OpenApp(ShopComputerApp.Desktop);
            // Screen is a scaled logical canvas; all camera offsets follow the physical monitor.
            float tangent=Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad*.5f);
            float distance=.5f*Mathf.Max(1.18f/tangent,1.95f/(Mathf.Max(.5f,view.aspect)*tangent))*1.04f;
            view.transform.position=monitor.TransformPoint(new Vector3(0,0,distance));
            view.transform.LookAt(monitor.TransformPoint(new Vector3(0,-.025f,0)),monitor.up);
        }
        else
        {
            registerDrawer.SetOpen(true);
            view.transform.position=tillAssembly.TransformPoint(new Vector3(0,.90f,.90f));
            view.transform.LookAt(tillAssembly.TransformPoint(new Vector3(0,.13f,.25f)),tillAssembly.up);
        }
    }
    public void Release()
    {
        if(!Focused)return;
        if(view!=null){view.transform.localPosition=cameraPosition;view.transform.localRotation=cameraRotation;}
        if(registerDrawer!=null)registerDrawer.SetOpen(false);
        FocusKind=ShopDeviceKind.None;ShopInputGate.Blocked=false;
    }
    private void Update()
    {
        if(shop==null||shop.Ledger==null)return;
        RefreshMonitor();
        for(int i=0;i<moneyLabels.Count;i++)
        {
            int v=ShopCatalog.Denominations[i];moneyLabels[i].text=v+" ×"+(shop.Drawer.Count(v)-shop.SelectedPieces(v));
            cashStacks[i].SetActive(shop.Drawer.Count(v)>shop.SelectedPieces(v));
        }
        cashText.text=shop.Stage==ServiceStage.Payment&&shop.ActiveCustomer!=null
            ? "FIZETENDŐ "+ShopCatalog.Money(ShopCatalog.Prices[shop.ActiveCustomer.Product])+"\nKAPOTT "+ShopCatalog.Money(shop.ActiveCustomer.Tender)+"\nVISSZAJÁRÓ "+ShopCatalog.Money(shop.ActiveCustomer.Tender-ShopCatalog.Prices[shop.ActiveCustomer.Product])+"\nKIVÉVE "+ShopCatalog.Money(shop.ChangeSelected)
            : "KASSZA\n"+ShopCatalog.Money(shop.Ledger.Cash);
        string selection=string.Join(",",ShopCatalog.Denominations.Select(shop.SelectedPieces));
        if(lastSelection!=selection){lastSelection=selection;RefreshTray();}
        if(!Focused||!Application.isFocused||Mouse.current==null||!Mouse.current.leftButton.wasPressedThisFrame)return;
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)return;
        Ray ray=view.ScreenPointToRay(Mouse.current.position.ReadValue());
        if(Physics.Raycast(ray,out RaycastHit hit,4f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
        {
            var control=hit.collider.GetComponentInParent<ShopWorldControl>();
            if(control==null)return;
            bool valid=FocusKind==ShopDeviceKind.Monitor?control.transform.IsChildOf(monitor):control.transform.IsChildOf(tillAssembly);
            if(control.Kind==WorldControlKind.Denomination&&!registerDrawer.IsOpen)valid=false;
            if(valid)control.Activate(shop);
        }
    }
    private void RefreshTray()
    {
        foreach(var go in selectedVisuals)if(go!=null)Destroy(go);selectedVisuals.Clear();
        int index=0;
        foreach(int v in ShopCatalog.Denominations)
            for(int n=0;n<shop.SelectedPieces(v);n++)
            {
                var root=new GameObject("Kivett "+v);root.transform.SetParent(changeTray,false);
                root.transform.localPosition=new Vector3((index%3-1)*.08f,.015f+(index/6)*.008f,(index/3%2)*.12f-.06f);
                Money(root.transform,v,Vector3.zero,.8f);selectedVisuals.Add(root);index++;
            }
    }
    private void OnDisable()=>Release();
    private void OnDestroy()
    { Font.textureRebuilt-=OnFontTextureRebuilt;if(bodyMaterial!=null)Destroy(bodyMaterial);if(paperMaterial!=null)Destroy(paperMaterial);if(coinMaterial!=null)Destroy(coinMaterial);foreach(var material in screenMaterials)if(material!=null)Destroy(material); }
}
