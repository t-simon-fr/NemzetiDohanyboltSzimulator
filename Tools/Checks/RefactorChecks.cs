using System;
using System.Linq;

public static class RefactorChecks
{
    public static object DayAndFocus()
    {
        int passed = 0;
        void Check(bool valid, string name) { if (!valid) throw new Exception(name); passed++; }
        var day = new ShopDayCycle(); day.Prepare(); day.TickClock(100);
        Check(day.ClockElapsed == 0 && !day.CanEnd(true), "Preparation clock waits for the opening sign");
        Check(day.Open() && !day.Open(), "Opening transition is accepted once");
        day.TickClock(65); day.TickService(30);
        Check(day.ClockElapsed == 65 && day.Elapsed == 30, "Clock and actively serviced time have separate responsibilities");
        Check(day.Close() && !day.Close() && !day.Stay(false), "Closing rejects repeat close and occupied evening transition");
        Check(day.CanDevelop(true, true) && !day.CanDevelop(true, false), "Closing upgrades require an empty shop");
        Check(day.Stay(true) && day.CanEnd(true), "Empty closed shop permits evening and explicit exit");
        day.TickClock(10); day.TickService(10);
        Check(day.ClockElapsed == 75 && day.Elapsed == 30, "Evening clock advances without serving new arrivals");
        day.Finish(); day.TickClock(100);
        Check(day.ClockElapsed == 75 && !day.CanDevelop(true, true), "Summary freezes clock and construction");
        day.Prepare();
        Check(day.ClockElapsed == 0 && day.Elapsed == 0, "Next preparation clears both clocks");
        var g = MvpShopController.Instance;
        var menu = g.GetComponent<ShopMenu>();
        var view = UnityEngine.Camera.main;
        var position = view.transform.localPosition; var rotation = view.transform.localRotation; float fov = view.fieldOfView;
        float scale = UnityEngine.Time.timeScale;
        try
        {
            g.Devices.Focus(ShopDeviceKind.Monitor);
            menu.Open();
            Check(ShopMenu.Active && UnityEngine.Time.timeScale == 0 && ShopInputGate.Blocked, "Menu owns pause while nested over a device");
            menu.Resume();
            Check(g.Devices.FocusKind == ShopDeviceKind.Monitor && ShopInputGate.Blocked && UnityEngine.Time.timeScale == scale, "Closing nested menu restores the device focus and prior time scale");
            g.ClosePanel();
            Check(!ShopInputGate.Blocked && g.Devices.FocusKind == ShopDeviceKind.None, "Device exit restores world input");
            Check(UnityEngine.Vector3.Distance(position, view.transform.localPosition) < .001f && UnityEngine.Quaternion.Angle(rotation, view.transform.localRotation) < .01f && Math.Abs(fov - view.fieldOfView) < .001f, "Focus exit restores camera pose and field of view");
            Check(ShopInputGate.WorldActionConsumed, "Closing UI consumes the transition frame to prevent a world action leak");
        }
        finally { menu.Resume(); g.ClosePanel(); UnityEngine.Time.timeScale = scale; }
        return new { passed, failed = 0 };
    }

    public static object Coffee()
    {
        int passed = 0;
        void Check(bool valid, string name) { if (!valid) throw new Exception(name); passed++; }
        var machine = new ShopCoffeeMachineState();
        Check(machine.TryBrew(false, false) == ShopBrewResult.Unavailable, "Unowned machine cannot brew");
        machine.Install(); machine.Refill(3, 1); machine.SelectRecipe(ShopCatalog.Latte);
        Check(machine.TryBrew(false, false) == ShopBrewResult.Started && machine.Coffee == 2 && machine.Milk == 0, "Latte consumes one shared coffee and milk serving");
        Check(machine.TryBrew(false, false) != ShopBrewResult.Started && machine.Coffee == 2, "Repeated start never consumes a second serving");
        Check(!machine.SelectRecipe(ShopCatalog.LongCoffee), "Recipe cannot change during an active brew");
        Check(machine.TickBrew(-1) == -1 && machine.BrewRemaining == 3.5f, "Negative time cannot advance brewing");
        Check(machine.TickBrew(3) == -1 && machine.TickBrew(.6f) == ShopCatalog.Latte, "Brew finishes with its original recipe at the required duration");
        Check(machine.TickBrew(10) == -1, "Ready event is emitted exactly once");
        machine.RefundBrew(ShopCatalog.Latte);
        Check(machine.Coffee == 3 && machine.Milk == 1, "Blocked output restores exactly the spent ingredients");
        machine.SelectRecipe(ShopCatalog.LongCoffee);
        Check(machine.TryBrew(false, true) == ShopBrewResult.Unavailable && machine.Coffee == 3, "Occupied tray cannot consume ingredients");
        Check(machine.TryBrew(true, false) == ShopBrewResult.Unavailable, "Closed-day machine cannot start");
        Check(machine.TryBrew(false, false) == ShopBrewResult.Started && machine.Milk == 1, "Long coffee does not consume milk");
        Check(!machine.TryOrder(false, 50, false, () => throw new Exception("Capacity must precede debit")), "Physical ready packages count toward ingredient capacity");
        Check(!machine.TryOrder(false, 0, false, () => false) && machine.PendingCoffee == 0, "Failed payment creates no ingredient delivery");
        Check(machine.TryOrder(false, 0, false, () => true) && machine.TryOrder(true, 0, false, () => true), "Coffee and milk have independent timed shipments");
        var arrival = machine.TickDeliveries(8);
        Check(arrival.coffee == 10 && arrival.milk == 10 && machine.Coffee == 2, "Delivery produces physical-package quantities without loading the machine");
        Check(machine.TickDeliveries(8) == (0, 0), "Ingredient shipment does not arrive twice");
        machine.Reset();
        Check(!machine.Owned && machine.Coffee == 0 && machine.Milk == 0 && machine.PendingCoffee == 0 && machine.PendingMilk == 0 && machine.BrewRemaining == 0, "New game clears machine, brew and all pending ingredients");
        return new { passed, failed = 0 };
    }

    public static object Checkout()
    {
        int passed = 0;
        void Check(bool valid, string name) { if (!valid) throw new Exception(name); passed++; }
        var session = new ShopCheckoutSession { Stage = ServiceStage.Payment };
        int postings = 0;
        Check(!session.TrySettle(() => false) && session.Stage == ServiceStage.Payment, "Failed settlement preserves payable session");
        Check(session.TrySettle(() => { postings++; return true; }), "Successful posting completes session");
        Check(!session.TrySettle(() => { postings++; return true; }) && postings == 1, "A second settlement cannot post again");
        var root = new UnityEngine.GameObject("Checkout session test");
        var products = new System.Collections.Generic.List<ShopProduct>();
        try
        {
            var basket = root.AddComponent<ProductSlot>(); basket.Configure(new UnityEngine.Vector3(.35f, .4f, .33f));
            root.transform.position = UnityEngine.Vector3.one * 1000;
            session.Configure(basket, root.transform, index => root.transform.position + new UnityEngine.Vector3(index == 2 ? -.24f : 0, 0, index == 0 ? 0 : -.22f));
            var requested = new[] { 0, 0, 0 };
            for (int i = 0; i < 3; i++)
            {
                var product = MvpShopController.Instance.CreateCoffee(0); products.Add(product);
                session.Collect(product, requested, out string rejection);
                Check(rejection == null && session.Missing(requested).Length == 2 - i, "Repeated SKU counted exactly once: " + i);
            }
            Check(products.Select(p => p.CurrentSlot).Distinct().Count() == 3, "Three identical goods use distinct receipt slots");
            foreach (var product in products) product.CurrentSlot.name = "Renamed receipt adapter";
            Check(session.IsComplete(requested), "Receipt ownership is independent of GameObject names");
            products[1].RemoveFromWorld();
            Check(!session.IsComplete(requested) && session.Missing(requested).Length == 1, "Removed physical good invalidates payment immediately");
        }
        finally
        {
            foreach (var product in products) if (product != null) product.RemoveFromWorld();
            session.CleanupEmptyReceipts();
            UnityEngine.Object.Destroy(root);
        }
        return new { passed, failed = 0 };
    }

    public static object Inventory()
    {
        int passed = 0;
        void Check(bool valid, string name) { if (!valid) throw new Exception(name); passed++; }
        var inventory = new ShopInventory(2);
        var ledger = new ShopLedger(10000);
        ShopPurchaseQuote Quote(int quantity) => inventory.Quote(new[] { quantity, 0 }, new[] { 100, 200 }, 2, ledger.Cash, false, 12);
        Check(!inventory.TryPurchase(Quote(3), _ => false) && inventory.Pending(0) == 0, "Failed debit creates no shipment");
        Check(inventory.TryPurchase(Quote(12), amount => ledger.TryBuy(amount, 1)) && inventory.Stored(0) == 0 && inventory.Pending(0) == 12, "Purchase creates pending, not arrived stock");
        Check(inventory.Tick(-1).Count == 0 && inventory.NextDelivery == 8, "Negative time cannot advance delivery");
        Check(inventory.Tick(7.9f).Count == 0, "Delivery waits the full eight seconds");
        Check(inventory.Tick(.2f).Sum(a => a.Quantity) == 12 && inventory.Stored(0) == 12 && inventory.Pending(0) == 0, "Arrival transfers quantity exactly once");
        Check(inventory.Tick(100).Count == 0 && inventory.Stored(0) == 12, "Later tick cannot deliver twice");
        var stale = Quote(12);
        Check(inventory.TryReturn(0) && !inventory.TryPurchase(stale, _ => throw new Exception("Must not debit a stale over-capacity quote")), "Capacity revalidated before debit");
        Check(inventory.TryWithdraw(0) && inventory.Stored(0) == 12, "Withdrawal reduces stored quantity once");
        inventory.RestoreWithdrawal(0);
        Check(inventory.Stored(0) == 13, "Failed physical pickup restores its withdrawal");
        Check(inventory.TryPurchase(Quote(2), amount => ledger.TryBuy(amount, 1)), "Second delivery scheduled");
        inventory.CompleteDeliveries(); inventory.CompleteDeliveries();
        Check(inventory.Stored(0) == 15 && inventory.Pending(0) == 0, "End-day delivery flush is idempotent");
        inventory.Reset();
        Check(inventory.Stored(0) == 0 && inventory.Pending(0) == 0 && !inventory.TryWithdraw(0), "Reset clears quantities and arrivals without underflow");
        return new { passed, failed = 0 };
    }

    public static object Purchasing()
    {
        int passed = 0;
        void Check(bool valid, string name) { if (!valid) throw new Exception(name); passed++; }
        int[] costs = { 100, 200, 300 }, stored = { 10, 0 }, pending = { 12, 0 }, quantities = { 2, 1 };
        ShopPurchaseQuote Quote(int[] input, int funds = 1000, bool closed = false) =>
            ShopPurchaseRules.Quote(input, costs, 2, stored, pending, funds, closed, 12, 24);
        var quote = Quote(quantities);
        Check(quote.Accepted && quote.Total == 400, "Exact capacity accepts a full basket without including prepared-product costs");
        quantities[0] = 12;
        Check(quote.Quantity(0) == 2, "Accepted quote snapshots mutable caller input");
        Check(Quote(quantities).Error == ShopPurchaseError.Capacity, "Pending shipments count against capacity");
        Check(Quote(new[] { 1, -1 }).Error == ShopPurchaseError.InvalidQuantity, "Invalid later row rejects the entire basket");
        Check(Quote(new[] { 0, 13 }).Error == ShopPurchaseError.InvalidQuantity, "Per-row order limit preserved");
        Check(Quote(new[] { 0, 0 }).Error == ShopPurchaseError.Empty, "Zero basket rejected");
        Check(Quote(new[] { 1, 1 }, 299).Error == ShopPurchaseError.Funds, "Full basket must be affordable");
        Check(Quote(new[] { 1, 1 }, 300).Total == 300, "Exact balance accepted");
        Check(Quote(new[] { 1, 1 }, 1000, true).Error == ShopPurchaseError.Closed, "Closed ledger cannot be quoted");
        Check(Quote(new[] { 1 }).Error == ShopPurchaseError.InvalidBasket && Quote(null).Error == ShopPurchaseError.InvalidBasket, "Invalid basket shape rejected");
        Check(stored.SequenceEqual(new[] { 10, 0 }) && pending.SequenceEqual(new[] { 12, 0 }), "Quote has no inventory side effects");
        var g = MvpShopController.Instance;
        try
        {
            g.ResetDemo();
            int cash = g.Ledger.Cash;
            var basket = new int[ShopCatalog.Count]; basket[0] = 1; basket[1] = -1;
            Check(!g.OrderBasket(basket) && g.Ledger.Cash == cash && g.PendingQuantity(0) == 0, "Invalid last row cannot partially debit or dispatch first row");
            basket[1] = 0; basket[ShopCatalog.Count - 1] = 1;
            Check(!g.OrderBasket(basket) && g.Ledger.Cash == cash && g.PendingQuantity(0) == 0, "Unavailable later row is atomic");
        }
        finally { g.ResetDemo(); }
        return new { passed, failed = 0 };
    }
}
