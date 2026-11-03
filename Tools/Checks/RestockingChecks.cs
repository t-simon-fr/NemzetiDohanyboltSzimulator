using System;
using System.Linq;
using UnityEngine;

public static class RestockingChecks
{
    private static int checks;
    private static MvpShopController game;
    private static PlayerInteractor player;
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);checks++;}
    private static Ray Aim(ProductSlot slot)=>new Ray(slot.HintCenter-slot.transform.forward*1.1f,slot.transform.forward);
    public static object Run()
    {
        checks=0;game=MvpShopController.Instance;player=UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();
        game.ManualTick=true;game.ResetDemo();game.ClosePanel();
        try
        {
            var station=game.Warehouse.Dock(0).GetComponent<ShopStation>();
            station.Use(player);Require(!player.HasHeldItem&&game.WarehouseCount(0)==0,"Vacant rack creates no free carton or goods");
            Require(game.Message.Contains("monitor"),"Vacant rack points to computer ordering app");
            Require(game.OrderSupply(0),"Order a three-unit paid case");
            station.Use(player);Require(!player.HasHeldItem&&game.Message.Contains("úton"),"Pending shipment cannot be picked prematurely");
            game.Tick(8.1f);station.Use(player);var carton=player.HeldCarton;
            Require(carton!=null&&carton.ProductType==0&&carton.Quantity==3&&!player.HasHeldProduct,"E picks the entire filled carton");
            Require(game.WarehouseCount(0)==3&&game.StockCount(0)==8,"Carrying a carton transfers no units into store stock");
            var camera=player.GetComponentInChildren<Camera>();
            Require(carton.transform.IsChildOf(camera.transform)&&carton.GetComponentsInChildren<Collider>().All(c=>!c.enabled),"Carried carton is attached to camera and does not block aiming");
            var visible=carton.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).Select(r=>camera.WorldToViewportPoint(r.bounds.center)).ToArray();
            Require(visible.All(v=>v.z>camera.nearClipPlane)&&visible.Any(v=>v.x>.1f&&v.x<.9f&&v.y>.2f&&v.y<.8f),"Branded carton remains visible in hand");
            var original=UnityEngine.Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId=="cig_red");var source=original.CurrentSlot;
            player.RefreshTarget(Aim(source));Require(!player.CanPlaceHeld&&!player.TryPlaceHeld()&&carton.Quantity==3,"Occupied shelf rejects unpacking without losing stock");
            original.RemoveFromWorld();Physics.SyncTransforms();player.RefreshTarget(Aim(source));
            Require(player.TargetSlot==source&&player.CanPlaceHeld,"Actual player ray targets an empty compatible shelf cell");
            Require(source.GetComponentInChildren<LineRenderer>().enabled,"Matching shelf cell shows green placement guide");
            Require(player.TryPlaceHeld()&&player.HeldCarton==carton&&carton.Quantity==2,"One E unpacks exactly one unit while retaining carton");
            Require(source.Occupant!=null&&source.Occupant.ProductId=="cig_red"&&game.WarehouseCount(0)==2,"Unpacked unit occupies a fixed matching shelf cell");
            player.RefreshTarget(Aim(source));Require(!player.TryPlaceHeld()&&carton.Quantity==2,"Repeated E cannot overwrite the same occupied cell");
            var wrong=UnityEngine.Object.FindObjectsByType<ProductSlot>().First(s=>s.Occupant!=null&&s.Occupant.ProductId=="cig_blue");
            wrong.Occupant.RemoveFromWorld();Physics.SyncTransforms();player.RefreshTarget(Aim(wrong));
            Require(player.TargetSlot==wrong&&!player.CanPlaceHeld&&!player.TryPlaceHeld()&&carton.Quantity==2,"Wrong-brand empty shelf rejects a carried case");
            var spare=GameObject.Find("Counter Slot 1").GetComponent<ProductSlot>();player.RefreshTarget(Aim(spare));
            Require(!player.CanPlaceHeld&&!player.TryPlaceHeld(),"Carton restocking cannot bypass sales onto unrestricted counter trays");
            game.Warehouse.Dock(1).GetComponent<ShopStation>().Use(player);Require(player.HeldCarton==carton,"Wrong-brand warehouse rack rejects carton return");
            station.Use(player);Require(!player.HasHeldItem&&game.Warehouse.Carton(0)==carton&&carton.Quantity==2,"Part-filled carton can be brought back into warehouse");
            Require(game.StockCount(0)+game.WarehouseCount(0)==10,"Eight original minus one removed plus three ordered units conserved");
            station.Use(player);Require(player.HeldCarton==carton&&carton.Quantity==2,"Part-filled carton can be picked up again");
            player.CancelHeld();Require(!player.HasHeldItem&&carton.IsDocked&&carton.Quantity==2,"Cancelling carry safely returns all remaining goods to storage");
            Require(!UnityEngine.Object.FindObjectsByType<LineRenderer>().Any(l=>l.name=="Placement guide"&&l.enabled),"No placement guides remain after putting carton away");
            game.ResetDemo();game.ClosePanel();Require(ShopCheckFixtures.Expand(game)&&game.OrderSupply(ShopCatalog.Index("water")),"Order convenience case after expansion");game.Tick(8.1f);
            game.Warehouse.Dock(ShopCatalog.Index("water")).GetComponent<ShopStation>().Use(player);carton=player.HeldCarton;
            var water=UnityEngine.Object.FindObjectsByType<ProductSlot>().First(s=>s.CanAcceptStock("water"));Physics.SyncTransforms();player.RefreshTarget(Aim(water));
            Require(player.TryPlaceHeld()&&water.Occupant.ProductId=="water"&&carton.Quantity==2,"Larger convenience item also unpacks one at a time");
            Require(game.StockCount(ShopCatalog.Index("water"))+game.WarehouseCount(ShopCatalog.Index("water"))==3,"Convenience stock conserved during carton restocking");
            return new{passed=checks,failed=0};
        }
        finally{game.ResetDemo();game.ManualTick=false;}
    }
}
