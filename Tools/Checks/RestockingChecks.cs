using System;
using System.Linq;
using UnityEngine;

public static class RestockingChecks
{
    private static int checks;
    private static MvpShopController game;
    private static PlayerInteractor player;
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);checks++;}
    private static Ray Aim(ProductSlot slot)
    {
        Vector3 target=slot.HintCenter;
        return new Ray(target-slot.transform.forward*1.15f,slot.transform.forward);
    }
    private static void VisibleInHand()
    {
        var item=player.HeldProduct;var camera=player.GetComponentInChildren<Camera>();
        Require(item!=null&&item.IsHeld&&item.transform.IsChildOf(camera.transform),"Actual player holds the product under the view camera");
        var b=item.GetComponent<Renderer>().bounds;
        Vector3 low=Vector3.one*float.MaxValue,high=Vector3.one*float.MinValue;
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
        {
            Vector3 p=camera.WorldToScreenPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
            low=Vector3.Min(low,p);high=Vector3.Max(high,p);
        }
        Require(low.z>camera.nearClipPlane&&low.x>0&&high.x<camera.pixelWidth&&low.y>116&&high.y<camera.pixelHeight,"Held model stays visible above the bottom controls");
    }
    public static object Run()
    {
        checks=0;game=MvpShopController.Instance;player=UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();
        game.ManualTick=true;game.ResetDemo();game.ClosePanel();
        var station=GameObject.Find("Supply Crate").GetComponent<ShopStation>();
        try
        {
            station.Use(player);Require(!player.HasHeldProduct&&game.WarehouseCount(0)==0,"Empty crate does not create free stock");
            Require(game.Message.Contains("monitor")&&station.Contents.Contains(ShopCatalog.Names[0]+": 0 db"),"Empty crate shows truthful stock and ordering instructions");
            Require(game.OrderSupply(0),"Order a paid cigarette pack");
            station.Use(player);Require(!player.HasHeldProduct&&game.Message.Contains("úton"),"Pending shipment is distinguished from missing stock");
            game.Tick(8.1f);game.CycleSupply();station.Use(player);
            Require(player.HasHeldProduct&&player.HeldProduct.ProductId=="cig_red"&&game.SelectedSupply==0&&game.WarehouseCount(0)==2,"Taking switches from empty type to available delivered stock");
            VisibleInHand();
            station.Use(player);Require(!player.HasHeldProduct&&game.WarehouseCount(0)==3&&game.StockCount(0)==8,"Return carried supply to crate without losing or duplicating stock");
            var old=UnityEngine.Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId=="cig_red");ProductSlot source=old.CurrentSlot;
            Require(player.TryHold(old),"Pick original shelf stock with actual player");
            var spare=UnityEngine.Object.FindObjectsByType<ProductSlot>().First(s=>s.name=="Counter Slot 1");
            Physics.SyncTransforms();player.RefreshTarget(Aim(spare));
            Require(player.TargetSlot==spare&&player.CanPlaceHeld&&player.TryPlaceHeld(),"Actual ray target and E action put product on temporary counter slot");
            Require(source.Occupant==null,"Original shelf cell becomes free");
            station.Use(player);Require(player.HasHeldProduct&&game.WarehouseCount(0)==2,"Delivered stock is taken into hand");
            Require(source.GetComponentInChildren<LineRenderer>()!=null&&source.GetComponentInChildren<LineRenderer>().enabled,"Compatible empty shelf cell has a visible green guide");
            // Aim at the shelf's front lip, outside the invisible slot trigger.
            Vector3 lip=source.transform.position-source.transform.forward*.20f-Vector3.up*.005f;
            Vector3 eye=source.transform.position-source.transform.forward*1.1f+Vector3.up*.30f;
            Physics.SyncTransforms();player.RefreshTarget(new Ray(eye,(lip-eye).normalized));
            Require(player.TargetSlot==source&&player.CanPlaceHeld,"Aiming at shelf lip snaps to the nearby visible cell");
            Require(player.TryPlaceHeld()&&!player.HasHeldProduct&&source.Occupant!=null&&source.Occupant.ProductId=="cig_red","Actual player action places supply into fixed shelf cell");
            Require(!UnityEngine.Object.FindObjectsByType<LineRenderer>().Any(l=>l.name=="Placement guide"&&l.enabled),"Placement guides disappear after putting down the product");
            Require(game.StockCount(0)+game.WarehouseCount(0)==11,"Initial eight plus three bought units are conserved");
            station.Use(player);var occupied=UnityEngine.Object.FindObjectsByType<ProductSlot>().First(s=>s.Occupant!=null&&s.Occupant.ProductId=="cig_red"&&s!=player.HeldProduct.CurrentSlot);
            player.RefreshTarget(Aim(occupied));Require(!player.CanPlaceHeld&&!player.TryPlaceHeld()&&player.HasHeldProduct,"Occupied shelf cannot swallow or replace carried stock");
            var wrong=UnityEngine.Object.FindObjectsByType<ProductSlot>().First(s=>s.Occupant!=null&&s.Occupant.ProductId=="cig_blue");
            player.RefreshTarget(Aim(wrong));Require(player.TargetSlot==wrong&&!player.CanPlaceHeld&&!player.TryPlaceHeld(),"Wrong product type is rejected by real targeting");
            station.Use(player);Require(!player.HasHeldProduct&&game.WarehouseCount(0)==2,"Failed placement remains recoverable by returning to crate");
            game.ResetDemo();game.ClosePanel();Require(game.TryExpand()&&game.OrderSupply(3),"Prepare larger convenience item");game.Tick(8.1f);station.Use(player);
            Require(player.HasHeldProduct&&player.HeldProduct.ProductId=="water","Convenience stock can be carried too");VisibleInHand();
            Require(station.Contents.Contains("Buborék ásványvíz: 2 db"),"Crate list reflects removed convenience unit");
            return new{passed=checks,failed=0};
        }
        finally{game.ResetDemo();game.ManualTick=false;}
    }
}
