using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ShopDeviceKind { None, Monitor, Till }
public sealed class ShopDevices : MonoBehaviour
{
    [SerializeField] private Transform monitor, drawer, changeTray, tillDisplay;
    [SerializeField] private Transform monitorAssembly, tillAssembly, keypad;
    [SerializeField] private CashRegisterDrawer registerDrawer;
    private MvpShopController shop;
    private Camera view;
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private TextMesh monitorText, cashText;
    private readonly List<TextMesh> stockLabels = new List<TextMesh>();
    private readonly List<TextMesh> moneyLabels = new List<TextMesh>();
    private readonly List<GameObject> selectedVisuals = new List<GameObject>();
    private readonly List<GameObject> cashStacks = new List<GameObject>();
    private string lastSelection = "";
    private Material bodyMaterial, paperMaterial, coinMaterial;
    public ShopDeviceKind FocusKind { get; private set; }
    public bool Focused => FocusKind != ShopDeviceKind.None;
    public Vector3 MonitorPosition => monitorAssembly.position;
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
    private void MonitorButton(string text, Vector3 position, WorldControlKind kind, int value = 0)
    {
        var go = Box(monitor, text, position, new Vector3(.84f,.105f,.02f), bodyMaterial);
        Control(go, kind, value); Label(monitor, text, position + Vector3.forward * .015f, .029f);
    }
    private void BuildMonitor()
    {
        Label(monitor,"TRAFIK • BESZERZÉS",new Vector3(0,.42f,.02f),.047f);
        monitorText = Label(monitor,"",new Vector3(0,.325f,.02f),.028f);
        for (int i = 0; i < ShopCatalog.Count; i++)
        {
            float y = .17f-(i/2)*.19f;
            float x = i%2==0 ? .45f : -.45f;
            stockLabels.Add(Label(monitor,"",new Vector3(x,y+.065f,.025f),.021f));
            MonitorButton("3 × "+ShopCatalog.Names[i]+"  |  "+ShopCatalog.Money(ShopCatalog.Costs[i]*3),new Vector3(x,y-.008f,.02f),WorldControlKind.Order,i);
        }
        MonitorButton("BŐVÍTÉS • "+ShopCatalog.Money(ShopLayout.ExpansionPrice),new Vector3(.45f,-.435f,.02f),WorldControlKind.Expand);
        MonitorButton("VISSZA • Esc",new Vector3(-.45f,-.435f,.02f),WorldControlKind.Exit);
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
            // Screen is a scaled logical canvas; all camera offsets follow the physical monitor.
            view.transform.position=monitor.TransformPoint(new Vector3(0,0,1.35f));
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
        monitorText.text="Kassza: "+ShopCatalog.Money(shop.Ledger.Cash)+" • "+(shop.Layout.Expanded?"BŐVÍTVE":"KEZDŐBOLT");
        for(int i=0;i<stockLabels.Count;i++)stockLabels[i].text=i>=shop.AvailableProducts?"BŐVÍTÉS UTÁN RENDELHETŐ":("Polc: "+shop.StockCount(i)+" • Doboz: "+shop.WarehouseCount(i)+" • Úton: "+shop.PendingQuantity(i));
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
    { if(bodyMaterial!=null)Destroy(bodyMaterial);if(paperMaterial!=null)Destroy(paperMaterial);if(coinMaterial!=null)Destroy(coinMaterial); }
}
