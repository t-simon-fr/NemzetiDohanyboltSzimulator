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
        Shop.AutomaticArrivals = false;
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
        if(scenario=="visual-refresh") {
            Shop.Ledger.TrySellCard(100000,0);Shop.SyncCashDrawer();
            Shop.Devices.Focus(ShopDeviceKind.Monitor);
        }
        else if (scenario == "requests-arrivals")
        {
            Shop.OpenShop();Aim(Shop.DeliverySlot.transform.position+Vector3.up*.1f,1.4f);
        }
        else if (scenario == "requests-coffee-order")
        {
            Shop.Ledger.TrySellCard(60000,0);Shop.SyncCashDrawer();Shop.Activities.BuyCoffeeMachine();
            Set(Shop.Activities,"CoffeeSupplies",3);Set(Shop.Activities,"MilkSupplies",1);
            Shop.SingleProductVisits=true;Visit(0,ShopCatalog.Coffee);
            Set(Shop.ActiveCustomer,"OrderTypes",new[]{ShopCatalog.Coffee,ShopCatalog.LongCoffee,ShopCatalog.Latte});
            Set(Shop.ActiveCustomer,"UnitPrices",new[]{650,750,1100});Set(Shop.ActiveCustomer,"Tender",5000);
            Set(Shop.ActiveCustomer,"UsesCard",true);
            AimCoffeeDisplay();Shop.ManualTick=false;
        }
        else if (scenario == "requests-lighting")
        {
            Set(Shop,"Phase",ShopPhase.AfterHours);Shop.AfterHours.Enter();ViewInteriorLighting();
        }
        else if (scenario == "requests-entrance")
        {
            Shop.AutomaticArrivals=false;Shop.OpenShop();Shop.RequestClose();Shop.Tick(1);
            Aim(Shop.Entrance.Position+Vector3.up*.9f,1.2f,Vector3.forward);
        }
        else if (scenario == "feature-cart")
        {
            Shop.Ledger.TrySellCard(100000,0); Shop.SyncCashDrawer();
            Shop.Devices.Focus(ShopDeviceKind.Monitor); Shop.Devices.OpenApp(ShopComputerApp.Orders);
        }
        else if (scenario == "feature-night")
        {
            Shop.Ledger.TrySellCard(50000,0); Shop.SyncCashDrawer();
            Set(Shop,"Phase",ShopPhase.AfterHours); Shop.AfterHours.Enter();
            Aim(Shop.AfterHours.Merchant.position+Vector3.up*1.3f,1.25f);
        }
        else if (scenario == "feature-tools")
        {
            Aim(Shop.Activities.Mop.transform.position+Vector3.up*.6f,1.15f,new Vector3(-1,0,-.6f).normalized);
        }
        else if (scenario == "feature-scanner")
        {
            Shop.SingleProductVisits=true; Visit(0,0);
            var product=FindObjectsByType<ShopProduct>().First(p=>p.ProductId==ShopCatalog.Ids[0]&&p.CurrentSlot!=null&&p.CurrentSlot.IsStockShelf);
            Player.TryHold(product);
            Aim(Shop.DeliverySlot.transform.position+Vector3.up*.05f,1.2f);
        }
        else if (scenario == "shelf-labels")
        {
            foreach (var product in FindObjectsByType<ShopProduct>().Where(product => product.CurrentSlot != null && product.CurrentSlot.IsStockShelf))
                product.RemoveFromWorld();
            foreach (var label in FindObjectsByType<ShopShelfPriceLabel>(FindObjectsInactive.Include)) label.Refresh();
            AimWarehouseShelf();
        }
        else if (scenario == "ingredient-pickup")
        {
            Shop.Ledger.TrySellCard(60000,0);Shop.SyncCashDrawer();Shop.Activities.BuyCoffeeMachine();
            Shop.Activities.OrderCoffeeSupplies();Shop.Activities.OrderMilkSupplies();Shop.Tick(8.1f);
            AimIngredient(false);
        }
        else if (scenario == "after-hours-upgrades")
        {
            Shop.Ledger.TrySellCard(200000,0);Shop.SyncCashDrawer();
            Shop.OpenShop();Shop.RequestClose();Shop.Tick(.1f);
            Shop.Devices.Focus(ShopDeviceKind.Monitor);Shop.Devices.OpenApp(ShopComputerApp.Furnishing);
        }
        else if (scenario == "stock-orders")
        {
            foreach (var product in FindObjectsByType<ShopProduct>()) product.RemoveFromWorld();
            Shop.Ledger.TrySellCard(60000,0); Shop.SyncCashDrawer(); Shop.TryExpand();
            Shop.OrderSupply(6,3); Shop.Tick(8.1f);
            Visit(0,6);
            Aim(Shop.DeliverySlot.transform.position + Vector3.up*.1f);
        }
        else if (scenario == "warehouse-storage")
        {
            foreach (var product in FindObjectsByType<ShopProduct>().Where(p => p.CurrentSlot != null && p.CurrentSlot.IsStockShelf))
                product.RemoveFromWorld();
            Shop.OrderSupply(0,2); Shop.OrderSupply(1,1); Shop.Tick(8.1f);
            AimStorageDock(Shop.Warehouse.Dock(0));
        }
        else if (scenario == "movement")
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
        else if (scenario == "coins" || scenario == "hand-interaction")
        {
            Visit(0, 0);
            // Isolated fixture price exercises all six circulating coin denominations.
            Set(Shop.ActiveCustomer, "UnitPrices", new[] { 385 });
            Set(Shop.ActiveCustomer, "UsesCoins", true);
            Set(Shop.ActiveCustomer, "Tender", 385);
            DeliverAll();
            AimCustomerHand();
            if (scenario == "coins") Shop.Devices.Focus(ShopDeviceKind.Coins);
        }
        else if (scenario == "card" || scenario == "cash")
        {
            Visit(scenario == "card" ? 3 : 0, 0);
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
        else if (scenario == "coffee" || scenario == "coffee-display")
        {
            Shop.Ledger.TrySellCard(60000, 0);
            Shop.SyncCashDrawer();
            Shop.Activities.BuyCoffeeMachine();
            Set(Shop.Activities, "CoffeeSupplies", 5);
            if (scenario == "coffee-display")
            {
                Set(Shop.Activities, "MilkSupplies", 2);
                AimCoffeeDisplay();
                Shop.ManualTick = false;
            }
            else Aim(Shop.Activities.CoffeeOutput.transform.position + Vector3.up * .25f);
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
        if(action=="design-exterior"||action=="design-interior") {
            Shop.ClosePanel();Player.GetComponent<FirstPersonController>().enabled=false;
            View.transform.position=action=="design-exterior"?new Vector3(-4.5f,2.5f,-12):new Vector3(1.7f,1.7f,-4);
            View.transform.LookAt(action=="design-exterior"?new Vector3(1,1.9f,-1.5f):new Vector3(-.4f,1.3f,.8f));
        }
        if (action == "aim-ingredient-coffee") AimIngredient(false);
        if (action == "advance-requests-arrivals")
        {
            for(int i=0;i<1000;i++){Shop.Tick(.5f);if(Shop.ActiveCustomer!=null)Shop.SkipCurrent();}
        }
        if (action == "aim-sign-stable")
        {
            var body=Player.GetComponent<CharacterController>();body.enabled=false;
            Player.transform.position=Shop.Entrance.Position+new Vector3(0,.08f,2.1f);body.enabled=true;
            Shop.Entrance.Tick(2);Physics.SyncTransforms();Aim(Shop.Entrance.Sign.position,2.05f,Vector3.forward);
        }
        if (action == "aim-customer-zone") Aim(Shop.Activities.BasketSlot.transform.position+Vector3.up*.1f,1.2f);
        if (action == "aim-scanner") Aim(Shop.DeliverySlot.transform.position+Vector3.up*.05f,1.2f);
        if (action.StartsWith("aim-coffee-handover-") && int.TryParse(action.Substring("aim-coffee-handover-".Length),out int coffeePlace) && coffeePlace>=0 && coffeePlace<3)
            Aim(Shop.Activities.CustomerHandoverSlots[coffeePlace].transform.position+Vector3.up*.20f,1.1f,Vector3.left);
        if (action == "aim-broom-rack") Aim(Shop.Activities.Mop.Rack.transform.TransformPoint(Vector3.up*.55f),1.1f,new Vector3(-1,0,-.6f).normalized);
        if (action == "aim-bat-rack") Aim(Shop.Activities.Bat.Rack.transform.TransformPoint(Vector3.up*.45f),1.1f,Vector3.forward);
        if (action == "fixture-lights-off") foreach(var light in Shop.InteriorLighting.Lights)light.enabled=false;
        if (action == "fixture-lights-on") foreach(var light in Shop.InteriorLighting.Lights)light.enabled=true;
        if (action == "focus-request-terminal") Shop.Devices.Focus(ShopDeviceKind.Terminal);
        if (action == "aim-ingredient-milk") AimIngredient(true);
        if (action == "aim-refill-machine") AimCoffeeDisplay();
        if (action == "view-refilled-machine") Shop.Devices.Focus(ShopDeviceKind.Coffee);
        if (action == "take-stock-order")
        {
            Shop.TakeSupply(Player,6);
            Aim(Shop.DeliverySlot.transform.position + Vector3.up*.1f);
        }
        if (action.StartsWith("aim-storage-") && int.TryParse(action.Substring(12),out int dockIndex)) AimStorageDock(Shop.Warehouse.Dock(dockIndex));
        if (action == "aim-storage-lower") AimStorageDock(LowerStorageDock());
        if (action == "aim-warehouse-shelf") AimWarehouseShelf();
        if (action == "order-shelf-delivery") { Shop.OrderSupply(0, 2); Shop.ManualTick = false; }
        if (action == "aim-stock-shelf")
        {
            var slot = GameObject.Find("Cigarette slot 0-0").GetComponent<ProductSlot>();
            Aim(slot.transform.position + Vector3.up * .09f, .85f, -slot.transform.forward);
        }
        if (action == "aim-coffee-display") AimCoffeeDisplay();
        if (action == "aim-customer-hand") { Shop.ClosePanel(); AimCustomerHand(); }
        if (action == "empty-coffee-milk") { Set(Shop.Activities, "MilkSupplies", 0); Shop.Activities.CoffeeDisplay.Refresh(); }
        if (action == "empty-coffee-supplies") { Set(Shop.Activities, "CoffeeSupplies", 0); Shop.Activities.CoffeeDisplay.Refresh(); }
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
        if (action == "aim-night-box" && Shop.AfterHours.DeliveryBox!=null) Aim(Shop.AfterHours.DeliveryBox.transform.position+Vector3.up*.13f,1.1f);
        if (action == "aim-private-storage") Aim(Shop.CounterStorage.Slots[0].transform.position+Vector3.up*.1f,1f);
        if (action == "view-private-storage") Aim(Shop.CounterStorage.Slots[5].transform.position+Vector3.up*.1f,1.4f);
        if (action == "aim-bat") { Player.CancelHeld(); Aim(Shop.Activities.Bat.transform.position+Vector3.up*.4f,1.1f,Vector3.forward); }
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
            if (Player.HeldProduct != null && Player.HeldProduct == Shop.Activities.CoffeeOutput.Occupant) Player.CancelHeld();
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

    private void AimCustomerHand()
    {
        var hand = Shop.Activities.CustomerHand;
        Vector3 target = hand.TransformPoint(new Vector3(.08f, .01f, .06f));
        Aim(target, 1.10f, hand.forward);
        // Let the real controller reach the floor before locking in the test aim.
        // Otherwise its first gravity step shifts a thin world target off-center.
        var controller = Player.GetComponent<FirstPersonController>();
        controller.StepMovement(Vector2.zero, false, false, .3f);
        Vector3 direction = target - View.transform.position;
        Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up);
        Player.transform.rotation = Quaternion.LookRotation(horizontal);
        float pitch = Mathf.Atan2(-direction.y, horizontal.magnitude) * Mathf.Rad2Deg;
        View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        typeof(FirstPersonController).GetField("pitch", Private).SetValue(controller, pitch);
        Physics.SyncTransforms();
        Player.RefreshTarget(View.ViewportPointToRay(new Vector3(.5f,.5f,0)));
    }

    private void AimWarehouseShelf()
    {
        var dock = Shop.Warehouse.Dock(0);
        Aim(dock.position + Vector3.up * .15f, 1.45f, new Vector3(1.2f, 0, .8f).normalized);
    }

    private void AimIngredient(bool milk)
    {
        Physics.SyncTransforms();
        var package=FindObjectsByType<ShopIngredientPackage>().First(p=>milk?p.Milk>0:p.Coffee>0);
        Aim(package.GetComponentInChildren<Collider>().bounds.center,1.25f,Vector3.right);
    }

    private Transform LowerStorageDock() => Shop.Warehouse.StorageDocks
        .First(d => d.parent == Shop.Warehouse.Dock(0) && d.position.y < Shop.Warehouse.Dock(0).position.y-.2f);

    private void AimStorageDock(Transform dock)
    {
        var body = Player.GetComponent<CharacterController>();
        body.enabled = false;
        Vector3 target = dock.position + Vector3.up*.18f - dock.forward*.24f;
        Vector3 eye = target - dock.forward*1.45f;
        // Stay in the real aisle; the left-hand drink rack blocks a straight approach.
        eye.x = Mathf.Clamp(eye.x,-4.65f,-3.05f);
        eye.y = 1.70f;
        Vector3 horizontal = Vector3.ProjectOnPlane(target-eye, Vector3.up);
        Player.transform.rotation = Quaternion.LookRotation(horizontal);
        Player.transform.position = eye - Player.transform.rotation*View.transform.localPosition;
        float pitch = Mathf.Atan2(eye.y-target.y,horizontal.magnitude)*Mathf.Rad2Deg;
        View.transform.localRotation = Quaternion.Euler(pitch,0,0);
        typeof(FirstPersonController).GetField("pitch",Private).SetValue(Player.GetComponent<FirstPersonController>(),pitch);
        body.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Physics.SyncTransforms();
        Player.RefreshTarget(View.ViewportPointToRay(new Vector3(.5f,.5f,0)));
    }

    private void AimCoffeeDisplay()
    {
        Shop.ClosePanel();
        var body = Player.GetComponent<CharacterController>();
        body.enabled = false;
        Vector3 target = Shop.Activities.CoffeeDisplay.transform.position;
        Vector3 eye = target + new Vector3(0, .38f, -.85f);
        Player.transform.rotation = Quaternion.identity;
        Player.transform.position = eye - View.transform.localPosition;
        float pitch = Mathf.Atan2(.38f, .85f) * Mathf.Rad2Deg;
        View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        typeof(FirstPersonController).GetField("pitch", Private).SetValue(Player.GetComponent<FirstPersonController>(), pitch);
        body.enabled = true;
        Physics.SyncTransforms();
        Player.RefreshTarget(View.ViewportPointToRay(new Vector3(.5f, .5f, 0)));
    }

    private void Aim(Vector3 target, float distance = .85f, Vector3? outward = null)
    {
        var root = Player.transform;
        var body = Player.GetComponent<CharacterController>();
        body.enabled = false;
        bool counter = target.x < .25f && target.z < -.4f && target.z > -4.2f;
        root.rotation = Quaternion.Euler(0, counter ? 90 : 180, 0);
        Vector3 eye = target + (outward ?? (counter ? Vector3.left : Vector3.forward)) * distance;
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

    private void ViewInteriorLighting()
    {
        Vector3 eye=new Vector3(1.8f,1.7f,-3.5f),target=new Vector3(.5f,2.4f,-.4f);
        var body=Player.GetComponent<CharacterController>();body.enabled=false;
        Vector3 horizontal=Vector3.ProjectOnPlane(target-eye,Vector3.up);
        Player.transform.rotation=Quaternion.LookRotation(horizontal);
        Player.transform.position=eye-Player.transform.rotation*View.transform.localPosition;
        float pitch=Mathf.Atan2(eye.y-target.y,horizontal.magnitude)*Mathf.Rad2Deg;
        View.transform.localRotation=Quaternion.Euler(pitch,0,0);
        typeof(FirstPersonController).GetField("pitch",Private).SetValue(Player.GetComponent<FirstPersonController>(),pitch);
        body.enabled=true;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
    }

    [Serializable]
    private sealed class State
    {
        public int offeredQuantity, boxedQuantity, scannerCount;
        public int requestsSpawned,customerGuides,fixtureLights,coffeeMerchandise;
        public bool broomAtHome,batAtHome;
        public string toolRack;
        public string computerApp,priceDraft,priceError;
        public int selectedPrice,priceCategory;
        public bool priceEditing;
        public string[] priceProducts,computerControlIds;
        public Rect[] computerControls;
        public Rect computerScreen;
        public int[] cartQuantities, pendingCart;
        public Rect[] cartFields;
        public Rect cartSubmit;
        public bool tradeOpen;
        public string clock, scannerProduct;
        public string stage, phase, modal, message, entered, held, cup, order, interactionKey, tool;
        public bool menu, blocked, unavailable, entranceOpen, canEndDay, applicationFocused, cursorLocked;
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
        public Rect[] cardDigits;
        public Rect cardUndo, cardClear;
        public string cardDisplayAmount, cardDisplayStatus, device;
        public string retailShelfLabel, warehouseShelfLabel, supplyPrompt;
        public int cartonQuantity, warehouseGoods, retailGoods;
        public string[] storageLabels;
        public string lowerStorageLabel;
        public int[] storageTypes;
        public int storagePlaces, heldCartonType;
        public bool canDevelop;
        public bool[] upgradesOwned;
        public Vector3[] upgradeButtons;
        public int[] upgradeCosts;
        public string ingredientTarget;
        public int ingredientCoffee,ingredientMilk,readyCoffee,readyMilk;
        public Rect[] cashDenominations, cashKeys;
        public int[] cashCounts;
        public int[] offeredCoins;
        public bool[] selectedCoins;
        public int coinSelected;
        public Rect[] coinTargets;
        public Rect coinClear, coinPay;
        public bool lookingAtHand, handUpright;
        public float handSide;
        public string handPrompt;
        public int cashPrice, cashTender;
        public float cameraFov;
        public string cashDue, cashReceived, cashChange, cashSelected;
        public Rect[] coffeeButtons;
        public string coffeeScreenRecipe, coffeeScreenStatus, coffeeScreenSupplies;
        public float brewRemaining;
        public bool coffeeActionEnabled, coffeeSlotHeld;
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
            applicationFocused = Application.isFocused, cursorLocked = Cursor.lockState == CursorLockMode.Locked,
            receipts = receipts.Select(p => p.transform.position).ToArray()
        };
        state.clock=ShopUiState.Clock(g.ClockElapsed);
        state.requestsSpawned=g.Spawned;
        state.coffeeMerchandise=Enumerable.Range(ShopCatalog.Coffee,3).Sum(g.StockCount);
        state.customerGuides=new[]{g.Activities.BasketSlot}.Concat(g.Activities.CustomerHandoverSlots)
            .Sum(slot=>slot.GetComponentsInChildren<LineRenderer>().Count(line=>line.enabled));
        state.fixtureLights=g.InteriorLighting.Lights.Count(light=>light.enabled);
        state.broomAtHome=!g.Activities.Mop.IsHeld&&Vector3.Distance(g.Activities.Mop.transform.position,g.Activities.Mop.Rack.transform.position)<.01f;
        state.batAtHome=!g.Activities.Bat.IsHeld&&Vector3.Distance(g.Activities.Bat.transform.position,g.Activities.Bat.Rack.transform.position)<.01f;
        state.toolRack=Player.TargetToolRack!=null?Player.TargetToolRack.Tool.Kind.ToString():"";
        state.offeredQuantity=g.AfterHours.OfferedQuantity;
        state.boxedQuantity=g.AfterHours.DeliveryBox!=null?g.AfterHours.DeliveryBox.Quantity:0;
        state.tradeOpen=g.AfterHours.TradeOpen;
        state.scannerCount=g.Scanner.ScanCount;
        state.scannerProduct=g.Scanner.LastProductId;
        state.computerApp=g.Devices.ComputerApp.ToString();
        state.selectedPrice=g.Pricing.Price(g.Pricing.SelectedType);
        state.priceCategory=(int)g.Pricing.Category;
        state.priceDraft=g.Pricing.Draft;state.priceError=g.Pricing.Error;state.priceEditing=g.Pricing.Editing;
        state.priceProducts=g.Pricing.VisibleTypes.Select(t=>ShopCatalog.Ids[t]).ToArray();
        if(g.Devices.ComputerShellActive) {
            var controls=g.Devices.KeyboardTargets();state.computerControlIds=controls.Select(t=>t.Id).ToArray();
            state.computerControls=controls.Select(t=>t.Bounds).ToArray();state.computerScreen=g.Devices.ComputerScreenRect;
        }
        state.cartQuantities=Enumerable.Range(0,ShopCatalog.Count).Select(g.Devices.CartQuantity).ToArray();
        state.pendingCart=Enumerable.Range(0,ShopCatalog.Count).Select(g.PendingQuantity).ToArray();
        if (g.Devices.Focused && g.Devices.FocusKind==ShopDeviceKind.Monitor)
        {
            var rect=g.Devices.AppRect;
            state.cartFields=Enumerable.Range(0,2).Select(i=>Shop.Devices.CartFieldRect(i)).ToArray();
            state.cartSubmit=Shop.Devices.ComputerToScreen(new Rect(ShopDevices.ComputerBody.x+802,ShopDevices.ComputerBody.y+400,330,46));
        }
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
        state.cardConfirm = g.Devices.Terminal.Bounds(View, WorldControlKind.CardConfirm);
        state.cardBack = g.Devices.Terminal.Bounds(View, WorldControlKind.Exit);
        state.cardUndo = g.Devices.Terminal.Bounds(View, WorldControlKind.CardUndo);
        state.cardClear = g.Devices.Terminal.Bounds(View, WorldControlKind.CardClear);
        state.cardDigits = Enumerable.Range(0, 10).Select(digit => g.Devices.Terminal.Bounds(View, WorldControlKind.CardDigit, digit)).ToArray();
        state.cardDisplayAmount = g.Devices.Terminal.DisplayAmount;
        state.cardDisplayStatus = g.Devices.Terminal.DisplayStatus;
        state.retailShelfLabel = GameObject.Find("Shelf price 0-0").GetComponent<TextMesh>().text;
        state.warehouseShelfLabel = g.Warehouse.Dock(0).Find("Brand dock label").GetComponent<TextMesh>().text;
        state.supplyPrompt = g.Warehouse.Dock(0).GetComponent<ShopStation>().Prompt;
        state.cartonQuantity = Player.HeldCarton != null ? Player.HeldCarton.Quantity : -1;
        state.warehouseGoods = g.WarehouseCount(0);
        state.storagePlaces = g.Warehouse.StorageDocks.Count;
        state.storageLabels = Enumerable.Range(0,ShopCatalog.Count).Select(i => g.Warehouse.Dock(i).Find("Brand dock label").GetComponent<TextMesh>().text).ToArray();
        state.storageTypes = Enumerable.Range(0,ShopCatalog.Count).Select(i => g.Warehouse.CartonAt(g.Warehouse.Dock(i))?.ProductType ?? -1).ToArray();
        state.lowerStorageLabel = LowerStorageDock().Find("Brand dock label").GetComponent<TextMesh>().text;
        state.heldCartonType = Player.HeldCarton != null ? Player.HeldCarton.ProductType : -1;
        state.canDevelop = g.CanDevelopShop;
        var targetTool=typeof(PlayerInteractor).GetField("targetTool",Private).GetValue(Player) as ShopHandTool;
        var targetPackage=targetTool!=null?targetTool.GetComponent<ShopIngredientPackage>():null;
        state.ingredientTarget=targetPackage!=null?(targetPackage.Milk>0?"milk":"coffee"):"";
        state.ingredientCoffee=g.Activities.CoffeeSupplies;state.ingredientMilk=g.Activities.MilkSupplies;
        state.readyCoffee=g.Activities.ReadyCoffee;state.readyMilk=g.Activities.ReadyMilk;
        state.upgradesOwned = new[]{g.Layout.Expanded,g.Layout.ExtraShelfOwned,g.Activities.CoffeeOwned,g.GetComponent<ShopNextUpgrades>().FridgeOwned,g.Security.Owned};
        state.upgradeCosts = new[]{ShopLayout.ExpansionPrice,ShopLayout.ExtraShelfPrice,ShopActivities.CoffeeMachinePrice,ShopNextUpgrades.FridgePrice,ShopSecurity.PurchasePrice};
        state.upgradeButtons = new[]{WorldControlKind.Expand,WorldControlKind.ExtraShelf,WorldControlKind.CoffeeMachineUpgrade,WorldControlKind.FridgeUpgrade,WorldControlKind.PanicButtonUpgrade}
            .Select(kind=>FindObjectsByType<ShopWorldControl>().FirstOrDefault(c=>c.Kind==kind&&c.isActiveAndEnabled))
            .Select(c=>c!=null?View.WorldToScreenPoint(c.GetComponent<Collider>().bounds.center):Vector3.zero).ToArray();
        state.retailGoods = g.StockCount(0);
        state.cameraFov = View.fieldOfView;
        state.cashDue = g.Devices.RegisterVisuals.DisplayDue;
        state.cashReceived = g.Devices.RegisterVisuals.DisplayReceived;
        state.cashChange = g.Devices.RegisterVisuals.DisplayChange;
        state.cashSelected = g.Devices.RegisterVisuals.DisplaySelected;
        state.cashPrice = g.ActiveCustomer != null ? g.ActiveCustomer.TotalPrice : 0;
        state.cashTender = g.ActiveCustomer != null ? g.ActiveCustomer.Tender : 0;
        var cashTargets = g.Devices.FocusKind == ShopDeviceKind.Till ? g.Devices.KeyboardTargets() : new System.Collections.Generic.List<ShopKeyboardTarget>();
        state.cashDenominations = ShopCatalog.Denominations.Select(value => cashTargets.FirstOrDefault(t => t.Id == WorldControlKind.Denomination + ":" + value)?.Bounds ?? Rect.zero).ToArray();
        state.cashKeys = new[] { WorldControlKind.Undo, WorldControlKind.Clear, WorldControlKind.Exchange, WorldControlKind.Pay }.Select(kind => cashTargets.FirstOrDefault(t => t.Id == kind + ":0")?.Bounds ?? Rect.zero).ToArray();
        state.cashCounts = ShopCatalog.Denominations.Select(g.Drawer.Count).ToArray();
        var coinOffer = g.Activities.CoinOffer;
        state.offeredCoins = coinOffer != null ? coinOffer.Coins.ToArray() : new int[0];
        state.selectedCoins = coinOffer != null ? coinOffer.Selected.ToArray() : new bool[0];
        state.coinSelected = coinOffer != null ? coinOffer.Total : 0;
        var hand = g.Activities.CustomerHand;
        if (hand != null)
        {
            Physics.SyncTransforms();
            if (Physics.Raycast(View.ViewportPointToRay(new Vector3(.5f,.5f,0)), out var handHit, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                state.lookingAtHand = handHit.collider.GetComponentInParent<ShopStation>() == hand.GetComponent<ShopStation>();
            state.handPrompt = hand.GetComponent<ShopStation>().Prompt;
            state.handUpright = Vector3.Dot(View.transform.up, Vector3.up) > .3f;
            state.handSide = Vector3.Dot(Vector3.ProjectOnPlane(View.transform.position-hand.position,Vector3.up).normalized,hand.forward);
        }
        var coinTargets = g.Devices.FocusKind == ShopDeviceKind.Coins ? g.Devices.KeyboardTargets() : new System.Collections.Generic.List<ShopKeyboardTarget>();
        state.coinTargets = Enumerable.Range(0, state.offeredCoins.Length).Select(i => coinTargets.FirstOrDefault(t => t.Id == "CustomerCoin:" + i)?.Bounds ?? Rect.zero).ToArray();
        state.coinClear = coinTargets.FirstOrDefault(t => t.Id == "Coins:Clear")?.Bounds ?? Rect.zero;
        state.coinPay = coinTargets.FirstOrDefault(t => t.Id == "Coins:Pay")?.Bounds ?? Rect.zero;
        state.device = g.Devices.FocusKind.ToString();
        state.coffeeButtons = Enumerable.Range(0, 4).Select(index => g.Activities.CoffeeDisplay.Bounds(View, index)).ToArray();
        state.coffeeScreenRecipe = g.Activities.CoffeeDisplay.DisplayRecipe;
        state.coffeeScreenStatus = g.Activities.CoffeeDisplay.DisplayStatus;
        state.coffeeScreenSupplies = g.Activities.CoffeeDisplay.DisplaySupplies;
        state.brewRemaining = g.Activities.BrewRemaining;
        state.coffeeActionEnabled = g.Activities.CoffeeDisplay.ActionControl.Interactable;
        state.coffeeSlotHeld = g.Activities.CoffeeOutput.Occupant != null && g.Activities.CoffeeOutput.Occupant.IsHeld;
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
