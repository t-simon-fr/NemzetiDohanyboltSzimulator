using System;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

public static class FreeShelfChecks
{
    private static int passed;
    private static void Check(bool valid,string text){if(!valid)throw new InvalidOperationException(text);passed++;}
    public static object AllProductsAllShelves()
    {
        passed=0;var game=MvpShopController.Instance;var player=Object.FindAnyObjectByType<PlayerInteractor>();var hand=new GameObject("Free shelf check hand").transform;hand.position=Vector3.one*1000;game.ManualTick=true;
        try
        {
            ShopCheckFixtures.Reset(game);game.ClosePanel();Check(!Object.FindObjectsByType<ProductSlot>(FindObjectsInactive.Include).First(s=>s.name.StartsWith("Public slot")).CanAcceptStock("cig_red"),"Inactive expansion shelf rejects stocking");
            ShopCheckFixtures.Fund(game,150000);Check(game.TryExpand()&&game.BuyExtraShelf(),"Unlock all three shelf families");
            foreach(var item in Object.FindObjectsByType<ShopProduct>())item.RemoveFromWorld();
            var shelves=Object.FindObjectsByType<ProductSlot>().Where(s=>s.IsStockShelf).ToArray();
            Check(shelves.Length==71,"32 starter, 30 public and nine extra shelf cells");
            for(int type=0;type<ShopCatalog.Count;type++)
            {
                Check(game.OrderSupply(type,1),"Buy actual stock "+type);ShopCheckFixtures.Tick(game,8.1f);Check(game.TryCreateSupply(type,out var item),"Receive finite product "+type);
                foreach(var slot in shelves)
                {
                    Check(item.PickUp(hand),"Pick product for shelf transfer");Physics.SyncTransforms();
                    Check(item.TryPlace(slot),"Product "+type+" fits real shelf "+slot.name);
                    Check(slot.Occupant==item&&item.CurrentSlot==slot&&!item.IsHeld&&game.StockCount(type)==1,"Transfer preserves actual product and stock count");
                }
                Check(player.TryHold(item)&&game.ReturnSupply(player,type),"Return relocated product to its own brand carton");
                foreach(string prefix in new[]{"Cigarette slot","Public slot","Extra cigarette"})
                {
                    var slot=shelves.First(s=>s.name.StartsWith(prefix));var carton=game.Warehouse.Carton(type);Check(player.TryHoldCarton(carton),"Carry finite case");Physics.SyncTransforms();
                    Check(carton.TryPlaceOne(slot)&&slot.Occupant.ProductId==ShopCatalog.Ids[type],"Case can stock product "+type+" in "+prefix);
                    Check(carton.Quantity==0&&game.WarehouseCount(type)==0&&game.StockCount(type)==1,"Case stocking conserves paid unit");player.ReturnCarton();
                    Check(player.TryHold(slot.Occupant)&&game.ReturnSupply(player,type),"Recover cross-stocked unit without duplicating inventory");
                }
            }
            Check(game.Activities.BuyCoffeeMachine()&&game.Activities.OrderCoffeeSupplies(),"Buy prepared coffee supplies");ShopCheckFixtures.Tick(game,8.1f);Check(game.Activities.BrewCoffee(),"Brew real cup");ShopCheckFixtures.Tick(game,3.6f);
            var coffee=game.Activities.CoffeeOutput.Occupant;
            foreach(var slot in shelves){Check(coffee.PickUp(hand),"Pick coffee for arbitrary shelf");Physics.SyncTransforms();Check(coffee.TryPlace(slot),"Prepared coffee fits "+slot.name);}
            Check(!GameObject.Find("Counter Slot 1").GetComponent<ProductSlot>().CanAcceptStock("water"),"Serving tray is not a carton restocking shortcut");
            Check(!game.Activities.CoffeeOutput.CanAcceptStock("beer"),"Coffee machine output remains a dedicated preparation station");
            var quantities=Enumerable.Range(0,ShopCatalog.Count).Select(game.WarehouseCount).ToArray();game.OpenShop();game.RequestClose();ShopCheckFixtures.Tick(game,.1f);game.NextDay();
            Check(coffee.CurrentSlot!=null&&coffee.CurrentSlot.Occupant==coffee&&Enumerable.Range(0,ShopCatalog.Count).All(i=>quantities[i]==game.WarehouseCount(i)),"Day change preserves rearranged shelves and warehouse stock");
            ShopCheckFixtures.Reset(game);game.ClosePanel();Check(Enumerable.Range(0,4).All(i=>game.StockCount(i)==8)&&!game.Layout.Expanded,"New game restores original starter stock and locks extension");
            return new {passed,failed=0,shelfCells=shelves.Length,packagedTypes=ShopCatalog.Count,preparedTypes=1};
        }
        finally{player.CancelHeld();ShopCheckFixtures.Reset(game);game.ManualTick=false;Object.Destroy(hand.gameObject);}
    }
    public static object MixedSelfService()
    {
        passed=0;var game=MvpShopController.Instance;var hand=new GameObject("Mixed shelf hand").transform;hand.position=Vector3.one*1000;game.ManualTick=true;
        try
        {
            ShopCheckFixtures.Reset(game);game.ClosePanel();ShopCheckFixtures.Fund(game,100000);Check(game.TryExpand(),"Expand mixed shelf scenario");
            var slots=Object.FindObjectsByType<ProductSlot>().Where(s=>s.name.StartsWith("Public slot")&&s.PreferredProductId=="water").ToArray();
            var pack=Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId=="cig_red");Check(pack.PickUp(hand),"Pick cigarette from staff rack");Physics.SyncTransforms();Check(pack.TryPlace(slots[0]),"Place cigarette on former water shelf");
            Check(game.OrderSupply(8,1),"Order beer for mixed shelf");ShopCheckFixtures.Tick(game,8.1f);game.TryCreateSupply(8,out var beer);Check(beer.PickUp(hand),"Pick beer");Physics.SyncTransforms();Check(beer.TryPlace(slots[1]),"Place beer next to cigarette");
            Check(game.Layout.FindPublicStock(0)==pack&&game.Layout.FindPublicStock(8)==beer&&game.Layout.FindPublicStock(4)==null,"Public selection uses actual products instead of original row types");
            Check(game.Layout.IsPublicProduct(0),"Cigarettes on public shelves become self-service stock");
            var label=Object.FindObjectsByType<ShopShelfPriceLabel>().First(l=>l.transform.parent==slots[0].transform.parent&&Mathf.Abs(l.transform.localPosition.y-slots[0].transform.localPosition.y+.09f)<.02f);label.Refresh();
            Check(label.GetComponent<TextMesh>().text.StartsWith("VEGYES POLC"),"Mixed row does not show misleading single-product price");
            var source=pack.CurrentSlot;game.OpenShop();
            for(int tick=0;tick<1200&&game.Stage!=ServiceStage.Payment;tick++)ShopCheckFixtures.Tick(game,.1f);
            Check(game.ActiveCustomer!=null&&game.ActiveCustomer.Product==0&&game.ActiveCustomer.BasketItem==pack&&pack.CurrentSlot==game.Activities.BasketSlot,"Real customer collects the requested cigarette from its new location and places it on counter");
            game.SkipCurrent();Check(pack.CurrentSlot==source&&source.Occupant==pack,"Cancelled mixed-shelf purchase returns to the actual source slot");
            // A drink on the staff rack is delivered by the cashier; shoppers never cross behind the counter.
            ShopCheckFixtures.Reset(game);game.ClosePanel();ShopCheckFixtures.Fund(game,100000);game.TryExpand();
            var starter=Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId=="cig_red");var staffSlot=starter.CurrentSlot;starter.RemoveFromWorld();
            game.OrderSupply(5,1);ShopCheckFixtures.Tick(game,8.1f);game.TryCreateSupply(5,out var cola);cola.AssignInitialSlot(staffSlot);game.OpenShop();
            for(int tick=0;tick<2200&&!(game.ActiveCustomer!=null&&game.ActiveCustomer.Product==5);tick++){if(game.ActiveCustomer!=null)game.SkipCurrent();ShopCheckFixtures.Tick(game,.1f);}
            Check(game.ActiveCustomer!=null&&game.ActiveCustomer.Product==5&&game.ActiveCustomer.BasketItem==null&&game.Stage==ServiceStage.Order,"Drink stored behind counter waits for cashier delivery");
            Check(cola.PickUp(hand),"Pick drink from former cigarette cell");Physics.SyncTransforms();Check(cola.TryPlace(game.DeliverySlot)&&game.Stage==ServiceStage.Payment,"Correct drink from any staff shelf begins normal payment");
            return new {passed,failed=0};
        }
        finally{ShopCheckFixtures.Reset(game);game.ManualTick=false;Object.Destroy(hand.gameObject);}
    }
}
