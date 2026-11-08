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
        if (scenario == "mobile" || scenario == "mobile-cash" || scenario == "mobile-locked")
        {
            Shop.AutomaticArrivals = false;
            if (scenario != "mobile-locked")
            {
                Set(Shop, "Day", 6); Shop.Mobile.BeginDay();
                Visit(3, -2);
                Set(Shop.ActiveCustomer, "UsesCard", scenario != "mobile-cash");
                Set(Shop.ActiveCustomer, "UsesCoins", false);
                if (scenario == "mobile-cash") Set(Shop.ActiveCustomer, "Tender", Shop.ActiveCustomer.TotalPrice + 500);
                Aim(Shop.Devices.Terminal.transform.position, 1.1f, Vector3.left);
                Shop.Mobile.SelectPage(0);
            }
            else { Shop.Devices.Focus(ShopDeviceKind.Monitor); Shop.Devices.OpenApp(ShopComputerApp.Mobile); }
            Shop.ManualTick = false;
        }
        else if (scenario == "hot-drinks")
        {
            Shop.Ledger.TrySellCard(60000, 0); Shop.SyncCashDrawer(); Shop.Activities.BuyCoffeeMachine();
            Shop.AutomaticArrivals = false;
            Shop.Devices.Focus(ShopDeviceKind.Monitor); Shop.Devices.OpenApp(ShopComputerApp.Coffee);
        }
        else if (scenario == "reputation" || scenario == "reputation-visual")
        {
            Shop.AutomaticArrivals=false;
            if (scenario=="reputation-visual")
            {
                var reputation=Shop.Reputation.State;
                for(int i=0;i<4;i++)
                {
                    reputation.Enter(i,40);reputation.Observe(45,.7,65,85);
                    reputation.Complete(i,i==0?ShopVisitOutcome.MissingStock:i==1?ShopVisitOutcome.Cancelled:ShopVisitOutcome.Served);
                }
                reputation.CloseDay();
            }
            Shop.Devices.Focus(ShopDeviceKind.Monitor);Shop.Devices.OpenApp(ShopComputerApp.Desktop);
        }
        else if (scenario == "partners" || scenario == "partners-visual")
        {
            Shop.Ledger.TrySellCard(500000,0);Shop.SyncCashDrawer();Shop.Layout.SetLevels(3,3);Shop.Navigation.Rebuild();
            Shop.OrderSupply(6,1);Shop.Tick(8.1f);
            if (scenario == "partners-visual") SetupPartnerVisuals();
            else { Shop.Devices.Focus(ShopDeviceKind.Monitor);Shop.Devices.OpenApp(ShopComputerApp.Furnishing); }
        }
        else if (scenario == "lottery" || scenario == "lottery-numbers")
        {
            Shop.Ledger.TrySellCard(60000,0);Shop.SyncCashDrawer();
            Set(Shop.Lottery,"Book",new ShopLotteryState(1));
            Shop.Devices.Focus(ShopDeviceKind.Monitor);Shop.Devices.OpenApp(ShopComputerApp.Furnishing);
            Shop.AutomaticArrivals=false;Shop.ManualTick=false;
            if(scenario=="lottery-numbers")
            {
                Shop.Lottery.TryBuy();Shop.Lottery.Order(15);Shop.Tick(8.1f);Shop.ClosePanel();
                Visit(3,15);AimLotteryTerminal();Shop.Lottery.Open();
            }
        }
        else if (scenario == "warehouse-opening")
        {
            Aim(new Vector3(-2.5f, 1.1f, 0), 1.25f, Vector3.right);
        }
        else if(scenario=="visual-refresh") {
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
        else if (scenario == "furniture-editor")
        {
            Shop.Ledger.TrySellCard(200000,0);Shop.SyncCashDrawer();Shop.Layout.SetLevels(3,3);Shop.Navigation.Rebuild();
            Shop.Devices.Focus(ShopDeviceKind.Monitor);Shop.Devices.OpenApp(ShopComputerApp.Furnishing);
        }
        else if (scenario == "expansion")
        {
            Shop.Ledger.TrySellCard(500000,0);Shop.SyncCashDrawer();
            Shop.Devices.Focus(ShopDeviceKind.Monitor);Shop.Devices.OpenApp(ShopComputerApp.Furnishing);
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
        if (action == "reputation-customer")
        {
            Shop.ManualTick=true;Shop.ClosePanel();Shop.Tick(10);
            Shop.SingleProductVisits=true;Visit(Shop.Spawned,0);
            foreach(var spill in Shop.Activities.ActiveSpills)spill.gameObject.SetActive(false);
            Shop.Tick(35);
            Set(Shop.ActiveCustomer,"UsesCard",true);Set(Shop.ActiveCustomer,"UsesCoins",false);
            DeliverAll();Shop.Devices.Focus(ShopDeviceKind.Terminal);Shop.ManualTick=false;
        }
        if (action == "reputation-monitor") { Shop.Devices.Focus(ShopDeviceKind.Monitor);Shop.Devices.OpenApp(ShopComputerApp.Desktop); }
        if (action == "reputation-depart") { Shop.ClosePanel();for(int i=0;i<100;i++)Shop.Tick(.1f); }
        if (action == "reputation-regular")
        {
            Shop.ManualTick=true;Shop.ClosePanel();Shop.SingleProductVisits=false;Shop.OpenShop();
            var reputation=new ShopReputationState(85);reputation.BeginDay(Shop.Day);reputation.Remember(0,0);
            Set(Shop.Reputation,"State",reputation);
            int visit=Enumerable.Range(0,5000).First(v=>v%4!=3 && Shop.Reputation.ChooseRegular(v,out var person,out var favourite));
            Set(Shop,"Spawned",visit);
            typeof(MvpShopController).GetMethod("SpawnCustomer",Private).Invoke(Shop,new object[]{-1,false});
            for(int i=0;i<1000 && Shop.ActiveCustomer==null;i++)Shop.Tick(.1f);
            if(Shop.ActiveCustomer==null || !Shop.ActiveCustomer.IsRegular)throw new Exception("Regular fixture failed.");
            Aim(Shop.ActiveCustomer.transform.position+Vector3.up*1.2f,1.5f,Vector3.back,true);
        }
        if (action == "partner-orders") { Shop.Devices.Focus(ShopDeviceKind.Monitor);Shop.Devices.OpenApp(ShopComputerApp.Orders); }
        if (action == "partner-deliver") Shop.Tick(8.1f);
        if (action == "partner-return-carton") Player.ReturnCarton();
        if (action.StartsWith("partner-carton:"))
        {
            int type = int.Parse(action.Substring("partner-carton:".Length));
            Shop.ClosePanel();
            var carton = Shop.Warehouse.Carton(type);
            if (Shop.Warehouse.IsStorageDock(carton.transform.parent)) AimStorageDock(carton.transform.parent);
            else Aim(carton.transform.TransformPoint(new Vector3(0,.14f,-.198f)),1.15f,-carton.transform.forward,true);
            Physics.SyncTransforms();Player.RefreshTarget(View.ViewportPointToRay(new Vector3(.5f,.5f,0)));
        }
        if (action.StartsWith("partner-slot:"))
        {
            int type = int.Parse(action.Substring("partner-slot:".Length));
            var slot = PartnerSlots(type).First(p=>p.Occupant==null);
            Vector3 front = slot.HintCenter - slot.transform.forward * slot.GetComponent<BoxCollider>().size.z * .4f;
            // Stand in the aisle between the counter and cigarette wall;
            // standing directly below either outside bottle overlaps furniture.
            bool spirit = ShopPartnerCatalog.IsSpirit(type);
            Vector3 approach = spirit ? new Vector3((-1.4f-front.x)/1.35f,0,1) : -slot.transform.forward;
            Aim(front,spirit ? 1.35f : 1.15f,approach,true);
            // Expanded floors can settle the standing capsule above the initial eye height.
            // Aim from its actual position, rather than retaining the pre-contact pitch.
            Player.GetComponent<CharacterController>().Move(Vector3.down*.1f);
            Vector3 direction=front-View.transform.position;
            Player.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(direction,Vector3.up));
            direction=front-View.transform.position;
            float pitch=-Mathf.Atan2(direction.y,Vector3.ProjectOnPlane(direction,Vector3.up).magnitude)*Mathf.Rad2Deg;
            View.transform.localRotation=Quaternion.Euler(pitch,0,0);
            typeof(FirstPersonController).GetField("pitch",Private).SetValue(Player.GetComponent<FirstPersonController>(),pitch);
            Physics.SyncTransforms();Player.RefreshTarget(View.ViewportPointToRay(new Vector3(.5f,.5f,0)));
        }
        if (action == "partner-fridges-view") Aim(new Vector3(6,1.15f,Shop.Layout.FurnitureArea(false).yMax-.15f),2.8f,Vector3.back,true);
        if (action == "partner-counter-view") Aim(new Vector3(-1.6f,2.05f,-4.15f),1.3f,Vector3.forward,true);
        if (action == "partner-customer")
        {
            Shop.ClosePanel();Shop.SingleProductVisits=true;Visit(0,16);
            for(int i=0;i<1500 && Shop.Stage!=ServiceStage.Payment;i++)Shop.Tick(.1f);
            if(Shop.Stage!=ServiceStage.Payment)throw new Exception("Partner customer could not collect real public stock.");
            Set(Shop.ActiveCustomer,"UsesCard",true);Set(Shop.ActiveCustomer,"UsesCoins",false);
            Shop.ManualTick=false;
        }
        if(action=="lottery-deliver")Shop.Tick(8.1f);
        if(action=="lottery-scratch-customer")
        {
            Shop.ClosePanel();Visit(0,13);AimLotteryStock(13);
        }
        if(action=="lottery-number-customer")
        {
            Shop.ClosePanel();Shop.Tick(3f);Visit(3,15);AimLotteryTerminal();
        }
        if(action=="lottery-terminal")AimLotteryTerminal();
        if(action=="lottery-cash")Shop.OpenCash();
        if(action=="lottery-home")
        {
            Shop.ClosePanel();Shop.Tick(3f);Shop.RequestClose();
            for(int i=0;i<2000&&!Shop.CanEndDay;i++)Shop.Tick(.1f);
            Aim(Shop.Entrance.Position+Vector3.up*.95f);
        }
        if(action=="lottery-return")
        {
            Shop.ClosePanel();Shop.AutomaticArrivals=false;Shop.OpenShop();Set(Shop,"Spawned",1);
            typeof(MvpShopController).GetMethod("SpawnCustomer",Private).Invoke(Shop,new object[]{-1,false});
            for(int i=0;i<2000&&Shop.ActiveCustomer==null;i++)Shop.Tick(.1f);
            AimLotteryTerminal();
        }
        if(action=="design-exterior"||action=="design-interior") {
            Shop.ClosePanel();Player.GetComponent<FirstPersonController>().enabled=false;
            View.transform.position=action=="design-exterior"?new Vector3(-4.5f,2.5f,-12):new Vector3(1.7f,1.7f,-4);
            View.transform.LookAt(action=="design-exterior"?new Vector3(1,1.9f,-1.5f):new Vector3(-.4f,1.3f,.8f));
        }
        if (action == "mobile-open") Shop.Mobile.Open();
        if (action == "mobile-account") { Shop.Mobile.Open(); Shop.Mobile.SelectPage(1); }
        if (action == "mobile-receipt") { Shop.Mobile.Open(); Shop.Mobile.SelectPage(2); }
        if (action == "mobile-aim-terminal") Aim(Shop.Devices.Terminal.transform.position, 1.1f, Vector3.left);
        if (action == "hot-drinks-deliver") Shop.Tick(8.1f);
        if (action == "hot-drinks-customer")
        {
            Shop.ClosePanel(); Shop.SingleProductVisits = true; Visit(3, ShopCatalog.Tea);
            Set(Shop.ActiveCustomer, "OrderTypes", new[] { ShopCatalog.Tea, ShopCatalog.HotChocolate });
            Set(Shop.ActiveCustomer, "UnitPrices", new[] { Shop.Pricing.Price(ShopCatalog.Tea), Shop.Pricing.Price(ShopCatalog.HotChocolate) });
            Set(Shop.ActiveCustomer, "UsesCard", true); Set(Shop.ActiveCustomer, "UsesCoins", false);
            AimCoffeeDisplay(); Shop.ManualTick = false;
        }
        if (action == "hot-drinks-live") Shop.ManualTick = false;
        if (action == "aim-ingredient-tea") AimIngredient(ShopDrinkIngredient.Tea);
        if (action == "aim-ingredient-cocoa") AimIngredient(ShopDrinkIngredient.Cocoa);
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
            Aim(Shop.Activities.CustomerHandoverSlots[coffeePlace].transform.position+Vector3.up*.20f,1.1f,new Vector3(-1,0,-.8f).normalized);
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
        if (action == "view-lighter-display") Aim(Shop.Devices.MonitorPosition, 1.1f, new Vector3(-1, 0, -.6f).normalized);
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
        if(action.StartsWith("expansion-plan:"))
        {
            var parts=action.Split(':');Shop.Arrangement.Cancel();Shop.ClosePanel();
            Shop.Layout.SetLevels(int.Parse(parts[1]),int.Parse(parts[2]));Shop.Navigation.Rebuild();Shop.Arrangement.Begin();
        }
        if(action=="stock-editor-fridge")
        {
            var fridge=Shop.Arrangement.Current().Keys.First(f=>f.CatalogKind==4);
            var item=Shop.CreateCoffee(ShopCatalog.Index("water"));item.AssignInitialSlot(fridge.Slots[0]);
        }
        if(action=="empty-editor-fridge")
            foreach(var fridge in Shop.Arrangement.Current().Keys.Where(f=>f.CatalogKind==4))
                foreach(var slot in fridge.Slots)if(slot.Occupant!=null)slot.Occupant.RemoveFromWorld();
        if(action=="expansion-staff-view")
        {
            Shop.Arrangement.Cancel();Shop.ClosePanel();
            var target=new Vector3(Shop.Layout.ShopArea.xMax-.7f,1.1f,-1.2f);
            var offset=new Vector3(-1.4f,0,.15f)-new Vector3(target.x,0,target.z);
            Aim(target,offset.magnitude,offset.normalized,true);
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

    private ProductSlot[] PartnerSlots(int type)
    {
        int partner=ShopPartnerCatalog.PartnerFor(type);
        return partner==2 ? Shop.Partners.CounterDisplay.GetComponentsInChildren<ProductSlot>() :
            Shop.Arrangement.Furniture.First(f=>f.CatalogKind==6+partner && f.gameObject.activeSelf).Slots;
    }
    private void SetupPartnerVisuals()
    {
        var arrangement=Shop.Arrangement;var area=Shop.Layout.FurnitureArea(false);
        arrangement.Begin();
        foreach(int partner in new[]{0,1})
        {
            arrangement.StartPurchase(6+partner);
            if(!arrangement.PlacePurchase(new ShopFurniturePose(new Vector3(5+partner*2,0,area.yMax-.4f),0)))throw new Exception(arrangement.Feedback);
        }
        if(!arrangement.Save()||!Shop.Partners.BuyCounterDisplay())throw new Exception("Partner fixture purchase failed.");
        foreach(int type in new[]{6,16,17,5,18,19,20,21,22})
        {
            var product=Shop.CreateCoffee(type);
            product.AssignInitialSlot(PartnerSlots(type).First(slot=>slot.Occupant==null));
        }
        Aim(new Vector3(6,1.15f,area.yMax-.15f),2.8f,Vector3.back,true);
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

    private void AimIngredient(bool milk) => AimIngredient(milk ? ShopDrinkIngredient.Milk : ShopDrinkIngredient.Coffee);
    private void AimIngredient(ShopDrinkIngredient ingredient)
    {
        Physics.SyncTransforms();
        var package = FindObjectsByType<ShopIngredientPackage>().First(p => p.Ingredient == ingredient);
        Aim(package.GetComponentInChildren<Collider>().bounds.center,1.25f,Vector3.right);
    }

    private void AimLotteryStock(int type)
    {
        var stock=Shop.Lottery.Terminal.Find("Lottery stock "+type);
        Aim(stock.GetComponent<Collider>().bounds.center,1.1f,-Shop.Lottery.Terminal.forward,true);
    }
    private void AimLotteryTerminal() => Aim(Shop.Lottery.Terminal.TransformPoint(new Vector3(0,.14f,-.15f)),1.1f,-Shop.Lottery.Terminal.forward);

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

    private void Aim(Vector3 target, float distance = .85f, Vector3? outward = null, bool standingEye = false)
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
        eye.y = standingEye ? 1.75f : Mathf.Max(target.y + .3f, 1.75f);
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
        public double reputation,reputationSeconds,reputationChange,arrivalScale;
        public double[] reputationScores;
        public bool[] reputationEvidence;
        public int reputationVisits,reputationServiceVisits,reputationMissing,reputationRefusals,reputationRegulars;
        public string[] reputationReasons;
        public bool reputationLast,regularCustomer;
        public bool reputationViewingLast;
        public int reputationViewDay;
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
        public int shopLevel, warehouseLevel, moduleCount, plannedCount, plannedCost;
        public string[] editorControlIds;
        public Rect[] editorControls;
        public Vector3[] purchaseTargets;
        public Vector3 editorPreviewPosition;
        public int editorSaleCredit, editorFridges;
        public bool[] partnerOwned;
        public int[] supplyTypes, partnerStock, partnerStored, partnerPending, partnerCosts;
        public int[] partnerCartQuantities, physicalProductCosts;
        public string[] physicalProductIds;
        public Vector3[] partnerDropTargets;
        public int partnerCartSubtotal, partnerCartPayment, lastPurchaseSubtotal, lastPurchaseTotal, purchaseRounding, costOfGoods;
        public int editorCorners, editorPreviewTurns;
        public Rect editorPhysicalArea;
        public Vector3[] editorCornerTestPoints, editorCornerPoints;
        public bool editorModelVisible;
        public string editorSnap, editorNotice;
        public Vector3 editorPreviewSize;
        public string[] editorFridgeIds;
        public Vector3[] editorFridgePoints, editorTestPoints;
        public string[] furnitureIds;
        public Vector3[] furniturePoints;
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
        public string ingredientBrand, heldIngredientBrand;
        public bool lotteryOwned, lotteryFirstSold, lotteryFirstRedeemed;
        public int lotterySales, lotteryPayouts, lotteryCommission, lotteryOutstanding, lotteryReserve, lotteryFunds, lotteryDrawCount;
        public int[] lotteryStock, lotteryPending, lotteryRequestedNumbers, lotteryDraw;
        public string lotteryFirstId, lotteryClaim, lotteryScanned, lotteryStatus, lotteryEntry, lotteryFeedback;
        public string[] lotteryControlIds;
        public Rect[] lotteryControls;
        public Rect lotteryNumberField;
        public bool mobileOwned, mobilePrepared;
        public int mobileProvider, mobileAmount, mobileCommission, mobileOutstanding, mobileDailySales, mobileDailyCount, mobileReserve, availableFunds;
        public string mobileNumber, mobileFeedback, mobileReceipt, mobileNumberEntry, mobileAmountEntry;
        public string[] mobileControlIds;
        public Rect[] mobileControls;
        public Rect mobileNumberField, mobileAmountField;
        public int ingredientCoffee,ingredientMilk,readyCoffee,readyMilk;
        public int tea, cocoa, readyTea, readyCocoa, pendingTea, pendingCocoa;
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
        var mobile = g.Mobile;
        state.mobileOwned = mobile.Owned; state.mobilePrepared = mobile.Book.IsPrepared(mobile.Request);
        state.mobileProvider = mobile.Request?.Provider ?? -1; state.mobileNumber = mobile.Request?.Number ?? ""; state.mobileAmount = mobile.Request?.Amount ?? 0;
        state.mobileCommission = mobile.Book.DailyCommission; state.mobileOutstanding = mobile.Book.Outstanding; state.mobileDailySales = mobile.Book.DailySales; state.mobileDailyCount = mobile.Book.DailyCount;
        state.mobileReserve = g.Ledger.MobileReserve; state.availableFunds = g.Ledger.AvailableFunds;
        state.mobileFeedback = mobile.Feedback; state.mobileReceipt = mobile.Book.LastReceipt?.Id ?? "";
        state.mobileNumberEntry = mobile.NumberEntry; state.mobileAmountEntry = mobile.AmountEntry;
        var mobileTargets = g.Devices.FocusKind == ShopDeviceKind.Mobile ? g.Devices.KeyboardTargets() : g.Devices.FocusKind == ShopDeviceKind.Monitor && g.Devices.ComputerApp == ShopComputerApp.Mobile ? g.Devices.ComputerKeyboardTargets() : new System.Collections.Generic.List<ShopKeyboardTarget>();
        state.mobileControlIds = mobileTargets.Select(t => t.Id).ToArray(); state.mobileControls = mobileTargets.Select(t => t.Bounds).ToArray();
        Rect mobileRect = g.Devices.FocusKind == ShopDeviceKind.Mobile ? ShopMobileTopUp.TerminalBody : ShopDevices.ComputerBody;
        Rect numberField = new Rect(mobileRect.x + 24, mobileRect.y + 208, 496, 48), amountField = new Rect(mobileRect.x + 566, mobileRect.y + 208, 474, 48);
        state.mobileNumberField = g.Devices.FocusKind == ShopDeviceKind.Mobile ? ShopUi.ScreenRect(numberField) : g.Devices.ComputerToScreen(numberField);
        state.mobileAmountField = g.Devices.FocusKind == ShopDeviceKind.Mobile ? ShopUi.ScreenRect(amountField) : g.Devices.ComputerToScreen(amountField);
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
        var reputation=g.Reputation.State;var reputationReport=reputation.Preview();
        state.reputation=reputation.Value;state.reputationSeconds=reputation.OpenSeconds;
        state.reputationLast=reputation.LastReport!=null;state.reputationChange=reputation.LastReport?.Change??0;
        state.reputationScores=reputationReport.Scores.ToArray();state.reputationEvidence=reputationReport.Evidence.ToArray();
        state.reputationVisits=reputationReport.Visits;state.reputationServiceVisits=reputationReport.ServiceVisits;
        state.reputationMissing=reputationReport.Missing;state.reputationRefusals=reputationReport.Refusals;
        state.reputationReasons=reputationReport.Reasons.ToArray();state.reputationRegulars=reputation.RegularVisits;
        state.arrivalScale=reputation.ArrivalScale;state.regularCustomer=g.ActiveCustomer!=null&&g.ActiveCustomer.IsRegular;
        state.reputationViewingLast=g.Reputation.SelectedTabId=="Reputation:Last";state.reputationViewDay=g.Reputation.VisibleReport.Day;
        state.selectedPrice=g.Pricing.Price(g.Pricing.SelectedType);
        state.priceCategory=(int)g.Pricing.Category;
        state.priceDraft=g.Pricing.Draft;state.priceError=g.Pricing.Error;state.priceEditing=g.Pricing.Editing;
        state.priceProducts=g.Pricing.VisibleTypes.Select(t=>ShopCatalog.Ids[t]).ToArray();
        if(g.Devices.ComputerShellActive) {
            var controls=g.Devices.KeyboardTargets();state.computerControlIds=controls.Select(t=>t.Id).ToArray();
            state.computerControls=controls.Select(t=>t.Bounds).ToArray();state.computerScreen=g.Devices.ComputerScreenRect;
        }
        state.partnerOwned=Enumerable.Range(0,3).Select(i=>g.Partners.State.Owned((ShopPartnerKind)i)).ToArray();
        state.supplyTypes=g.SupplyTypes;
        state.partnerStock=Enumerable.Range(0,ShopCatalog.SupplyCapacity).Select(g.StockCount).ToArray();
        state.partnerStored=Enumerable.Range(0,ShopCatalog.SupplyCapacity).Select(g.WarehouseCount).ToArray();
        state.partnerPending=Enumerable.Range(0,ShopCatalog.SupplyCapacity).Select(g.PendingQuantity).ToArray();
        state.partnerCosts=Enumerable.Range(0,ShopCatalog.SupplyCapacity).Select(g.ProcurementCost).ToArray();
        state.partnerCartQuantities=Enumerable.Range(0,ShopCatalog.SupplyCapacity).Select(g.Devices.CartQuantity).ToArray();
        state.partnerCartSubtotal=g.Devices.CartSubtotal;state.partnerCartPayment=g.Devices.CartPayment;
        state.lastPurchaseSubtotal=g.LastPurchaseSubtotal;state.lastPurchaseTotal=g.LastPurchaseTotal;
        state.purchaseRounding=g.Ledger.PurchaseRounding;state.costOfGoods=g.Ledger.CostOfGoods;
        var partnerArea=g.Layout.FurnitureArea(false);
        state.partnerDropTargets=new[]{new Vector3(5,0,partnerArea.yMax-.4f),new Vector3(7,0,partnerArea.yMax-.4f)}.Select(point=>View.WorldToScreenPoint(point)).ToArray();
        var physicalProducts=FindObjectsByType<ShopProduct>().Where(product=>product.UnitCost>=0).OrderBy(product=>product.ProductId).ToArray();
        state.physicalProductIds=physicalProducts.Select(product=>product.ProductId).ToArray();
        state.physicalProductCosts=physicalProducts.Select(product=>product.UnitCost).ToArray();
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
        state.shopLevel=g.Layout.ShopLevel;state.warehouseLevel=g.Layout.WarehouseLevel;
        state.moduleCount=g.Arrangement.Furniture.Count;
        state.plannedCount=g.Arrangement.PlannedCount;state.plannedCost=g.Arrangement.PlannedCost;
        state.editorPreviewPosition=g.Arrangement.PreviewPosition;
        state.editorCorners=g.Arrangement.Current().Keys.Count(f=>f.CatalogKind==5);
        state.editorPreviewTurns=g.Arrangement.PreviewQuarterTurns;
        state.editorPhysicalArea=g.Layout.FurnitureArea(false);
        var cornerArea=g.Layout.FurnitureArea(false);
        var cornerCentre = new Vector3(cornerArea.xMax - .6f, 0, cornerArea.yMin + .6f);
        state.editorCornerTestPoints = new[] {
            new Vector3(5.4f, 0, cornerArea.yMin + .25f), cornerCentre, new Vector3(5.4f, 0, -1.2f),
            cornerCentre + new Vector3(.35f, 0, 1.2f), cornerCentre + new Vector3(-1.2f, 0, -.35f)
        }.Select(p => View.WorldToScreenPoint(p)).ToArray();
        state.editorCornerPoints=g.Arrangement.Current().Keys.Where(f=>f.CatalogKind==5).Select(f=>View.WorldToScreenPoint(f.transform.position)).ToArray();
        state.editorSaleCredit=g.Arrangement.SaleCredit;state.editorModelVisible=g.Arrangement.ModelPreviewVisible;
        state.editorSnap=g.Arrangement.SnapFeedback;state.editorNotice=g.Arrangement.Feedback;
        var fridgeItems=g.Arrangement.Current().Keys.Where(f=>f.CatalogKind==4).ToArray();
        state.editorFridges=fridgeItems.Length;
        state.editorFridgeIds=fridgeItems.Select(f=>f.Id).ToArray();
        state.editorFridgePoints=fridgeItems.Select(f=>View.WorldToScreenPoint(f.transform.position)).ToArray();
        state.editorTestPoints=new[]{new Vector3(g.Layout.ShopArea.xMax-.65f,0,-1.2f),new Vector3(5,0,-2.8f),new Vector3(7,0,-2.8f)}.Select(p=>View.WorldToScreenPoint(p)).ToArray();
        var chosen=g.Arrangement.PurchasePreview??g.Arrangement.Furniture.FirstOrDefault(f=>f.Id==g.Arrangement.SelectedFurnitureId);
        if(chosen!=null)state.editorPreviewSize=new Vector3(chosen.Size.x,chosen.Height,chosen.Size.y);
        state.editorControlIds = g.Arrangement.Active ? g.Arrangement.EditorControls().Select(t => t.Id).ToArray() : new string[0];
        state.editorControls = g.Arrangement.Active ? g.Arrangement.EditorControls().Select(t => t.Bounds).ToArray() : new Rect[0];
        state.purchaseTargets = new[] { new Vector3(5f,0,-2.8f), new Vector3(6.6f,0,-2.8f), new Vector3(7.4f,0,-.3f), new Vector3(-6.4f,0,-1.6f) }.Select(p => View.WorldToScreenPoint(p)).ToArray();
        state.furnitureIds=g.Arrangement.Current().Keys.Select(f=>f.Id).ToArray();
        state.furniturePoints=g.Arrangement.Current().Keys.Select(f=>View.WorldToScreenPoint(f.transform.position)).ToArray();
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
        state.moveTarget = View.WorldToScreenPoint(new Vector3(2.75f,0,-1.75f));
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
        state.ingredientTarget=targetPackage!=null?targetPackage.Ingredient.ToString().ToLowerInvariant():"";
        state.ingredientBrand = targetPackage != null ? targetPackage.Brand : "";
        var heldIngredient = Player.HeldTool != null ? Player.HeldTool.GetComponent<ShopIngredientPackage>() : null;
        state.heldIngredientBrand = heldIngredient != null ? heldIngredient.Brand : "";
        var lottery=g.Lottery;
        var firstTicket=lottery.Book.Tickets.FirstOrDefault();
        state.lotteryOwned=lottery.Owned;state.lotteryFirstSold=firstTicket?.Sold??false;state.lotteryFirstRedeemed=firstTicket?.Redeemed??false;
        state.lotteryFirstId=firstTicket?.Id??"";state.lotteryClaim=g.ActiveCustomer?.LotteryClaimId??"";
        state.lotteryStock=Enumerable.Range(13,3).Select(lottery.Book.Stock).ToArray();state.lotteryPending=Enumerable.Range(13,3).Select(lottery.Book.Pending).ToArray();
        state.lotterySales=lottery.Book.Sales;state.lotteryPayouts=lottery.Book.Payouts;state.lotteryCommission=lottery.Book.Commission;
        state.lotteryOutstanding=lottery.Book.Outstanding;state.lotteryReserve=g.Ledger.LotteryReserve;state.lotteryFunds=g.Ledger.AvailableFunds;
        state.lotteryEntry=lottery.NumberEntry;state.lotteryFeedback=lottery.Feedback;state.lotteryScanned=lottery.ScannedId;
        state.lotteryStatus=lottery.Book.Scan(lottery.ScannedId,g.Day).ToString();
        state.lotteryRequestedNumbers=g.ActiveCustomer?.LotteryNumbers??Array.Empty<int>();
        state.lotteryDrawCount=lottery.Book.DrawCount;state.lotteryDraw=lottery.Book.DrawNumbers(g.Day);
        var lotteryTargets=g.Devices.FocusKind==ShopDeviceKind.Lottery?g.Devices.KeyboardTargets():
            g.Devices.FocusKind==ShopDeviceKind.Monitor&&g.Devices.ComputerApp==ShopComputerApp.Lottery?g.Devices.ComputerKeyboardTargets():new System.Collections.Generic.List<ShopKeyboardTarget>();
        state.lotteryControlIds=lotteryTargets.Select(t=>t.Id).ToArray();state.lotteryControls=lotteryTargets.Select(t=>t.Bounds).ToArray();
        state.lotteryNumberField=g.Devices.FocusKind==ShopDeviceKind.Lottery?ShopUi.ScreenRect(new Rect(128,318,500,54)):
            g.Devices.ComputerToScreen(new Rect(ShopDevices.ComputerBody.x+24,ShopDevices.ComputerBody.y+178,500,54));
        state.tea=g.Activities.LoadedIngredient(ShopDrinkIngredient.Tea); state.cocoa=g.Activities.LoadedIngredient(ShopDrinkIngredient.Cocoa);
        state.readyTea=g.Activities.ReadyIngredient(ShopDrinkIngredient.Tea); state.readyCocoa=g.Activities.ReadyIngredient(ShopDrinkIngredient.Cocoa);
        state.pendingTea=g.Activities.PendingIngredient(ShopDrinkIngredient.Tea); state.pendingCocoa=g.Activities.PendingIngredient(ShopDrinkIngredient.Cocoa);
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
        state.coffeeButtons = Enumerable.Range(0, g.Activities.CoffeeDisplay.Controls.Count).Select(index => g.Activities.CoffeeDisplay.Bounds(View, index)).ToArray();
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
        // WebGL's log transport truncates large individual messages. Keep complete
        // JSON across bounded chunks instead of silently losing a snapshot.
        string json=JsonUtility.ToJson(state);
        const int chunkSize=3000;
        int count=(json.Length+chunkSize-1)/chunkSize;
        for(int i=0;i<count;i++)
            Debug.Log("ACCEPTANCE_CHUNK:"+i+"/"+count+":"+json.Substring(i*chunkSize,Mathf.Min(chunkSize,json.Length-i*chunkSize)));
    }
}
#endif
