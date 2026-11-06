using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public static class FeatureBatchChecks
{
    public static object Run()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var g=MvpShopController.Instance;
        var player=Object.FindAnyObjectByType<PlayerInteractor>();
        var position=player.transform.position;
        var body=player.GetComponent<CharacterController>();
        try
        {
            player.CancelHeld();g.ResetDemo();g.ClosePanel();g.ManualTick=true;
            g.Tick(30);check(g.ClockElapsed==0 && ShopUiState.Clock(g.ClockElapsed)=="08:00","Preparation does not advance the clock");
            g.OpenShop();g.Tick(30);check(ShopUiState.Clock(g.ClockElapsed)=="08:30","One real second maps to one game minute after opening");
            g.RequestClose();g.Tick(15);check(ShopUiState.Clock(g.ClockElapsed)=="08:45","Clock continues after manual closing");
            check(ShopUiState.Clock(410)=="14:50" && ShopUiState.Clock(-10)=="08:00","Clock uses 24-hour format with valid leading zeroes");
            g.ResetDemo();g.ClosePanel();ShopCheckFixtures.Fund(g,100000);
            int cash=g.Ledger.Cash;
            g.Devices.SetCartQuantity(0,2);g.Devices.SetCartQuantity(1,1);
            check(g.Ledger.Cash==cash && g.PendingQuantity(0)==0,"Editing cart does not buy products");
            check(g.Devices.SubmitCart() && g.Ledger.Cash==cash-4600 && g.Drawer.Total==g.Ledger.Cash,"Mixed cart charges exact total once");
            check(g.PendingQuantity(0)==2 && g.PendingQuantity(1)==1 && g.Devices.CartQuantity(0)==0,"All cart lines submitted together and cleared");
            cash=g.Ledger.Cash;
            check(!g.Devices.SubmitCart() && g.Ledger.Cash==cash,"Repeat submission of empty cart cannot charge twice");
            var invalid=new int[ShopCatalog.Count];invalid[0]=1;invalid[1]=-1;
            check(!g.OrderBasket(invalid)&&g.PendingQuantity(0)==2&&g.Ledger.Cash==cash,"Invalid later row rejects entire cart before mutation");
            g.ResetDemo();g.ClosePanel();cash=g.Ledger.Cash;
            var expensive=new int[ShopCatalog.Count];expensive[0]=4;expensive[1]=4;
            check(!g.OrderBasket(expensive)&&g.PendingQuantity(0)==0&&g.PendingQuantity(1)==0&&g.Ledger.Cash==cash,"Insufficient cash leaves every order line unchanged");
            var offers=Enumerable.Range(1,60).Select(day=>ShopAfterHours.QuantityForEvening(1234,day)).ToArray();
            check(offers.Contains(0)&&offers.Any(q=>q>0)&&offers.Distinct().Count()>3&&offers.All(q=>q>=0&&q<=10),"Night schedule includes absent evenings and bounded varying amounts");
            // The Editor pipeline can run multiple synchronous suites before an end-of-frame.
            // ResetDemo has already deactivated this old fixture and scheduled Destroy;
            // finish that test-only cleanup before asserting a new evening purchase.
            var staleBox = g.AfterHours.DeliveryBox;
            if (staleBox != null && !staleBox.gameObject.activeSelf) Object.DestroyImmediate(staleBox.gameObject);
            ShopCheckFixtures.Fund(g,50000);
            typeof(MvpShopController).GetProperty("Phase").SetValue(g,ShopPhase.AfterHours);g.AfterHours.Enter();
            body.enabled=false;player.transform.position=g.AfterHours.Merchant.position+Vector3.right;body.enabled=true;
            int quantity=g.AfterHours.OfferedQuantity;cash=g.Ledger.Cash;
            bool tradeOpened = g.AfterHours.OpenTrade();
            check(tradeOpened && g.AfterHours.BuyBundle(), "First evening offer creates a paid physical box; trade=" + tradeOpened + "; oldBox=" + (g.AfterHours.DeliveryBox != null) + "; activeBox=" + (g.AfterHours.DeliveryBox != null && g.AfterHours.DeliveryBox.gameObject.activeSelf) + "; quantity=" + quantity + "; funds=" + g.Ledger.AvailableFunds + "; message=" + g.Message);
            check(g.Ledger.Cash==cash-quantity*700 && g.StockCount(ShopCatalog.Contraband)==quantity,"Night quantity and exact purchase money agree");
            check(!g.AfterHours.BuyBundle(),"An evening cannot be purchased twice");g.AfterHours.CloseTrade();
            var box=g.AfterHours.DeliveryBox;
            check(player.TryHoldTool(box.GetComponent<ShopHandTool>()),"Whole paid delivery is carried as one item");
            var blockers=g.CounterStorage.Slots.Select(slot=>{var product=g.CreateNightProduct();product.AssignInitialSlot(slot);return product;}).ToArray();
            check(!box.Unpack(g.CounterStorage)&&box.Quantity==quantity,"Full private storage rejects the box without losing its contents");
            foreach(var product in blockers)product.RemoveFromWorld();
            check(box.Unpack(g.CounterStorage)&&box.Quantity==0,"Delivery unpacks into free private shelf places");
            player.ConsumeHeldTool();
            check(g.CounterStorage.Slots.Count(slot=>slot.Occupant!=null)==quantity&&g.StockCount(ShopCatalog.Contraband)==quantity,"Unpacking preserves exact paid stock");
            check(g.CounterStorage.Slots.Select(slot=>slot.transform.position.y).Distinct().Count()==2,"Private storage has two physical shelf rows");
            check(g.Activities.Mop.DisplayName=="Seprű"&&g.Activities.Mop.transform.position.x< -2.5f,"Broom is stored in the warehouse");
            check(g.Activities.Bat.transform.position.z< -4 && g.Activities.Bat.GetComponentInChildren<MeshFilter>()!=null,"Bat has a modeled staff-side wall pickup");
            g.ResetDemo();check(g.ClockElapsed==0&&g.StockCount(ShopCatalog.Contraband)==0,"New game resets clock and boxed stock");
            return new{passed,failed=0};
        }
        finally
        {
            player.CancelHeld();g.ClosePanel();g.ResetDemo();g.ManualTick=false;
            body.enabled=false;player.transform.position=position;body.enabled=true;
        }
    }
}
