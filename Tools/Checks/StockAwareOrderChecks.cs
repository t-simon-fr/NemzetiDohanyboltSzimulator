using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class StockAwareOrderChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool,string> check = (ok,name) => { if (!ok) throw new Exception(name); passed++; };
        var shop = MvpShopController.Instance;
        var player = Object.FindAnyObjectByType<PlayerInteractor>();
        Action clear = () => { foreach (var p in Object.FindObjectsByType<ShopProduct>()) p.RemoveFromWorld(); };
        Action<int> spawn = type => typeof(MvpShopController).GetMethod("SpawnCustomer",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(shop,new object[]{type,false});
        Action reset = () => { player.CancelHeld(); shop.ClosePanel(); shop.ResetDemo(); shop.ManualTick=true; };
        Action machine = () => { reset(); clear(); ShopCheckFixtures.Fund(shop,60000); shop.Activities.BuyCoffeeMachine(); };
        Action<int,int> ingredients = (coffee,milk) => {
            typeof(ShopActivities).GetProperty("CoffeeSupplies").SetValue(shop.Activities,coffee);
            typeof(ShopActivities).GetProperty("MilkSupplies").SetValue(shop.Activities,milk);
        };
        try
        {
            reset();
            int cash=shop.Ledger.Cash, units=Enumerable.Range(0,ShopCatalog.Ids.Length).Sum(shop.StockCount);
            check(Enumerable.Range(0,8).All(v=>shop.CreateCustomerOrder(6,v).All(t=>t<ShopCatalog.CigaretteCount)), "Never-bought energy drink and other unstocked types are never requested");
            check(shop.CreateCustomerOrder(0,2).SequenceEqual(new[]{0,1,2}), "Existing preferred basket remains unchanged when all its stock exists");
            reset(); ShopCheckFixtures.Fund(shop,100000); shop.TryExpand();
            shop.OrderSupply(5,2); shop.OrderSupply(6,2); ShopCheckFixtures.Tick(shop,8.1f);
            foreach(int type in new[]{5,6})for(int i=0;i<2;i++){ShopProduct p=null;shop.TryCreateSupply(type,out p);p.AssignInitialSlot(Object.FindObjectsByType<ProductSlot>().First(s=>s.name.StartsWith("Public slot")&&s.PreferredProductId==p.ProductId&&s.CanAcceptStock(p.ProductId)));}
            check(shop.CreateCustomerOrder(5,2,3).SequenceEqual(shop.CreateCustomerOrder(5,2,3)),"Same seed repeats the same personal basket");
            check(Enumerable.Range(0,12).Any(v=>!shop.CreateCustomerOrder(5,v,0).SequenceEqual(shop.CreateCustomerOrder(5,v,3))),"Persons shop differently from the same shelf");
            check(Enumerable.Range(0,24).All(v=>{var o=shop.CreateCustomerOrder(5,v,v%4);return o.Skip(1).All(t=>shop.Layout.FindPublicStock(t)!=null);}),"Extra lines are always publicly pickable");
            reset();
            check(ShopPeople.Order(0,new[]{0},2,1)==ShopPeople.Order(0,new[]{0},2,1),"Same visit repeats the same sentence");
            check(Enumerable.Range(0,12).Select(v=>ShopPeople.Order(1,new[]{0},v,1)).Distinct().Count()>1,"Visits vary the spoken order");
            check(Enumerable.Range(0,12).Select(v=>ShopPeople.Thank(2,v,1)).Distinct().Count()>1,"Visits vary the spoken thanks");
            check(shop.Ledger.Cash==cash && Enumerable.Range(0,ShopCatalog.Ids.Length).Sum(shop.StockCount)==units, "Choosing orders consumes no real cash or stock");
            var last=Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId==ShopCatalog.Ids[0]);
            foreach(var p in Object.FindObjectsByType<ShopProduct>().Where(p=>p!=last))p.RemoveFromWorld();
            check(shop.CreateCustomerOrder(6,2).SequenceEqual(new[]{0}), "Basket fallback never requests more units than currently exist");
            shop.SingleProductVisits=true; shop.OpenShop();
            spawn(0); spawn(0); spawn(0);
            check(shop.QueueCount==3 && shop.Spawned==3 && shop.StockCount(0)==1, "Three waiting customers may request the same last unit without reserving or consuming it");
            var customers=Object.FindObjectsByType<MvpCustomerActor>().Where(c=>c.gameObject.activeSelf).ToArray();
            check(customers.Length==3 && customers.All(c=>c.OrderTypes.SequenceEqual(new[]{0})), "Queued requests all retain the genuinely stocked product");
            foreach(var customer in customers)typeof(MvpCustomerActor).GetProperty("UsesCard").SetValue(customer,true);
            for(int tick=0;tick<1500&&shop.ActiveCustomer==null;tick++)shop.Tick(.1f);
            check(shop.ActiveCustomer!=null && !shop.HasUnavailableRequest, "First queued customer reaches checkout with the last unit available");
            check(last.AssignInitialSlot(shop.DeliverySlot), "Deliver the last actual unit");
            shop.DeliverySlot.NotifyProductPlaced(last);
            check(shop.Stage==ServiceStage.Payment, "Real delivery starts ordinary payment");
            foreach(char digit in shop.ActiveCustomer.TotalPrice.ToString())shop.EnterCardDigit(digit-'0');
            check(shop.ConfirmCardPayment() && shop.StockCount(0)==0 && shop.Ledger.Sales==1, "First sale consumes the last unit exactly once");
            for(int tick=0;tick<1500&&(shop.ActiveCustomer==null||shop.Stage==ServiceStage.Thanks);tick++)shop.Tick(.1f);
            check(shop.ActiveCustomer!=null && shop.ActiveCustomer.OrderTypes[0]==0 && shop.HasUnavailableRequest, "Product may genuinely run out before a waiting customer reaches the till");
            check(shop.CreateCustomerOrder(0,2).Length==0, "New arrivals cannot order the unit already sold, while older queued requests remain");

            reset(); clear(); shop.OpenShop(); shop.Tick(30);
            check(shop.QueueCount==0 && shop.Spawned==0 && shop.Missed==0 && shop.Phase==ShopPhase.Open, "Completely empty shop produces no impossible visitor or artificial missed sale");
            shop.RequestClose(); shop.Tick(.1f);
            check(shop.CanEndDay, "Empty shop remains manually closable");
            reset(); clear();
            check(shop.OrderSupply(0,3), "Buy real replenishment");
            check(shop.CreateCustomerOrder(0,2).Length==0, "Pending shipment is not on-hand stock");
            shop.Tick(8.1f);
            check(shop.CreateCustomerOrder(0,2).SequenceEqual(new[]{0,0,0}), "Arrived warehouse carton supports exact repeated-product quantity");
            shop.OpenShop(); spawn(0);
            for(int tick=0;tick<1500&&shop.ActiveCustomer==null;tick++)shop.Tick(.1f);
            check(shop.ActiveCustomer!=null && !shop.HasUnavailableRequest, "Warehouse goods are requestable and do not show a false shortage");

            reset(); clear(); ShopCheckFixtures.Fund(shop,100000); shop.TryExpand();
            check(shop.CreateCustomerOrder(6,2).Length==0, "Expansion alone does not make never-bought Hamster available");
            check(shop.OrderSupply(6,1), "Buy first energy drink");
            check(!shop.CreateCustomerOrder(6,2).Contains(6), "Energy is not available while its first shipment is pending");
            shop.Tick(8.1f);
            check(shop.CreateCustomerOrder(6,2).SequenceEqual(new[]{6}), "Only the genuinely bought energy drink is offered after delivery");

            machine();
            check(shop.CreateCustomerOrder(ShopCatalog.Latte,2).Length==0, "Coffee machine without coffee or milk cannot create demand");
            check(shop.Activities.OrderCoffeeSupplies(), "Order physical coffee ingredients");
            check(shop.CreateCustomerOrder(ShopCatalog.Coffee,2).Length==0, "Pending coffee ingredients do not enable coffee requests");
            shop.Tick(8.1f);
            check(shop.CreateCustomerOrder(ShopCatalog.Coffee,2).All(ShopCatalog.IsCoffee) && shop.CreateCustomerOrder(ShopCatalog.Coffee,2).Length==3, "Delivered refillable ingredient package enables real coffee recipes");
            check(!shop.CreateCustomerOrder(ShopCatalog.Latte,2).Contains(ShopCatalog.Latte), "Latte is absent without milk, even with coffee available");
            machine(); ingredients(1,0);
            check(shop.CreateCustomerOrder(ShopCatalog.Latte,2).SequenceEqual(new[]{ShopCatalog.Coffee}), "One shared coffee serving cannot be promised as several different recipes in one order");
            ingredients(1,1);
            check(shop.CreateCustomerOrder(ShopCatalog.Latte,2).SequenceEqual(new[]{ShopCatalog.Latte}), "Single latte consumes both ingredients only in the temporary planning budget");
            check(shop.Activities.CoffeeSupplies==1 && shop.Activities.MilkSupplies==1, "Planning leaves real machine ingredients untouched");
            shop.Activities.SelectRecipe(ShopCatalog.Latte);
            check(shop.Activities.BrewCoffee(), "Start the last real latte");
            check(shop.CreateCustomerOrder(ShopCatalog.Latte,2).SequenceEqual(new[]{ShopCatalog.Latte}), "Already brewing final cup remains requestable");
            shop.Activities.Tick(4);
            check(shop.CreateCustomerOrder(ShopCatalog.Latte,2).SequenceEqual(new[]{ShopCatalog.Latte}), "Prepared last cup remains requestable without more ingredients");
            check(shop.Activities.CoffeeSupplies==0 && shop.Activities.MilkSupplies==0, "Prepared cup demand does not invent new ingredients");
            return new { passed, failed=0 };
        }
        finally { reset(); shop.ManualTick=false; }
    }
}
