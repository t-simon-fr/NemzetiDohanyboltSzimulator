using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public static class EntranceDayChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, message) => { if (!ok) throw new Exception(message); passed++; };
        var shop = MvpShopController.Instance;
        var player = Object.FindAnyObjectByType<PlayerInteractor>();
        var body = player.GetComponent<CharacterController>();
        Vector3 home = player.transform.position;
        shop.ManualTick = true;
        try
        {
            shop.ResetDemo();
            check(shop.Modal == ShopModal.None && !ShopInputGate.Blocked, "Preparation starts without a modal");
            shop.Tick(60);
            check(shop.Elapsed == 0 && shop.Spawned == 0, "Preparation does not start clock or visits");
            check(shop.Entrance.PlayerBoundary.enabled && shop.Entrance.PlayerBoundary.isTrigger,
                "Doorway remains targetable without an invisible player wall");
            check(!shop.TryEndDay(), "Cannot end an unopened day");
            var sign = GameObject.Find("Open Close Sign").GetComponent<ShopStation>();
            sign.Use(player);
            shop.Tick(.1f);
            check(shop.Phase == ShopPhase.Open && shop.Elapsed > 0 && shop.Spawned == 1, "Using sign starts clock and customers");
            sign.Use(player);
            int visitors = shop.Spawned;
            float elapsed = shop.Elapsed;
            check(shop.Phase == ShopPhase.Closing && !shop.TryEndDay(), "Closing with a customer does not end day");
            for (int n = 0; n < 2000 && !shop.CanEndDay; n++)
            {
                if (shop.ActiveCustomer != null) shop.SkipCurrent();
                shop.Tick(.1f);
            }
            check(shop.CanEndDay && shop.Phase == ShopPhase.AfterHours && shop.Modal == ShopModal.None && shop.AfterHours.IsNight,
                "Empty closed shop stays playable without summary");
            shop.Tick(100);
            check(shop.Spawned == visitors && shop.Elapsed == elapsed && !shop.Ledger.Closed,
                "Closed shop stops visitors and clock without closing ledger");
            body.enabled = false;
            player.transform.position = shop.AfterHours.Merchant.position + Vector3.right;
            body.enabled = true;
            ShopCheckFixtures.Fund(shop,10000);
            int beforeTrade = shop.Ledger.Cash;
            int offered = shop.AfterHours.OfferedQuantity;
            check(shop.AfterHours.OpenTrade() && shop.AfterHours.BuyBundle() &&
                shop.Ledger.Cash == beforeTrade - offered*700 && !shop.Ledger.Closed,
                "Evening trade remains available before the door closes the ledger");
            shop.AfterHours.CloseTrade();
            body.enabled = false;
            player.transform.position = Vector3.up * 10;
            body.enabled = true;
            check(!shop.TryEndDay(), "Day cannot end away from entrance");
            body.enabled = false;
            player.transform.position = shop.Layout.EntryInside;
            body.enabled = true;
            var product = Object.FindObjectsByType<ShopProduct>().First(p => p.CurrentSlot != null && p.CurrentSlot.IsStockShelf);
            check(player.TryHold(product) && !shop.TryEndDay(), "Held goods block exit");
            player.CancelHeld();
            int cash = shop.Ledger.Cash;
            shop.Entrance.GetComponentInChildren<ShopStation>().Use(player);
            check(shop.Phase == ShopPhase.Summary && shop.Ledger.Cash == cash - 300,
                "Entrance action closes ledger and charges operating cost once");
            check(!shop.TryEndDay() && shop.Ledger.Cash == cash - 300, "Repeated exit cannot double-charge");
            shop.NextDay();
            check(shop.Day == 2 && shop.Phase == ShopPhase.Preparing && shop.Modal == ShopModal.None && shop.Elapsed == 0,
                "Next day starts without another welcome modal");
            check(shop.StockCount(ShopCatalog.Contraband) == offered && shop.Ledger.Cash == cash - 300,
                "Next day preserves paid evening goods and charges no second operating fee");
            return new { passed, failed = 0 };
        }
        finally
        {
            player.CancelHeld();
            body.enabled = false;
            player.transform.position = home;
            body.enabled = true;
            shop.ResetDemo();
            shop.ManualTick = false;
        }
    }
}
