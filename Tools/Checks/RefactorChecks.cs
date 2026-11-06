using System;
using System.Linq;

public static class RefactorChecks
{
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
