using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ShopDeviceKind { None, Monitor, Till, Coins, Terminal, Coffee, Lottery, Mobile }
public enum ShopComputerApp { Desktop, Orders, Furnishing, Coffee, Cameras, Games, Prices, Lottery, Reputation, Mobile }
public sealed partial class ShopDevices : MonoBehaviour
{
    [SerializeField] private Transform monitor, drawer, changeTray, tillDisplay;
    [SerializeField] private Transform monitorAssembly, tillAssembly, keypad;
    [SerializeField] private CashRegisterDrawer registerDrawer;
    private MvpShopController shop;
    private Camera view;
    private readonly ShopDeviceFocus focus = new ShopDeviceFocus();
    private TextMesh monitorText, appTitle;
    private TextMesh shopStatusText;
    public Rect AppRect
    {
        get
        {
            return ComputerToScreen(ComputerBody);
        }
    }
    private string furnishingFeedback="";
    private readonly ShopRuntimeResources resources = new ShopRuntimeResources();
    private bool initialized;
    private Material screenBackground, screenCard, screenAccent, screenBar;
    private int orderQuantity = 3, lastOrderType = -1;
    private bool lastOrderSucceeded;
    private string orderFeedback = "";
    public int OrderQuantity => orderQuantity;
    private Transform desktopPage;
    public ShopComputerApp ComputerApp { get; private set; }
    private readonly List<TMP_Text> moneyLabels = new List<TMP_Text>();
    private readonly List<GameObject> selectedVisuals = new List<GameObject>();
    private readonly List<GameObject> cashStacks = new List<GameObject>();
    private string lastSelection = "";
    private int refreshMonitorFrame=-1;
    private Material bodyMaterial;
    public ShopDeviceKind FocusKind => focus.Kind;
    public bool Focused => FocusKind != ShopDeviceKind.None;
    public Vector3 MonitorPosition => monitor.position;
    public CashRegisterDrawer RegisterDrawer => registerDrawer;
    public Transform TillAssembly => tillAssembly;
    public ShopCardTerminal Terminal { get; private set; }
    public ShopRegisterVisuals RegisterVisuals { get; private set; }
    public ShopMoneyVisuals MoneyVisuals { get; private set; }
    public void Configure(Transform screen, Transform contents, Transform tray, Transform display,
        Transform monitorRoot, Transform tillRoot, Transform keyboard, CashRegisterDrawer mechanism)
    {
        monitor = screen; drawer = contents; changeTray = tray; tillDisplay = display;
        monitorAssembly = monitorRoot; tillAssembly = tillRoot; keypad = keyboard; registerDrawer = mechanism;
    }
    private void Start()
    {
        shop = GetComponent<MvpShopController>(); view = Camera.main;
        bodyMaterial = resources.Own(Material(new Color(.045f,.06f,.07f)));
        MoneyVisuals = gameObject.AddComponent<ShopMoneyVisuals>();
        RegisterVisuals = gameObject.AddComponent<ShopRegisterVisuals>();
        RegisterVisuals.Configure(tillAssembly, registerDrawer, keypad, tillDisplay);
        BuildMonitor(); BuildDrawer(); RegisterVisuals.BuildKeys(keypad);
        Terminal = new GameObject("Bank card terminal").AddComponent<ShopCardTerminal>();
        Terminal.Configure(shop, tillAssembly);
        initialized = true;
        SubscribeFont();
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
        => ProductVisuals.Text(parent, "Felirat", text, position, Quaternion.Euler(0,180,0), size, ShopUi.Paper);
    private Transform Page(string name)
    {
        var page=new GameObject(name).transform;page.SetParent(monitor,false);return page;
    }
    private Material ScreenMaterial(string name,Color color)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name=name,color=color};resources.Own(m);return m;
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
    private void BuildMonitor()
    {
        screenBackground=ScreenMaterial("TobaccOS background",ShopComputerTheme.Brown);
        screenCard=ScreenMaterial("TobaccOS card",new Color(.36f,.23f,.16f));
        screenAccent=ScreenMaterial("TobaccOS accent",ShopComputerTheme.Green);
        screenBar=ScreenMaterial("TobaccOS header",ShopComputerTheme.Green);
        ScreenPanel(monitor,"TobaccOS screen",new Vector3(0,0,.001f),new Vector3(1.80f,1.02f,.006f),screenBackground);
        ScreenPanel(monitor,"TobaccOS title bar",new Vector3(0,.445f,.010f),new Vector3(1.80f,.12f,.006f),screenBar);
        appTitle=Label(monitor,"TobaccOS",new Vector3(.52f,.445f,.024f),.042f);
        monitorText=Label(monitor,"",new Vector3(-.35f,.445f,.024f),.026f);
        shopStatusText=Label(monitor,"",new Vector3(0,-.45f,.024f),.025f);
        desktopPage=Page("Computer Desktop");
        for(int i=1;i<=6;i++) {
            var p=new Vector3(.47f-(i-1)%2*.94f,.22f-(i-1)/2*.21f,.02f);
            MonitorButton(desktopPage,AppNames[i],p,WorldControlKind.OpenApp,i,.84f,.16f);
        }
        var red=ScreenMaterial("TobaccOS tricolor red",ShopComputerTheme.Red);
        var cream=ScreenMaterial("TobaccOS tricolor cream",ShopComputerTheme.Paper);
        ScreenPanel(monitor,"Tricolor red",new Vector3(0,.378f,.018f),new Vector3(1.8f,.008f,.004f),red);
        ScreenPanel(monitor,"Tricolor white",new Vector3(0,.369f,.018f),new Vector3(1.8f,.008f,.004f),cream);
        ScreenPanel(monitor,"Tricolor green",new Vector3(0,.360f,.018f),new Vector3(1.8f,.008f,.004f),screenBar);
        OpenApp(ShopComputerApp.Desktop);
    }
    public void OpenApp(ShopComputerApp app)
    {
        shop.Pricing?.CancelEdit();
        ResetKeyboardFocus();
        cartEditing=false;
        ComputerApp=app;
        if(app==ShopComputerApp.Cameras)shop.Activities.RenderCamera(shop.Activities.SelectedCamera);
        RefreshMonitor();
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
    public bool PurchasePanicButton()
    {
        bool purchased=shop.BuyPanicButton();furnishingFeedback=shop.Message;RefreshMonitor();return purchased;
    }
    public bool BeginArrangement()
    {
        bool started = shop.Arrangement.Begin();
        furnishingFeedback = shop.Arrangement.Feedback;
        RefreshMonitor();
        return started;
    }
    public bool PurchaseFridge()
    {
        bool purchased = shop.GetComponent<ShopNextUpgrades>().BuyFridge();
        furnishingFeedback = shop.Message;
        RefreshMonitor();
        return purchased;
    }
    public void ResetComputer()
    {
        System.Array.Fill(cartInputs,"0");
        orderQuantity=3;lastOrderType=-1;lastOrderSucceeded=false;orderFeedback="";furnishingFeedback="";OpenApp(ShopComputerApp.Desktop);
    }
    public void RefreshMonitor()
    {
        if(shop==null||shop.Ledger==null||monitorText==null)return;
        monitorText.text=ShopCatalog.Money(shop.Ledger.Cash);
        shopStatusText.text=shop.Day+". műszak   "+ShopUiState.Clock(shop.ClockElapsed);
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
            moneyLabels.Add(RegisterVisuals.DenominationLabel(drawer, value, p.x));
        }
    }
    private GameObject Money(Transform parent, int value, Vector3 position, float scale)
        => MoneyVisuals.Create(parent, value, position, scale);
    public void Focus(ShopDeviceKind kind)
    {
        if (kind == ShopDeviceKind.Coffee && (shop == null || !shop.Activities.CoffeeOwned)) return;
        if (kind == ShopDeviceKind.Mobile && (shop == null || !shop.Mobile.Owned)) return;
        if (kind == ShopDeviceKind.Lottery && (shop == null || !shop.Lottery.Owned)) return;
        if (kind == ShopDeviceKind.Till && shop != null && shop.CardPayment) kind = ShopDeviceKind.Terminal;
        if(view==null||shop==null||shop.HasHeldProduct)return;
        if (kind == ShopDeviceKind.Coins && (shop.Activities.CustomerHand == null ||
            shop.Activities.CoinOffer == null || !shop.CoinPayment || shop.Stage != ServiceStage.Payment)) return;
        if(Focused)Release(false);
        ResetKeyboardFocus();focus.Enter(kind, view);
        if(kind==ShopDeviceKind.Monitor)
        {
            OpenApp(ShopComputerApp.Desktop);
            // Screen is a scaled logical canvas; all camera offsets follow the physical monitor.
            float tangent=Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad*.5f);
            float distance=.5f*Mathf.Max(1.18f/tangent,1.95f/(Mathf.Max(.5f,view.aspect)*tangent))*1.04f;
            view.transform.position=monitor.TransformPoint(new Vector3(0,0,distance));
            view.transform.LookAt(monitor.TransformPoint(new Vector3(0,-.025f,0)),monitor.up);
        }
        else if (kind == ShopDeviceKind.Lottery)
        {
            registerDrawer.SetOpen(false);
            shop.Lottery.Focus(view);
        }
        else if (kind == ShopDeviceKind.Coffee)
        {
            registerDrawer.SetOpen(false);
            shop.Activities.CoffeeDisplay.Focus(view);
        }
        else if (kind == ShopDeviceKind.Terminal || kind == ShopDeviceKind.Mobile)
        {
            registerDrawer.SetOpen(false);
            Terminal.Focus(view);
        }
        else if(kind==ShopDeviceKind.Coins)
        {
            shop.Activities.FocusCustomerCoins(view);
        }
        else
        {
            registerDrawer.SetOpen(!shop.CardPayment);
            RegisterVisuals.Focus(view);
        }
    }
    public void Release(bool restoreCursor = true)
    {
        if(!Focused)return;
        if (FocusKind == ShopDeviceKind.Coins) shop.Activities.SetCustomerHandFocused(false);

        if(registerDrawer!=null)registerDrawer.SetOpen(false);
        focus.Exit(restoreCursor);
    }
    private void Update()
    {
        if(shop==null||shop.Ledger==null||ShopMenu.Active)return;
        RefreshMonitor();
        Terminal.Refresh();
        for(int i=0;i<moneyLabels.Count;i++)
        {
            int v=ShopCatalog.Denominations[i];moneyLabels[i].text=ShopCatalog.Money(v).Replace(" Ft", "")+"\n<size=70%>× "+(shop.Drawer.Count(v)-shop.SelectedPieces(v))+"</size>";
            cashStacks[i].SetActive(shop.Drawer.Count(v)>shop.SelectedPieces(v));
        }
        RegisterVisuals.Refresh(shop);
        if (FocusKind == ShopDeviceKind.Till && RegisterVisuals.NeedsReframe(view)) RegisterVisuals.Focus(view);
        string selection=string.Join(",",ShopCatalog.Denominations.Select(shop.SelectedPieces));
        if(lastSelection!=selection){lastSelection=selection;RefreshTray();}
        PollDeviceKeyboard();
        if(FocusKind==ShopDeviceKind.Monitor||FocusKind==ShopDeviceKind.Lottery||FocusKind==ShopDeviceKind.Mobile)return;
        if(ShopInputGate.WorldActionConsumed||!Focused||!Application.isFocused||Mouse.current==null||!Mouse.current.leftButton.wasPressedThisFrame)return;
        if(Keyboard.current!=null&&ShopBindings.Pressed(UnityEngine.InputSystem.Key.Escape))return;
        Ray ray=view.ScreenPointToRay(Mouse.current.position.ReadValue());
        if(Physics.Raycast(ray,out RaycastHit hit,4f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
        {
            var control=hit.collider.GetComponentInParent<ShopWorldControl>();
            if(control==null)return;
            bool valid=FocusKind==ShopDeviceKind.Monitor?control.transform.IsChildOf(monitor):FocusKind==ShopDeviceKind.Coins?shop.Activities.CustomerHand!=null&&control.transform.IsChildOf(shop.Activities.CustomerHand):FocusKind==ShopDeviceKind.Terminal?control.transform.IsChildOf(Terminal.transform):FocusKind==ShopDeviceKind.Coffee?control.transform.IsChildOf(shop.Activities.CoffeeDisplay.transform):control.transform.IsChildOf(tillAssembly);
            if(control.Kind==WorldControlKind.Denomination&&!registerDrawer.IsOpen)valid=false;
            if(FocusKind==ShopDeviceKind.Till&&control.transform.IsChildOf(Terminal.transform))valid=false;
            if(valid){keyboardOutline=false;control.Activate(shop);}
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
    private void SubscribeFont() { Font.textureRebuilt -= OnFontTextureRebuilt; Font.textureRebuilt += OnFontTextureRebuilt; }
    private void OnEnable() { if (initialized) SubscribeFont(); }
    private void OnDisable() { Font.textureRebuilt -= OnFontTextureRebuilt; Release(); }
    private void OnDestroy()
    { Font.textureRebuilt -= OnFontTextureRebuilt; resources.Dispose(); }
}
