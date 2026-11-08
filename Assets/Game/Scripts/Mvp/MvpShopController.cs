using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ShopPhase { Preparing, Open, Closing, Summary, AfterHours }
public enum ShopModal { None, Welcome, Help, Supplier, Cash, Summary, Dialogue }
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
    private readonly ShopInventory inventory = new ShopInventory(ShopCatalog.SupplyCapacity);
    private readonly List<int> changeCoins = new List<int>();
    private MvpCustomerActor active;
    private ShopProduct prepared;
    private float spawnTimer;
    private float thanksTimer;
    private Transform stockRoot;
    private Transform actorRoot;
    public static MvpShopController Instance { get; private set; }
    private readonly ShopDayCycle dayCycle = new ShopDayCycle();
    public ShopPhase Phase { get => dayCycle.Phase; private set => dayCycle.Phase = value; }
    public ShopModal Modal { get; private set; }
    public ServiceStage Stage { get => checkout.Stage; private set => checkout.Stage = value; }
    public ShopLedger Ledger { get; private set; }
    public ShopPartnerships Partners { get; internal set; }
    public ShopReputation Reputation { get; internal set; }
    public ShopPricing Pricing {get;internal set;}
    public ShopAfterHours AfterHours {get;internal set;}
    public int PriceRefusals {get;private set;}
    public int Day { get => dayCycle.Day; private set => dayCycle.Day = value; }
    public float Elapsed { get => dayCycle.Elapsed; private set => dayCycle.Elapsed = value; }
    public float ClockElapsed { get => dayCycle.ClockElapsed; private set => dayCycle.ClockElapsed = value; }
    public int Spawned { get; private set; }
    // Fixtures can own their explicitly spawned visits without changing the live arrival policy.
    public bool AutomaticArrivals { get; set; } = true;
    public int Missed { get; private set; }
    public int QueueCount => queue.Count;
    public int SelectedSupply { get; private set; }
    public int ChangeSelected => changeCoins.Sum();
    public int TotalVisitors => ShopCatalog.VisitProducts.Length;
    public MvpCustomerActor ActiveCustomer => active;
    public ProductSlot DeliverySlot => delivery;
    public ShopBarcodeScanner Scanner { get; internal set; }
    public ShopLotteryRetailer Lottery { get; internal set; }
    public string Dialogue { get; private set; } = "";
    private string message = "";
    public int MessageRevision { get; private set; }
    public string Message
    {
        get => message;
        private set { message = value; MessageRevision++; }
    }
    public bool SingleProductVisits {get;set;}
    private readonly ShopCheckoutSession checkout = new ShopCheckoutSession();
    private System.Collections.Generic.IReadOnlyList<ShopProduct> receivedOrder => checkout.Items;
    public int[] MissingOrderTypes => checkout.Missing(active?.OrderTypes);
    public bool ManualTick { get; set; }
    public ShopCashDrawer Drawer { get; private set; }
    public ShopDevices Devices { get; private set; }
    public ShopLayout Layout { get; private set; }
    public ShopSecurity Security { get; internal set; }
    public ShopEntrance Entrance { get; internal set; }
    public ShopFloorPlan FloorPlan { get; internal set; }
    public ShopInteriorLighting InteriorLighting { get; internal set; }
    public ShopNavigation Navigation { get; internal set; }
    public ShopArrangement Arrangement { get; internal set; }
    public ShopCounterStorage CounterStorage { get; internal set; }
    private bool ShopEmpty => queue.Count == 0 && departing.Count == 0 && fleeing.Count == 0 && !HasHeldProduct;
    public bool CanEndDay => dayCycle.CanEnd(ShopEmpty);
    public bool BuyPanicButton() => Security != null && Security.TryPurchase();
    public int AvailableProducts => Layout != null && Layout.Expanded ? ShopCatalog.Count : ShopCatalog.CigaretteCount;
    public int[] SupplyTypes => Enumerable.Range(0, AvailableProducts)
        .Concat(Enumerable.Range(ShopPartnerCatalog.FirstVariant, 7).Where(IsSupplyAvailable)).ToArray();
    public bool IsSupplyAvailable(int type) => ShopCatalog.IsPackaged(type) &&
        (type < ShopCatalog.Count ? type < AvailableProducts : Partners != null && Partners.State.Unlocked(type));
    public int ProcurementCost(int type) => Partners?.State.UnitCost(type) ?? ShopCatalog.Costs[type];
    public bool CanDevelopShop => Ledger != null && dayCycle.CanDevelop(!Ledger.Closed, ShopEmpty);
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
        foreach(int type in SupplyTypes)OrderSupply(type,2);Tick(8.1f);
        foreach(int type in SupplyTypes)while(WarehouseCount(type)>0)
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
    private readonly HashSet<ProductSlot> deliveryBindings = new HashSet<ProductSlot>();
    private void OnEnable()
    {
        Instance = this;
        BindDeliveryEvents();
    }
    private void OnDisable()
    {
        UnbindDeliveryEvents();
        Arrangement?.Cancel();
        GetComponent<ShopMenu>()?.Resume();
        Devices?.Release();
        Security?.ReleaseEntrance();
        ShopInputGate.Blocked = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (Instance == this) Instance = null;
    }
    private void Start() => new ShopBootstrap().Initialize(this, player);
    private void OnDestroy() => ProductVisuals.ReleaseWorldFont();

    internal void InitializeRuntimeRoots()
    {
        stockRoot = new GameObject("MVP Stock").transform;
        stockRoot.SetParent(transform, false);
        actorRoot = new GameObject("MVP Customers").transform;
        actorRoot.SetParent(transform, false);
        stock.AddRange(FindObjectsByType<ShopProduct>().Where(product => product.gameObject.scene == gameObject.scene));
    }
    internal void InitializeCheckout()
    {
        checkout.Configure(Activities.BasketSlot, delivery.transform.parent, ReceiptPosition);
        delivery.SetManualPlacementRule(product => ShopCatalog.IsCigarette(ShopCatalog.Index(product.ProductId)) || ShopLotteryCatalog.IsTicket(ShopCatalog.Index(product.ProductId)));
        BindDeliveryEvents();
    }
    private void BindDeliveryEvents()
    {
        UnbindDeliveryEvents();
        if (!isActiveAndEnabled) return;
        if (delivery != null) deliveryBindings.Add(delivery);
        if (Activities != null)
        {
            if (Activities.BasketSlot != null) deliveryBindings.Add(Activities.BasketSlot);
            if (Activities.CustomerHandoverSlots != null)
                foreach (var slot in Activities.CustomerHandoverSlots) if (slot != null) deliveryBindings.Add(slot);
        }
        foreach (var slot in deliveryBindings) slot.ProductPlaced += OnDelivery;
    }
    private void UnbindDeliveryEvents()
    {
        foreach (var slot in deliveryBindings) if (slot != null) slot.ProductPlaced -= OnDelivery;
        deliveryBindings.Clear();
    }
    private void Update()
    {
        if (ManualTick || Ledger == null || ShopMenu.Active || Arrangement != null && Arrangement.Active) return;
        Keyboard keyboard = Keyboard.current;
        if (Application.isFocused && keyboard != null)
        {
            if (Modal != ShopModal.None)
            {
                if(Modal==ShopModal.Summary)
                {
                    if(ShopBindings.Pressed(UnityEngine.InputSystem.Key.Enter)||ShopBindings.Pressed(UnityEngine.InputSystem.Key.NumpadEnter))NextDay();
                    else if(ShopBindings.Pressed(UnityEngine.InputSystem.Key.R))ResetDemo();
                }
                else if (!ShopMenu.Active&&ShopBindings.Pressed(UnityEngine.InputSystem.Key.Escape)) ClosePanel();
            }
            else if (Devices != null && Devices.Focused)
            { if (ShopBindings.Pressed(UnityEngine.InputSystem.Key.Escape) && !(Devices.ComputerApp==ShopComputerApp.Prices && Pricing.CancelEdit())) ClosePanel(); }
            else if (Cursor.lockState == CursorLockMode.Locked)
            {
                if (ShopBindings.Pressed(UnityEngine.InputSystem.Key.B)) OpenSupplier();
                else if (ShopBindings.Pressed(UnityEngine.InputSystem.Key.H)) SetModal(ShopModal.Help);
                else if (ShopBindings.Pressed(UnityEngine.InputSystem.Key.F)) OpenCash();
                else if (ShopBindings.Pressed(Key.T)) OfferAlternative();
                else if (ShopBindings.Pressed(UnityEngine.InputSystem.Key.X)) SkipCurrent();
            }
        }
        if (Application.isFocused && (!ShopInputGate.Blocked || (Devices != null && Devices.Focused))) Tick(Time.deltaTime);
    }
    public void Tick(float dt)
    {
        if (Ledger == null || Phase == ShopPhase.Summary || Arrangement != null && Arrangement.Active) return;
        dt = Mathf.Max(0f, dt);
        bool liveMonitor=Devices!=null&&Devices.FocusKind==ShopDeviceKind.Monitor&&(Devices.ComputerApp==ShopComputerApp.Cameras||Devices.ComputerApp==ShopComputerApp.Games);
        if (!ShopMenu.Active && Modal==ShopModal.None) Reputation?.Tick(dt);
        Lottery?.Tick(dt);
        dayCycle.TickClock(dt);
        if(Phase==ShopPhase.AfterHours){Activities?.Tick(dt);return;}
        foreach (var arrival in inventory.Tick(dt))
        {
            Warehouse?.SyncBoxes();
            Message = "Megérkezett: " + arrival.Quantity + " × " + ShopCatalog.Names[arrival.Type] + ". A raktárban megtalálod a márka dobozában!";
        }
        Activities?.Tick(dt);
        if (Modal != ShopModal.None || (Devices != null && Devices.Focused && !liveMonitor) || Phase == ShopPhase.Preparing) return;
        AdvanceFleeing(dt);
        dayCycle.TickService(dt);
        if (Phase == ShopPhase.Open && AutomaticArrivals && (Security == null || !Security.Locked))
        {
            spawnTimer -= dt;
            if (spawnTimer <= 0f && queue.Count < 3)
            {
                SpawnCustomer();
                spawnTimer = UnityEngine.Random.Range(12f,28f)*(float)(Reputation?.State.ArrivalScale ?? 1);
            }
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
            Reputation?.Observe(customer);
            if(customer.IsStealing){queue.RemoveAt(i--);fleeing.Add(customer);continue;}
            if (i == 0 && active == null && arrived)
            {
                active = customer; Stage = ServiceStage.Order;
                ResetCardTerminal();
                if(!customer.AcceptsPrices){Reputation?.Complete(customer,ShopVisitOutcome.PriceRefusal);PriceRefusals++;Missed++;Notify(ShopPeople.Names[customer.Person]+": Ennyiért inkább nem kérem. Túl drága! Állíts kedvezőbb árat az Árak alkalmazásban.");DismissActive();i--;continue;}
                Dialogue = Lottery.Arrive(active);
                if (active.IsRegular) Dialogue = Reputation.RegularOrder(active);
                if (active.BasketItem != null)
                { if(Activities.PlaceBasket(active)){prepared=active.BasketItem;BeginPayment();} }
                else
                {
                    if(delivery.Occupant!=null)OnDelivery(delivery.Occupant);
                    foreach(var slot in Activities.CustomerHandoverSlots)
                        if(Stage==ServiceStage.Order && slot.Occupant!=null)OnDelivery(slot.Occupant);
                }
            }
            if (customer.Entered && (customer != active || Stage == ServiceStage.Order)) customer.WaitTime += dt;
        }
        if(active!=null&&Stage==ServiceStage.Order&&active.BasketItem!=null&&active.BasketItem.IsHeld&&Activities.PlaceBasket(active))
        {prepared=active.BasketItem;BeginPayment();}
        if(active!=null&&Stage==ServiceStage.Order&&receivedOrder.Count>0&&active.ExtraItems.Any(p=>p!=null&&p.IsHeld))BeginPayment();
        if (active != null && Stage == ServiceStage.Order && active.WaitTime > 180f) SkipCurrent();
        if (active != null && Stage == ServiceStage.Payment && !PreparedIsValid())
        {
            Activities.ClearHand();Drawer.CancelTender(); changeCoins.Clear(); prepared = null; Stage = ServiceStage.Order; ResetCardTerminal(); if (Devices != null && (Devices.FocusKind == ShopDeviceKind.Till || Devices.FocusKind == ShopDeviceKind.Coins || Devices.FocusKind == ShopDeviceKind.Terminal)) ClosePanel();
            Dialogue = "A termék visszakerült a pultról. Tedd újra az átadóhelyre.";
        }
        if (active != null && Stage == ServiceStage.Thanks)
        {
            thanksTimer -= dt;
            if (thanksTimer <= 0f) DismissActive();
        }
        // The sign closes the shop; only the player's exit action ends the day.
        if (Phase == ShopPhase.Closing && CanEndDay) StayAfterHours();
    }
    private void SpawnCustomer(int forcedProduct=-1,bool forceThief=false)
    {
        int visit = Spawned;
        var additional = SupplyTypes.Where(t => t >= ShopCatalog.CigaretteCount).ToArray();
        int type = additional.Length > 0 && visit % 2 == 1 ? additional[(visit + Day - 1) % additional.Length] : ShopCatalog.VisitProducts[visit % TotalVisitors];
        if(!SingleProductVisits&&StockCount(ShopCatalog.Contraband)>0&&visit%4==1)type=ShopCatalog.Contraband;
        if(forcedProduct>=0)type=forcedProduct;
        if(forcedProduct<0&&Activities.CoffeeOwned&&visit%4==2)type=SingleProductVisits?ShopCatalog.Coffee:ShopHotDrinkRecipes.Types[(visit/4+Day-1)%ShopHotDrinkRecipes.Types.Length];
        LotteryTicket returning = null;
        if(forcedProduct<0&&!SingleProductVisits&&Lottery.Owned&&visit%4==1)
            returning=Lottery.Book.ScheduleReturn(Day);
        if(forcedProduct<0&&!SingleProductVisits&&returning==null&&visit%4==3)
        {int lotteryType=Lottery.ChooseOrder(visit);if(lotteryType>=0)type=lotteryType;}
        int person = returning!=null ? returning.Owner : (visit+Day-1)%4;
        bool regular = false;
        if (forcedProduct<0 && !SingleProductVisits && returning==null && !ShopLotteryCatalog.IsTicket(type) &&
            Reputation.ChooseRegular(visit,out int familiar,out int favourite))
        { person=familiar; type=favourite; regular=true; }
        int[] order = returning!=null ? Array.Empty<int>() : CreateCustomerOrder(type, visit);
        if (order.Length == 0 && returning==null) return;
        Spawned++;
        GameObject clone = Instantiate(customerTemplate, actorRoot);
        clone.SetActive(false);
        var legacy = clone.GetComponent<CustomerOrderController>(); if (legacy != null) legacy.enabled = false;
        MvpCustomerActor actor = clone.AddComponent<MvpCustomerActor>();
        actor.Initialize(person, order.Length>0?order[0]:-1, visit, order, returning?.Id);
        if (regular) { actor.MarkRegular(); Reputation.State.RecordRegular(); }
        if(forceThief)actor.SetTheftIntent();clone.SetActive(true); queue.Add(actor);
    }
    private void OnDelivery(ShopProduct product)
    {
        if (active == null || Stage != ServiceStage.Order || product == null || product.IsHeld ||
            (product.CurrentSlot != delivery&&product.CurrentSlot!=Activities.BasketSlot&&!Activities.CustomerHandoverSlots.Contains(product.CurrentSlot))) return;
        if (!active.OrderTypes.Contains(ShopCatalog.Index(product.ProductId)))
        {
            Dialogue = ShopPeople.Wrong[active.Person]; Message = "Vedd vissza a téves árut, és tedd félre egy másik pulthelyre."; return;
        }
        if (product.CurrentSlot == delivery && !Scanner.TryScan(product)) return;
        prepared = product; BeginPayment();
    }
    private void BeginPayment()
    {
        CollectOrderItem(prepared);foreach(var item in active.ExtraItems)CollectOrderItem(item);if(!PreparedIsValid())return;
        Stage = ServiceStage.Payment; changeCoins.Clear();
        if(CardPayment){ClearCardAmount();Drawer.CancelTender();Dialogue="Bankkártyával fizetnék.";Message="E vagy F a kasszánál: kártyás fizetés jóváhagyása.";return;}
        if(CoinPayment){Drawer.CancelTender();Activities.ShowCoins(active);Dialogue="Kiválasztaná belőle? Nekem ma nem megy a számolás.";Message="Nézz a vásárló tenyerére: E vagy bal kattintás az aprópénzhez. F a kasszánál is használható.";return;}
        Drawer.OfferTender(active.Tender);
        Dialogue = "Tessék a pénz. A visszajárót azért megvárom.";
        Message = "A pénz a kasszánál van. E vagy F – pénztárgép.";
    }
    private bool PreparedIsValid() => active != null && active.LotteryClaimId == null && checkout.IsComplete(active.OrderTypes);
    private Vector3 ReceiptPosition(int index) => Activities.BasketSlot.transform.position + Counter.transform.rotation * new Vector3(index == 2 ? -.24f : 0, 0, index == 0 ? 0 : -.22f);
    private bool CollectOrderItem(ShopProduct product)
    {
        bool complete = checkout.Collect(product, active.OrderTypes, out string rejection);
        if (rejection != null) { Notify(rejection); return false; }
        if (complete) return true;
        Stage = ServiceStage.Order;
        Notify("Kosár: " + receivedOrder.Count + " / " + active.OrderTypes.Length + " termék. Kérés: " + active.OrderDescription);
        return false;
    }
    public bool SubmitChange(int amount)
    {
        if(CardPayment){Message="A terminálon hagyd jóvá a kártyás fizetést.";return false;}
        if(CoinPayment){Message="A vásárló tenyeréből válaszd ki a pontos összeget.";return false;}
        if (Stage != ServiceStage.Payment || !PreparedIsValid()) { Message = "Előbb a megfelelő árut tedd az átadóhelyre."; return false; }
        int type = active.Product;
        int[] pieces = ShopCatalog.Denominations.Select(SelectedPieces).ToArray();
        if (amount != ChangeSelected || !Drawer.CanSettle(active.Tender,pieces)) { Message = "A visszajárót a kassza rekeszeiből vedd ki."; return false; }
        if (!TrySettleSale(() => Ledger.TrySell(active.TotalPrice, CurrentOrderCost, active.Tender, amount)))
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
        if (Ledger == null || !dayCycle.Open()) return;
        spawnTimer = 0f; SetModal(ShopModal.None);
        Message = "Nyitva! Készítsd elő a rendelést, majd számolj el a kasszánál.";
    }
    public void RequestClose()
    {
        if (!dayCycle.Close()) return;
        Security?.ReleaseEntrance(); Message = "Zárunk. A bent várakozókat még kiszolgálhatod; X-szel udvariasan elküldheted az aktuális vásárlót.";
    }
    public bool IsRequestedTypeUnavailable(int type)
    {
        if (active == null) return false;
        if (ShopLotteryCatalog.IsTicket(type)) return AvailableInventory(type) < active.OrderTypes.Count(t=>t==type);
        int needed = active.OrderTypes.Count(t => t == type) - StockCount(type) - (ShopCatalog.IsPackaged(type) ? WarehouseCount(type) : 0);
        if (ShopCatalog.IsCoffee(type) && Activities.BrewRemaining > 0 && Activities.SelectedRecipe == type) needed--;
        if (needed <= 0) return false;
        if (!ShopCatalog.IsCoffee(type)) return true;
        return !Activities.CanPrepareDrink(type, true, needed);
    }
    public bool HasUnavailableRequest => active != null && active.OrderTypes.Any(IsRequestedTypeUnavailable);
    public bool OfferAlternative()
    {
        if(active==null||Stage!=ServiceStage.Order||!HasUnavailableRequest)return false;
        var missing=MissingOrderTypes;
        if(missing.Length==0)return false;
        int original = missing.Where(IsRequestedTypeUnavailable).DefaultIfEmpty(-1).First();
        if (original < 0) return false;
        Reputation?.MarkMissing(active);
        int replacement=SupplyTypes.Where(t=>t!=original&&ShopCatalog.IsCigarette(t)==ShopCatalog.IsCigarette(original)&&StockCount(t)>active.OrderTypes.Count(x=>x==t)&&ShopPricing.Accepts(t,Pricing.Price(t),Spawned-1,Day)).DefaultIfEmpty(-1).First();
        if(replacement<0||active.Person==2){Notify("Köszönöm, akkor most inkább elmegyek.");SkipCurrent();return false;}
        active.ReplaceRequested(original,replacement);Dialogue="Akkor "+ShopCatalog.ShortNames[replacement]+" jó lesz helyette, köszönöm.";Notify(Dialogue);ClosePanel();return true;
    }
    public void SkipCurrent()
    {
        if (active == null || (Stage != ServiceStage.Order && Stage != ServiceStage.Payment)) return;
        Reputation?.Complete(active,active.OrderTypes.Any(IsRequestedTypeUnavailable) ? ShopVisitOutcome.MissingStock : ShopVisitOutcome.Cancelled);
        Missed++; Dialogue = "Most nem tudtuk kiszolgálni. Majd legközelebb!";
        if (Devices != null && Devices.Focused) ClosePanel();
        DismissActive();
    }
    private void DismissActive()
    {
        Lottery?.CancelVisit(active);
        active.ReturnBasket();
        foreach(var item in receivedOrder)
        {
            if(item==null||item.IsHeld||item.CurrentSlot==null||!checkout.IsHandover(item.CurrentSlot))continue;
            var old=item.CurrentSlot;var free=FindObjectsByType<ProductSlot>().FirstOrDefault(s=>s.IsStockShelf&&s.CanAcceptStock(item.ProductId)&&s.Fits(item.transform.lossyScale));
            if(free!=null){item.AssignInitialSlot(free);if(old!=Activities.BasketSlot)checkout.RemoveReceipt(old);}else if(old!=Activities.BasketSlot){old.transform.position=Layout.EntryInside+Vector3.right*(receivedOrder.ToList().IndexOf(item)*.35f);item.AssignInitialSlot(old);}
        }
        CleanupReceipts();checkout.ClearItems();Activities.ClearHand(); Drawer.CancelTender(); changeCoins.Clear(); queue.Remove(active); active.Leave(); departing.Add(active); active = null; prepared = null; Stage = ServiceStage.None;
    }
    private void CleanupReceipts() => checkout.CleanupEmptyReceipts();
    private void FinishDay()
    {
        Reputation?.State.CloseDay();
        Lottery?.Book.Tick(8.1f);
        inventory.CompleteDeliveries();
        Warehouse?.SyncBoxes();
        ClosePanel(); Ledger.Close(300); SyncCashDrawer(); dayCycle.Finish(); SetModal(ShopModal.Summary);
    }
    public bool TryEndDay()
    {
        if (Phase == ShopPhase.Preparing || Phase == ShopPhase.Open)
        { Notify("Előbb fordítsd ZÁRVA állásba a táblát."); return false; }
        if (!CanEndDay)
        { Notify("Várd meg, amíg minden vendég távozik, és tedd le a kezedben lévő tárgyat."); return false; }
        if (player == null || Entrance == null ||
            Vector3.Distance(player.transform.position, Entrance.Position) > 2.5f) return false;
        AfterHours.EndEvening();
        FinishDay();
        return true;
    }
    public bool StayAfterHours()
    {
        if(!dayCycle.Stay(ShopEmpty))return false;
        SetModal(ShopModal.None);AfterHours.Enter();Notify("Zárás után maradtál. A raktáron át a hátsó udvarra is kimehetsz. E a bejárati ajtónál – hazamegyek.");return true;
    }
    public void ReturnToSummary()
    {
        if(Phase!=ShopPhase.AfterHours)return;
        TryEndDay();
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
        Reputation?.BeginDay();
        Lottery?.AdvanceDay();
        ResetCardTerminal();
        Security?.ReleaseEntrance();
        dayCycle.Prepare(); Stage = ServiceStage.None; Spawned = 0; Missed = 0;
        StolenToday=0;CaughtToday=0;PriceRefusals=0;
        checkout.ClearItems();active = null; prepared = null; changeCoins.Clear(); Message = "Készülj fel, majd E-vel fordítsd meg az ajtón lévő táblát. Az óra csak nyitáskor indul.";
        SetModal(ShopModal.None);
    }
    public void ResetDemo()
    {
        Reputation?.ResetAll();
        Partners?.ResetAll();
        Arrangement?.ResetLayout();
        AfterHours?.ResetEvenings();Pricing?.ResetPrices();GetComponent<ShopNextUpgrades>()?.ResetUpgrades();
        SingleProductVisits=false;AutomaticArrivals=true;checkout.ClearItems();
        if (stockRoot == null) return;
        if (player != null) player.CancelHeld();
        foreach (MvpCustomerActor actor in queue.Concat(departing).Concat(fleeing)) if (actor != null) { actor.ReturnBasket(); actor.gameObject.SetActive(false); Destroy(actor.gameObject); }
        queue.Clear(); departing.Clear(); fleeing.Clear(); active = null;
        foreach (ShopProduct item in stock) if (item != null) item.RemoveFromWorld();
        stock.Clear();CleanupReceipts(); inventory.Reset(); Warehouse?.ResetStorage();
        ClosePanel(); if (Layout != null) { Layout.SetExpanded(false); Layout.SetExtraShelf(false); }
        LastPurchaseSubtotal = 0; LastPurchaseTotal = 0;
        SelectedSupply = 0; Day = 1; Ledger = new ShopLedger(6000); Drawer = new ShopCashDrawer();
        Lottery?.ResetLottery();
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
        Navigation?.Rebuild();
    }
    private ShopProduct CreateProduct(int type)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = ShopCatalog.Names[type]; item.transform.SetParent(stockRoot, false);
        item.transform.localScale = ProductVisuals.Size(type);
        item.GetComponent<Renderer>().sharedMaterial = productMaterials[Mathf.Min(type,productMaterials.Length-1)];
        ShopProduct product = item.AddComponent<ShopProduct>(); product.ConfigureProduct(ShopCatalog.Ids[type], ShopCatalog.Names[type]);
        product.SetUnitCost(ShopCatalog.Costs[type]);
        ProductVisuals.Add(item,type);
        stock.Add(product); return product;
    }
    public int StockCount(int type) => stock.Count(p => p != null && p.gameObject.activeSelf && p.ProductId == ShopCatalog.Ids[type]);
    public int WarehouseCount(int type) => inventory.Stored(type);
    public int PendingQuantity(int type) => inventory.Pending(type);
    public float NextDelivery => inventory.NextDelivery;
    public int AvailableInventory(int type) => ShopPartnerCatalog.IsVariant(type) && !IsSupplyAvailable(type) ? 0 : ShopLotteryCatalog.IsTicket(type) ? Lottery?.Available(type) ?? 0 : StockCount(type) + WarehouseCount(type);
    public ShopProduct CreateLotteryProduct(int type)
    {
        if(!ShopLotteryCatalog.IsTicket(type))throw new ArgumentOutOfRangeException(nameof(type));
        return CreateProduct(type);
    }
    private int CurrentOrderCost => receivedOrder.Sum(item => item.UnitCost >= 0 ? item.UnitCost : ShopCatalog.Costs[ShopCatalog.Index(item.ProductId)]);
    private bool TrySettleSale(Func<bool> payment) => checkout.TrySettle(() => Lottery.SettleSale(active,receivedOrder,payment));
    public void FinishLotteryVisit(string text)
    {
        if(active==null||active.LotteryClaimId==null||Stage!=ServiceStage.Order)return;
        Reputation?.Complete(active,ShopVisitOutcome.Served);
        Stage=ServiceStage.Thanks;thanksTimer=2.5f;Dialogue=text;Notify(text);ClosePanel();
    }
    public bool OrderSupply(int type) => OrderSupply(type,3);
    public bool OrderSupply(int type,int quantity)
    {
        if(Ledger.Closed){Message="A mai elszámolás már lezárult. Árurendeléshez kezdd el a következő napot.";return false;}
        if (!IsSupplyAvailable(type) || Phase == ShopPhase.Summary) { Message = "Ez az áru a boltbővítés után rendelhető."; return false; }
        if (quantity < 1 || quantity > ShopCatalog.MaxOrderQuantity) { Message = "Egy rendelésben 1–12 terméket választhatsz."; return false; }
        var quantities = new int[ShopCatalog.SupplyCapacity];
        quantities[type] = quantity;
        if (!TryPurchase(quantities, out var quote))
        {
            Message = quote.Error == ShopPurchaseError.Capacity ? "A dobozban már elég áru vár ebből a termékből." :
                "Nincs elég elkölthető pénz. Rendelés: "+ShopCatalog.Money((int)ShopPurchaseRules.Payment((long)ProcurementCost(type)*quantity,5))+"; kasszaegyenleg: "+ShopCatalog.Money(Ledger.AvailableFunds)+".";
            return false;
        }
        SelectedSupply = type;
        Message = "Megrendelve " + quantity + " × " + ShopCatalog.Names[type] + ". A szállítás 8 másodperc.";
        return true;
    }
    public void CycleSupply() { var types = SupplyTypes; SelectedSupply = types[(Array.IndexOf(types, SelectedSupply) + 1) % types.Length]; Message = "Dobozból kiválasztva: " + ShopCatalog.Names[SelectedSupply]; }
    public bool TryCreateSupply(int type, out ShopProduct product)
    {
        product = null;
        if (!IsSupplyAvailable(type) || Phase == ShopPhase.Summary || WarehouseCount(type) <= 0) return false;
        product = CreateProduct(type); product.transform.position = (Warehouse!=null ? Warehouse.Dock(type).position : supplyPoint.position) + Vector3.up*.25f;
        product.SetUnitCost(inventory.NextUnitCost(type));
        inventory.TryWithdraw(type); Warehouse?.SyncBoxes(); return true;
    }
    public bool StockFromCarton(WarehouseCarton carton,ProductSlot slot)
    {
        if(carton==null||!carton.IsHeld||Phase==ShopPhase.Summary||!carton.CanPlace(slot)||WarehouseCount(carton.ProductType)<=0)return false;
        var product=CreateProduct(carton.ProductType);
        if(!(product.IsHeld?product.TryPlace(slot):product.AssignInitialSlot(slot))){product.RemoveFromWorld();return false;}
        product.SetUnitCost(inventory.NextUnitCost(carton.ProductType));
        inventory.TryWithdraw(carton.ProductType);carton.AdjustQuantity(-1);
        slot.NotifyProductPlaced(product);
        Message=product.DisplayName+" a polcon. A dobozban még "+carton.Quantity+" db.";
        return true;
    }
    public void TakeSupply(PlayerInteractor interactor) => TakeSupply(interactor,-1);
    public void TakeSupply(PlayerInteractor interactor,int brand)
    {
        if (interactor.HasHeldItem) return;
        if (brand>=0 && IsSupplyAvailable(brand)) SelectedSupply=brand;
        else if (brand>=0 && !IsSupplyAvailable(brand)) { Message="Ez a márka a boltbővítés után rendelhető.";return; }
        if (brand<0 && WarehouseCount(SelectedSupply) == 0)
        {
            int available = SupplyTypes.Where(t => WarehouseCount(t) > 0).DefaultIfEmpty(-1).First();
            if (available >= 0 && IsSupplyAvailable(available)) SelectedSupply = available;
        }
        if (!TryCreateSupply(SelectedSupply, out ShopProduct product))
        {
            Message = PendingQuantity(SelectedSupply) > 0
                ? ShopCatalog.Names[SelectedSupply] + " még úton van. Várd meg a szállítást!"
                : "A dobozban nincs megérkezett áru. Rendelj a pult bal oldalán lévő monitoron; a szállítás 8 másodperc.";
            return;
        }
        if (!interactor.TryHold(product)) { inventory.RestoreWithdrawal(SelectedSupply, product.UnitCost); product.RemoveFromWorld(); Warehouse?.SyncBoxes(); return; }
        Message = "Kézben: " + product.DisplayName + ". A zöld keretek jelzik a megfelelő helyeket. E – lerakás; a doboznál E – visszarakás.";
    }
    public bool ReturnSupply(PlayerInteractor interactor,int brand=-1,WarehouseCarton targetCarton=null)
    {
        ShopProduct product = interactor.HeldProduct;
        int type = product == null ? -1 : ShopCatalog.Index(product.ProductId);
        if (!ShopCatalog.IsPackaged(type) || Phase == ShopPhase.Summary) return false;
        if (brand>=0 && brand!=type) { Message="Ez másik márka doboza. Keresd a "+ShopCatalog.Names[type]+" feliratot.";return false; }
        if (targetCarton != null && (targetCarton.ProductType != type || targetCarton.IsHeld || !targetCarton.gameObject.activeInHierarchy)) return false;
        if (!inventory.CanReturn(type)) { Message = "Ebből a termékből megtelt a szállítódoboz."; return false; }
        if (!interactor.RemoveHeld(product)) return false;
        inventory.TryReturn(type, product.UnitCost);
        if (targetCarton != null) targetCarton.AdjustQuantity(1);
        SelectedSupply = type; Warehouse?.SyncBoxes();
        Message = "Visszatéve a dobozba: " + ShopCatalog.Names[type] + ".";
        return true;
    }
    public void SetModal(ShopModal modal)
    {
        if (modal == ShopModal.Welcome || modal == ShopModal.Dialogue) modal = ShopModal.None;
        if (modal == ShopModal.Cash) { OpenCash(); return; }
        if (modal == ShopModal.Supplier) { OpenSupplier(); return; }
        if (Devices != null) Devices.Release(false);
        Modal = modal; ShopInputGate.SetBlocked(modal != ShopModal.None);
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
        if (Layout == null || Layout.ShopLevel>=3 || !CanDevelopShop) { Message = "Bővíteni nyitás előtt vagy zárás után, üres boltban lehet."; return false; }
        if (!Ledger.TryImprove(Layout.NextShopPrice)) { Message = "Nincs elég pénz a bővítéshez."; return false; }
        SyncCashDrawer(); Layout.SetLevels(Layout.ShopLevel+1,Layout.WarehouseLevel); Navigation?.Rebuild(); Devices?.RefreshMonitor();
        Message = "Megnyílt az önkiszolgáló rész! Rendelj árut, és töltsd fel az új polcokat. A pult a helyén maradt.";
        return true;
    }
    public bool BuyExtraShelf()
    {
        if(Layout==null||Layout.ExtraShelfOwned||!CanDevelopShop){Message="Berendezést nyitás előtt vagy zárás után, üres boltban vehetsz. Ez a polc csak egyszer vásárolható meg.";return false;}
        if(!Ledger.TryImprove(ShopLayout.ExtraShelfPrice)){Message="Nincs elég pénz a kiegészítő polcra.";return false;}
        SyncCashDrawer();Layout.SetExtraShelf(true);Navigation?.Rebuild();
        Message="Kiegészítő cigarettapolc a jobb oldali személyzeti falnál: 9 új termékhely. A pult a helyén maradt.";return true;
    }
    public void ClosePanel() { if (Phase != ShopPhase.Summary) SetModal(ShopModal.None); }
}
