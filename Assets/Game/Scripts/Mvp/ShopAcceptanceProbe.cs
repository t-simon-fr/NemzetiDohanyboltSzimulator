#if UNITY_EDITOR || SHOP_ACCEPTANCE
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;

// Built only into the dedicated local acceptance player, never the shipping player.
[Preserve]
public sealed class ShopAcceptanceProbe : MonoBehaviour
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private MvpShopController Shop => MvpShopController.Instance;
    private PlayerInteractor Player => FindAnyObjectByType<PlayerInteractor>();
    private Camera View => Player.GetComponentInChildren<Camera>();

#if SHOP_ACCEPTANCE
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() => new GameObject("AcceptanceProbe").AddComponent<ShopAcceptanceProbe>();
#endif

    private static void Set(object target, string property, object value)
    {
        target.GetType().GetProperty(property).SetValue(target, value);
    }

    private void Visit(int visit, int type)
    {
        Shop.OpenShop();
        Set(Shop, "Spawned", visit);
        typeof(MvpShopController).GetMethod("SpawnCustomer", Private).Invoke(Shop, new object[] { type, false });
        for (int i = 0; i < 2000 && Shop.ActiveCustomer == null; i++) Shop.Tick(.1f);
        if (Shop.ActiveCustomer == null) throw new Exception("Fixture customer did not arrive.");
    }

    private void DeliverAll()
    {
        foreach (int type in Shop.ActiveCustomer.OrderTypes)
        {
            var item = FindObjectsByType<ShopProduct>().First(p => p.ProductId == ShopCatalog.Ids[type] && p.CurrentSlot != null && p.CurrentSlot.IsStockShelf);
            item.AssignInitialSlot(Shop.DeliverySlot);
            Shop.DeliverySlot.NotifyProductPlaced(item);
        }
    }

    [Preserve]
    public void Prepare(string scenario)
    {
        var oldFloor = GameObject.Find("Acceptance movement floor");
        if (oldFloor != null) { oldFloor.SetActive(false); Destroy(oldFloor); }
        Player.CancelHeld();
        Shop.GetComponent<ShopMenu>().Resume();
        Shop.ResetDemo();
        Shop.ClosePanel();
        Shop.ManualTick = true;
        if (scenario == "movement")
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Acceptance movement floor";
            floor.transform.position = new Vector3(100, -.1f, 100);
            floor.transform.localScale = new Vector3(24, .2f, 24);
            var body = Player.GetComponent<CharacterController>();
            body.enabled = false;
            Player.transform.SetPositionAndRotation(new Vector3(100, .1f, 100), Quaternion.identity);
            View.transform.localRotation = Quaternion.Euler(15, 0, 0);
            typeof(FirstPersonController).GetField("pitch", Private).SetValue(Player.GetComponent<FirstPersonController>(), 15f);
            typeof(FirstPersonController).GetField("verticalVelocity", Private).SetValue(Player.GetComponent<FirstPersonController>(), -2f);
            body.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (scenario == "ui-order")
        {
            Visit(2, 0);
            Set(Shop.ActiveCustomer, "OrderTypes", new[] { 0, 0, 1 });
            Set(Shop.ActiveCustomer, "UnitPrices", new[] { 2200, 2200, 2300 });
            Set(Shop, "Dialogue", ShopPeople.Order(Shop.ActiveCustomer.Person, Shop.ActiveCustomer.OrderTypes));
            Shop.Notify("A rendelés megérkezett. Tedd a kért árut az átadóhelyre.");
            Aim(Shop.DeliverySlot.transform.position + Vector3.up * .15f, 1.5f);
        }
        else if (scenario == "counter-storage")
        {
            foreach (var slot in Shop.CounterStorage.Slots) Shop.CreateNightProduct().AssignInitialSlot(slot);
            Aim(Shop.CounterStorage.Slots[2].transform.position + Vector3.up*.13f);
        }
        else if (scenario == "arrangement")
        {
            Shop.Ledger.TrySellCard(150000, 0);
            Shop.SyncCashDrawer();
            Shop.TryExpand();
            Shop.GetComponent<ShopNextUpgrades>().BuyFridge();
            var slot = Shop.Arrangement.Furniture.First(f => f.Id == "public-shelf").Slots.First(s => s.Occupant == null);
            Shop.CreateCoffee(ShopCatalog.Index("water")).AssignInitialSlot(slot);
            var body = Player.GetComponent<CharacterController>();
            body.enabled = false;
            Player.transform.SetPositionAndRotation(ShopFloorPlan.StaffSpawn, Quaternion.Euler(0, 90, 0));
            body.enabled = true;
            Shop.Devices.Focus(ShopDeviceKind.Monitor);
            Shop.Devices.OpenApp(ShopComputerApp.Furnishing);
            Shop.ManualTick = false;
        }
        else if (scenario == "entrance")
        {
            Aim(GameObject.Find("Open Close Sign").transform.position);
            Shop.ManualTick = false;
        }
        else if (scenario == "card")
        {
            Visit(3, 0);
            DeliverAll();
            Shop.Devices.Focus(ShopDeviceKind.Till);
        }
        else if (scenario == "triple")
        {
            Visit(2, 0);
            Set(Shop.ActiveCustomer, "OrderTypes", new[] { 0, 0, 0 });
            Set(Shop.ActiveCustomer, "UnitPrices", new[] { 2200, 2200, 2200 });
            DeliverAll();
        }
        else if (scenario == "pickup")
        {
            var item = Shop.CreateCoffee(0);
            item.AssignInitialSlot(Shop.DeliverySlot);
            Aim(item.transform.position);
        }
        else if (scenario == "bat")
        {
            Player.TryHoldTool(Shop.Activities.Bat);
            Aim(Shop.DeliverySlot.transform.position);
        }
        else if (scenario == "mop")
        {
            var spill = Shop.Activities.SpawnSpill();
            if (spill == null) throw new Exception("No spill fixture location available.");
            Player.TryHoldTool(Shop.Activities.Mop);
            Aim(spill.transform.position + Vector3.up * .02f);
        }
        else if (scenario == "coffee")
        {
            Shop.Ledger.TrySellCard(60000, 0);
            Shop.SyncCashDrawer();
            Shop.Activities.BuyCoffeeMachine();
            Set(Shop.Activities, "CoffeeSupplies", 5);
            Aim(Shop.Activities.CoffeeOutput.transform.position + Vector3.up * .25f);
        }
        else if (scenario == "dialogue" || scenario == "refuse")
        {
            Visit(scenario == "refuse" ? 2 : 0, 0);
            foreach (var item in FindObjectsByType<ShopProduct>().Where(p => p.ProductId == ShopCatalog.Ids[0])) item.RemoveFromWorld();
            Shop.ClosePanel();
            Shop.ManualTick = false;
        }
        else if (scenario == "summary")
        {
            Shop.OpenShop();
            Shop.RequestClose();
            Shop.Tick(.1f);
            Aim(Shop.Entrance.Position + Vector3.up * .95f);
            Shop.ManualTick = false;
        }
        else if (scenario == "menu") Shop.GetComponent<ShopMenu>().Open();
        Physics.SyncTransforms();
        Snapshot("");
    }

    [Preserve]
    public void Action(string action)
    {
        if (action == "deliver-ui-one")
        {
            var item = FindObjectsByType<ShopProduct>().First(p => p.ProductId == ShopCatalog.Ids[0] && p.CurrentSlot != null && p.CurrentSlot.IsStockShelf);
            item.AssignInitialSlot(Shop.DeliverySlot);
            Shop.DeliverySlot.NotifyProductPlaced(item);
        }
        if (action == "ui-long-dialogue")
            Set(Shop, "Dialogue", "Kérek két Lucky Stroke-ot és egy Marlbroke-ot. A szomszéd szerint rövidebb az út, ha a másik irányból jövök. Kipróbáltam, most a másik szomszédnál lakom. De a rendelés biztos, azt felírtam.");
        if (action == "view-door-outside")
        {
            var body = Player.GetComponent<CharacterController>();
            body.enabled = false;
            Vector3 eye = Shop.Entrance.Position + new Vector3(0,1.75f,-1.3f);
            Player.transform.rotation = Quaternion.identity;
            Player.transform.position = eye - View.transform.localPosition;
            float pitch = Mathf.Atan2(.25f,1.3f)*Mathf.Rad2Deg;
            View.transform.localRotation = Quaternion.Euler(pitch,0,0);
            typeof(FirstPersonController).GetField("pitch",Private).SetValue(Player.GetComponent<FirstPersonController>(),pitch);
            body.enabled = true;
            Player.RefreshTarget(View.ViewportPointToRay(new Vector3(.5f,.5f,0)));
        }
        if (action == "view-door-inside") Aim(Shop.Entrance.Sign.position,1.3f);
        if (action == "monitor-furnishing")
        {
            Shop.Devices.Focus(ShopDeviceKind.Monitor);
            Shop.Devices.OpenApp(ShopComputerApp.Furnishing);
        }
        if (action == "finish-arranged-day")
        {
            Shop.OpenShop();
            Shop.RequestClose();
            Shop.Tick(.1f);
            Aim(Shop.Entrance.Position + Vector3.up * .95f);
        }
        if (action == "aim-sign") Aim(Shop.Entrance.Sign.position);
        if (action == "aim-door") Aim(Shop.Entrance.Position + Vector3.up * .95f);
        if (action == "view-entrance") Aim(Shop.Entrance.Position + Vector3.up * .95f, 1.65f);
        if (action == "drain-shop")
        {
            for (int i = 0; i < 3000 && !Shop.CanEndDay; i++)
            {
                if (Shop.ActiveCustomer != null) Shop.SkipCurrent();
                Shop.Tick(.1f);
            }
        }
        if (action == "aim-place") Aim(Shop.Activities.BasketSlot.transform.position + Vector3.up * .05f);
        if (action == "milk") Set(Shop.Activities, "MilkSupplies", 2);
        if (action == "finish-brew") Shop.Activities.Tick(4);
        if (action == "remove-cup")
        {
            Shop.Activities.CoffeeOutput.Occupant?.RemoveFromWorld();
        }
        if (action == "monitor-coffee")
        {
            Shop.Devices.Focus(ShopDeviceKind.Monitor);
            Shop.Devices.OpenApp(ShopComputerApp.Coffee);
        }
        if (action == "live-clock") { Shop.OpenShop(); Shop.ManualTick = false; }
        if (action == "resume") Shop.GetComponent<ShopMenu>().Resume();
        Snapshot("");
    }

    private void Aim(Vector3 target, float distance = .85f)
    {
        var root = Player.transform;
        var body = Player.GetComponent<CharacterController>();
        body.enabled = false;
        bool counter = target.x < .25f && target.z < -.4f && target.z > -4.2f;
        root.rotation = Quaternion.Euler(0, counter ? 90 : 180, 0);
        Vector3 eye = target + (counter ? Vector3.left : Vector3.forward) * distance;
        // The relocated sign is beside the counter end; approach from the door
        // rather than placing the test player inside the counter body.
        if (target.z < -4.2f && Mathf.Abs(target.y - 1.5f) < .1f) eye.x = ShopFloorPlan.EntranceX + .7f;
        eye.y = Mathf.Max(target.y + .3f, 1.75f);
        Vector3 horizontal = target - eye;
        horizontal.y = 0;
        root.rotation = Quaternion.LookRotation(horizontal);
        root.position = eye - root.rotation * View.transform.localPosition;
        float pitch = Mathf.Atan2(eye.y - target.y, horizontal.magnitude) * Mathf.Rad2Deg;
        View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        typeof(FirstPersonController).GetField("pitch", Private).SetValue(Player.GetComponent<FirstPersonController>(), pitch);
        body.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Player.RefreshTarget(View.ViewportPointToRay(new Vector3(.5f, .5f, 0)));
    }

    [Serializable]
    private sealed class State
    {
        public string stage, phase, modal, message, entered, held, cup, order, interactionKey, tool;
        public bool menu, blocked, unavailable, entranceOpen, canEndDay;
        public int balance, drawer, sales, missing, received, recipe, milk, coffee, day, missed, dirt;
        public float elapsed, timeScale;
        public Vector3 position;
        public Vector3[] receipts;
        public bool arranging, dragging, previewValid, routeToShelf;
        public string selectedFurniture, arrangementFeedback;
        public Vector3 shelfPosition, draftPosition, fridgePosition, arrangeButton, moveTarget, badTarget, fridgeTarget;
        public int shelfTurns, draftTurns, shelfStock;
        public string stockedItem;
        public int privateStock;
        public string outwardSign;
        public string uiNotice, spoken;
        public ShopOrderLine[] orderLines;
        public Rect menuStart, menuSettings, menuBinding, menuDefaults, menuBack, cardConfirm, cardBack;
        public bool settingsOpen, movementLearned;
        public bool grounded;
        public float verticalSpeed;
    }

    [Preserve]
    public void Snapshot(string unused)
    {
        var g = Shop;
        if (g == null) return;
        var receipts = FindObjectsByType<ShopProduct>().Where(p => p.CurrentSlot != null && (p.CurrentSlot == g.Activities.BasketSlot || p.CurrentSlot.name.StartsWith("Order receipt"))).ToArray();
        var state = new State
        {
            tool = Player.HeldTool == null ? "" : Player.HeldTool.Kind.ToString(), dirt = g.Activities.DirtCount,
            stage = g.Stage.ToString(), phase = g.Phase.ToString(), modal = g.Modal.ToString(),
            message = g.Message, entered = g.CardAmount, held = Player.HeldProduct?.ProductId ?? "",
            cup = g.Activities.CoffeeOutput.Occupant?.ProductId ?? "", order = g.ActiveCustomer?.OrderDescription ?? "",
            interactionKey = ShopBindings.Resolve(Key.E).ToString(), menu = ShopMenu.Active, blocked = ShopInputGate.Blocked,
            unavailable = g.HasUnavailableRequest, balance = g.Ledger.Cash, drawer = g.Drawer.Total, sales = g.Ledger.Sales,
            entranceOpen = g.Entrance.IsOpen, canEndDay = g.CanEndDay,
            missing = g.MissingOrderTypes.Length, received = receipts.Length, recipe = g.Activities.SelectedRecipe,
            milk = g.Activities.MilkSupplies, coffee = g.Activities.CoffeeSupplies, day = g.Day, missed = g.Missed,
            elapsed = g.Elapsed, timeScale = Time.timeScale, position = Player.transform.position,
            receipts = receipts.Select(p => p.transform.position).ToArray()
        };
        state.arranging = g.Arrangement.Active;
        state.dragging = g.Arrangement.Dragging;
        state.previewValid = g.Arrangement.PreviewValid;
        state.selectedFurniture = g.Arrangement.SelectedFurnitureId;
        state.arrangementFeedback = g.Arrangement.PreviewFeedback;
        var shelf = g.Arrangement.Furniture.First(f => f.Id == "public-shelf");
        var fridge = g.Arrangement.Furniture.First(f => f.Id == "drinks-fridge");
        state.shelfPosition = shelf.transform.position;
        state.fridgePosition = fridge.transform.position;
        state.shelfTurns = shelf.Pose.QuarterTurns;
        var poses = g.Arrangement.Draft;
        if (poses.TryGetValue(shelf, out var pose))
        {
            state.draftPosition = pose.Position;
            state.draftTurns = pose.QuarterTurns;
        }
        state.shelfStock = shelf.Slots.Count(s => s.Occupant != null);
        state.stockedItem = shelf.Slots.FirstOrDefault(s => s.Occupant != null)?.Occupant.GetEntityId().ToString() ?? "";
        var control = FindObjectsByType<ShopWorldControl>().FirstOrDefault(c => c.Kind == WorldControlKind.Arrange && c.isActiveAndEnabled);
        if (control != null) state.arrangeButton = View.WorldToScreenPoint(control.GetComponent<Collider>().bounds.center);
        state.moveTarget = View.WorldToScreenPoint(new Vector3(3.75f,0,-1.75f));
        state.badTarget = View.WorldToScreenPoint(new Vector3(1.25f,0,-3.5f));
        state.fridgeTarget = View.WorldToScreenPoint(new Vector3(4.75f,0,.75f));
        state.routeToShelf = g.Navigation.TryPath(g.Layout.EntryInside, shelf.AccessAt(shelf.Pose).First(), out var path);
        state.privateStock = g.CounterStorage.Slots.Count(s => s.Occupant != null);
        state.outwardSign = g.Entrance.Sign.GetComponentsInChildren<TextMesh>()
            .FirstOrDefault(t => Vector3.Dot(-t.transform.forward,-g.Entrance.DoorLeaf.forward) > .99f)?.text ?? "";
        var menu = g.GetComponent<ShopMenu>();
        var hud = g.GetComponent<MvpShopHud>();
        state.menuStart = ShopUi.ScreenRect(ShopMenu.StartRect);
        state.menuSettings = ShopUi.ScreenRect(ShopMenu.SettingsRect);
        state.menuBinding = ShopUi.ScreenRect(menu.InteractionBindingRect);
        state.menuDefaults = ShopUi.ScreenRect(ShopMenu.DefaultsRect);
        state.menuBack = ShopUi.ScreenRect(ShopMenu.BackRect);
        state.cardConfirm = ShopUi.ScreenRect(MvpShopHud.CardConfirmRect);
        state.cardBack = ShopUi.ScreenRect(MvpShopHud.CardBackRect);
        state.settingsOpen = menu.SettingsOpen;
        state.movementLearned = hud.MovementLearned;
        state.orderLines = hud.OrderLines;
        state.uiNotice = hud.CurrentNotice;
        state.spoken = g.Dialogue;
        state.grounded = Player.GetComponent<FirstPersonController>().IsGrounded;
        state.verticalSpeed = Player.GetComponent<FirstPersonController>().VerticalSpeed;
        Debug.Log("ACCEPTANCE_STATE:" + JsonUtility.ToJson(state));
    }
}
#endif
