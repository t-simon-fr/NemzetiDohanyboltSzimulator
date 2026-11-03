using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Object=UnityEngine.Object;
public static class EveningPricingChecks
{
    static int passed;
    static void Check(bool ok,string name){if(!ok)throw new InvalidOperationException(name);passed++;}
    static void CloseShift(MvpShopController g){g.OpenShop();g.RequestClose();g.Tick(.1f);Check(g.Phase==ShopPhase.Summary,"Shift closed");}
    static void Move(PlayerInteractor p,Vector3 point){var c=p.GetComponent<CharacterController>();c.enabled=false;p.transform.position=point;c.enabled=true;Physics.SyncTransforms();}
    public static object Pricing()
    {
        passed=0;var g=MvpShopController.Instance;g.ManualTick=true;var hand=new GameObject("Price check hand").transform;hand.position=Vector3.one*1000;
        try
        {
            g.ResetDemo();g.ClosePanel();var p=g.Pricing;Check(p.Price(0)==2200&&p.AvailableTypes.Length==4,"Initial prices and unlocked products");
            Check(!p.TrySetPrice(-1,2200)&&!p.TrySetPrice(0,-5)&&!p.TrySetPrice(0,2201)&&!p.TrySetPrice(0,500)&&!p.TrySetPrice(0,10000),"Invalid and out-of-range prices rejected");
            Check(!p.TrySetPrice(ShopCatalog.Coffee,650),"Locked coffee price unavailable");
            Check(p.TrySetPrice(0,2420)&&p.DemandPercent(0)==80,"Ten percent markup retains eighty percent budget groups");Check(p.TrySetPrice(0,4400)&&p.DemandPercent(0)==0,"Double price eliminates willing budget groups");
            Check(p.TrySetPrice(0,1100)&&p.DemandPercent(0)==100,"Discount retains all groups");p.ResetPrices();
            g.Devices.Focus(ShopDeviceKind.Monitor);Check(g.Devices.HandleDeviceKey(Key.Digit6)&&g.Devices.ComputerApp==ShopComputerApp.Prices,"Desktop shortcut opens prices");
            var targets=g.Devices.KeyboardTargets();Check(targets.Any(t=>t.Id=="Price:100")&&targets.Any(t=>t.Id=="Price:Reset"),"Price controls support keyboard navigation");
            g.Devices.HandleDeviceKey(Key.Equals);Check(p.Price(0)==2300,"Plus changes selected price by 100");g.Devices.HandleDeviceKey(Key.Minus,true);Check(p.Price(0)==2295,"Shift minus changes selected price by five");p.ResetPrices();g.ClosePanel();
            Check(p.TrySetPrice(0,2295),"Set customer sale price");g.SingleProductVisits=true;g.OpenShop();
            for(int n=0;n<2000&&g.ActiveCustomer==null;n++)g.Tick(.1f);
            // The first visitor's budget is 100 percent, so an increased quote must be refused.
            Check(g.PriceRefusals>=1&&g.Ledger.Sales==0,"Real visitor refuses expensive quote without paying");g.ResetDemo();g.ClosePanel();g.SingleProductVisits=true;
            Check(g.Pricing.TrySetPrice(0,2100),"Discount before arrival");g.OpenShop();for(int n=0;n<2000&&g.ActiveCustomer==null;n++)g.Tick(.1f);
            var c=g.ActiveCustomer;Check(c!=null&&c.Product==0&&c.TotalPrice==2100,"Customer locks discounted price on arrival");
            Check(g.Pricing.TrySetPrice(0,3000)&&c.TotalPrice==2100,"Later edit does not alter outstanding order");
            var item=Object.FindObjectsByType<ShopProduct>().First(x=>x.ProductId==ShopCatalog.Ids[0]&&x.CurrentSlot!=null&&x.CurrentSlot.IsStockShelf);Check(item.PickUp(hand),"Pick discounted order");Physics.SyncTransforms();Check(item.TryPlace(g.DeliverySlot),"Deliver discounted order");
            int cash=g.Ledger.Cash,rest=c.Tender-c.TotalPrice;foreach(int v in ShopCatalog.Denominations)while(rest>=v&&g.Drawer.CanSelect(v,g.SelectedPieces(v))){g.AddChange(v);rest-=v;}
            Check(rest==0&&g.SubmitChange(g.ChangeSelected)&&g.Ledger.Cash==cash+2100,"Till settles fixed custom quote with actual change");
            g.Tick(3);g.RequestClose();for(int n=0;n<10000&&g.Phase!=ShopPhase.Summary;n++){if(g.ActiveCustomer!=null)g.SkipCurrent();g.Tick(.1f);}g.NextDay();Check(g.Pricing.Price(0)==3000,"Prices survive next day");g.ResetDemo();Check(g.Pricing.Price(0)==2200,"New game restores catalogue prices");return new {passed,failed=0};
        }
        finally{g.ResetDemo();g.ManualTick=false;Object.Destroy(hand.gameObject);}
    }
    public static object Evening()
    {
        passed=0;var g=MvpShopController.Instance;var player=Object.FindAnyObjectByType<PlayerInteractor>();var position=player.transform.position;var hand=new GameObject("Night item hand").transform;hand.position=Vector3.one*1000;g.ManualTick=true;
        try
        {
            g.ResetDemo();g.ClosePanel();ShopCheckFixtures.Fund(g,50000);g.BuyExtraShelf();var a=g.AfterHours;var sky=RenderSettings.skybox;int cash=g.Ledger.Cash;
            Check(!a.Merchant.gameObject.activeSelf&&!a.OpenTrade()&&!a.BuyBundle(),"No night vendor trade during day");Check(!g.StayAfterHours(),"Cannot skip open shift into evening");CloseShift(g);int dayCash=g.Ledger.Cash;
            Check(g.StayAfterHours()&&g.Phase==ShopPhase.AfterHours&&g.Modal==ShopModal.None&&a.IsNight&&a.Merchant.gameObject.activeSelf,"Summary stay action enables actual night and vendor");
            Check(!a.OpenTrade(),"Cannot speak to vendor remotely");Move(player,new Vector3(-4.4f,0,3.35f));Check(a.OpenTrade()&&a.TradeOpen&&ShopInputGate.Blocked,"Nearby E trade locks movement and opens offer");
            Check(a.BuyBundle()&&a.BoughtTonight&&g.StockCount(ShopCatalog.Contraband)==5,"Buying produces five actual distinct cigarettes");Check(g.Ledger.Cash==dayCash-3500&&g.Drawer.Total==g.Ledger.Cash,"Night purchase paid exactly once from finite funds");int paid=g.Ledger.Cash;
            Check(!a.BuyBundle()&&g.Ledger.Cash==paid&&g.StockCount(ShopCatalog.Contraband)==5,"Repeated confirmation cannot duplicate purchase");a.CloseTrade();Check(!a.TradeOpen&&!ShopInputGate.Blocked,"Leaving conversation restores movement");
            var pack=a.PickupSlots[0].Occupant;Check(pack!=null&&pack.ProductId=="dnyipro"&&pack.DisplayName.Contains("ukrán"),"Night stock has its own product identity");
            Check(pack.PickUp(hand),"Pick up physical night purchase");var shelf=Object.FindObjectsByType<ProductSlot>().First(s=>s.name.StartsWith("Extra cigarette")&&s.Occupant==null);Physics.SyncTransforms();Check(pack.TryPlace(shelf),"Night cigarettes fit ordinary shop shelves");
            Check(!g.OrderSupply(ShopCatalog.Contraband,1)&&g.WarehouseCount(ShopCatalog.Contraband)==0,"Night brand cannot be ordered through legal warehouse");
            Check(g.Pricing.AvailableTypes.Contains(ShopCatalog.Contraband)&&g.Pricing.TrySetPrice(ShopCatalog.Contraband,1300),"Night brand can have a custom retail price");
            g.Tick(1000);Check(g.Spawned==0&&g.Ledger.Cash==paid,"Evening spawns no new customers and charges no repeated operating cost");g.RequestClose();Check(g.Phase==ShopPhase.Summary&&!a.IsNight&&!a.Merchant.gameObject.activeSelf&&RenderSettings.skybox==sky,"O returns to summary and restores lighting");
            Check(g.StayAfterHours()&&a.BoughtTonight,"Re-entering same evening does not reset vendor stock");g.ReturnToSummary();g.NextDay();Check(g.Day==2&&g.StockCount(ShopCatalog.Contraband)==5&&pack.CurrentSlot==shelf&&g.Pricing.Price(ShopCatalog.Contraband)==1300,"Next day preserves all paid goods and prices");
            CloseShift(g);g.StayAfterHours();Move(player,new Vector3(-4.4f,0,3.35f));Check(!a.BoughtTonight&&a.OpenTrade()&&!a.BuyBundle(),"New evening offer requires clearing previous pickup table");a.CloseTrade();g.ReturnToSummary();g.ResetDemo();Check(g.StockCount(ShopCatalog.Contraband)==0&&!a.IsNight&&!a.BoughtTonight,"New game clears night goods and offer");
            g.ClosePanel();g.Ledger.TryBuy(6000,1);g.SyncCashDrawer();CloseShift(g);g.StayAfterHours();Move(player,new Vector3(-4.4f,0,3.35f));Check(a.OpenTrade()&&!a.BuyBundle()&&g.StockCount(ShopCatalog.Contraband)==0,"Insolvent evening cannot create unpaid goods");
            return new {passed,failed=0};
        }
        finally{player.CancelHeld();g.ResetDemo();Move(player,position);g.ManualTick=false;Object.Destroy(hand.gameObject);}
    }
    public static object NightSale()
    {
        passed=0;var g=MvpShopController.Instance;var player=Object.FindAnyObjectByType<PlayerInteractor>();var position=player.transform.position;var hand=new GameObject("Night sale hand").transform;hand.position=Vector3.one*1000;g.ManualTick=true;
        try
        {
            g.ResetDemo();g.ClosePanel();ShopCheckFixtures.Fund(g,50000);g.BuyExtraShelf();CloseShift(g);g.StayAfterHours();Move(player,new Vector3(-4.4f,0,3.35f));g.AfterHours.OpenTrade();Check(g.AfterHours.BuyBundle(),"Acquire real night merchandise");g.AfterHours.CloseTrade();
            var packs=g.AfterHours.PickupSlots.Select(s=>s.Occupant).ToArray();foreach(var pack in packs){pack.PickUp(hand);var s=Object.FindObjectsByType<ProductSlot>().First(s=>s.name.StartsWith("Extra cigarette")&&s.Occupant==null);Physics.SyncTransforms();Check(pack.TryPlace(s),"Stock paid night pack");}
            g.ReturnToSummary();g.NextDay();g.ClosePanel();g.OpenShop();
            for(int n=0;n<10000&&!(g.ActiveCustomer!=null&&g.ActiveCustomer.Product==ShopCatalog.Contraband);n++){if(g.ActiveCustomer!=null)g.SkipCurrent();g.Tick(.1f);}var c=g.ActiveCustomer;Check(c!=null&&c.Product==ShopCatalog.Contraband,"Following day's real visitor requests night brand");
            foreach(int type in g.MissingOrderTypes){var item=Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId==ShopCatalog.Ids[type]&&p.CurrentSlot!=null&&p.CurrentSlot.IsStockShelf);item.PickUp(hand);Physics.SyncTransforms();Check(item.TryPlace(g.DeliverySlot),"Hand over full mixed night-brand order");}
            Check(g.Stage==ServiceStage.Payment,"Mixed night-brand basket ready");int price=c.TotalPrice,cash=g.Ledger.Cash,rest=c.Tender-price;foreach(int v in ShopCatalog.Denominations)while(rest>=v&&g.Drawer.CanSelect(v,g.SelectedPieces(v))){g.AddChange(v);rest-=v;}
            Check(rest==0&&g.SubmitChange(g.ChangeSelected)&&g.Ledger.Cash==cash+price&&g.StockCount(ShopCatalog.Contraband)==4,"Night merchandise can be sold once through normal checkout");return new {passed,failed=0};
        }
        finally{player.CancelHeld();g.ResetDemo();Move(player,position);g.ManualTick=false;Object.Destroy(hand.gameObject);}
    }
}
