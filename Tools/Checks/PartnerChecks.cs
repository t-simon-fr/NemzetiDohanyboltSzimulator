using System;
using System.Linq;
using UnityEngine;

public static class PartnerChecks
{
    public static object Pricing()
    {
        int passed = 0;
        Action<bool,string> check = (ok, text) => { if (!ok) throw new Exception(text); passed++; };
        var state = new ShopPartnerState();
        check(ShopCatalog.Coffee == 9 && ShopCatalog.Contraband == 12 && ShopLotteryCatalog.FirstType == 13 && ShopCatalog.Index("energy") == 6 && ShopCatalog.Index("cola") == 5, "Existing product ids remain stable");
        check(ShopCatalog.Ids.Length == 23 && ShopCatalog.Prices.Length == 23 && ShopCatalog.Costs.Length == 23 && ShopCatalog.Ids.Distinct().Count() == 23, "All seven variants have unique catalog/price/cost entries");
        check(!Enumerable.Range(16,7).Any(state.Unlocked), "No variant unlocked before a contract");
        check(state.UnitCost(6) == 300 && state.UnitCost(5) == 180 && state.UnitCost(7) == 220, "Existing goods keep normal costs before contract");
        check(state.Activate(ShopPartnerKind.Hamster) && !state.Activate(ShopPartnerKind.Hamster), "Hamster contract activates once");
        check(state.Unlocked(16) && state.Unlocked(17) && !state.Unlocked(18) && !state.Unlocked(20), "Only the purchased family's variants unlock");
        check(state.UnitCost(6) == 270 && state.UnitCost(17) == 297 && state.UnitCost(5) == 180 && state.UnitCost(0) == 1500, "Ten percent applies only to matching future procurement");
        check(state.Activate(ShopPartnerKind.Muszakkezdo) && !state.Owned(ShopPartnerKind.LocaCola), "Spirit contract has no other partner prerequisite");
        check(state.UnitCost(7) == 198 && state.UnitCost(20) == 234 && state.UnitCost(21) == 243 && state.UnitCost(22) == 225, "Existing Kevert and all three spirits share the matching discount");
        check(ShopPartnerCatalog.DiscountedCost(10) == 9 && ShopPartnerCatalog.DiscountedCost(185) == 166, "Whole-forint unit price rounds down");
        var inventory = new ShopInventory(ShopCatalog.SupplyCapacity);
        var normal = new int[ShopCatalog.SupplyCapacity]; normal[6] = 1;
        var costs = Enumerable.Range(0,ShopCatalog.SupplyCapacity).Select(t=>ShopCatalog.Costs[t]).ToArray();
        var allowed = Enumerable.Range(0,ShopCatalog.SupplyCapacity).Select(ShopCatalog.IsPackaged).ToArray();
        var before = inventory.Quote(normal,costs,23,10000,false,12,allowed);
        costs[6] = 270;
        check(before.UnitCost(6) == 300 && inventory.TryPurchase(before,_=>true), "Shipment snapshots cost independently of later cost/contract edits");
        var after = inventory.Quote(normal,costs,23,10000,false,12,allowed);
        check(after.Total == 270 && inventory.TryPurchase(after,_=>true), "New shipment takes the discounted cost exactly once");
        inventory.Tick(8.1f);
        check(inventory.Stored(6) == 2 && inventory.NextUnitCost(6) == 300, "Simultaneous deliveries retain oldest procurement cost first");
        inventory.TryWithdraw(6);
        check(inventory.NextUnitCost(6) == 270, "Next real unit retains its own discounted cost");
        inventory.TryWithdraw(6); inventory.TryReturn(6,300);
        check(inventory.NextUnitCost(6) == 300, "Returned goods retain original cost");
        normal[9] = 1;
        check(!inventory.Quote(normal,costs,23,10000,false,12,allowed).Accepted, "Sparse catalog never makes prepared coffee a wholesale product");
        var one = new int[23];one[18] = 1;
        var rounded = inventory.Quote(one,costs,23,10000,false,12,allowed,5);
        check(rounded.Subtotal == 180 && rounded.Total == 180, "Normal-price procurement remains unchanged by cash rounding");
        costs[18] = 162;
        rounded = inventory.Quote(one,costs,23,10000,false,12,allowed,5);
        check(rounded.Subtotal == 162 && rounded.Total == 160 && rounded.Rounding == -2 && rounded.UnitCost(18) == 162, "Only final cash payment rounds; discounted unit cost stays exact");
        one[18] = 0;one[17] = 1;costs[17] = 297;
        check(inventory.Quote(one,costs,23,294,false,12,allowed,5).Error == ShopPurchaseError.Funds && inventory.Quote(one,costs,23,295,false,12,allowed,5).Total == 295, "Funds check uses the exact rounded payable total");
        state.Reset();
        check(!state.AnyOwned && !state.Unlocked(16) && state.UnitCost(6) == 300, "New game resets contracts, variants and discount");
        return new { passed, failed = 0 };
    }

    public static object Integration()
    {
        int passed = 0;
        Action<bool,string> check = (ok, text) => { if (!ok) throw new Exception(text); passed++; };
        var g = MvpShopController.Instance;
        var e = g.Arrangement;
        var p = g.Partners;
        try
        {
            g.GetComponent<ShopMenu>().Resume(); g.ResetDemo(); g.ClosePanel();
            g.ManualTick = true; ShopCheckFixtures.Fund(g,500000); g.Layout.SetLevels(3,3);
            check(!g.OrderSupply(16,1) && !g.SupplyTypes.Contains(16), "Locked variants cannot be ordered or shown in procurement");
            check(g.OrderSupply(6,1), "Order base Hamster before signing its contract");
            int cash = g.Ledger.Cash;
            g.Ledger.SetLotteryReserve(cash-19995);
            check(e.Begin() && !e.StartPurchase(6), "Partner cannot spend protected lottery funds");
            e.Cancel(); g.Ledger.SetLotteryReserve(0);
            var area = g.Layout.FurnitureArea(false);
            var hamsterPose = new ShopFurniturePose(new Vector3(5,0,area.yMax-.4f),0);
            var colaPose = new ShopFurniturePose(new Vector3(7,0,area.yMax-.4f),0);
            check(e.Begin() && e.StartPurchase(6) && e.PlacePurchase(hamsterPose), "Hamster package can be placed through shared furniture planning: "+e.Feedback);
            check(!e.StartPurchase(6), "One contract cannot be queued twice");
            e.Cancel();
            check(!p.State.AnyOwned && g.Ledger.Cash == cash, "Cancel grants no fridge, unlock or discount and charges nothing");
            check(e.Begin() && e.StartPurchase(6), "Restart Hamster package");
            var hamster = e.PurchasePreview;
            check(e.PlacePurchase(hamsterPose) && e.StartPurchase(7), "Independent Loca package shares one plan");
            var cola = e.PurchasePreview;
            check(e.PlacePurchase(colaPose) && e.PlannedCost == 40000 && g.Ledger.Cash == cash, "Both packages remain one exact unpaid plan");
            check(e.Save() && g.Ledger.Cash == cash-40000 && g.Drawer.Total == g.Ledger.Cash && p.State.Owned(ShopPartnerKind.Hamster) && p.State.Owned(ShopPartnerKind.LocaCola), "Save atomically activates both contracts and debits once");
            check(hamster.Size == cola.Size && Math.Abs(hamster.Height-cola.Height) < .001f && hamster.Size == new Vector2(1.65f,.8f), "Branded fridge dimensions agree with each other and standard fridge");
            check(hamster.Slots[0].CanAcceptStock("energy_zero") && !hamster.Slots[0].CanAcceptStock("cola") && cola.Slots[0].CanAcceptStock("fantom") && !cola.Slots[0].CanAcceptStock("energy"), "Physical branded slots accept only their own product family");
            cash = g.Ledger.Cash;
            check(p.BuyCounterDisplay() && !p.BuyCounterDisplay() && g.Ledger.Cash == cash-20000, "Counter display contract buys once independently");
            var spiritSlots = p.CounterDisplay.GetComponentsInChildren<ProductSlot>();
            var bat = UnityEngine.Object.FindObjectsByType<Renderer>().First(r=>r.name=="Turned wooden bat");
            check(p.CounterDisplay.GetComponentsInChildren<Collider>().Min(c=>c.bounds.min.y)>bat.bounds.max.y+.05f, "Counter display clears the existing bat rack without obstructing its tool");
            check(spiritSlots.Length == 6 && spiritSlots[0].CanAcceptStock("muszak_plum") && !spiritSlots[0].CanAcceptStock("water"), "Fixed counter display has six family-restricted usable slots");
            check(g.OrderSupply(6,1), "Order discounted base Hamster after signing the contract");
            foreach (int type in Enumerable.Range(16,7))
            {
                check(!g.CreateCustomerOrder(type,0).Contains(type), "Unlocked but never acquired variant is not requested: "+type);
                check(g.OrderSupply(type,5), "Actual variant shipment orders: "+type);
                check(!g.CreateCustomerOrder(type,0).Contains(type), "Pending delivery is not an available customer order: "+type);
            }
            g.Tick(8.1f);
            bool oldCreated = g.TryCreateSupply(6,out var oldEnergy);
            bool newCreated = g.TryCreateSupply(6,out var newEnergy);
            check(oldCreated && newCreated && oldEnergy.UnitCost == 300 && newEnergy.UnitCost == 270,
                "Actual base goods preserve old and new costs after contract activation");
            oldEnergy.RemoveFromWorld();newEnergy.RemoveFromWorld();
            var interactor = UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();
            foreach (int type in Enumerable.Range(16,7))
            {
                check(g.WarehouseCount(type) == 5 && g.Warehouse.Carton(type) != null && g.Warehouse.Carton(type).Quantity == 5, "Real labeled cartons arrive for variant "+type);
                check(g.CreateCustomerOrder(type,0).Contains(type), "Acquired variant can be requested without reserving it: "+type);
                var carton = g.Warehouse.Carton(type);
                check(interactor.TryHoldCarton(carton), "Pick up actual delivered carton: "+type);
                var slot = ShopPartnerCatalog.IsSpirit(type) ? spiritSlots.First(s=>s.Occupant==null) :
                    (ShopPartnerCatalog.PartnerFor(type)==0 ? hamster.Slots : cola.Slots).First(s=>s.Occupant==null);
                check(carton.TryPlaceOne(slot) && slot.Occupant.UnitCost == g.ProcurementCost(type), "Hand replenishment places a real product with its original cost: "+type);
                interactor.ReturnCarton();
                foreach (var label in slot.GetComponentsInParent<Transform>().Last().GetComponentsInChildren<ShopShelfPriceLabel>(true)) label.Refresh();
            }
            var topCarton = g.Warehouse.Carton(20);
            check(interactor.TryHoldCarton(topCarton) && topCarton.TryPlaceOne(spiritSlots.Last()), "Upper counter-display shelf also accepts real hand replenishment");
            interactor.ReturnCarton();
            cash = g.Ledger.Cash;
            check(g.OrderSupply(18,1) && g.LastPurchaseSubtotal == 162 && g.LastPurchaseTotal == 160 && g.LastPurchaseRounding == -2 && g.Ledger.Cash == cash-160 && g.Drawer.Total == g.Ledger.Cash,
                "Single discounted Fantom order preserves exact unit cost and a representable physical till");
            check(g.Pricing.AvailableTypes.Contains(16) && g.Pricing.AvailableTypes.Contains(22) && ShopPricing.CategoryOf(20) == ShopPriceCategory.Alcohol, "All unlocked products integrate with existing price categories");
            g.Devices.Focus(ShopDeviceKind.Monitor);g.Devices.OpenApp(ShopComputerApp.Furnishing);
            g.Devices.KeyboardTargets().Single(t=>t.Id=="Furnishing:Partners").Activate();
            var controls=g.Devices.KeyboardTargets();var screen=g.Devices.ComputerScreenRect;
            check(controls.Count(t=>t.Id.StartsWith("Partner:Buy:"))==3 && controls.Where(t=>t.Id.StartsWith("Partner:Buy:")).All(t=>!t.Enabled), "Partner UI shows all three contracts as owned and non-repeatable");
            check(controls.All(t=>screen.Contains(t.Bounds.min)&&screen.Contains(t.Bounds.max)), "All partner controls fit the monitor");
            g.ClosePanel();g.AutomaticArrivals=false;g.SingleProductVisits=true;g.OpenShop();
            typeof(MvpShopController).GetMethod("SpawnCustomer",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(g,new object[] { 22,false });
            for (int i=0;i<2000 && g.ActiveCustomer==null;i++) g.Tick(.1f);
            check(g.ActiveCustomer!=null && g.ActiveCustomer.OrderTypes.SequenceEqual(new[]{22}), "Actual NPC requests stocked partner spirit from the counter display");
            var spirit = spiritSlots.Select(slot=>slot.Occupant).First(item=>item!=null && item.ProductId==ShopCatalog.Ids[22]);
            var handover = g.Activities.CustomerHandoverSlots[0];
            check(spirit.AssignInitialSlot(handover), "Partner bottle fits the existing drink handover zone");
            handover.NotifyProductPlaced(spirit);
            check(g.Stage==ServiceStage.Payment, "Partner product uses shared checkout receipt membership");
            typeof(MvpCustomerActor).GetProperty("UsesCard").SetValue(g.ActiveCustomer,true);
            typeof(MvpCustomerActor).GetProperty("UsesCoins").SetValue(g.ActiveCustomer,false);
            int costBefore = g.Ledger.CostOfGoods; cash = g.Ledger.Cash;
            foreach (char digit in g.ActiveCustomer.TotalPrice.ToString()) g.EnterCardDigit(digit-'0');
            check(g.ConfirmCardPayment() && g.Ledger.Cash==cash+550 && g.Ledger.CostOfGoods==costBefore+225 && g.Drawer.Total==g.Ledger.Cash,
                "Real partner sale settles its captured discounted cost with exact cash/card reconciliation");
            check(!g.ConfirmCardPayment(), "Repeated approval does not sell the partner item twice");
            for(int i=0;i<100;i++)g.Tick(.1f);
            g.RequestClose();g.Tick(.2f);
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();var body=player.GetComponent<CharacterController>();var home=player.transform.position;
            body.enabled=false;player.transform.position=g.Layout.EntryInside;body.enabled=true;
            check(g.TryEndDay(),"End day after partner stocking");g.NextDay();
            body.enabled=false;player.transform.position=home;body.enabled=true;
            check(p.State.AnyOwned && p.CounterDisplay.activeSelf && hamster.gameObject.activeSelf && g.WarehouseCount(16)==4 && g.StockCount(16)==1, "Next day retains contracts, displays and exact new stock");
            g.ResetDemo();
            check(!p.State.AnyOwned && !g.SupplyTypes.Contains(16) && g.StockCount(16)==0 && g.WarehouseCount(16)==0, "New game clears partner state, new stock and owned fixtures");
            return new { passed, failed = 0 };
        }
        finally { e.Cancel();g.ClosePanel();g.ResetDemo();g.ManualTick=false; }
    }
    public static object Targeting()
    {
        int passed=0;
        Action<bool,string> check=(ok,text)=>{if(!ok)throw new Exception(text);passed++;};
        var g=MvpShopController.Instance;
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();
        var probe=UnityEngine.Object.FindAnyObjectByType<ShopAcceptanceProbe>();
        GameObject raisedFloor=null;
        bool own=probe==null;
        if(own)probe=new GameObject("Partner targeting probe").AddComponent<ShopAcceptanceProbe>();
        try
        {
            g.GetComponent<ShopMenu>().Resume();probe.Prepare("partners-visual");
            foreach(var product in UnityEngine.Object.FindObjectsByType<ShopProduct>().Where(p=>ShopPartnerCatalog.PartnerFor(ShopCatalog.Index(p.ProductId))>=0))product.RemoveFromWorld();
            var quantities=new int[ShopCatalog.SupplyCapacity];
            foreach(int type in new[]{6,16,17,18,19,20,21,22})quantities[type]=1;
            check(g.OrderBasket(quantities),"UI fixture orders one distinct unit of every partner variant");g.Tick(8.1f);
            foreach(int type in new[]{6,16,17,18,19,20,21,22})
            {
                probe.Action("partner-carton:"+type);
                var station=(ShopStation)typeof(PlayerInteractor).GetField("station",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(player);
                check(station!=null && station.IsSupplies,
                    "Standing camera ray reaches a physical supply station "+type+": "+(station==null?"none":station.name));
                station.Use(player);
                check(player.HeldCarton!=null && player.HeldCarton.ProductType==type,"Real targeted station picks up the correct partner carton "+type+"; got "+(player.HeldCarton==null?-1:player.HeldCarton.ProductType)+"; station "+station.name+"; carton "+g.Warehouse.Carton(type)?.transform.position+"; eye "+Camera.main.transform.position);
                if(type==6)
                {
                    var slot=g.Arrangement.Furniture.First(f=>f.CatalogKind==6).Slots.First(p=>p.Occupant==null);
                    var front=slot.HintCenter-slot.transform.forward*slot.GetComponent<BoxCollider>().size.z*.4f;
                    raisedFloor=GameObject.CreatePrimitive(PrimitiveType.Cube);raisedFloor.name="Partner targeting raised floor";
                    raisedFloor.transform.position=new Vector3(front.x,.12f,front.z-1.15f);
                    raisedFloor.transform.localScale=new Vector3(.8f,.2f,.8f);raisedFloor.GetComponent<Renderer>().enabled=false;
                    Physics.SyncTransforms();
                }
                var expectedSlot = type>=20 ? g.Partners.CounterDisplay.GetComponentsInChildren<ProductSlot>().First(s=>s.Occupant==null) : null;
                probe.Action("partner-slot:"+type);
                player.GetComponent<CharacterController>().Move(Vector3.down*.1f);
                Physics.SyncTransforms();
                player.RefreshTarget(Camera.main.ViewportPointToRay(new Vector3(.5f,.5f,0)));
                check(player.TargetSlot!=null && player.HeldCarton.CanPlace(player.TargetSlot) && (expectedSlot==null || expectedSlot==player.TargetSlot),"Standing camera has an unobstructed usable family stock target "+type+
                    "; eye "+Camera.main.transform.position+"; expected "+expectedSlot?.transform.position+"; target "+player.TargetSlot?.transform.position+"; hits "+string.Join(",",Physics.RaycastAll(Camera.main.ViewportPointToRay(new Vector3(.5f,.5f,0)),3).OrderBy(h=>h.distance).Select(h=>h.collider.name+"@"+h.point)));
                check(player.HeldCarton.TryPlaceOne(player.TargetSlot),"Targeted hand replenishment succeeds "+type);
                probe.Action("partner-return-carton");
                if(raisedFloor!=null){raisedFloor.SetActive(false);UnityEngine.Object.Destroy(raisedFloor);raisedFloor=null;}
            }
            probe.Action("partner-customer");
            check(g.Stage==ServiceStage.Payment,"New product customer traverses the real fridge route and reaches checkout");
            return new {passed,failed=0};
        }
        finally{if(raisedFloor!=null){raisedFloor.SetActive(false);UnityEngine.Object.Destroy(raisedFloor);}player.ReturnCarton();g.ClosePanel();g.ResetDemo();g.ManualTick=false;if(own)UnityEngine.Object.Destroy(probe.gameObject);}
    }

}
