using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Object=UnityEngine.Object;
public static class CardAndSummaryChecks
{
    static int passed;
    static void Check(bool ok,string name){if(!ok)throw new InvalidOperationException(name);passed++;}
    public static object Run()
    {
        passed=0;var g=MvpShopController.Instance;var player=Object.FindAnyObjectByType<PlayerInteractor>();var hand=new GameObject("Card test hand").transform;hand.position=Vector3.one*1000;g.ManualTick=true;
        try
        {
            g.ResetDemo();g.ClosePanel();var u=g.GetComponent<ShopNextUpgrades>();Check(u.HandleDeveloperKey(Key.F1)&&u.DeveloperVisible&&ShopInputGate.Blocked,"F1 opens developer and blocks movement");Check(u.HandleDeveloperKey(Key.F1)&&!u.DeveloperVisible&&!ShopInputGate.Blocked,"F1 closes developer and restores input");
            Check(GameObject.Find("Bank Card Terminal")!=null,"Physical POS exists");Check(!g.ConfirmCardPayment(),"No card charge without customer");g.OpenShop();
            for(int tick=0;tick<4000&&!(g.ActiveCustomer!=null&&g.ActiveCustomer.UsesCard);tick++){if(g.ActiveCustomer!=null)g.SkipCurrent();g.Tick(.1f);}
            var c=g.ActiveCustomer;Check(c!=null&&c.UsesCard&&g.Stage==ServiceStage.Order,"Real card-paying visit reaches checkout");
            Check(!g.ConfirmCardPayment(),"Incomplete basket cannot be charged");var item=Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId==ShopCatalog.Ids[c.Product]&&p.CurrentSlot!=null&&p.CurrentSlot.IsStockShelf);Check(item.PickUp(hand),"Pick requested card item");Physics.SyncTransforms();Check(item.TryPlace(g.DeliverySlot)&&g.Stage==ServiceStage.Payment,"Complete basket enables payment");
            int cash=g.Ledger.Cash,price=c.TotalPrice,cost=c.TotalCost;Check(g.Drawer.PendingTender==0,"Card creates no cash tender");Check(!g.SubmitChange(0),"Cash settlement rejected for card");g.AddChange(5);Check(g.ChangeSelected==0,"No change for card");
            Check(item.PickUp(hand),"Remove unpaid card item");g.Tick(.1f);Check(!g.ConfirmCardPayment()&&g.Ledger.BankBalance==0,"Removing product cancels card authorization");Physics.SyncTransforms();Check(item.TryPlace(g.DeliverySlot),"Restore card basket");
            g.Devices.Focus(ShopDeviceKind.Till);Check(g.Devices.Focused&&!g.Devices.RegisterDrawer.IsOpen,"Card terminal does not open cash drawer");Check(g.Devices.HandleDeviceKey(Key.Enter),"Enter authorizes terminal");
            Check(g.Stage==ServiceStage.Thanks&&g.Ledger.BankBalance==price&&g.Ledger.CardRevenue==price&&g.Ledger.Revenue==price&&g.Ledger.CostOfGoods==cost,"Exact card total posted once");Check(g.Ledger.Cash==cash&&g.Drawer.Total==cash,"Card never creates physical notes or coins");Check(!g.ConfirmCardPayment(),"Repeated confirmation cannot duplicate charge");
            g.Tick(3);g.RequestClose();for(int tick=0;tick<10000&&g.Phase!=ShopPhase.Summary;tick++){if(g.ActiveCustomer!=null)g.SkipCurrent();g.Tick(.1f);}Check(g.Phase==ShopPhase.Summary&&g.Modal==ShopModal.Summary,"Actual shift reaches summary");
            Check(g.Ledger.BankBalance==price-300&&g.Ledger.Cash==cash&&g.Drawer.Total==cash,"Operating cost uses bank funds first");int bank=g.Ledger.BankBalance;g.NextDay();Check(g.Day==2&&g.Phase==ShopPhase.Preparing&&g.Ledger.BankBalance==bank,"Next day preserves bank and exits summary");
            Check(g.OrderSupply(0,1)&&g.Ledger.BankBalance==bank-1500&&g.Drawer.Total==cash,"Supplies can use card proceeds without inventing cash");
            var details=MvpShopHud.SummaryDetailsRect;var actions=MvpShopHud.SummaryActionsRect;Check(details.yMax<actions.y&&actions.yMax<=672&&actions.height>=3*40,"Summary buttons have reserved visible space independent of stock rows");
            g.ResetDemo();g.ClosePanel();g.Ledger.TryBuy(6000,1);g.SyncCashDrawer();g.OpenShop();g.RequestClose();g.Tick(.1f);Check(g.Phase==ShopPhase.Summary&&g.Ledger.Cash<0,"Insolvent day reaches summary");g.NextDay();Check(g.Phase==ShopPhase.Summary,"Debt cannot silently start next day");g.ResetDemo();Check(g.Phase==ShopPhase.Preparing&&g.Ledger.BankBalance==0&&g.Ledger.Cash==6000,"New game recovers insolvency and clears bank");
            return new {passed,failed=0};
        }
        finally{player.CancelHeld();g.ResetDemo();g.ManualTick=false;Object.Destroy(hand.gameObject);}
    }
}
