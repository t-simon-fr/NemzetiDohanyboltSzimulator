using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

// #175: dialogue zoom + RPG panel + typewriter state coverage.
// Money, inventory and camera invariants ride along with every transition.
public static class DialogueZoomChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); passed++; };
        var shop = MvpShopController.Instance;
        var menu = shop.GetComponent<ShopMenu>();
        var view = Camera.main;
        check(view != null, "Dialogue checks need the player camera");
        shop.ManualTick = true;
        try
        {
            shop.ResetDemo();
            shop.ClosePanel();
            check(!shop.DialoguePanelVisible, "No panel without an active customer");
            for (int i = 0; i < 5; i++) shop.Tick(0.2f);
            float rest = view.fieldOfView;
            check(Math.Abs(rest - 60f) < 1f, "Camera rests at the scene default without dialogue");
            int cash = shop.Ledger.Cash;
            int stock = shop.StockCount(0) + shop.StockCount(1) + shop.StockCount(2) + shop.StockCount(3);

            shop.OpenShop();
            for (int n = 0; n < 2000 && shop.ActiveCustomer == null; n++) shop.Tick(0.1f);
            check(shop.ActiveCustomer != null && shop.Stage == ServiceStage.Order, "First customer reaches the order stage");
            for (int i = 0; i < 5; i++) shop.Tick(0.1f);
            check(shop.DialoguePanelVisible, "Order stage shows the dialogue panel");
            check(shop.Dialogue.Length > 20, "Order line is a full sentence");
            string early = shop.DialogueShown;
            check(early.Length < shop.Dialogue.Length, "Sentence types out gradually");
            for (int i = 0; i < 5; i++) shop.Tick(0.1f);
            check(shop.DialogueShown.Length >= early.Length, "Typing advances with game time");
            float zooming = view.fieldOfView;
            check(zooming < rest - 1f, "Camera zooms in on the ordering customer");
            check(shop.Ledger.Cash == cash, "Dialogue alone never moves money");
            check(shop.StockCount(0) + shop.StockCount(1) + shop.StockCount(2) + shop.StockCount(3) == stock, "Dialogue alone never moves stock");

            check(shop.TryAdvanceDialogue(), "E press completes the typed sentence");
            check(shop.DialogueShown == shop.Dialogue, "Completed sentence matches the full line");
            check(!shop.TryAdvanceDialogue(), "Completed text leaves later presses alone");
            check(!ShopInputGate.WorldActionConsumed, "Completing text never consumes the world action");
            check(shop.Stage == ServiceStage.Order && shop.ActiveCustomer != null, "Completing text never advances the sale");

            menu.Open();
            float frozen = view.fieldOfView;
            shop.Tick(0.5f);
            check(Math.Abs(view.fieldOfView - frozen) < 0.01f, "Pause freezes the camera instead of breaking zoom state");
            menu.Resume();
            shop.Tick(0.1f);
            check(shop.DialoguePanelVisible, "Dialogue resumes after pause");

            shop.Devices.Focus(ShopDeviceKind.Monitor);
            float held = view.fieldOfView;
            for (int i = 0; i < 5; i++) shop.Tick(0.2f);
            check(Math.Abs(view.fieldOfView - held) < 0.01f, "Device focus owns the camera during dialogue");
            shop.ClosePanel();
            shop.Tick(0.1f);
            check(shop.DialoguePanelVisible, "Closing the device returns to the dialogue");

            foreach (int type in shop.ActiveCustomer.OrderTypes)
            {
                var item = Object.FindObjectsByType<ShopProduct>().First(p => p.ProductId == ShopCatalog.Ids[type] && p.CurrentSlot != null && p.CurrentSlot.IsStockShelf);
                item.AssignInitialSlot(shop.DeliverySlot);
                shop.DeliverySlot.NotifyProductPlaced(item);
            }
            check(shop.Stage == ServiceStage.Payment, "Full handover reaches payment");
            shop.Tick(0.1f);
            check(!shop.DialoguePanelVisible, "Payment hides the dialogue panel");
            for (int i = 0; i < 10; i++) shop.Tick(0.2f);
            check(Math.Abs(view.fieldOfView - rest) < 0.5f, "Camera restores after the order phase");

            shop.SkipCurrent();
            shop.Tick(0.1f);
            check(shop.ActiveCustomer == null && shop.Stage == ServiceStage.None, "Refused payment leaves the shop");
            check(!shop.DialoguePanelVisible, "No panel without a customer after refusal");
            for (int i = 0; i < 10; i++) shop.Tick(0.2f);
            check(Math.Abs(view.fieldOfView - rest) < 0.5f, "Camera rests after refusal");

            for (int n = 0; n < 2000 && shop.ActiveCustomer == null; n++) shop.Tick(0.1f);
            for (int i = 0; i < 5; i++) shop.Tick(0.1f);
            check(shop.DialoguePanelVisible, "Fast succession retriggers the panel");
            check(shop.DialogueShown.Length < shop.Dialogue.Length, "Retriggered sentence types from the start");
            foreach (int type in shop.ActiveCustomer.OrderTypes)
            {
                var item = Object.FindObjectsByType<ShopProduct>().First(p => p.ProductId == ShopCatalog.Ids[type] && p.CurrentSlot != null && p.CurrentSlot.IsStockShelf);
                item.AssignInitialSlot(shop.DeliverySlot);
                shop.DeliverySlot.NotifyProductPlaced(item);
            }
            check(shop.Stage == ServiceStage.Payment, "Handover works mid-typing without an extra E press");
            shop.SkipCurrent();
            for (int i = 0; i < 10; i++) shop.Tick(0.2f);
            check(!shop.DialoguePanelVisible && Math.Abs(view.fieldOfView - rest) < 0.5f, "Mid-sentence leave restores the camera");
            check(shop.Ledger.Cash == cash, "Refusals never credit money");

            check(ShopPeople.Mood(2) == "SIETŐS", "Pisti reads as hurried");
            check(ShopPeople.Mood(1) == "BESZÉLGETŐS", "Marika reads as chatty");
            check(ShopPeople.Monogram(0) == "J" && ShopPeople.Monogram(3) == "L", "Speaker chips use name initials");
            return new { passed, failed = 0 };
        }
        finally
        {
            menu.Resume();
            if (shop.Devices != null && shop.Devices.Focused) shop.ClosePanel();
            shop.ResetDemo();
            shop.ManualTick = false;
            for (int i = 0; i < 10; i++) shop.Tick(0.2f);
        }
    }
}
