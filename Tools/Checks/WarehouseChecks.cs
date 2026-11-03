using System;
using System.Linq;
using UnityEngine;

public static class WarehouseChecks
{
    private static int checks;
    private static int Water => ShopCatalog.Index("water");
    private static MvpShopController game;
    private static PlayerInteractor player;
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks++;}
    private static void PutOnShelf(int type)
    {
        var slot=UnityEngine.Object.FindObjectsByType<ProductSlot>().First(s=>s.Occupant==null&&s.isActiveAndEnabled&&s.CanAcceptStock(ShopCatalog.Ids[player.HeldCarton.ProductType])&&s.name.StartsWith(ShopCatalog.IsCigarette(type)?"Cigarette slot":"Public slot"));
        var point=slot.HintCenter;Physics.SyncTransforms();player.RefreshTarget(new Ray(point-slot.transform.forward*1.1f,slot.transform.forward));
        Require(player.TargetSlot==slot&&player.TryPlaceHeld(),"Carry unit from brand carton onto actual fixed shop shelf");
    }
    private static WarehouseCarton EmptyCarton(int type)
    {
        Require(game.OrderSupply(type),"Order goods for brand carton");game.Tick(8.1f);
        var carton=game.Warehouse.Carton(type);Require(carton!=null&&carton.Quantity==3,"Shipment creates its branded physical carton");
        var units=UnityEngine.Object.FindObjectsByType<ShopProduct>().Where(p=>p.ProductId==ShopCatalog.Ids[type]&&!p.IsHeld).Take(3).ToArray();
        foreach(var item in units)item.RemoveFromWorld();
        carton.GetComponent<ShopStation>().Use(player);Require(player.HeldCarton==carton&&!player.HasHeldProduct,"Pick the filled branded carton, rather than a single unit");
        for(int i=0;i<3;i++)PutOnShelf(type);
        player.ReturnCarton();
        Require(carton.Quantity==0,"Unpacking all units leaves a real empty carton");return carton;
    }
    public static object InventoryAndTrash()
    {
        checks=0;game=MvpShopController.Instance;player=UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();game.ManualTick=true;game.ResetDemo();game.ClosePanel();
        var area=game.Warehouse;var bin=UnityEngine.Object.FindAnyObjectByType<WarehouseBin>();
        try
        {
            Require(area!=null&&Enumerable.Range(0,ShopCatalog.Count).Select(i=>area.Dock(i)).Distinct().Count()==ShopCatalog.Count,"Seven separate labelled brand docks");
            Require(Enumerable.Range(0,ShopCatalog.Count).All(i=>area.Carton(i)==null),"No goods or empty cartons are created before ordering");
            area.Dock(Water).GetComponent<ShopStation>().Use(player);Require(!player.HasHeldItem&&game.WarehouseCount(Water)==0,"Locked brands cannot be unpacked before expansion");
            var carton=EmptyCarton(0);
            Require(player.TryHoldCarton(carton)&&player.HeldCarton==carton&&!player.HasHeldProduct&&player.HasHeldItem,"Empty carton can be carried with both hands free of products");
            Require(area.Carton(0)==null&&carton.Quantity==0,"Carried empty packaging no longer occupies delivery dock");
            area.Dock(1).GetComponent<ShopStation>().Use(player);Require(player.HeldCarton==carton&&area.Carton(0)==null,"Empty carton return also respects brand dock");
            area.Dock(0).GetComponent<ShopStation>().Use(player);Require(area.Carton(0)==carton&&!player.HasHeldItem,"Empty carton can be brought back into storage");
            Require(player.TryHoldCarton(carton)&&bin.Use(player)&&!player.HasHeldItem&&area.DiscardedCartons==1,"Rear paper dumpster accepts carried empty carton");
            Require(!bin.Use(player)&&area.DiscardedCartons==1,"Empty-hand interaction cannot duplicate disposal");
            game.OpenShop();game.RequestClose();game.Tick(.1f);game.NextDay();
            Require(game.Day==2&&area.DiscardedCartons==1&&area.Carton(0)==null,"Next day retains storage and removed packaging");
            game.ResetDemo();game.ClosePanel();Require(area.DiscardedCartons==0&&Enumerable.Range(0,ShopCatalog.Count).All(i=>area.Carton(i)==null),"New game resets physical storage and waste count");
            Require(ShopCheckFixtures.Expand(game),"Unlock convenience brands");
            var old=EmptyCarton(Water);Require(player.TryHoldCarton(old),"Pick empty water carton");
            Require(game.OrderSupply(Water),"Another shipment can be ordered while old packaging is carried");game.Tick(8.1f);
            var replacement=area.Carton(Water);Require(replacement!=null&&replacement!=old&&replacement.Quantity==3&&old.Quantity==0,"New delivery has a separate filled box and does not refill carried empty cardboard");
            Require(bin.Use(player)&&game.WarehouseCount(Water)==3&&replacement.gameObject.activeSelf,"Throwing old packaging cannot delete newly arrived stock");
            Require(player.TryHoldCarton(replacement)&&!bin.Use(player)&&replacement.Quantity==3,"Filled carton is carryable but cannot be thrown away");player.ReturnCarton();
            var product=UnityEngine.Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId=="water");Require(player.TryHold(product),"Bring shelf stock back to storage");
            area.Dock(0).GetComponent<ShopStation>().Use(player);Require(player.HasHeldProduct&&game.WarehouseCount(0)==0,"Wrong-brand box rejects return without losing product");
            Require(!bin.Use(player)&&player.HasHeldProduct,"Paper dumpster rejects saleable goods");
            area.Dock(Water).GetComponent<ShopStation>().Use(player);Require(!player.HasHeldItem&&game.WarehouseCount(Water)==4&&replacement.Quantity==4,"Correct-brand dock receives returned product");
            Require(game.StockCount(Water)+game.WarehouseCount(Water)==6,"Two paid water shipments conserve six units through unpacking, return and waste disposal");
            return new{passed=checks,failed=0};
        }
        finally{game.ResetDemo();game.ManualTick=false;}
    }
    public static object WalkingRoute()
    {
        checks=0;game=MvpShopController.Instance;player=UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();game.ManualTick=true;game.ResetDemo();game.ClosePanel();
        var movement=player.GetComponent<FirstPersonController>();var body=player.GetComponent<CharacterController>();var position=player.transform.position;var rotation=player.transform.rotation;
        movement.enabled=false;
        try
        {
            var carton=EmptyCarton(0);Require(player.TryHoldCarton(carton),"Carry cardboard along physical route");
            Physics.SyncTransforms();Walk(body,new Vector3(-1.6f,0,0));Walk(body,new Vector3(-3.15f,0,0));Walk(body,new Vector3(-4.25f,0,0));
            Walk(body,new Vector3(-4.25f,0,2.65f));Walk(body,new Vector3(-4.25f,0,3.7f));
            Require(body.transform.position.y>-.05f,"Warehouse and rear doorway floors are continuous");
            Require(UnityEngine.Object.FindAnyObjectByType<WarehouseBin>().Use(player),"Dispose carried carton after walking into rear yard");
            Walk(body,new Vector3(-4.25f,0,0));Walk(body,new Vector3(-3.15f,0,0));Walk(body,new Vector3(-1.6f,0,0));Walk(body,position);
            return new{passed=checks,failed=0};
        }
        finally
        {
            body.enabled=false;player.transform.SetPositionAndRotation(position,rotation);body.enabled=true;movement.enabled=true;game.ResetDemo();game.ManualTick=false;
        }
    }
    private static void Walk(CharacterController body,Vector3 target)
    {
        for(int i=0;i<350;i++){Vector3 delta=target-body.transform.position;delta.y=0;if(delta.magnitude<.05f)break;body.Move(Vector3.ClampMagnitude(delta,.04f)+Vector3.down*.025f);}
        Vector3 remaining=target-body.transform.position;remaining.y=0;Require(remaining.magnitude<.1f,"Walkable route reaches "+target+" actual "+body.transform.position);
    }
}
