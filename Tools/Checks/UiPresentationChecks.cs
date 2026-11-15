using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

public static class UiPresentationChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); passed++; };
        var shop = MvpShopController.Instance;
        var player = Object.FindAnyObjectByType<PlayerInteractor>();
        var fixture = new GameObject("UI check fixture").AddComponent<ShopAcceptanceProbe>();
        var saved = ShopBindings.Actions.ToDictionary(a => a.Key, a => PlayerPrefs.HasKey("Shop.Key." + a.Key) ? PlayerPrefs.GetString("Shop.Key." + a.Key) : null);
        try
        {
            fixture.Prepare("ui-order");
            var lines = ShopUiState.Order(shop);
            check(lines.Length == 2 && lines[0].Required == 2 && lines[1].Required == 1, "Duplicate order groups preserve exact quantities");
            check(lines.All(l => l.Delivered == 0), "New order has no phantom deliveries");
            check(JsonUtility.FromJson<ShopOrderLine>(JsonUtility.ToJson(lines[0])).Required == 2, "Order quantities survive the WebGL fixture serialization boundary");
            check(shop.Dialogue.Contains("2 × Lucky Stroke") && shop.Dialogue.Contains(ShopPeople.Greet(shop.ActiveCustomer.Person, shop.ActiveCustomer.Visit, shop.Day)), "Spoken order includes quantities and actual character personality");
            int cash = shop.Ledger.Cash;
            fixture.Action("deliver-ui-one");
            check(ShopUiState.Order(shop)[0].Delivered == 1 && shop.Ledger.Cash == cash, "View reads partial physical handover without crediting money");
            var delivered = Object.FindObjectsByType<ShopProduct>().First(p => p.ProductId == ShopCatalog.Ids[0] && p.CurrentSlot != null && !p.CurrentSlot.IsStockShelf);
            check(player.TryHold(delivered), "Take back partial delivered product");
            check(ShopUiState.Order(shop)[0].Delivered == 0, "Held receipt is no longer shown as handed over");
            player.CancelHeld();
            check(ShopUiState.Order(shop)[0].Delivered == 1, "Cancellation restores original delivered count");
            foreach (var product in Object.FindObjectsByType<ShopProduct>().Where(p => p.ProductId == ShopCatalog.Ids[1])) product.RemoveFromWorld();
            check(ShopUiState.Order(shop).First(l => l.Type == 1).Unavailable, "Shortage is read from real stock availability");
            var state = new ShopUiState();
            state.Observe("Hibás összeg", 1, 10);
            check(state.Notice(15) == "Hibás összeg" && state.Notice(16) == "", "Notice expires at six seconds");
            state.Observe("Hibás összeg", 2, 17);
            check(state.Notice(18) == "Hibás összeg", "Repeating same failed action renews feedback");
            state.Observe("Hibás összeg", 2, 21);
            check(state.Notice(24) == "", "Rendering unchanged state cannot extend notice lifetime");
            ShopBindings.Reset();
            check(ShopBindings.Bind(Key.E, Key.R) && ShopBindings.Bind(Key.X, Key.V), "Remap interaction and refusal for visible labels");
            check(ShopUi.BoundText("E – Felvétel • X: Elutasítás") == "R – Felvétel • V: Elutasítás", "Context labels follow actual bindings");
            check(ShopUi.BoundText("KÉSZLET • EXTRA") == "KÉSZLET • EXTRA", "Rebinding never replaces letters inside words");
            check(MvpShopHud.SummaryDetailsRect.yMax < MvpShopHud.SummaryActionsRect.y, "Receipt actions stay outside scrolling stock details");
            fixture.Prepare("card");
            check(ShopUiState.Order(shop).All(l => l.Delivered == l.Required), "Complete physical basket is shown as ready for payment");
            foreach (char digit in shop.ActiveCustomer.TotalPrice.ToString()) shop.EnterCardDigit(digit - '0');
            check(shop.ConfirmCardPayment(), "Settle actual card customer for completed UI state");
            check(shop.Stage == ServiceStage.Thanks && ShopUiState.Order(shop).All(l => l.Delivered == l.Required), "Paid order stays complete after sold products leave the world");
            shop.ResetDemo();
            check(ShopUiState.Order(shop).Length == 0 && shop.Modal == ShopModal.None && shop.Ledger.Cash == 6000, "Reset clears view without creating modal or changing opening money");
            return new { passed, failed = 0 };
        }
        finally
        {
            player.CancelHeld();
            shop.ResetDemo();
            shop.ManualTick = false;
            Object.Destroy(fixture.gameObject);
            foreach (var item in saved)
            {
                string key = "Shop.Key." + item.Key;
                if (item.Value == null) PlayerPrefs.DeleteKey(key); else PlayerPrefs.SetString(key, item.Value);
            }
            PlayerPrefs.Save();
        }
    }
}
