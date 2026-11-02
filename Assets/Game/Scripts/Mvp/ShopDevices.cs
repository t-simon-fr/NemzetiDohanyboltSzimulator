using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ShopDeviceKind { None, Monitor, Till }
public sealed class ShopDevices : MonoBehaviour
{
    [SerializeField] private Transform monitor,drawer,changeTray,tillDisplay;
    private MvpShopController shop;
    private Camera view;
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private TextMesh monitorText,cashText;
    private readonly List<TextMesh> stockLabels=new List<TextMesh>();
    private readonly List<TextMesh> moneyLabels=new List<TextMesh>();
    private readonly List<GameObject> selectedVisuals=new List<GameObject>();
    private string lastSelection="";
    private readonly List<GameObject> cashStacks=new List<GameObject>();
    private Material bodyMaterial,paperMaterial,coinMaterial;
    public ShopDeviceKind FocusKind { get; private set; }
    public bool Focused=>FocusKind!=ShopDeviceKind.None;
    public Vector3 MonitorPosition=>monitor.position;
    public void Configure(Transform screen,Transform cashDrawer,Transform tray,Transform display)
    {monitor=screen;drawer=cashDrawer;changeTray=tray;tillDisplay=display;}
    private void Start()
    {
        shop=GetComponent<MvpShopController>();view=Camera.main;
        bodyMaterial=Material(new Color(.07f,.09f,.11f));paperMaterial=Material(new Color(.65f,.73f,.6f));coinMaterial=Material(new Color(.72f,.62f,.34f));
        BuildMonitor();BuildDrawer();
    }
    private static Material Material(Color color)=>new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};
    private GameObject Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;
    }
    private void Control(GameObject go,WorldControlKind kind,int value=0)
    {var c=go.AddComponent<ShopWorldControl>();c.Kind=kind;c.Value=value;}
    private TextMesh Label(Transform parent,string text,Vector3 position,float size=.035f)
        =>ProductVisuals.Text(parent,"Felirat",text,position,Quaternion.Euler(0,180,0),size,Color.white);
    private void MonitorButton(string text,Vector3 position,WorldControlKind kind,int value=0)
    {
        var go=Box(monitor,text,position,new Vector3(1.12f,.065f,.02f),bodyMaterial);Control(go,kind,value);
        Label(monitor,text,position+Vector3.forward*.015f,.029f);
    }
    private void BuildMonitor()
    {
        Label(monitor,"TRAFIK • BESZERZÉS",new Vector3(0,.42f,.02f),.047f);
        monitorText=Label(monitor,"",new Vector3(0,.325f,.02f),.028f);
        for(int i=0;i<ShopCatalog.Count;i++)
        {
            float y=.22f-i*.095f;
            stockLabels.Add(Label(monitor,"",new Vector3(0,y+.019f,.025f),.022f));
            MonitorButton("3 × "+ShopCatalog.Names[i]+"  |  "+ShopCatalog.Money(ShopCatalog.Costs[i]*3),new Vector3(0,y-.027f,.02f),WorldControlKind.Order,i);
        }
        MonitorButton("BOLTBŐVÍTÉS • "+ShopCatalog.Money(ShopLayout.ExpansionPrice),new Vector3(0,-.41f,.02f),WorldControlKind.Expand);
        MonitorButton("VISSZA A BOLTBA (Esc)",new Vector3(0,-.495f,.02f),WorldControlKind.Exit);
    }
    private void BuildDrawer()
    {
        cashText=Label(tillDisplay,"KASSZA",new Vector3(0,0,.015f),.038f);
        for(int i=0;i<ShopCatalog.Denominations.Length;i++)
        {
            int value=ShopCatalog.Denominations[i];int col=i%4,row=i/4;
            Vector3 p=new Vector3((1.5f-col)*.275f,.025f,(1-row)*.19f);
            var compartment=Box(drawer,ShopCatalog.Money(value),p,new Vector3(.26f,.035f,.18f),bodyMaterial);Control(compartment,WorldControlKind.Denomination,value);
            var money=Money(drawer,value,p+Vector3.up*.028f,.8f);Control(money,WorldControlKind.Denomination,value);cashStacks.Add(money);
            moneyLabels.Add(ProductVisuals.Text(drawer,"Címlet és darabszám","",p+new Vector3(0,.057f,-.05f),Quaternion.Euler(90,180,0),.024f,Color.white));
        }
        DrawerButton("VÁLTÓPÉNZ",new Vector3(0,.02f,.4f),WorldControlKind.Exchange);
        DrawerButton("UTOLSÓ VISSZA",new Vector3(.35f,.02f,-.43f),WorldControlKind.Undo);
        DrawerButton("VISSZATÖLT",new Vector3(0,.02f,-.43f),WorldControlKind.Clear);
        DrawerButton("ÁTADÁS",new Vector3(-.35f,.02f,-.43f),WorldControlKind.Pay);
    }
    private void DrawerButton(string label,Vector3 p,WorldControlKind kind)
    {
        var go=Box(drawer,label,p,new Vector3(.32f,.03f,.13f),bodyMaterial);Control(go,kind);
        ProductVisuals.Text(drawer,label,label,p+Vector3.up*.022f,Quaternion.Euler(90,180,0),.025f,Color.white);
    }
    private GameObject Money(Transform parent,int value,Vector3 p,float scale)
    {
        GameObject go;
        if(value>=500)go=Box(parent,"Bankjegy "+value,p,new Vector3(.23f,.01f,.12f)*scale,paperMaterial);
        else
        {
            go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name="Érme "+value;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=new Vector3(.09f,.006f,.09f)*scale;go.GetComponent<Renderer>().sharedMaterial=coinMaterial;
        }
        // Distinct color families, printed denomination: placeholder for final HUF artwork.
        var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",MoneyColor(value));go.GetComponent<Renderer>().SetPropertyBlock(block);
        ProductVisuals.Text(parent,"Pénzérték",value.ToString(),p+Vector3.up*.014f,Quaternion.Euler(90,180,0),.032f*scale,Color.black);
        return go;
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
            Vector3 p=monitor.position+new Vector3(0,0,1.35f);view.transform.position=p;view.transform.LookAt(monitor.position+Vector3.up*-.025f);
        }
        else
        {
            Vector3 p=drawer.position+new Vector3(.06f,1.25f,.7f);view.transform.position=p;view.transform.LookAt(drawer.position+new Vector3(-.05f,0,-.08f));
        }
    }
    public void Release()
    {
        if(!Focused)return;
        if(view!=null){view.transform.localPosition=cameraPosition;view.transform.localRotation=cameraRotation;}
        FocusKind=ShopDeviceKind.None;ShopInputGate.Blocked=false;
    }
    private void Update()
    {
        if(shop==null||shop.Ledger==null)return;
        monitorText.text="Kassza: "+ShopCatalog.Money(shop.Ledger.Cash)+" • "+(shop.Layout.Expanded?"BŐVÍTVE":"KEZDŐBOLT");
        for(int i=0;i<stockLabels.Count;i++)stockLabels[i].text=i>=shop.AvailableProducts?"BŐVÍTÉS UTÁN RENDELHETŐ":("Polc: "+shop.StockCount(i)+" • Doboz: "+shop.WarehouseCount(i)+" • Úton: "+shop.PendingQuantity(i));
        for(int i=0;i<moneyLabels.Count;i++)
        {
            int v=ShopCatalog.Denominations[i];moneyLabels[i].text=ShopCatalog.Money(v)+"  × "+(shop.Drawer.Count(v)-shop.SelectedPieces(v));
            cashStacks[i].SetActive(shop.Drawer.Count(v)>shop.SelectedPieces(v));
        }
        if(shop.Stage==ServiceStage.Payment&&shop.ActiveCustomer!=null)
            cashText.text="Fizetendő: "+ShopCatalog.Money(ShopCatalog.Prices[shop.ActiveCustomer.Product])+"\nKapott pénz: "+ShopCatalog.Money(shop.ActiveCustomer.Tender)+"\nKivéve: "+ShopCatalog.Money(shop.ChangeSelected);
        else cashText.text="KASSZA\n"+ShopCatalog.Money(shop.Ledger.Cash);
        string selection=string.Join(",",ShopCatalog.Denominations.Select(shop.SelectedPieces));
        if(lastSelection!=selection){lastSelection=selection;RefreshTray();}
        if(!Focused||!Application.isFocused||Mouse.current==null||!Mouse.current.leftButton.wasPressedThisFrame)return;
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)return;
        Ray ray=view.ScreenPointToRay(Mouse.current.position.ReadValue());
        if(Physics.Raycast(ray,out RaycastHit hit,4f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
        {
            var control=hit.collider.GetComponent<ShopWorldControl>();
            if(control!=null && (FocusKind==ShopDeviceKind.Monitor?control.transform.IsChildOf(monitor):control.transform.IsChildOf(drawer)))control.Activate(shop);
        }
    }
    private void RefreshTray()
    {
        foreach(var go in selectedVisuals)if(go!=null)Destroy(go);selectedVisuals.Clear();
        // Each selected piece is shown, including repeated denominations.
        int index=0;
        foreach(int v in ShopCatalog.Denominations)
            for(int n=0;n<shop.SelectedPieces(v);n++)
            {
                var root=new GameObject("Kivett "+v);root.transform.SetParent(changeTray,false);
                root.transform.localPosition=new Vector3((index%3-1)*.12f,.025f+(index/6)*.018f,(index/3%2)*.13f-.065f);
                Money(root.transform,v,Vector3.zero,.65f);selectedVisuals.Add(root);index++;
            }
    }
    private void OnDisable(){Release();}
    private void OnDestroy(){if(bodyMaterial!=null)Destroy(bodyMaterial);if(paperMaterial!=null)Destroy(paperMaterial);if(coinMaterial!=null)Destroy(coinMaterial);}
}
