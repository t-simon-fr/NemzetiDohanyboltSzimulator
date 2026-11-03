using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

public static class AcceptanceLogicChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); passed++; };
        var g = MvpShopController.Instance;
        var player = Object.FindAnyObjectByType<PlayerInteractor>();
        g.GetComponent<ShopMenu>().Resume();
        g.ManualTick = true;
        var hand = new GameObject("Acceptance test hand").transform;
        hand.position = Vector3.one * 1000;
        try
        {
            g.ResetDemo(); g.ClosePanel(); g.OpenShop();
            typeof(MvpShopController).GetProperty("Spawned").SetValue(g, 2);
            typeof(MvpShopController).GetMethod("SpawnCustomer", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(g, new object[] { 0, false });
            for (int i = 0; i < 2000 && g.ActiveCustomer == null; i++) g.Tick(.1f);
            var customer = g.ActiveCustomer;
            typeof(MvpCustomerActor).GetProperty("OrderTypes").SetValue(customer, new[] { 0, 0, 0 });
            typeof(MvpCustomerActor).GetProperty("UnitPrices").SetValue(customer, new[] { 2200, 2200, 2200 });
            check(g.MissingOrderTypes.Length == 3, "Repeated SKU initially requires all three units");
            var items = Object.FindObjectsByType<ShopProduct>().Where(p => p.ProductId == "cig_red" && p.CurrentSlot != null && p.CurrentSlot.IsStockShelf).Take(3).ToArray();
            for (int i = 0; i < items.Length; i++)
            {
                check(items[i].PickUp(hand), "Pick repeated SKU " + i);
                Physics.SyncTransforms();
                check(items[i].TryPlace(g.DeliverySlot), "Deliver repeated SKU " + i);
                check(g.MissingOrderTypes.Length == 2 - i, "Exact remaining repeated SKU count " + i);
            }
            check(items.Select(p => p.CurrentSlot).Distinct().Count() == 3 && g.Stage == ServiceStage.Payment, "Repeated SKU occupies three separate slots");
            int stock = g.StockCount(0);
            g.SkipCurrent();
            check(g.StockCount(0) == stock && g.Drawer.Total == g.Ledger.Cash, "Cancelled repeated-SKU order preserves stock and money");

            // A prepared cup is valid stock even when its ingredients were just consumed.
            g.ResetDemo(); g.ClosePanel();
            g.Ledger.TrySellCard(60000, 0); g.SyncCashDrawer();
            g.Activities.BuyCoffeeMachine();
            typeof(ShopActivities).GetProperty("CoffeeSupplies").SetValue(g.Activities, 1);
            typeof(ShopActivities).GetProperty("MilkSupplies").SetValue(g.Activities, 1);
            g.Activities.SelectRecipe(ShopCatalog.Latte);
            check(g.Activities.BrewCoffee(), "Brew last paid serving for shortage regression");
            g.Activities.Tick(4);
            g.OpenShop();
            typeof(MvpShopController).GetMethod("SpawnCustomer", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(g, new object[] { ShopCatalog.Latte, false });
            for (int i = 0; i < 2000 && g.ActiveCustomer == null; i++) g.Tick(.1f);
            check(g.Activities.CoffeeOutput.Occupant != null, "Last serving is a physical prepared latte");
            check(!g.HasUnavailableRequest, "Existing prepared latte satisfies request without more ingredients");
            return new { passed, failed = 0 };
        }
        finally { player.CancelHeld(); g.ResetDemo(); g.ManualTick = false; Object.Destroy(hand.gameObject); }
    }
}
