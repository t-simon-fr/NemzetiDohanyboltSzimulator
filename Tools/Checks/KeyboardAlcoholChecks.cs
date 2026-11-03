using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Object=UnityEngine.Object;

public static class KeyboardAlcoholChecks
{
    private static int passed;
    private static void Check(bool valid,string label){if(!valid)throw new InvalidOperationException(label);passed++;}
    public static object Keyboard()
    {
        passed=0;var game=MvpShopController.Instance;var d=game.Devices;game.ManualTick=true;
        try
        {
            ShopCheckFixtures.Reset(game);game.ClosePanel();ShopCheckFixtures.Fund(game,100000);d.Focus(ShopDeviceKind.Monitor);
            Check(!d.HandleDeviceKey(Key.Digit9)&&d.ComputerApp==ShopComputerApp.Desktop,"Unavailable desktop shortcut does nothing");
            Check(d.HandleDeviceKey(Key.Digit1)&&d.ComputerApp==ShopComputerApp.Orders,"Desktop digit opens orders");
            Check(d.KeyboardTargets().Count(t=>t.Id.StartsWith("Order:"))==4,"Locked alcohol is absent from keyboard targets");
            int cash=game.Ledger.Cash;d.HandleDeviceKey(Key.Digit1);Check(game.Ledger.Cash==cash&&d.KeyboardTargetId=="Order:0","Product shortcut focuses without charging");
            d.HandleDeviceKey(Key.NumpadMinus);Check(d.OrderQuantity==2,"Numpad quantity decrement");
            d.HandleDeviceKey(Key.NumpadEnter);Check(game.PendingQuantity(0)==2&&game.Ledger.Cash==cash-3000,"Numpad Enter places selected order once");
            d.HandleDeviceKey(Key.Backspace);Check(d.ComputerApp==ShopComputerApp.Desktop,"Backspace returns to desktop");
            d.HandleDeviceKey(Key.Digit2);Check(d.ComputerApp==ShopComputerApp.Furnishing,"Developments shortcut");
            Check(game.TryExpand(),"Paid expansion unlocks keyboard and stock catalog");d.OpenApp(ShopComputerApp.Orders);
            Check(d.KeyboardTargets().Count(t=>t.Id.StartsWith("Order:"))==9,"Nine packaged products are keyboard reachable");
            d.HandleDeviceKey(Key.Digit8);d.HandleDeviceKey(Key.Enter);d.HandleDeviceKey(Key.Digit9);d.HandleDeviceKey(Key.Enter);
            Check(game.PendingQuantity(7)==2&&game.PendingQuantity(8)==2,"Alcohol orders use selected quantities via keyboard");
            d.HandleDeviceKey(Key.Equals);Check(d.OrderQuantity==3,"Main keyboard plus increases quantity");
            d.OpenApp(ShopComputerApp.Cameras);d.HandleDeviceKey(Key.Tab);Check(d.KeyboardTargetId=="Camera:0","Tab begins at entrance camera");
            d.HandleDeviceKey(Key.RightArrow);Check(d.KeyboardTargetId=="Camera:1","Arrow reaches second camera");d.HandleDeviceKey(Key.Enter);
            d.HandleDeviceKey(Key.Backspace);d.HandleDeviceKey(Key.Digit3);Check(d.ComputerApp==ShopComputerApp.Coffee,"Coffee desktop shortcut");
            Check(!d.KeyboardTargets().Any(t=>t.Id=="Coffee:Order"),"Unpurchased coffee cannot order by keyboard");
            Check(game.Activities.BuyCoffeeMachine(),"Purchase coffee upgrade");d.OpenApp(ShopComputerApp.Coffee);d.HandleDeviceKey(Key.Tab);d.HandleDeviceKey(Key.Enter);
            Check(game.Activities.PendingCoffee==10,"Coffee supplies ordered by keyboard");
            d.OpenApp(ShopComputerApp.Games);d.HandleDeviceKey(Key.Tab);Check(d.KeyboardTargetId=="Game:Snake","Game menu begins with Snake");d.HandleDeviceKey(Key.Enter);
            Check(game.Activities.SnakeRunning&&!d.HandleDeviceKey(Key.UpArrow),"Snake owns arrow keys while running");d.HandleDeviceKey(Key.Backspace);
            d.HandleDeviceKey(Key.Tab);d.HandleDeviceKey(Key.RightArrow);Check(d.KeyboardTargetId=="Game:Memory","Arrow selects Memory");d.HandleDeviceKey(Key.Space);
            Check(d.KeyboardTargets().Count(t=>t.Id.StartsWith("Memory:"))==16,"All memory cards have keyboard targets");
            foreach(var pair in Enumerable.Range(0,16).GroupBy(i=>game.Activities.Memory.Cards[i]))
            {
                foreach(int index in pair)
                {
                    for(int n=0;n<25&&d.KeyboardTargetId!="Memory:"+index;n++)d.HandleDeviceKey(Key.Tab);
                    Check(d.KeyboardTargetId=="Memory:"+index,"Tab reaches unmatched card "+index);d.HandleDeviceKey(Key.Space);
                }
            }
            Check(game.Activities.Memory.Won&&game.Activities.Memory.Moves==8,"Memory completed entirely through keyboard activations");
            d.HandleDeviceKey(Key.Backspace);Check(!d.KeyboardTargets().Any(t=>t.Id.StartsWith("Memory:")),"Backspace leaves board for games menu");
            d.HandleDeviceKey(Key.Backspace);Check(d.ComputerApp==ShopComputerApp.Desktop,"Second Backspace leaves games");
            d.HandleDeviceKey(Key.Backspace);Check(!d.Focused&&!d.HandleDeviceKey(Key.Enter),"Desktop Backspace releases device and unfocused input is ignored");
            var offer=new ShopCoinOffer(450,42);offer.Toggle(0);offer.Toggle(1);offer.Toggle(0);offer.Toggle(0);offer.Undo();
            Check(!offer.Selected[0]&&offer.Selected[1],"Coin undo follows most recent selection, including reselection");offer.Clear();Check(offer.Total==0&&!offer.Undo(),"Coin clear resets undo history");
            return new {passed,failed=0};
        }
        finally{d.Release();ShopCheckFixtures.Reset(game);game.ManualTick=false;}
    }
    public static object PaymentsAndAlcohol()
    {
        passed=0;var game=MvpShopController.Instance;var d=game.Devices;var hand=new GameObject("Keyboard sale test hand").transform;hand.position=Vector3.one*1000;
        game.ManualTick=true;var sold=new int[ShopCatalog.Names.Length];int cashSales=0,coinSales=0;
        try
        {
            ShopCheckFixtures.Reset(game);game.ClosePanel();ShopCheckFixtures.Fund(game,150000);Check(game.TryExpand(),"Expand for full catalog shift");
            for(int type=4;type<ShopCatalog.Count;type++)Check(game.OrderSupply(type,6),"Buy six units of drink "+type);ShopCheckFixtures.Tick(game,8.1f);
            for(int type=4;type<ShopCatalog.Count;type++)for(int n=0;n<6;n++)
            {
                Check(game.TryCreateSupply(type,out var item),"Withdraw actual delivered unit");
                var slot=Object.FindObjectsByType<ProductSlot>().First(s=>s.PreferredProductId==ShopCatalog.Ids[type]&&s.CanAcceptStock(ShopCatalog.Ids[type]));Check(item.AssignInitialSlot(slot),"Stock actual unit on matching public shelf");
            }
            // Three shifts exercise both new drinks as ordinary paid purchases as well as theft.
            for(int day=0;day<3;day++)
            {
                game.OpenShop();
                for(int tick=0;tick<22000&&game.Phase!=ShopPhase.Summary;tick++)
                {
                    ShopCheckFixtures.Tick(game,.1f);var customer=game.ActiveCustomer;if(customer==null||game.Stage==ServiceStage.Thanks)continue;
                    if(game.Stage==ServiceStage.Order&&customer.Product<4)
                    {
                        var item=Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId==ShopCatalog.Ids[customer.Product]&&!p.IsHeld);
                        Check(item.PickUp(hand),"Pick cigarette");Physics.SyncTransforms();Check(item.TryPlace(game.DeliverySlot),"Prepare requested cigarette");
                    }
                    if(game.Stage!=ServiceStage.Payment)continue;
                    int type=customer.Product,before=game.Ledger.Cash;game.OpenCash();
                    if(game.CoinPayment||customer.Tender>ShopCatalog.Prices[type]){d.HandleDeviceKey(Key.Enter);Check(game.Stage==ServiceStage.Payment&&game.Ledger.Cash==before,"Premature Enter cannot settle incorrect amount");}
                    if(game.CoinPayment)
                    {
                        var offer=game.Activities.CoinOffer;for(int n=0;n<30;n++){d.HandleDeviceKey(Key.Tab);if(d.KeyboardTargetId.StartsWith("CustomerCoin:"))break;}d.HandleDeviceKey(Key.Space);int amount=offer.Total;d.HandleDeviceKey(Key.Backspace);
                        Check(amount>0&&offer.Total==0,"Backspace returns the selected customer coin");
                        game.ToggleCustomerCoin(0);d.HandleDeviceKey(Key.Delete);Check(offer.Total==0,"Delete clears customer coin selection");
                        int rest=offer.Price;foreach(int i in Enumerable.Range(0,offer.Coins.Count).OrderByDescending(i=>offer.Coins[i]))if(offer.Coins[i]<=rest){game.ToggleCustomerCoin(i);rest-=offer.Coins[i];}
                        Check(rest==0,"Finite coin offer can cover price");d.HandleDeviceKey(Key.NumpadEnter);coinSales++;
                    }
                    else
                    {
                        int denomination=ShopCatalog.Denominations.First(v=>v<=customer.Tender&&game.Drawer.Count(v)>0);int index=Array.IndexOf(ShopCatalog.Denominations,denomination);
                        var keys=new[]{Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6};d.HandleDeviceKey(keys[index%6],index>=6);
                        Check(game.ChangeSelected==denomination,"Digit shortcut takes one real denomination");d.HandleDeviceKey(Key.Backspace);Check(game.ChangeSelected==0,"Backspace restores last physical denomination");
                        d.HandleDeviceKey(keys[index%6],index>=6);d.HandleDeviceKey(Key.Delete);Check(game.ChangeSelected==0,"Delete restores all selected cash");
                        int rest=customer.Tender-ShopCatalog.Prices[type];
                        for(int attempt=0;attempt<80;attempt++)
                        {
                            game.ClearChange();int due=rest;foreach(int v in ShopCatalog.Denominations){int count=Math.Min(due/v,game.Drawer.Count(v));for(int n=0;n<count;n++)game.AddChange(v);due-=v*count;}if(due==0)break;game.ExchangeCash();
                        }
                        Check(game.ChangeSelected==rest,"Select exact finite change");d.HandleDeviceKey(Key.Enter);cashSales++;
                    }
                    Check(game.Stage==ServiceStage.Thanks&&!d.Focused&&game.Ledger.Cash==before+ShopCatalog.Prices[type],"Enter settles exact payment and restores camera");
                    Check(game.Drawer.Total==game.Ledger.Cash&&!d.HandleDeviceKey(Key.Enter),"Cash and ledger agree, repeated Enter cannot duplicate sale");sold[type]++;
                }
                Check(game.Phase==ShopPhase.Summary&&game.Missed==0,"Complete shift with nine-product catalog");if(day<2)game.NextDay();
            }
            Check(sold[7]>0&&sold[8]>0,"Both Kevert and Kőfejtői were bought by real self-service customers");
            Check(coinSales>0&&cashSales>0,"Both coin and banknote payments exercised with Enter");
            return new {passed,failed=0,kevertSales=sold[7],beerSales=sold[8],cashSales,coinSales};
        }
        finally{d.Release();ShopCheckFixtures.Reset(game);game.ManualTick=false;Object.Destroy(hand.gameObject);}
    }
}
