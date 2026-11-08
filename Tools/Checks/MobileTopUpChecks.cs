using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public static class MobileTopUpChecks
{
    public static object State()
    {
        int passed = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; }
        Check(ShopMobileConfig.Providers.SequenceEqual(new[] { "Üreskom", "Notyet tel", "Two" }), "Requested provider names retain stable provider ids");
        var book = new ShopMobileTopUpState();
        Check(!book.ActivateForDay(1) && !book.ActivateForDay(5) && book.CreateRequest(0) == null, "No service requests during the first week");
        Check(book.ActivateForDay(6) && !book.ActivateForDay(6) && !book.ActivateForDay(11), "Second Monday activates once, without a debit callback");
        var rotation = new ShopMobileTopUpState(); rotation.ActivateForDay(6);
        Check(new[] { 4, 10, 16 }.Select(visit => rotation.CreateRequest(visit).Provider).Distinct().Count() == 3, "Normal six-visit arrival cadence reaches all three providers instead of one modulo bucket");
        var request = book.CreateRequest(3);
        Check(request != null && request.Number.StartsWith("000") && request.Number.Length == 11, "Only synthetic test numbers are issued");
        Check(ShopMobileConfig.Normalize("+36301234567") == "" && ShopMobileConfig.Normalize("000 0000 0001") == "00000000001", "Real-style numbers rejected, test-number formatting normalized");
        Check(book.Validate(request, -1, request.Number, "2000") != "", "Missing provider rejected");
        Check(book.Validate(request, request.Provider, "123", "2000") != "", "Malformed number rejected");
        Check(book.Validate(request, request.Provider, request.Number, "2001") != "", "Unsupported amount rejected");
        Check(book.Validate(request, (request.Provider + 1) % 3, request.Number, request.Amount.ToString()) != "", "Valid but wrong provider rejected");
        Check(book.Validate(request, request.Provider, "00099999999", request.Amount.ToString()) != "", "Valid but wrong requested number rejected");
        Check(!book.TryComplete(request, 6, () => throw new Exception("Cannot charge before validation")), "No payment before details are confirmed");
        Check(book.Prepare(request, request.Provider, request.Number, request.Amount.ToString()), "Validated immutable request is prepared");
        Check(!book.TryComplete(request, 6, () => false) && book.LastReceipt == null && book.Outstanding == 0, "Failed payment creates no top-up, receipt, reserve or commission");
        book.Cancel(); Check(!book.IsPrepared(request) && book.DailySales == 0, "Cancellation has no economic effects");
        book.Prepare(request, request.Provider, request.Number, request.Amount.ToString());
        int postings = 0;
        Check(book.TryComplete(request, 6, () => { postings++; return true; }), "One successful service transaction");
        Check(!book.TryComplete(request, 6, () => { postings++; return true; }) && postings == 1, "Repeated approval cannot post twice");
        Check(book.DailySales == 2000 && book.DailyCommission == 100 && book.Outstanding == 1900 && book.DailyCount == 1, "Nominal amount splits exactly into commission and operator liability");
        var receipt = book.LastReceipt;
        Check(receipt.Request == request && receipt.OperatorShare == 1900 && receipt.Day == 6, "Receipt snapshots the paid request and accounting");
        book.BeginDay();
        Check(book.Owned && book.Outstanding == 1900 && book.LastReceipt == receipt && book.DailySales == 0 && book.DailyCommission == 0, "Day transition retains contract, receipt and operator debt, resets daily counters");
        Check(!book.TrySettle(x => false) && book.Outstanding == 1900, "Failed operator transfer retains debt");
        Check(book.TrySettle(x => x == 1900) && !book.TrySettle(x => true) && book.Outstanding == 0, "Provider debt paid exactly once");
        var ledger = new ShopLedger(6000); ledger.SetLotteryReserve(500); ledger.SetMobileReserve(1900);
        Check(ledger.AvailableFunds == 3600 && !ledger.TryImprove(3605), "Both operator reserves are excluded from purchases");
        Check(!ledger.TryOperatorTransfer(-4500) && ledger.Cash == 6000, "Lottery payouts cannot spend mobile funds");
        Check(ledger.TrySettleMobile(1900) && ledger.Cash == 4100 && ledger.MobileReserve == 0 && ledger.AvailableFunds == 3600, "Mobile settlement releases only its reserve and does not change spendable balance");
        Check(ledger.Revenue == 0 && ledger.Profit == 0 && !ledger.TrySettleMobile(1900), "Operator settlement is not another sale, cost or profit");
        var reentrant = new ShopMobileTopUpState(); reentrant.ActivateForDay(6); var req = reentrant.CreateRequest(0);
        reentrant.Prepare(req, req.Provider, req.Number, req.Amount.ToString());
        Check(reentrant.TryComplete(req, 6, () => !reentrant.TryComplete(req, 6, () => true)), "Reentrant payment cannot complete the same request twice");
        Check(reentrant.TrySettle(amount => !reentrant.TrySettle(inner => true)), "Reentrant operator callback is guarded");
        return new { passed, failed = 0 };
    }
    public static object Integration()
    {
        int passed = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; }
        var shop = MvpShopController.Instance;
        var fixture = new GameObject("Mobile integration fixture").AddComponent<ShopAcceptanceProbe>();
        var player = Object.FindAnyObjectByType<PlayerInteractor>();
        try
        {
            fixture.Prepare("mobile-locked"); shop.ManualTick = true;
            int balance = shop.Ledger.Cash;
            Check(!shop.Mobile.Owned && shop.Mobile.Book.CreateRequest(3) == null, "New game does not request an unavailable service");
            typeof(MvpShopController).GetProperty("Day").SetValue(shop, 5);
            shop.ClosePanel(); shop.OpenShop(); shop.RequestClose(); shop.Tick(1);
            var body = player.GetComponent<CharacterController>(); body.enabled = false; player.transform.position = shop.Entrance.Position + Vector3.up * .1f; body.enabled = true;
            Check(shop.TryEndDay(), "Fifth workday ends through the real door flow");
            shop.NextDay(); shop.Mobile.BeginDay();
            Check(shop.Day == 6 && shop.Mobile.Owned && shop.Ledger.Cash == balance - 300 && shop.Ledger.Improvements == 0, "Second Monday automatically activates without an upgrade debit; only daily operating cost is charged");
            foreach (string scenario in new[] { "mobile", "mobile-cash" })
            {
                fixture.Prepare(scenario); shop.ManualTick = true;
                var mobile = shop.Mobile; var request = mobile.Request;
                Check(request != null && shop.ActiveCustomer.OrderTypes.Length == 0 && shop.ActiveCustomer.TotalPrice == request.Amount, "A real customer has a service-only order " + scenario);
                Check(ShopUiState.Order(shop).Single().Description.Contains(request.Number), "Request number stays visible in the customer HUD");
                fixture.Action("mobile-open");
                Check(shop.Devices.FocusKind == ShopDeviceKind.Mobile && ShopInputGate.Blocked, "Separate mobile mode reuses terminal focus");
                mobile.SelectProvider(request.Provider); mobile.NumberEntry = "wrong"; mobile.AmountEntry = request.Amount.ToString();
                Check(!mobile.ConfirmDetails() && shop.Stage == ServiceStage.Order && mobile.Book.LastReceipt == null, "Invalid number cannot enter checkout");
                mobile.NumberEntry = request.Number;
                Check(mobile.ConfirmDetails() && shop.Stage == ServiceStage.Payment && mobile.Book.IsPrepared(request), "Verified details prepare existing cash/card checkout");
                int cash = shop.Ledger.Cash; shop.ClosePanel(); fixture.Action("mobile-open"); mobile.CancelDraft();
                Check(shop.Stage == ServiceStage.Order && !mobile.Book.IsPrepared(request) && shop.Ledger.Cash == cash && mobile.Book.LastReceipt == null, "Cancelling prepared payment clears tender and terminal without charging");
                mobile.SelectProvider(request.Provider); mobile.NumberEntry = request.Number; mobile.AmountEntry = request.Amount.ToString();
                Check(mobile.ConfirmDetails(), "Corrected data can be submitted again");
                bool paid;
                if (shop.CardPayment)
                {
                    Check(!shop.ConfirmCardPayment(), "Empty card amount cannot complete mobile service");
                    foreach (char c in request.Amount.ToString()) shop.EnterCardDigit(c - '0');
                    paid = shop.ConfirmCardPayment();
                }
                else { shop.AddChange(500); paid = shop.SubmitChange(500); }
                Check(paid && shop.Stage == ServiceStage.Thanks && shop.Ledger.Cash == cash + request.Amount && shop.Drawer.Total == shop.Ledger.Cash, "Real payment posts one top-up and reconciles drawer " + scenario);
                Check(mobile.Book.LastReceipt != null && mobile.Book.DailyCount == 1 && shop.Ledger.Profit == 100 && shop.Ledger.CostOfGoods == 1900 && shop.Ledger.MobileReserve == 1900, "Real receipt and ledger show commission rather than nominal profit " + scenario);
                Check(!shop.ConfirmCardPayment() && !shop.SubmitChange(500) && mobile.Book.DailyCount == 1, "Repeated payment cannot duplicate service or receipt");
                shop.Tick(3); shop.RequestClose();
                for (int i = 0; i < 1000 && !shop.CanEndDay; i++) shop.Tick(.1f);
                var cc = player.GetComponent<CharacterController>(); cc.enabled = false; player.transform.position = shop.Entrance.Position + Vector3.up * .1f; cc.enabled = true;
                Check(shop.TryEndDay(), "Close actual mobile-sales day at the entrance");
                shop.NextDay();
                Check(shop.Day == 7 && mobile.Owned && mobile.Book.Outstanding == 1900 && mobile.Book.DailySales == 0 && shop.Ledger.MobileReserve == 1900, "Next-day ledger preserves operator funds and automatic activation");
                cash = shop.Ledger.Cash; int profit = shop.Ledger.Profit;
                Check(mobile.SettleOperator() && shop.Ledger.Cash == cash - 1900 && shop.Ledger.Profit == profit && mobile.Book.Outstanding == 0, "Provider settlement is a single non-sale movement");
                Check(!mobile.SettleOperator(), "Provider settlement cannot repeat");
            }
            foreach (int visit in new[] { 4, 10 })
            {
                fixture.Prepare("mobile-locked"); shop.ManualTick = true; shop.ClosePanel();
                typeof(MvpShopController).GetProperty("Day").SetValue(shop, 6); shop.Mobile.BeginDay(); shop.OpenShop();
                typeof(MvpShopController).GetProperty("Spawned").SetValue(shop, visit);
                typeof(MvpShopController).GetMethod("SpawnCustomer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(shop, new object[] { -1, false });
                for (int i = 0; i < 2000 && shop.ActiveCustomer == null; i++) shop.Tick(.1f);
                Check(shop.ActiveCustomer?.MobileRequest != null, "Normal arrival policy can request unlocked top-up " + visit);
                Check(shop.ActiveCustomer.UsesCard == (visit == 10), "Natural phone customers include both cash and card, not only a forced fixture " + visit);
            }
            fixture.Prepare("mobile-locked"); shop.ManualTick = true; shop.ClosePanel();
            typeof(MvpShopController).GetProperty("Day").SetValue(shop, 6); shop.Mobile.BeginDay();
            foreach (var product in Object.FindObjectsByType<ShopProduct>()) product.RemoveFromWorld();
            shop.OpenShop();
            typeof(MvpShopController).GetMethod("SpawnCustomer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(shop, new object[] { -1, false });
            for (int i = 0; i < 2000 && shop.ActiveCustomer == null; i++) shop.Tick(.1f);
            Check(shop.ActiveCustomer?.MobileRequest != null, "Top-up customers still arrive when merchandise is sold out");
            shop.ResetDemo();
            Check(!shop.Mobile.Owned && shop.Mobile.Book.Outstanding == 0 && shop.Mobile.Book.LastReceipt == null && shop.Ledger.MobileReserve == 0, "New game clears all mobile ownership, receipts and debt");
            return new { passed, failed = 0 };
        }
        finally { player.CancelHeld(); shop.ClosePanel(); shop.ResetDemo(); shop.ManualTick = false; Object.Destroy(fixture.gameObject); }
    }
}
