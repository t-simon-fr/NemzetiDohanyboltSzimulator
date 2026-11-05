using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object=UnityEngine.Object;

public static class IngredientPickupChecks
{
    public static object Run()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var shop=MvpShopController.Instance;
        var player=Object.FindAnyObjectByType<PlayerInteractor>();
        var targetTool=typeof(PlayerInteractor).GetField("targetTool",BindingFlags.NonPublic|BindingFlags.Instance);
        GameObject obstruction=null;
        try
        {
            player.CancelHeld();shop.ClosePanel();shop.ResetDemo();shop.ManualTick=true;ShopCheckFixtures.Fund(shop,60000);
            check(shop.Activities.BuyCoffeeMachine()&&shop.Activities.OrderCoffeeSupplies()&&shop.Activities.OrderMilkSupplies(),"Buy real machine, coffee and milk packages");
            shop.Tick(8.1f);
            var packages=Object.FindObjectsByType<ShopIngredientPackage>();
            check(packages.Length==2&&shop.Activities.ReadyCoffee==10&&shop.Activities.ReadyMilk==10,"Both paid ingredient packages physically arrive on warehouse shelves");
            int cash=shop.Ledger.Cash;
            foreach(var package in packages)
            {
                Physics.SyncTransforms();
                var tool=package.GetComponent<ShopHandTool>();
                var center=package.GetComponentInChildren<Collider>().bounds.center;
                var eye=center+Vector3.right*1.25f;eye.y=1.70f;
                var ray=new Ray(eye,(center-eye).normalized);
                Physics.SyncTransforms();
                var hits=Physics.RaycastAll(ray,2.5f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance).ToArray();
                check(hits.Any(h=>h.collider.isTrigger&&shop.Warehouse.IsStorageDock(h.collider.transform)),"Real package overlaps its warehouse storage selection trigger");
                player.RefreshTarget(ray);
                check((ShopHandTool)targetTool.GetValue(player)==tool&&!player.LookingAtSupplies,"Visible ingredient wins over empty shelf trigger: "+tool.DisplayName);
                check(tool.DisplayName.StartsWith(package.Milk>0?"Tej":"Kávéalapanyag"),"Tooltip identifies the actual ingredient");
                obstruction=GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstruction.transform.position=Vector3.Lerp(eye,center,.45f);obstruction.transform.localScale=new Vector3(.10f,.65f,.65f);
                Physics.SyncTransforms();player.RefreshTarget(ray);
                check((ShopHandTool)targetTool.GetValue(player)!=tool,"A real solid obstacle still blocks ingredient pickup");
                obstruction.SetActive(false);Object.Destroy(obstruction);obstruction=null;Physics.SyncTransforms();player.RefreshTarget(ray);
                check((ShopHandTool)targetTool.GetValue(player)==tool&&player.TryHoldTool(tool),"Ingredient can be held once the real obstacle is removed");
                shop.Activities.UseCoffeeMachine();
                check(!player.HasHeldItem,"Real package is consumed by refilling the machine");
                check(shop.Ledger.Cash==cash&&shop.Drawer.Total==cash,"Pickup and refilling do not charge or lose money");
            }
            check(shop.Activities.CoffeeSupplies==10&&shop.Activities.MilkSupplies==10&&shop.Activities.ReadyCoffee==0&&shop.Activities.ReadyMilk==0,"Each paid coffee and milk serving transfers exactly once");
            shop.Activities.UseCoffeeMachine();
            check(shop.Devices.FocusKind==ShopDeviceKind.Coffee&&shop.Activities.BrewRemaining==0&&shop.Activities.CoffeeSupplies==10,"Empty-handed machine use opens menu without brewing or duplicating refill");
            shop.ClosePanel();
            check(shop.OrderSupply(0,1),"Buy carton to preserve empty-cell interaction");shop.Tick(8.1f);
            var carton=shop.Warehouse.Carton(0);check(player.TryHoldCarton(carton),"Carry normal warehouse carton after ingredient handling");
            var dock=shop.Warehouse.Dock(1);var target=dock.TransformPoint(Vector3.up*.18f);
            player.RefreshTarget(new Ray(target-dock.forward*.8f,dock.forward));
            check(player.LookingAtSupplies&&player.TryPlaceCarton(dock)&&shop.Warehouse.CartonAt(dock)==carton,"Empty storage cells still accept cartons after tool-priority fix");
            return new{passed,failed=0};
        }
        finally{if(obstruction!=null)Object.Destroy(obstruction);player.CancelHeld();shop.ClosePanel();shop.ResetDemo();shop.ManualTick=false;}
    }
}
