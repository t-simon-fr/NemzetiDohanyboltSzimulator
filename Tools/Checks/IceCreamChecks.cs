using System;
using System.Linq;
using UnityEngine;

public static class IceCreamChecks
{
    public static object Catalog()
    {
        int passed = 0;
        Action<bool, string> check = (ok, text) => { if (!ok) throw new Exception(text); passed++; };
        check(ShopCatalog.IceChoc == 27 && ShopCatalog.IceStraw == 28 && ShopCatalog.IceVan == 29, "Ice cream type indices are stable");
        check(ShopIceCreamCatalog.FirstType == 27 && ShopIceCreamCatalog.Types == 3, "Ice cream catalog covers all three types");
        check(ShopIceCreamCatalog.IsIceCream(27) && ShopIceCreamCatalog.IsIceCream(29) && !ShopIceCreamCatalog.IsIceCream(26) && !ShopIceCreamCatalog.IsIceCream(30) && ShopIceCreamCatalog.Index(29) == 2 && ShopIceCreamCatalog.Index(26) < 0, "Ice cream guard accepts only the three new types");
        check(ShopCatalog.Ids.Length == ShopCatalog.SupplyCapacity && ShopCatalog.IceChoc == 27 && ShopCatalog.IceStraw == 28 && ShopCatalog.IceVan == 29, "Catalog arrays grow with supply capacity, ice cream indices stay stable");
        check(ShopCatalog.Prices.Length == ShopCatalog.Ids.Length && ShopCatalog.Costs.Length == ShopCatalog.Ids.Length && ShopCatalog.Names.Length == ShopCatalog.Ids.Length && ShopCatalog.ShortNames.Length == ShopCatalog.Ids.Length, "All catalog arrays stay aligned");
        check(ShopCatalog.Ids.Distinct().Count() == ShopCatalog.Ids.Length && ShopCatalog.Index("ice_choc") == 27 && ShopCatalog.Index("ice_straw") == 28 && ShopCatalog.Index("ice_van") == 29, "Ice cream ids are unique and resolvable");
        check(ShopPricing.CategoryOf(27) == ShopPriceCategory.IceCream && ShopPricing.CategoryOf(29) == ShopPriceCategory.IceCream && ShopPricing.CategoryNames.Contains("Jégkrém"), "Ice cream has its own listed price category");
        check(ShopCatalog.Prices[27] == 450 && ShopCatalog.Prices[28] == 500 && ShopCatalog.Prices[29] == 550 && ShopCatalog.Costs[27] == 200 && ShopCatalog.Costs[28] == 220 && ShopCatalog.Costs[29] == 240, "Ice cream prices and procurement costs match the issue");
        check(ShopPricing.Minimum(27) == 225 && ShopPricing.Maximum(29) == 1650, "Ice cream price bounds derive from list prices");
        check(ShopPricing.Accepts(27, 450, 0, 1) && !ShopPricing.Accepts(27, 900, 0, 1), "Overpriced ice cream is rejected by NPC budgets");
        check(ProductVisuals.Size(27) == new Vector3(.16f, .14f, .09f) && ProductVisuals.Size(29) == new Vector3(.16f, .14f, .09f), "Ice cream packs are squat frozen boxes");
        check(ShopModuleKind.Freezer == (ShopModuleKind)6 && ShopModularFurniture.Prices[6] == 12000 && ShopModularFurniture.Names[6] == "Fagyasztóláda", "Freezer furniture kind, price and name are registered");
        check(ShopPartnerCatalog.FirstFurnitureKind == 7, "Partner kinds shifted above the freezer kind");
        return new { passed, failed = 0 };
    }

    public static object Integration()
    {
        int passed = 0;
        Action<bool, string> check = (ok, text) => { if (!ok) throw new Exception(text); passed++; };
        var g = MvpShopController.Instance;
        try
        {
            g.GetComponent<ShopMenu>().Resume(); g.ResetDemo(); g.ClosePanel();
            g.ManualTick = true; ShopCheckFixtures.Fund(g, 500000); g.AutomaticArrivals = false;
            check(g.SupplyTypes.Contains(27) && g.SupplyTypes.Contains(29) && g.IsSupplyAvailable(27) && g.IsSupplyAvailable(29), "Ice cream is orderable without expansion");
            check(!g.CreateCustomerOrder(27, 0).Contains(27) && !g.CreateCustomerOrder(29, 0).Contains(29), "Unstocked ice cream is never requested");
            var e = g.Arrangement;
            int cash = g.Ledger.Cash;
            check(e.Begin() && e.StartPurchase(6), "Freezer purchase can start in the starter shop");
            e.Cancel();
            check(g.Ledger.Cash == cash && !g.FreezerOwned, "Cancel grants no freezer and charges nothing");
            check(e.Begin(), "Restart freezer planning");
            var area = g.Layout.FurnitureArea(false);
            int tried = 0; bool placed = false; string last = "";
            for (float z = area.yMin + .65f; !placed && z < area.yMax - .4f && tried < 400; z += .5f)
                for (float x = area.xMin + .65f; !placed && x < area.xMax - .4f && tried < 400; x += .5f)
                    for (int turn = 0; turn < 4 && !placed && tried < 400; turn++)
                    {
                        tried++;
                        if (!e.StartPurchase(6)) { last = "start:" + e.Feedback; continue; }
                        if (e.PlacePurchase(new ShopFurniturePose(new Vector3(x, 0, z), turn))) { placed = true; last = "placed at " + x.ToString("0.0") + "," + z.ToString("0.0") + " turn " + turn; }
                        else last = "place:" + e.Feedback;
                    }
            check(placed, "Starter-valid freezer placement found (" + tried + " tried, last: " + last + ")");
            cash = g.Ledger.Cash;
            check(e.Save() && g.FreezerOwned, "Arrangement save activates the freezer");
            check(g.Ledger.Cash == cash - 12000 && g.Drawer.Total == g.Ledger.Cash, "Freezer debits exactly once");
            var freezer = g.Arrangement.Current().Keys.First(f => f.CatalogKind == 6);
            var freezerSlots = freezer.GetComponentsInChildren<ProductSlot>();
            check(freezerSlots.Length == 12, "Freezer has twelve exclusive slots");
            check(freezerSlots.Count(s => s.CanAcceptStock("ice_choc")) == 4 && freezerSlots.Count(s => s.CanAcceptStock("ice_straw")) == 4 && freezerSlots.Count(s => s.CanAcceptStock("ice_van")) == 4, "Each flavor owns four slots");
            check(!freezerSlots.Any(s => s.CanAcceptStock("dnyipro") || s.CanAcceptStock("water") || s.CanAcceptStock("ticket_single")), "Freezer slots reject everything else");
            check(g.OrderSupply(27, 2) && g.OrderSupply(28, 1) && g.OrderSupply(29, 1), "Ice cream shipments can be ordered");
            g.Tick(8.1f);
            check(g.WarehouseCount(27) == 2 && g.WarehouseCount(28) == 1 && g.WarehouseCount(29) == 1, "Ice cream shipments arrive with exact quantities");
            check(g.CreateCustomerOrder(27, 0).Contains(27), "Stocked ice cream can be requested without reserving it");
            check(g.TryCreateSupply(27, out var singleA), "Take first choc ice from the warehouse box");
            check(g.TryCreateSupply(27, out var singleB), "Take second choc ice from the warehouse box");
            check(g.TryCreateSupply(28, out var straw), "Take strawberry ice from the warehouse box");
            check(g.TryCreateSupply(29, out var van), "Take vanilla ice from the warehouse box");
            var chocSlots = freezerSlots.Where(s => s.CanAcceptStock("ice_choc")).ToArray();
            var strawSlots = freezerSlots.Where(s => s.CanAcceptStock("ice_straw")).ToArray();
            var vanSlots = freezerSlots.Where(s => s.CanAcceptStock("ice_van")).ToArray();
            check(singleA.AssignInitialSlot(chocSlots[0]) && singleB.AssignInitialSlot(chocSlots[1]) && straw.AssignInitialSlot(strawSlots[0]) && van.AssignInitialSlot(vanSlots[0]), "Hand replenishment fills the freezer slots");
            check(g.StockCount(27) == 2 && g.StockCount(28) == 1 && g.StockCount(29) == 1 && g.WarehouseCount(27) == 0 && g.WarehouseCount(28) == 0 && g.WarehouseCount(29) == 0, "Freezer stock counts track real placed packs");
            g.OpenShop();
            typeof(MvpShopController).GetProperty("Spawned").SetValue(g, 3);
            typeof(MvpShopController).GetMethod("SpawnCustomer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(g, new object[] { -1, false });
            for (int i = 0; i < 2000 && g.ActiveCustomer == null; i++) g.Tick(.1f);
            for (int i = 0; i < 2000 && g.Stage != ServiceStage.Order; i++) g.Tick(.1f);
            check(g.ActiveCustomer != null && (g.ActiveCustomer.OrderTypes.Contains(27) || g.ActiveCustomer.OrderTypes.Contains(28) || g.ActiveCustomer.OrderTypes.Contains(29)), "Visit slot requests stocked ice cream");
            g.SkipCurrent();
            for (int i = 0; i < 50 && g.ActiveCustomer != null; i++) g.Tick(.1f);
            g.SingleProductVisits = true; g.OpenShop();
            typeof(MvpShopController).GetMethod("SpawnCustomer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(g, new object[] { 27, false });
            for (int i = 0; i < 2000 && g.ActiveCustomer == null; i++) g.Tick(.1f);
            for (int i = 0; i < 2000 && g.Stage != ServiceStage.Order; i++) g.Tick(.1f);
            check(g.ActiveCustomer != null && g.ActiveCustomer.OrderTypes.SequenceEqual(new[] { 27 }), "Actual NPC requests the stocked choc ice");
            typeof(MvpCustomerActor).GetProperty("UsesCard").SetValue(g.ActiveCustomer, true);
            typeof(MvpCustomerActor).GetProperty("UsesCoins").SetValue(g.ActiveCustomer, false);
            var handover = g.Activities.CustomerHandoverSlots[0];
            var payable = chocSlots.Select(s => s.Occupant).First(p => p != null && p.ProductId == ShopCatalog.Ids[27]);
            check(payable.AssignInitialSlot(handover), "Ice cream fits the existing handover zone");
            handover.NotifyProductPlaced(payable);
            check(g.Stage == ServiceStage.Payment, "Ice cream uses shared checkout receipt membership");
            int costBefore = g.Ledger.CostOfGoods; cash = g.Ledger.Cash;
            foreach (char digit in g.ActiveCustomer.TotalPrice.ToString()) g.EnterCardDigit(digit - '0');
            check(g.ConfirmCardPayment() && g.Ledger.Cash == cash + 450 && g.Ledger.CostOfGoods == costBefore + 200 && g.Drawer.Total == g.Ledger.Cash, "Real ice sale settles its captured procurement cost with exact card reconciliation");
            check(!g.ConfirmCardPayment(), "Repeated approval does not sell the ice cream twice");
            for (int i = 0; i < 100; i++) g.Tick(.1f);
            typeof(MvpShopController).GetMethod("SpawnCustomer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(g, new object[] { 28, false });
            for (int i = 0; i < 2000 && g.ActiveCustomer == null; i++) g.Tick(.1f);
            for (int i = 0; i < 2000 && g.Stage != ServiceStage.Order; i++) g.Tick(.1f);
            check(g.ActiveCustomer != null && g.ActiveCustomer.OrderTypes.SequenceEqual(new[] { 28 }), "Actual NPC requests the stocked strawberry ice");
            typeof(MvpCustomerActor).GetProperty("UsesCard").SetValue(g.ActiveCustomer, false);
            typeof(MvpCustomerActor).GetProperty("UsesCoins").SetValue(g.ActiveCustomer, false);
            var payableStraw = strawSlots.Select(s => s.Occupant).First(p => p != null && p.ProductId == ShopCatalog.Ids[28]);
            check(payableStraw.AssignInitialSlot(handover), "Strawberry pack fits the existing handover zone");
            handover.NotifyProductPlaced(payableStraw);
            check(g.Stage == ServiceStage.Payment, "Strawberry ice reaches payment");
            costBefore = g.Ledger.CostOfGoods; cash = g.Ledger.Cash;
            check(g.SubmitChange(g.ChangeSelected) && g.Ledger.Cash == cash + 500 && g.Ledger.CostOfGoods == costBefore + 220 && g.Drawer.Total == g.Ledger.Cash, "Real strawberry sale settles its captured procurement cost with exact cash reconciliation");
            check(!g.SubmitChange(0), "No duplicate cash settlement");
            for (int i = 0; i < 100; i++) g.Tick(.1f);
            typeof(MvpShopController).GetMethod("SpawnCustomer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(g, new object[] { 27, false });
            for (int i = 0; i < 2000 && g.ActiveCustomer == null; i++) g.Tick(.1f);
            for (int i = 0; i < 2000 && g.Stage != ServiceStage.Order; i++) g.Tick(.1f);
            var wrong = vanSlots.Select(s => s.Occupant).First(p => p != null && p.ProductId == ShopCatalog.Ids[29]);
            check(wrong.AssignInitialSlot(handover), "Wrong ice cream flavor can be handed over physically");
            handover.NotifyProductPlaced(wrong);
            check(g.Stage == ServiceStage.Order, "Wrong flavor does not complete the order");
            check(g.StockCount(27) == 1 && g.StockCount(28) == 0 && g.StockCount(29) == 1 && g.WarehouseCount(27) == 0 && g.WarehouseCount(28) == 0 && g.WarehouseCount(29) == 0, "Rejected handover keeps exact stock with no duplication");
            g.SkipCurrent();
            for (int i = 0; i < 300 && !g.CanEndDay; i++) g.Tick(.1f);
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();
            var body = player.GetComponent<CharacterController>(); var home = player.transform.position;
            body.enabled = false; player.transform.position = g.Layout.EntryInside; body.enabled = true;
            g.RequestClose();
            for (int i = 0; i < 300 && !g.CanEndDay; i++) g.Tick(.1f);
            check(g.CanEndDay, "Skipped visit drains before closing");
            check(g.TryEndDay(), "End day after ice cream sales");
            g.NextDay();
            body.enabled = false; player.transform.position = home; body.enabled = true;
            check(g.StockCount(27) + g.StockCount(28) + g.StockCount(29) > 0, "Next day retains remaining ice cream stock");
            g.ResetDemo();
            check(g.StockCount(27) == 0 && g.StockCount(28) == 0 && g.StockCount(29) == 0 && g.WarehouseCount(27) == 0 && g.WarehouseCount(28) == 0 && g.WarehouseCount(29) == 0, "New game clears ice cream stock");
            return new { passed, failed = 0 };
        }
        finally { g.ClosePanel(); g.ResetDemo(); g.ManualTick = false; }
    }
}
