using System;
using System.Linq;
using UnityEngine;

public static class TicketChecks
{
    public static object Catalog()
    {
        int passed = 0;
        Action<bool, string> check = (ok, text) => { if (!ok) throw new Exception(text); passed++; };
        check(ShopCatalog.TicketSingle == 25 && ShopCatalog.TicketBook == 26, "Ticket type indices are stable");
        check(ShopTicketCatalog.FirstType == 25 && ShopTicketCatalog.Types == 2, "Ticket catalog covers both types");
        check(ShopTicketCatalog.IsTicket(25) && ShopTicketCatalog.IsTicket(26) && !ShopTicketCatalog.IsTicket(24) && !ShopTicketCatalog.IsTicket(27) && ShopTicketCatalog.Index(26) == 1 && ShopTicketCatalog.Index(24) < 0, "Ticket type guard accepts only the two new types");
        check(ShopTicketCatalog.RidesPerBook == 10, "Collector ticket carries ten rides");
        check(ShopCatalog.Ids.Length == ShopCatalog.SupplyCapacity && ShopCatalog.TicketSingle == 25 && ShopCatalog.TicketBook == 26, "Catalog arrays grow with supply capacity, ticket indices stay stable");
        check(ShopCatalog.Prices.Length == ShopCatalog.Ids.Length && ShopCatalog.Costs.Length == ShopCatalog.Ids.Length && ShopCatalog.Names.Length == ShopCatalog.Ids.Length && ShopCatalog.ShortNames.Length == ShopCatalog.Ids.Length, "All catalog arrays stay aligned");
        check(ShopCatalog.Ids.Distinct().Count() == ShopCatalog.Ids.Length && ShopCatalog.Index("ticket_single") == 25 && ShopCatalog.Index("ticket_book") == 26, "Ticket ids are unique and resolvable");
        check(ShopPricing.CategoryOf(25) == ShopPriceCategory.Tickets && ShopPricing.CategoryOf(26) == ShopPriceCategory.Tickets && ShopPricing.CategoryNames.Contains("Jegyek"), "Tickets have their own listed price category");
        check(ShopCatalog.Prices[25] == 600 && ShopCatalog.Prices[26] == 5000 && ShopCatalog.Costs[25] == 300 && ShopCatalog.Costs[26] == 2800, "Ticket prices and procurement costs match the issue");
        check(ShopPricing.Minimum(25) == 300 && ShopPricing.Maximum(26) == 15000, "Ticket price bounds derive from list prices");
        check(ShopPricing.Accepts(25, 600, 0, 1) && !ShopPricing.Accepts(25, 1200, 0, 1), "Overpriced single tickets are rejected by NPC budgets");
        check(ProductVisuals.Size(25) == new Vector3(.15f, .21f, .008f) && ProductVisuals.Size(26) == new Vector3(.15f, .21f, .008f), "Tickets are flat like lottery slips");
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
            g.ManualTick = true; ShopCheckFixtures.Fund(g, 500000); g.Layout.SetLevels(3, 3);
            check(g.SupplyTypes.Contains(25) && g.SupplyTypes.Contains(26) && g.IsSupplyAvailable(25) && g.IsSupplyAvailable(26), "Tickets are orderable from the supplier monitor");
            check(!g.CreateCustomerOrder(25, 0).Contains(25) && !g.CreateCustomerOrder(26, 0).Contains(26), "Unstocked tickets are never requested");
            check(g.OrderSupply(25, 3) && g.OrderSupply(26, 3), "Ticket shipments can be ordered");
            g.Tick(8.1f);
            check(g.WarehouseCount(25) == 3 && g.WarehouseCount(26) == 3, "Ticket shipments arrive with exact quantities");
            check(g.CreateCustomerOrder(25, 0).Contains(25) && g.CreateCustomerOrder(26, 0).Contains(26), "Stocked tickets can be requested without reserving them");
            check(!g.CounterStorage.Slots.Any(s => s.CanAcceptStock("ticket_single") || s.CanAcceptStock("ticket_book")), "Dnyipro slots reject tickets");
            check(!g.CounterStorage.TicketSlots.Any(s => s.CanAcceptStock("dnyipro")), "Ticket slots reject Dnyipro");
            check(g.TryCreateSupply(25, out var singleA), "Take first single ticket from the warehouse box");
            check(g.TryCreateSupply(25, out var singleB), "Take second single ticket from the warehouse box");
            check(g.TryCreateSupply(26, out var bookA), "Take first collector book from the warehouse box");
            check(g.TryCreateSupply(26, out var bookB), "Take second collector book from the warehouse box");
            var singleSlots = g.CounterStorage.TicketSlots.Where(s => s.CanAcceptStock("ticket_single")).ToArray();
            var bookSlots = g.CounterStorage.TicketSlots.Where(s => s.CanAcceptStock("ticket_book")).ToArray();
            check(singleSlots.Length == 5 && bookSlots.Length == 5, "Each ticket type has five exclusive counter slots");
            var scanner = UnityEngine.Object.FindAnyObjectByType<ShopBarcodeScanner>();
            check(singleA.AssignInitialSlot(g.DeliverySlot) && scanner.TryScan(singleA), "Single ticket passes the existing barcode scanner");
            singleA.RemoveFromWorld();
            check(singleB.AssignInitialSlot(singleSlots[0]) && bookA.AssignInitialSlot(bookSlots[0]) && bookB.AssignInitialSlot(bookSlots[1]), "Hand replenishment fills the behind-counter ticket slots");
            check(g.StockCount(25) == 1 && g.StockCount(26) == 2 && g.WarehouseCount(25) == 1 && g.WarehouseCount(26) == 1, "Counter stock counts track real placed tickets");
            g.Devices.Focus(ShopDeviceKind.Monitor); g.Devices.OpenApp(ShopComputerApp.Prices);
            var priceControls = g.Devices.KeyboardTargets();
            var priceScreen = g.Devices.ComputerScreenRect;
            check(priceControls.Any(t => t.Id == "Price:Category:4"), "Tickets category button exists and is clickable");
            check(priceControls.Where(t => t.Id.StartsWith("Price:Category:")).All(t => priceScreen.Contains(t.Bounds.min) && priceScreen.Contains(t.Bounds.max)), "Price category buttons fit the monitor");
            priceControls.Single(t => t.Id == "Price:Category:4").Activate();
            check(g.Pricing.Category == ShopPriceCategory.Tickets && g.Pricing.VisibleTypes.SequenceEqual(new[] { 25, 26 }), "Tickets category lists both ticket types");
            g.ClosePanel();
            g.AutomaticArrivals = false; g.SingleProductVisits = true; g.OpenShop();
            typeof(MvpShopController).GetMethod("SpawnCustomer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(g, new object[] { 25, false });
            for (int i = 0; i < 2000 && g.ActiveCustomer == null; i++) g.Tick(.1f);
            for (int i = 0; i < 2000 && g.Stage != ServiceStage.Order; i++) g.Tick(.1f);
            check(g.ActiveCustomer != null && g.ActiveCustomer.OrderTypes.SequenceEqual(new[] { 25 }), "Actual NPC requests the stocked single ticket");
            typeof(MvpCustomerActor).GetProperty("UsesCard").SetValue(g.ActiveCustomer, true);
            typeof(MvpCustomerActor).GetProperty("UsesCoins").SetValue(g.ActiveCustomer, false);
            var handover = g.Activities.CustomerHandoverSlots[0];
            var payable = singleSlots.Select(s => s.Occupant).First(p => p != null && p.ProductId == ShopCatalog.Ids[25]);
            check(payable.AssignInitialSlot(handover), "Ticket fits the existing handover zone");
            handover.NotifyProductPlaced(payable);
            check(g.Stage == ServiceStage.Payment, "Ticket uses shared checkout receipt membership");
            int costBefore = g.Ledger.CostOfGoods, cash = g.Ledger.Cash;
            foreach (char digit in g.ActiveCustomer.TotalPrice.ToString()) g.EnterCardDigit(digit - '0');
            check(g.ConfirmCardPayment() && g.Ledger.Cash == cash + 600 && g.Ledger.CostOfGoods == costBefore + 300 && g.Drawer.Total == g.Ledger.Cash, "Real ticket sale settles its captured procurement cost with exact card reconciliation");
            check(!g.ConfirmCardPayment(), "Repeated approval does not sell the ticket twice");
            for (int i = 0; i < 100; i++) g.Tick(.1f);
            typeof(MvpShopController).GetMethod("SpawnCustomer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(g, new object[] { 26, false });
            for (int i = 0; i < 2000 && g.ActiveCustomer == null; i++) g.Tick(.1f);
            for (int i = 0; i < 2000 && g.Stage != ServiceStage.Order; i++) g.Tick(.1f);
            check(g.ActiveCustomer != null && g.ActiveCustomer.OrderTypes.SequenceEqual(new[] { 26 }), "Actual NPC requests the stocked collector book");
            typeof(MvpCustomerActor).GetProperty("UsesCard").SetValue(g.ActiveCustomer, false);
            typeof(MvpCustomerActor).GetProperty("UsesCoins").SetValue(g.ActiveCustomer, false);
            var payableBook = bookSlots.Select(s => s.Occupant).First(p => p != null && p.ProductId == ShopCatalog.Ids[26]);
            check(payableBook.AssignInitialSlot(handover), "Collector book fits the existing handover zone");
            handover.NotifyProductPlaced(payableBook);
            check(g.Stage == ServiceStage.Payment, "Collector book reaches payment");
            costBefore = g.Ledger.CostOfGoods; cash = g.Ledger.Cash;
            check(g.SubmitChange(g.ChangeSelected) && g.Ledger.Cash == cash + 5000 && g.Ledger.CostOfGoods == costBefore + 2800 && g.Drawer.Total == g.Ledger.Cash, "Real book sale settles its captured procurement cost with exact cash reconciliation");
            check(!g.SubmitChange(0), "No duplicate cash settlement");
            for (int i = 0; i < 100; i++) g.Tick(.1f);
            typeof(MvpShopController).GetMethod("SpawnCustomer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(g, new object[] { 25, false });
            for (int i = 0; i < 2000 && g.ActiveCustomer == null; i++) g.Tick(.1f);
            for (int i = 0; i < 2000 && g.Stage != ServiceStage.Order; i++) g.Tick(.1f);
            var wrong = bookSlots.Select(s => s.Occupant).First(p => p != null && p.ProductId == ShopCatalog.Ids[26]);
            check(wrong.AssignInitialSlot(handover), "Wrong ticket type can be handed over physically");
            handover.NotifyProductPlaced(wrong);
            check(g.Stage == ServiceStage.Order, "Wrong ticket type does not complete the order");
            check(g.StockCount(25) == 0 && g.StockCount(26) == 1 && g.WarehouseCount(25) == 1 && g.WarehouseCount(26) == 1, "Rejected handover keeps exact stock with no duplication");
            g.SkipCurrent();
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();
            var body = player.GetComponent<CharacterController>(); var home = player.transform.position;
            body.enabled = false; player.transform.position = g.Layout.EntryInside; body.enabled = true;
            g.RequestClose();
            for (int i = 0; i < 300 && !g.CanEndDay; i++) g.Tick(.1f);
            check(g.CanEndDay, "Skipped visit drains before closing");
            check(g.TryEndDay(), "End day after ticket sales");
            g.NextDay();
            body.enabled = false; player.transform.position = home; body.enabled = true;
            check(g.StockCount(25) + g.StockCount(26) + g.WarehouseCount(25) + g.WarehouseCount(26) > 0, "Next day retains remaining ticket stock");
            g.ResetDemo();
            check(g.StockCount(25) == 0 && g.StockCount(26) == 0 && g.WarehouseCount(25) == 0 && g.WarehouseCount(26) == 0, "New game clears ticket stock");
            return new { passed, failed = 0 };
        }
        finally { g.ClosePanel(); g.ResetDemo(); g.ManualTick = false; }
    }
}
