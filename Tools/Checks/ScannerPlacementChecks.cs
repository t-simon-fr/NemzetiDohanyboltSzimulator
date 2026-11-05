using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object=UnityEngine.Object;

public static class ScannerPlacementChecks
{
    public static object Run()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var g=MvpShopController.Instance;
        var player=Object.FindAnyObjectByType<PlayerInteractor>();
        Action<ProductSlot> aim=slot=>{Physics.SyncTransforms();player.RefreshTarget(new Ray(slot.HintCenter+Vector3.up*.8f,Vector3.down));};
        Action<int> visit=type=>{
            g.AutomaticArrivals=false;g.SingleProductVisits=true;g.OpenShop();
            typeof(MvpShopController).GetMethod("SpawnCustomer",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(g,new object[]{type,false});
            for(int i=0;i<1000&&g.ActiveCustomer==null;i++)g.Tick(.1f);
        };
        try
        {
            player.CancelHeld();g.ResetDemo();g.ClosePanel();g.ManualTick=true;visit(0);
            var cigarette=Object.FindObjectsByType<ShopProduct>().First(product=>product.ProductId==ShopCatalog.Ids[0]&&product.CurrentSlot!=null&&product.CurrentSlot.IsStockShelf);
            int cash=g.Ledger.Cash,scans=g.Scanner.ScanCount;
            check(player.TryHold(cigarette),"Hold real requested cigarette");
            aim(g.Activities.BasketSlot);
            check(!player.CanPlaceHeld && !player.TryPlaceHeld() && player.HeldProduct==cigarette,"Player cannot put cigarettes directly in the customer zone");
            check(new[]{g.Activities.BasketSlot}.Concat(g.Activities.CustomerHandoverSlots).All(slot=>{var line=slot.GetComponentInChildren<LineRenderer>();return line==null||!line.enabled;}),"Customer zone has no cigarette placement guides");
            aim(g.DeliverySlot);
            check(player.CanPlaceHeld && player.TryPlaceHeld(),"Cigarette can be placed on the scanner");
            check(g.Scanner.ScanCount==scans+1 && cigarette.CurrentSlot==g.Activities.BasketSlot && g.Stage==ServiceStage.Payment,"Successful scan transfers the physical cigarette through the internal route");
            check(g.Ledger.Cash==cash,"Scanning and transfer do not charge the customer");
            g.ResetDemo();g.ClosePanel();g.ManualTick=true;
            var coffee=g.CreateCoffee(ShopCatalog.Coffee);
            check(player.TryHold(coffee),"Hold a prepared coffee");aim(g.DeliverySlot);
            check(!player.CanPlaceHeld && !player.TryPlaceHeld(),"Prepared coffee cannot be placed on the cigarette scanner");
            player.CancelHeld();coffee.RemoveFromWorld();
            var internalCigarette=g.CreateCoffee(0);
            check(internalCigarette.AssignInitialSlot(g.Activities.BasketSlot),"NPC/internal basket placement still accepts a cigarette");internalCigarette.RemoveFromWorld();
            visit(ShopCatalog.Coffee);
            typeof(MvpCustomerActor).GetProperty("OrderTypes").SetValue(g.ActiveCustomer,new[]{ShopCatalog.Coffee,ShopCatalog.Coffee,ShopCatalog.Latte});
            typeof(MvpCustomerActor).GetProperty("UnitPrices").SetValue(g.ActiveCustomer,new[]{650,650,1100});
            var cups=new ShopProduct[3];scans=g.Scanner.ScanCount;
            for(int i=0;i<3;i++)
            {
                cups[i]=g.CreateCoffee(i==2?ShopCatalog.Latte:ShopCatalog.Coffee);
                check(player.TryHold(cups[i]),"Carry prepared cup "+i);
                var target=g.Activities.CustomerHandoverSlots[i].transform.position+Vector3.up*.20f;
                var eye=target+Vector3.left*1.1f;eye.y=Mathf.Max(target.y+.3f,1.75f);
                Physics.SyncTransforms();player.RefreshTarget(new Ray(eye,(target-eye).normalized));
                check(player.TargetSlot==g.Activities.CustomerHandoverSlots[i] && player.CanPlaceHeld && player.TryPlaceHeld(),"Oblique targeting passes earlier selection volumes and serves a separate cup "+i+" target="+(player.TargetSlot==null?"null":player.TargetSlot.name)+" hits="+string.Join(",",Physics.RaycastAll(new Ray(eye,(target-eye).normalized),2.5f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance).Select(h=>h.collider.name+":"+h.distance.ToString("F2"))));
            }
            check(g.Stage==ServiceStage.Payment && g.MissingOrderTypes.Length==0 && cups.Select(cup=>cup.CurrentSlot).Distinct().Count()==3,"Three cups reach three independent receipt places and enable checkout");
            check(g.Scanner.ScanCount==scans,"Coffee service never pretends to scan a barcode");
            return new{passed,failed=0};
        }
        finally{player.CancelHeld();g.ClosePanel();g.ResetDemo();g.ManualTick=false;}
    }
}



