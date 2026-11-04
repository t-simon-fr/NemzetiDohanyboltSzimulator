using System;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;
public static class NextFeatureChecks
{
    static int passed;
    static void Check(bool ok,string name){if(!ok)throw new InvalidOperationException(name);passed++;}
    public static object WorldAndCoffee()
    {
        passed=0;var g=MvpShopController.Instance;var player=Object.FindAnyObjectByType<PlayerInteractor>();g.ManualTick=true;
        try
        {
            g.ResetDemo();g.ClosePanel();var u=g.GetComponent<ShopNextUpgrades>();Check(u!=null,"Upgrade system exists");
            Check(!u.BuyFridge(),"Fridge needs expanded shop");ShopCheckFixtures.Fund(g,150000);Check(g.TryExpand(),"Expand");u.RefreshLayout();
            Check(GameObject.Find("Expansion L Counter")==null,"Expansion preserves straight left-wall counter");Check(u.BuyFridge()&&!u.BuyFridge(),"Fridge purchased once");
            Check(g.Drawer.Total==g.Ledger.Cash,"Fridge and expansion conserve cash");Check(Object.FindObjectsByType<ProductSlot>().Count(s=>s.name.StartsWith("Fridge slot"))==16,"Sixteen usable fridge slots");
            Check(g.Activities.BuyCoffeeMachine(),"Buy separate coffee machine");Check(g.Activities.OrderCoffeeSupplies()&&g.Activities.OrderMilkSupplies(),"Order coffee and milk independently");g.Tick(8.1f);
            Check(g.Activities.CoffeeSupplies==0&&g.Activities.MilkSupplies==0,"Delivery does not automatically fill machine");
            var packs=Object.FindObjectsByType<ShopIngredientPackage>();Check(packs.Length==2,"Two physical supply packages arrive");
            foreach(var pack in packs){Check(player.TryHoldTool(pack.GetComponent<ShopHandTool>()),"Pick real supply package");g.Activities.UseCoffeeMachine();Check(!player.HasHeldItem,"Refill consumes held package");}
            Check(g.Activities.CoffeeSupplies==10&&g.Activities.MilkSupplies==10,"Machine receives exact paid ingredients");
            for(int type=ShopCatalog.Coffee;type<=ShopCatalog.Latte;type++)
            {
                g.Activities.SelectRecipe(type);Check(g.Activities.BrewCoffee(),"Brew selected recipe "+type);g.Tick(3.6f);
                var cup=g.Activities.CoffeeOutput.Occupant;Check(cup!=null&&cup.ProductId==ShopCatalog.Ids[type],"Correct physical cup recipe");
                Check(player.TryHold(cup)&&player.DropHeld(),"Finished coffee can be picked up and dropped");
            }
            Check(g.Activities.CoffeeSupplies==7&&g.Activities.MilkSupplies==9,"Three recipes use three coffees and one milk");
            Physics.SyncTransforms();Check(Physics.Raycast(new Vector3(.1f,2,-5.5f),Vector3.down,out var ground,5)&&ground.collider.name=="Entrance pavement","Dropped entrance goods have real pavement below");
            g.OpenShop();g.RequestClose();g.Tick(.1f);g.NextDay();Check(u.FridgeOwned&&g.Layout.Expanded,"Developments survive day change");
            g.ResetDemo();g.ClosePanel();Check(!u.FridgeOwned&&g.Activities.CoffeeSupplies==0&&g.Activities.MilkSupplies==0,"New game resets development and recipes");return new {passed,failed=0};
        }
        finally{player.CancelHeld();g.ResetDemo();g.ManualTick=false;}
    }
    public static object BasketDay()
    {
        passed=0;var g=MvpShopController.Instance;var hand=new GameObject("Basket test hand").transform;hand.position=Vector3.one*1000;g.ManualTick=true;int multiple=0;
        try
        {
            g.ResetDemo();g.ClosePanel();ShopCheckFixtures.Fund(g,150000);Check(g.TryExpand(),"Expand for mixed orders");
            for(int type=4;type<ShopCatalog.Count;type++)Check(g.OrderSupply(type,6),"Order drink stock");g.Tick(8.1f);
            for(int type=4;type<ShopCatalog.Count;type++)for(int n=0;n<6;n++){g.TryCreateSupply(type,out var item);var slot=Object.FindObjectsByType<ProductSlot>().First(s=>s.name.StartsWith("Public slot")&&s.PreferredProductId==ShopCatalog.Ids[type]&&s.CanAcceptStock(item.ProductId));item.AssignInitialSlot(slot);}
            g.OpenShop();
            for(int tick=0;tick<23000&&g.Phase!=ShopPhase.Summary;tick++)
            {
                g.Tick(.1f);
                if (g.Spawned >= g.TotalVisitors) g.RequestClose();
                if (g.CanEndDay)
                {
                    var player = Object.FindAnyObjectByType<PlayerInteractor>();
                    var body = player.GetComponent<CharacterController>();
                    body.enabled = false;
                    player.transform.position = g.Layout.EntryInside;
                    body.enabled = true;
                    g.TryEndDay();
                }
                var c=g.ActiveCustomer;if(c==null||g.Stage==ServiceStage.Thanks)continue;
                if(g.Stage==ServiceStage.Order)
                {
                    foreach(int type in g.MissingOrderTypes)
                    {
                        var item=Object.FindObjectsByType<ShopProduct>().FirstOrDefault(p=>p.ProductId==ShopCatalog.Ids[type]&&!p.IsHeld&&p.CurrentSlot!=null&&p.CurrentSlot.IsStockShelf);
                        if(item==null)continue;
                        Check(item.PickUp(hand),"Pick requested basket item");Physics.SyncTransforms();if(!item.TryPlace(g.DeliverySlot)){var size=ProductVisuals.Size(type);var pose=g.DeliverySlot.GetPose(size*.5f,Vector3.zero);var blockers=Physics.OverlapBox(pose.position,size*.49f,pose.rotation,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);throw new InvalidOperationException("Delivery failed for "+type+" order "+c.OrderDescription+"; blockers="+string.Join(",",blockers.Select(b=>b.name)));}Check(true,"Deliver requested basket item");if(g.Stage==ServiceStage.Payment)break;
                    }
                }
                if(g.Stage!=ServiceStage.Payment)continue;
                int cash=g.Ledger.AvailableFunds,total=c.TotalPrice;if(c.OrderTypes.Length>1)multiple++;
                if(g.CardPayment){foreach(char digit in c.TotalPrice.ToString())g.EnterCardDigit(digit-'0');Check(g.ConfirmCardPayment(),"Pay complete basket by card");}
                else if(g.CoinPayment)
                {
                    var offer=g.Activities.CoinOffer;int rest=offer.Price;foreach(int i in Enumerable.Range(0,offer.Coins.Count).OrderByDescending(i=>offer.Coins[i]))if(offer.Coins[i]<=rest){g.ToggleCustomerCoin(i);rest-=offer.Coins[i];}Check(g.ConfirmCustomerCoins(),"Pay exact basket in coins");
                }
                else
                {
                    int due=c.Tender-total;for(int attempt=0;attempt<80;attempt++){g.ClearChange();int rest=due;foreach(int value in ShopCatalog.Denominations){int count=Math.Min(rest/value,g.Drawer.Count(value));for(int n=0;n<count;n++)g.AddChange(value);rest-=value*count;}if(rest==0)break;g.ExchangeCash();}Check(g.SubmitChange(g.ChangeSelected),"Pay complete basket once");
                }
                Check(g.Ledger.AvailableFunds==cash+total&&g.Drawer.Total==g.Ledger.Cash,"One payment charges aggregate basket total");Check(!g.SubmitChange(0),"Basket cannot be charged twice");
            }
            Check(g.Phase==ShopPhase.Summary&&g.Missed==0,"Complete actual multi-item shift");Check(multiple>=3,"Several customers bought multiple items");return new {passed,failed=0,multiple};
        }
        finally{g.ResetDemo();g.ManualTick=false;Object.Destroy(hand.gameObject);}
    }
    public static object RecoveryAndCancellation()
    {
        passed=0;var g=MvpShopController.Instance;var hand=new GameObject("Cancel basket hand").transform;hand.position=Vector3.one*1000;g.ManualTick=true;
        try
        {
            g.ResetDemo();g.ClosePanel();ShopCheckFixtures.Fund(g,100000);g.TryExpand();
            foreach(int type in new[]{5,6}){g.OrderSupply(type,2);g.Tick(8.1f);for(int i=0;i<2;i++){g.TryCreateSupply(type,out var p);p.AssignInitialSlot(Object.FindObjectsByType<ProductSlot>().First(s=>s.name.StartsWith("Public slot")&&s.PreferredProductId==p.ProductId&&s.CanAcceptStock(p.ProductId)));}}
            int units=Enumerable.Range(0,ShopCatalog.Count).Sum(g.StockCount);g.OpenShop();
            for(int n=0;n<2000&&!(g.ActiveCustomer!=null&&g.ActiveCustomer.OrderTypes.Length>1&&g.Stage==ServiceStage.Payment);n++){if(g.ActiveCustomer!=null&&g.ActiveCustomer.OrderTypes.Length==1)g.SkipCurrent();g.Tick(.1f);}
            Check(g.ActiveCustomer!=null&&g.Stage==ServiceStage.Payment&&g.ActiveCustomer.ExtraItems.Count>0,"NPC brings several real public-shelf products to checkout");
            int cash=g.Ledger.Cash;var item=g.ActiveCustomer.BasketItem;Check(item.PickUp(hand),"Recover unpaid item from basket");g.Tick(.1f);
            Check(g.Stage==ServiceStage.Order&&g.Drawer.PendingTender==0&&g.MissingOrderTypes.Length==1,"Removing basket item cancels tender and marks exact missing product");
            Physics.SyncTransforms();Check(item.TryPlace(g.DeliverySlot)&&g.Stage==ServiceStage.Payment,"Re-delivery restores complete basket payment");
            g.SkipCurrent();Check(g.Ledger.Cash==cash&&g.Drawer.Total==cash,"Cancelling complete basket charges nothing");
            Check(Enumerable.Range(0,ShopCatalog.Count).Sum(g.StockCount)==units,"Cancelled basket retains every real unit");
            Check(!Object.FindObjectsByType<ProductSlot>().Any(s=>s.name.StartsWith("Order receipt")),"Cancelled receipt slots are deactivated and cleaned up");
            var loose=Object.FindObjectsByType<ShopProduct>().First();Check(loose.PickUp(hand),"Pick item for outside recovery");loose.Drop(new Vector3(0,-8,-5),Vector3.down);
            g.GetComponent<ShopDroppedItemRecovery>().RecoverNow();Check(loose.transform.position.y>0&&loose.gameObject.activeSelf,"Out-of-bounds goods return to reachable entrance ground");
            return new {passed,failed=0};
        }
        finally{g.ResetDemo();g.ManualTick=false;Object.Destroy(hand.gameObject);}
    }

}
