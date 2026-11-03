using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ShopPhase { Preparing, Open, Closing, Summary, AfterHours }
public enum ShopModal { None, Welcome, Help, Supplier, Cash, Summary }
public enum ServiceStage { None, Order, Payment, Thanks }

public sealed partial class MvpShopController : MonoBehaviour
{
    [SerializeField] private PlayerInteractor player;
    [SerializeField] private ProductSlot delivery;
    [SerializeField] private GameObject customerTemplate;
    [SerializeField] private ProductSlot[] shelfSlots;
    [SerializeField] private int[] shelfTypes;
    [SerializeField] private Material[] productMaterials;
    [SerializeField] private Transform supplyPoint;
    [SerializeField] private Transform tillPoint;
    private readonly List<ShopProduct> stock = new List<ShopProduct>();
    private readonly List<MvpCustomerActor> queue = new List<MvpCustomerActor>();
    private readonly List<MvpCustomerActor> departing = new List<MvpCustomerActor>();
    private readonly List<Shipment> shipments = new List<Shipment>();
    private readonly List<int> changeCoins = new List<int>();
    private readonly int[] warehouse = new int[ShopCatalog.Count];
    private MvpCustomerActor active;
    private ShopProduct prepared;
    private float spawnTimer;
    private float thanksTimer;
    private Transform stockRoot;
    private Transform actorRoot;
    private sealed class Shipment { public int Type; public int Quantity; public float Remaining; }
    public static MvpShopController Instance { get; private set; }
    public ShopPhase Phase { get; private set; }
    public ShopModal Modal { get; private set; }
    public ServiceStage Stage { get; private set; }
    public ShopLedger Ledger { get; private set; }
    public ShopPricing Pricing {get;private set;}
    public ShopAfterHours AfterHours {get;private set;}
    public int PriceRefusals {get;private set;}
    public int Day { get; private set; } = 1;
    public float Elapsed { get; private set; }
    public int Spawned { get; private set; }
    public int Missed { get; private set; }
    public int QueueCount => queue.Count;
    public int SelectedSupply { get; private set; }
    public int ChangeSelected => changeCoins.Sum();
    public int TotalVisitors => ShopCatalog.VisitProducts.Length;
    public MvpCustomerActor ActiveCustomer => active;
    public ProductSlot DeliverySlot => delivery;
    public string Dialogue { get; private set; } = "";
    public string Message { get; private set; } = "";
    public bool SingleProductVisits {get;set;}
    private readonly List<ShopProduct> receivedOrder=new List<ShopProduct>();
    public int[] MissingOrderTypes=>active==null?new int[0]:active.OrderTypes.Where(t=>!receivedOrder.Any(p=>p!=null&&!p.IsHeld&&p.CurrentSlot!=null&&(p.CurrentSlot.name.StartsWith("Order receipt")||p.CurrentSlot==Activities.BasketSlot)&&p.ProductId==ShopCatalog.Ids[t])).ToArray();
    public bool ManualTick { get; set; }
    public ShopCashDrawer Drawer { get; private set; }
    public ShopDevices Devices { get; private set; }
    public ShopLayout Layout { get; private set; }
    public ShopSecurity Security { get; private set; }
    public bool BuyPanicButton() => Security != null && Security.TryPurchase();
    public int AvailableProducts => Layout != null && Layout.Expanded ? ShopCatalog.Count : ShopCatalog.CigaretteCount;
    public int SelectedPieces(int value) => changeCoins.Count(c=>c==value);
    public bool HasHeldProduct => player != null && player.HasHeldItem;
    public WarehouseArea Warehouse { get; private set; }
    public void Notify(string text) => Message=text;
    public void DebugSpawnVisit(bool thief)
    {
        if(!(Application.isEditor||Debug.isDebugBuild)||Spawned>=TotalVisitors||Phase==ShopPhase.Summary)return;
        int type=thief?Enumerable.Range(0,AvailableProducts).FirstOrDefault(t=>Layout.FindPublicStock(t)!=null):-1;
        if(thief&&Layout.FindPublicStock(type)==null){Notify("A tolvajpróbához előbb tölts fel egy nyilvános polcot.");return;}
        if(Phase==ShopPhase.Preparing)OpenShop();SpawnCustomer(type,thief);spawnTimer=22;
    }
    public void DebugRestock()
    {
        if(!(Application.isEditor||Debug.isDebugBuild))return;
        for(int type=0;type<AvailableProducts;type++)OrderSupply(type,2);Tick(8.1f);
        foreach(int type in Enumerable.Range(0,AvailableProducts))while(warehouse[type]>0)
        {
            var slot=FindObjectsByType<ProductSlot>().FirstOrDefault(s=>s.CanAcceptStock(ShopCatalog.Ids[type])&&s.Fits(ProductVisuals.Size(type)));if(slot==null)break;
            if(!TryCreateSupply(type,out var item))break;item.AssignInitialSlot(slot);
        }
    }
    public void ConfigureWarehouse(WarehouseArea area) { Warehouse=area;supplyPoint=area.Dock(0); }

    public void Configure(PlayerInteractor interactor, ProductSlot tray, GameObject template, ProductSlot[] slots, int[] types, Material[] materials, Transform crate, Transform till)
    {
        player = interactor; delivery = tray; customerTemplate = template; shelfSlots = slots; shelfTypes = types;
        productMaterials = materials; supplyPoint = crate; tillPoint = till;
    }
    public CounterLayout Counter { get; private set; }
    public void ConfigureCounter(ProductSlot tray, Transform till)
    {
        delivery = tray; tillPoint = till;
    }
    private void Awake() { Instance = this; Warehouse=GetComponent<WarehouseArea>(); Counter = FindAnyObjectByType<CounterLayout>(); Devices = GetComponent<ShopDevices>(); Layout = GetComponent<ShopLayout>(); }
    private void OnEnable() { if (delivery != null) delivery.ProductPlaced += OnDelivery; }
    private void OnDisable()
    {
        if (delivery != null) delivery.ProductPlaced -= OnDelivery;
        if(Activities!=null&&Activities.BasketSlot!=null)Activities.BasketSlot.ProductPlaced-=OnDelivery;
        Security?.ReleaseEntrance();
        ShopInputGate.Blocked = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        if (Instance == this) Instance = null;
    }
    private void Start()
    {
        stockRoot = new GameObject("MVP Stock").transform; stockRoot.SetParent(transform, false);
        actorRoot = new GameObject("MVP Customers").transform; actorRoot.SetParent(transform, false);
        stock.AddRange(FindObjectsByType<ShopProduct>().Where(p => p.gameObject.scene == gameObject.scene));
        Security = GetComponent<ShopSecurity>();
        if (Security == null) Security = gameObject.AddComponent<ShopSecurity>();
        Security.Configure(this);
        Activities=gameObject.AddComponent<ShopActivities>();Activities.Configure(this,player);delivery.Configure(new Vector3(.4f,.4f,.4f));gameObject.AddComponent<ShopNextUpgrades>().Configure(this);
        Pricing=gameObject.AddComponent<ShopPricing>();Pricing.Configure(this);AfterHours=gameObject.AddComponent<ShopAfterHours>();AfterHours.Configure(this);
        Activities.BasketSlot.ProductPlaced+=OnDelivery;
        ResetDemo();
    }
    private void Update()
    {
        if (ManualTick || Ledger == null) return;
        Keyboard keyboard = Keyboard.current;
        if (Application.isFocused && keyboard != null)
        {
            if (Modal != ShopModal.None)
            {
                if(Modal==ShopModal.Summary)
                {
                    if(keyboard.enterKey.wasPressedThisFrame||keyboard.numpadEnterKey.wasPressedThisFrame)NextDay();
                    else if(keyboard.rKey.wasPressedThisFrame)ResetDemo();
                    else if(keyboard.sKey.wasPressedThisFrame)StayAfterHours();
                }
                else if (keyboard.escapeKey.wasPressedThisFrame) ClosePanel();
            }
            else if (Devices != null && Devices.Focused)
            { if (keyboard.escapeKey.wasPressedThisFrame) ClosePanel(); }
            else if (Cursor.lockState == CursorLockMode.Locked)
            {
                if (keyboard.bKey.wasPressedThisFrame) OpenSupplier();
                else if (keyboard.hKey.wasPressedThisFrame) SetModal(ShopModal.Help);
                else if (keyboard.fKey.wasPressedThisFrame) OpenCash();
                else if (keyboard.oKey.wasPressedThisFrame) { if (Phase == ShopPhase.Preparing) OpenShop(); else RequestClose(); }
                else if (keyboard.xKey.wasPressedThisFrame) SkipCurrent();
            }
        }
        if (Application.isFocused && (Cursor.lockState == CursorLockMode.Locked || (Devices != null && Devices.Focused))) Tick(Time.deltaTime);
    }
    public void Tick(float dt)
    {
        if (Ledger == null || Phase == ShopPhase.Summary) return;
        dt = Mathf.Max(0f, dt);
        if(Phase==ShopPhase.AfterHours){Activities?.Tick(dt);return;}
        for (int i = shipments.Count - 1; i >= 0; i--)
        {
            shipments[i].Remaining -= dt;
            if (shipments[i].Remaining > 0f) continue;
            warehouse[shipments[i].Type] += shipments[i].Quantity; Warehouse?.SyncBoxes();
            Message = "Megérkezett: " + shipments[i].Quantity + " × " + ShopCatalog.Names[shipments[i].Type] + ". A raktárban megtalálod a márka dobozában!";
            shipments.RemoveAt(i);
        }
        Activities?.Tick(dt);
        bool liveMonitor=Devices!=null&&Devices.FocusKind==ShopDeviceKind.Monitor&&(Devices.ComputerApp==ShopComputerApp.Cameras||Devices.ComputerApp==ShopComputerApp.Games);
        if (Modal != ShopModal.None || (Devices != null && Devices.Focused && !liveMonitor) || Phase == ShopPhase.Preparing) return;
        AdvanceFleeing(dt);
        Elapsed += dt;
        if (Phase == ShopPhase.Open && Spawned < TotalVisitors && (Security == null || !Security.Locked))
        {
            spawnTimer -= dt;
            if (spawnTimer <= 0f && queue.Count < 3) { SpawnCustomer(); spawnTimer = Spawned >= 3 && Spawned <= 5 ? 12f : 22f; }
        }
        for (int i = departing.Count - 1; i >= 0; i--)
        {
            if (!departing[i].Advance(dt, Vector3.zero)) continue;
            departing[i].gameObject.SetActive(false);Destroy(departing[i].gameObject); departing.RemoveAt(i);
        }
        for (int i = 0; i < queue.Count; i++)
        {
            MvpCustomerActor customer = queue[i];
            Vector3 target = Counter.QueuePoint(i);
            bool arrived = customer.Advance(dt, target);
            if(customer.IsStealing){queue.RemoveAt(i--);fleeing.Add(customer);continue;}
            if (i == 0 && active == null && arrived)
            {
                active = customer; Stage = ServiceStage.Order;
                if(!customer.AcceptsPrices){PriceRefusals++;Missed++;Notify(ShopPeople.Names[customer.Person]+": Ennyiért inkább nem kérem. Túl drága! Állíts kedvezőbb árat az Árak alkalmazásban.");DismissActive();i--;continue;}
                Dialogue = ShopPeople.Greetings[active.Person];
                if (active.BasketItem != null)
                { if(Activities.PlaceBasket(active)){prepared=active.BasketItem;BeginPayment();} }
                else if (delivery.Occupant != null) OnDelivery(delivery.Occupant);
            }
            if (customer.Entered && (customer != active || Stage == ServiceStage.Order)) customer.WaitTime += dt;
        }
        if(active!=null&&Stage==ServiceStage.Order&&active.BasketItem!=null&&active.BasketItem.IsHeld&&Activities.PlaceBasket(active))
        {prepared=active.BasketItem;BeginPayment();}
        if(active!=null&&Stage==ServiceStage.Order&&receivedOrder.Count>0&&active.ExtraItems.Any(p=>p!=null&&p.IsHeld))BeginPayment();
        if (active != null && Stage == ServiceStage.Order && active.WaitTime > 180f) SkipCurrent();
        if (active != null && Stage == ServiceStage.Payment && !PreparedIsValid())
        {
            Activities.ClearHand();Drawer.CancelTender(); changeCoins.Clear(); prepared = null; Stage = ServiceStage.Order; if (Devices != null && (Devices.FocusKind == ShopDeviceKind.Till || Devices.FocusKind == ShopDeviceKind.Coins)) ClosePanel();
            Dialogue = "A termék visszakerült a pultról. Tedd újra az átadóhelyre.";
        }
        if (active != null && Stage == ServiceStage.Thanks)
        {
            thanksTimer -= dt;
            if (thanksTimer <= 0f) DismissActive();
        }
        if (Phase == ShopPhase.Open && Spawned >= TotalVisitors && queue.Count == 0 && fleeing.Count == 0)
        { Phase = ShopPhase.Closing; Security?.ReleaseEntrance(); }
        if (Phase == ShopPhase.Closing && queue.Count == 0 && departing.Count == 0 && fleeing.Count == 0) FinishDay();
    }
    private void SpawnCustomer(int forcedProduct=-1,bool forceThief=false)
    {
        int visit = Spawned++;
        GameObject clone = Instantiate(customerTemplate, actorRoot);
        clone.SetActive(false);
        var legacy = clone.GetComponent<CustomerOrderController>(); if (legacy != null) legacy.enabled = false;
        MvpCustomerActor actor = clone.AddComponent<MvpCustomerActor>();
        int type = Layout != null && Layout.Expanded && visit % 2 == 1 ? ShopCatalog.CigaretteCount + ((visit + Day - 1) % (ShopCatalog.Count - ShopCatalog.CigaretteCount)) : ShopCatalog.VisitProducts[visit];
        if(!SingleProductVisits&&StockCount(ShopCatalog.Contraband)>0&&visit%4==1)type=ShopCatalog.Contraband;
        if(forcedProduct>=0)type=forcedProduct;
        if(forcedProduct<0&&Activities.CoffeeOwned&&visit%4==2)type=SingleProductVisits?ShopCatalog.Coffee:ShopCatalog.Coffee+(visit/4+Day-1)%3;
        actor.Initialize((visit + Day - 1) % 4, type, visit);
        if(forceThief)actor.SetTheftIntent();clone.SetActive(true); queue.Add(actor);
    }
    private void OnDelivery(ShopProduct product)
    {
        if (active == null || Stage != ServiceStage.Order || product == null || product.IsHeld || (product.CurrentSlot != delivery&&product.CurrentSlot!=Activities.BasketSlot)) return;
        if (!active.OrderTypes.Contains(ShopCatalog.Index(product.ProductId)))
        {
            Dialogue = ShopPeople.Wrong[active.Person]; Message = "Vedd vissza a téves árut, és tedd félre egy másik pulthelyre."; return;
        }
        prepared = product; BeginPayment();
    }
    private void BeginPayment()
    {
        CollectOrderItem(prepared);foreach(var item in active.ExtraItems)CollectOrderItem(item);if(!PreparedIsValid())return;
        Stage = ServiceStage.Payment; changeCoins.Clear();
        if(CardPayment){Drawer.CancelTender();Dialogue="Bankkártyával fizetnék.";Message="E vagy F a kasszánál: kártyás fizetés jóváhagyása.";return;}
        if(CoinPayment){Drawer.CancelTender();Activities.ShowCoins(active);Dialogue="Kiválasztaná belőle? Nekem ma nem megy a számolás.";Message="E vagy F a kasszánál: érmék kiválogatása a tenyérből.";return;}
        Drawer.OfferTender(active.Tender);
        Dialogue = "Tessék a pénz. A visszajárót azért megvárom.";
        Message = "A pénz a kasszánál van. E vagy F – pénztárgép.";
    }
    private bool PreparedIsValid()=>active!=null&&receivedOrder.Count==active.OrderTypes.Length&&receivedOrder.All(p=>p!=null&&p.gameObject.activeSelf&&!p.IsHeld&&p.CurrentSlot!=null&&(p.CurrentSlot.name.StartsWith("Order receipt")||p.CurrentSlot==Activities.BasketSlot));
    private Vector3 ReceiptPosition(int index)=>index==0?Activities.BasketSlot.transform.position:Layout.Expanded?new Vector3(2.85f,1.18f,-1.45f+index*.4f):new Vector3(.25f,1.18f,-2.05f+index*.4f);
    private bool CollectOrderItem(ShopProduct product)
    {
        receivedOrder.RemoveAll(p=>p==null||p.IsHeld||p.CurrentSlot==null||(!p.CurrentSlot.name.StartsWith("Order receipt")&&p.CurrentSlot!=Activities.BasketSlot));
        if(product!=null&&!receivedOrder.Contains(product))
        {
            int type=ShopCatalog.Index(product.ProductId);
            if(active.OrderTypes.Count(t=>t==type)<=receivedOrder.Count(p=>p.ProductId==product.ProductId)){Notify("Ez a termék már a kosárban van.");return false;}
            if(product.CurrentSlot==Activities.BasketSlot){receivedOrder.Add(product);return receivedOrder.Count==active.OrderTypes.Length;}
            int freeIndex=Enumerable.Range(0,active.OrderTypes.Length).Where(i=>(i!=0||Activities.BasketSlot.Occupant==null)&&!receivedOrder.Any(p=>p.CurrentSlot!=null&&Vector3.Distance(p.CurrentSlot.transform.position,ReceiptPosition(i))<.02f)).DefaultIfEmpty(-1).First();
            if(freeIndex<0){Notify("Tedd szabaddá a kosár pulthelyeit.");return false;}
            if(freeIndex==0){if(!(product.IsHeld?product.TryPlace(Activities.BasketSlot):product.AssignInitialSlot(Activities.BasketSlot)))return false;if(!receivedOrder.Contains(product))receivedOrder.Add(product);return PreparedIsValid();}
            var slot=new GameObject("Order receipt "+freeIndex).AddComponent<ProductSlot>();slot.transform.SetParent(delivery.transform.parent,false);slot.transform.position=ReceiptPosition(freeIndex);slot.Configure(new Vector3(.35f,.4f,.33f));
            if(!(product.IsHeld?product.TryPlace(slot):product.AssignInitialSlot(slot))){Destroy(slot.gameObject);return false;}receivedOrder.Add(product);
        }
        if(receivedOrder.Count<active.OrderTypes.Length){Stage=ServiceStage.Order;Notify("Kosár: "+receivedOrder.Count+" / "+active.OrderTypes.Length+" termék. Kérés: "+active.OrderDescription);return false;}
        return PreparedIsValid();
    }
    public bool SubmitChange(int amount)
    {
        if(CardPayment){Message="A terminálon hagyd jóvá a kártyás fizetést.";return false;}
        if(CoinPayment){Message="A vásárló tenyeréből válaszd ki a pontos összeget.";return false;}
        if (Stage != ServiceStage.Payment || !PreparedIsValid()) { Message = "Előbb a megfelelő árut tedd az átadóhelyre."; return false; }
        int type = active.Product;
        int[] pieces = ShopCatalog.Denominations.Select(SelectedPieces).ToArray();
        if (amount != ChangeSelected || !Drawer.CanSettle(active.Tender,pieces)) { Message = "A visszajárót a kassza rekeszeiből vedd ki."; return false; }
        if (!Ledger.TrySell(active.TotalPrice, active.TotalCost, active.Tender, amount))
        {
            Message = "Nem stimmel a visszajáró. Számold újra, vagy töröld a kiválasztott címleteket."; return false;
        }
        Drawer.Settle(active.Tender,pieces);
        CompleteSale();return true;
    }
    public void AddChange(int value)
    {
        if (CardPayment || CoinPayment || Stage != ServiceStage.Payment || Array.IndexOf(ShopCatalog.Denominations,value)<0) return;
        if (!Drawer.CanSelect(value,SelectedPieces(value))) { Message = "Ebből a címletből kiürült a rekesz. Más címletekből állítsd össze!"; return; }
        if (ChangeSelected + value > active.Tender) return;
        changeCoins.Add(value);
    }
    public void UndoChange() { if (changeCoins.Count > 0) changeCoins.RemoveAt(changeCoins.Count - 1); }
    public void ClearChange() => changeCoins.Clear();
    public void ExchangeCash()
    {
        ClearChange();
        foreach(int value in ShopCatalog.Denominations)
            if(value>=50 && Drawer.Exchange(value)) { Message = "Felváltva: " + ShopCatalog.Money(value) + ". A kassza összege nem változott."; return; }
        Message = "Nincs további felváltható címlet.";
    }
    public void OpenCash()
    {
        if (Stage != ServiceStage.Payment || !PreparedIsValid()) { Message = "A kasszázáshoz előbb készítsd elő a kért terméket."; return; }
        if (player != null && tillPoint != null && Vector3.Distance(player.transform.position, tillPoint.position) > 3.5f) { Message = "Menj a kasszához a fizetéshez."; return; }
        Message = "Számold meg a kapott pénzt, és válaszd ki a visszajárót.";
        if (Devices != null) Devices.Focus(CoinPayment?ShopDeviceKind.Coins:ShopDeviceKind.Till);
    }
    public void OpenShop()
    {
        if (Phase != ShopPhase.Preparing || Ledger == null) return;
        Phase = ShopPhase.Open; spawnTimer = 0f; SetModal(ShopModal.None);
        Message = "Nyitva! Készítsd elő a rendelést, majd számolj el a kasszánál.";
    }
    public void RequestClose()
    {
        if(Phase==ShopPhase.AfterHours){ReturnToSummary();return;}
        if (Phase != ShopPhase.Open) return;
        Security?.ReleaseEntrance();
        Phase = ShopPhase.Closing; Message = "Zárunk. A bent várakozókat még kiszolgálhatod; X-szel udvariasan elküldheted az aktuális vásárlót.";
    }
    public void SkipCurrent()
    {
        if (active == null || (Stage != ServiceStage.Order && Stage != ServiceStage.Payment)) return;
        Missed++; Dialogue = "Most nem tudtuk kiszolgálni. Majd legközelebb!";
        if (Devices != null && Devices.Focused) ClosePanel();
        DismissActive();
    }
    private void DismissActive()
    {
        active.ReturnBasket();
        foreach(var item in receivedOrder)
        {
            if(item==null||item.IsHeld||item.CurrentSlot==null||(!item.CurrentSlot.name.StartsWith("Order receipt")&&item.CurrentSlot!=Activities.BasketSlot))continue;
            var old=item.CurrentSlot;var free=FindObjectsByType<ProductSlot>().FirstOrDefault(s=>s.IsStockShelf&&s.CanAcceptStock(item.ProductId)&&s.Fits(item.transform.lossyScale));
            if(free!=null){item.AssignInitialSlot(free);if(old!=Activities.BasketSlot)Destroy(old.gameObject);}else if(old!=Activities.BasketSlot){old.transform.position=Layout.EntryInside+Vector3.right*(receivedOrder.IndexOf(item)*.35f);item.AssignInitialSlot(old);}
        }
        CleanupReceipts();receivedOrder.Clear();Activities.ClearHand(); Drawer.CancelTender(); changeCoins.Clear(); queue.Remove(active); active.Leave(); departing.Add(active); active = null; prepared = null; Stage = ServiceStage.None;
    }
    private void CleanupReceipts(){foreach(var slot in delivery.transform.parent.GetComponentsInChildren<ProductSlot>(true))if(slot.name.StartsWith("Order receipt")&&slot.Occupant==null){slot.gameObject.SetActive(false);Destroy(slot.gameObject);}}
    private void FinishDay()
    {
        foreach (Shipment order in shipments) warehouse[order.Type] += order.Quantity;
        Warehouse?.SyncBoxes();
        shipments.Clear(); ClosePanel(); Ledger.Close(300); SyncCashDrawer(); Phase = ShopPhase.Summary; SetModal(ShopModal.Summary);
    }
    public bool StayAfterHours()
    {
        if(Phase!=ShopPhase.Summary||HasHeldProduct)return false;
        Phase=ShopPhase.AfterHours;SetModal(ShopModal.None);AfterHours.Enter();Notify("Zárás után maradtál. A raktáron át a hátsó udvarra is kimehetsz. O – vissza az elszámoláshoz.");return true;
    }
    public void ReturnToSummary()
    {
        if(Phase!=ShopPhase.AfterHours)return;
        AfterHours.EndEvening();Phase=ShopPhase.Summary;SetModal(ShopModal.Summary);
    }
    public ShopProduct CreateNightProduct()=>CreateProduct(ShopCatalog.Contraband);
    public void NextDay()
    {
        if (Phase != ShopPhase.Summary) return;
        if (Ledger.Cash < 0) { Message = "Elfogyott a kasszapénz. Indíts új játékot."; return; }
        AfterHours.EndEvening();Day++; Ledger = new ShopLedger(Ledger.Cash,Ledger.BankBalance); BeginPreparation();
    }
    private void BeginPreparation()
    {
        Security?.ReleaseEntrance();
        Phase = ShopPhase.Preparing; Stage = ServiceStage.None; Spawned = 0; Missed = 0; Elapsed = 0f;
        StolenToday=0;CaughtToday=0;PriceRefusals=0;
        receivedOrder.Clear();active = null; prepared = null; changeCoins.Clear(); Message = "Nézd át a cigarettapolcot. A pult bal oldalán találod a rendelési monitort. O – Nyitás.";
        SetModal(ShopModal.Welcome);
    }
    public void ResetDemo()
    {
        AfterHours?.ResetEvenings();Pricing?.ResetPrices();GetComponent<ShopNextUpgrades>()?.ResetUpgrades();
        SingleProductVisits=false;receivedOrder.Clear();
        if (stockRoot == null) return;
        if (player != null) player.CancelHeld();
        foreach (MvpCustomerActor actor in queue.Concat(departing).Concat(fleeing)) if (actor != null) { actor.ReturnBasket(); actor.gameObject.SetActive(false); Destroy(actor.gameObject); }
        queue.Clear(); departing.Clear(); fleeing.Clear(); active = null;
        foreach (ShopProduct item in stock) if (item != null) item.RemoveFromWorld();
        stock.Clear();CleanupReceipts(); shipments.Clear(); Array.Clear(warehouse,0,warehouse.Length); Warehouse?.ResetStorage();
        ClosePanel(); if (Layout != null) { Layout.SetExpanded(false); Layout.SetExtraShelf(false); }
        SelectedSupply = 0; Day = 1; Ledger = new ShopLedger(6000); Drawer = new ShopCashDrawer();
        for (int type = 0; type < ShopCatalog.CigaretteCount; type++)
        {
            int seeded = 0;
            for (int i = 0; i < shelfSlots.Length && seeded < 8; i++)
            {
                if (shelfTypes[i] != type || shelfSlots[i] == null || !shelfSlots[i].isActiveAndEnabled) continue;
                ShopProduct product = CreateProduct(type);
                if (product.AssignInitialSlot(shelfSlots[i])) seeded++;
                else product.RemoveFromWorld();
            }
        }
        Activities?.ResetFeatures();
        Security?.ResetUpgrade();
        Devices?.ResetComputer();
        BeginPreparation();
    }
    private ShopProduct CreateProduct(int type)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = ShopCatalog.Names[type]; item.transform.SetParent(stockRoot, false);
        item.transform.localScale = ProductVisuals.Size(type);
        item.GetComponent<Renderer>().sharedMaterial = productMaterials[Mathf.Min(type,productMaterials.Length-1)];
        ShopProduct product = item.AddComponent<ShopProduct>(); product.ConfigureProduct(ShopCatalog.Ids[type], ShopCatalog.Names[type]);
        ProductVisuals.Add(item,type);
        stock.Add(product); return product;
    }
    public int StockCount(int type) => stock.Count(p => p != null && p.gameObject.activeSelf && p.ProductId == ShopCatalog.Ids[type]);
    public int WarehouseCount(int type) => type>=0&&type<warehouse.Length?warehouse[type]:0;
    public int PendingQuantity(int type) => shipments.Where(s => s.Type == type).Sum(s => s.Quantity);
    public float NextDelivery => shipments.Count == 0 ? 0f : shipments.Min(s => s.Remaining);
    public bool OrderSupply(int type) => OrderSupply(type,3);
    public bool OrderSupply(int type,int quantity)
    {
        if(Ledger.Closed){Message="A mai elszámolás már lezárult. Árurendeléshez kezdd el a következő napot.";return false;}
        if (type < 0 || type >= AvailableProducts || Phase == ShopPhase.Summary) { Message = "Ez az áru a boltbővítés után rendelhető."; return false; }
        if (quantity < 1 || quantity > ShopCatalog.MaxOrderQuantity) { Message = "Egy rendelésben 1–12 terméket választhatsz."; return false; }
        if (warehouse[type] + PendingQuantity(type) + quantity > 24) { Message = "A dobozban már elég áru vár ebből a termékből."; return false; }
        if (!Ledger.TryBuy(ShopCatalog.Costs[type],quantity)) { Message = "Nincs elég elkölthető pénz. Rendelés: "+ShopCatalog.Money(ShopCatalog.Costs[type]*quantity)+"; készpénz és bank együtt: "+ShopCatalog.Money(Ledger.AvailableFunds)+"."; return false; }
        SyncCashDrawer();
        shipments.Add(new Shipment { Type = type, Quantity = quantity, Remaining = 8f }); SelectedSupply = type;
        Message = "Megrendelve " + quantity + " × " + ShopCatalog.Names[type] + ". A szállítás 8 másodperc.";
        return true;
    }
    public void CycleSupply() { SelectedSupply = (SelectedSupply + 1) % AvailableProducts; Message = "Dobozból kiválasztva: " + ShopCatalog.Names[SelectedSupply]; }
    public bool TryCreateSupply(int type, out ShopProduct product)
    {
        product = null;
        if (type < 0 || type >= AvailableProducts || Phase == ShopPhase.Summary || warehouse[type] <= 0) return false;
        product = CreateProduct(type); product.transform.position = (Warehouse!=null ? Warehouse.Dock(type).position : supplyPoint.position) + Vector3.up*.25f;
        warehouse[type]--; Warehouse?.SyncBoxes(); return true;
    }
    public bool StockFromCarton(WarehouseCarton carton,ProductSlot slot)
    {
        if(carton==null||!carton.IsHeld||Phase==ShopPhase.Summary||!carton.CanPlace(slot)||warehouse[carton.ProductType]<=0)return false;
        var product=CreateProduct(carton.ProductType);
        if(!(product.IsHeld?product.TryPlace(slot):product.AssignInitialSlot(slot))){product.RemoveFromWorld();return false;}
        warehouse[carton.ProductType]--;carton.AdjustQuantity(-1);
        slot.NotifyProductPlaced(product);
        Message=product.DisplayName+" a polcon. A dobozban még "+carton.Quantity+" db.";
        return true;
    }
    public void TakeSupply(PlayerInteractor interactor) => TakeSupply(interactor,-1);
    public void TakeSupply(PlayerInteractor interactor,int brand)
    {
        if (interactor.HasHeldItem) return;
        if (brand>=0 && brand<AvailableProducts) SelectedSupply=brand;
        else if (brand>=AvailableProducts) { Message="Ez a márka a boltbővítés után rendelhető.";return; }
        if (brand<0 && warehouse[SelectedSupply] == 0)
        {
            int available = Array.FindIndex(warehouse, n => n > 0);
            if (available >= 0 && available < AvailableProducts) SelectedSupply = available;
        }
        if (!TryCreateSupply(SelectedSupply, out ShopProduct product))
        {
            Message = PendingQuantity(SelectedSupply) > 0
                ? ShopCatalog.Names[SelectedSupply] + " még úton van. Várd meg a szállítást!"
                : "A dobozban nincs megérkezett áru. Rendelj a pult bal oldalán lévő monitoron; a szállítás 8 másodperc.";
            return;
        }
        if (!interactor.TryHold(product)) { warehouse[SelectedSupply]++; product.RemoveFromWorld(); Warehouse?.SyncBoxes(); return; }
        Message = "Kézben: " + product.DisplayName + ". A zöld keretek jelzik a megfelelő helyeket. E – lerakás; a doboznál E – visszarakás.";
    }
    public bool ReturnSupply(PlayerInteractor interactor,int brand=-1)
    {
        ShopProduct product = interactor.HeldProduct;
        int type = product == null ? -1 : ShopCatalog.Index(product.ProductId);
        if (type < 0 || type>=ShopCatalog.Count || Phase == ShopPhase.Summary) return false;
        if (brand>=0 && brand!=type) { Message="Ez másik márka doboza. Keresd a "+ShopCatalog.Names[type]+" feliratot.";return false; }
        if (warehouse[type] + PendingQuantity(type) >= 24) { Message = "Ebből a termékből megtelt a szállítódoboz."; return false; }
        if (!interactor.RemoveHeld(product)) return false;
        warehouse[type]++; SelectedSupply = type; Warehouse?.SyncBoxes();
        Message = "Visszatéve a dobozba: " + ShopCatalog.Names[type] + ".";
        return true;
    }
    public void SetModal(ShopModal modal)
    {
        if (modal == ShopModal.Cash) { OpenCash(); return; }
        if (modal == ShopModal.Supplier) { OpenSupplier(); return; }
        if (Devices != null) Devices.Release();
        Modal = modal; ShopInputGate.Blocked = modal != ShopModal.None;
        Cursor.lockState = ShopInputGate.Blocked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = ShopInputGate.Blocked;
    }
    public void OpenSupplier()
    {
        if (Devices == null || Phase == ShopPhase.Summary) return;
        if (HasHeldProduct) { Message = "Előbb tedd le a kezedben levő árut."; return; }
        if (Vector3.Distance(player.GetComponentInChildren<Camera>().transform.position, Devices.MonitorPosition) > 3.5f) { Message = "Menj a pult közelébe; B – számítógép."; return; }
        Devices.Focus(ShopDeviceKind.Monitor);
    }
    public bool TryExpand()
    {
        if (Layout == null || Layout.Expanded || Phase != ShopPhase.Preparing) { Message = "Bővíteni nyitás előtt lehet, üres boltban."; return false; }
        if (!Ledger.TryImprove(ShopLayout.ExpansionPrice)) { Message = "Nincs elég pénz a bővítéshez."; return false; }
        SyncCashDrawer(); Layout.SetExpanded(true); Devices?.RefreshMonitor();
        Message = "Megnyílt az önkiszolgáló rész! Rendelj árut, és töltsd fel az új polcokat. A pult a helyén maradt.";
        return true;
    }
    public bool BuyExtraShelf()
    {
        if(Layout==null||Layout.ExtraShelfOwned||Phase!=ShopPhase.Preparing){Message="Berendezést nyitás előtt vehetsz. Ez a polc csak egyszer vásárolható meg.";return false;}
        if(!Ledger.TryImprove(ShopLayout.ExtraShelfPrice)){Message="Nincs elég pénz a kiegészítő polcra.";return false;}
        SyncCashDrawer();Layout.SetExtraShelf(true);
        Message="Kiegészítő cigarettapolc a jobb oldali személyzeti falnál: 9 új termékhely. A pult a helyén maradt.";return true;
    }
    public void ClosePanel() { if (Phase != ShopPhase.Summary) SetModal(ShopModal.None); }
}
