using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public static class IngredientPackagingChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool,string> check = (ok,name) => { if (!ok) throw new Exception(name); passed++; };
        var shop = MvpShopController.Instance;
        var player = Object.FindAnyObjectByType<PlayerInteractor>();
        try
        {
            player.CancelHeld(); shop.ClosePanel(); shop.ResetDemo(); shop.ManualTick = true;
            ShopCheckFixtures.Fund(shop,60000);
            check(shop.Activities.BuyCoffeeMachine() && shop.Activities.OrderCoffeeSupplies()
                && shop.Activities.OrderMilkSupplies(), "Both branded ingredients use existing paid procurement");
            shop.Tick(8.1f);
            int cash = shop.Ledger.Cash;
            foreach (var package in Object.FindObjectsByType<ShopIngredientPackage>())
            {
                bool milk = package.Milk > 0;
                string key = milk ? "Albro" : "LaVazze";
                var prefab = Resources.Load<GameObject>("Ingredients/" + key);
                check(prefab != null && prefab.GetComponentsInChildren<Collider>().Length == 0,
                    key + " visual asset borrows its meshes/materials without child colliders");
                check(prefab.GetComponentsInChildren<Renderer>().All(r => r.sharedMaterial != null
                    && r.sharedMaterial.shader.name == "Universal Render Pipeline/Lit"),
                    key + " uses imported URP materials");
                var print = package.GetComponentsInChildren<Renderer>().Single(r => r.name.Contains("printed panels"));
                check(print.sharedMaterial.mainTexture != null && print.sharedMaterial ==
                    prefab.GetComponentsInChildren<Renderer>().Single(r => r.name.Contains("printed panels")).sharedMaterial,
                    key + " live model uses the shared readable print asset");
                check(package.GetComponentsInChildren<Collider>().Length == 1,
                    key + " has a single root interaction/throw collider");
                var bounds = package.GetComponent<BoxCollider>().bounds;
                bounds.Expand(.004f);
                check(package.GetComponentsInChildren<Renderer>().All(r => bounds.Contains(r.bounds.min) && bounds.Contains(r.bounds.max)),
                    key + " visual geometry fits its physical package");
                var tool = package.GetComponent<ShopHandTool>();
                check(tool.DisplayName.Contains(package.Brand) && player.TryHoldTool(tool),
                    key + " can be picked up with an identifying tooltip");
                check(Vector3.Dot(package.transform.up,Camera.main.transform.up) > .9f
                    && Vector3.Dot(package.transform.forward,Camera.main.transform.forward) > .8f,
                    key + " remains upright with printed front toward the player");
                check(player.DropHeld() && player.TryHoldTool(tool),
                    key + " survives a physical drop and re-pickup");
                shop.Activities.UseCoffeeMachine();
                check(!player.HasHeldItem && shop.Ledger.Cash == cash && shop.Drawer.Total == cash,
                    key + " refill consumes one package without changing money");
            }
            check(shop.Activities.CoffeeSupplies == 10 && shop.Activities.MilkSupplies == 10
                && shop.Activities.ReadyCoffee == 0 && shop.Activities.ReadyMilk == 0,
                "Branded packages preserve exact ten-serving refill quantities");
            shop.Activities.UseCoffeeMachine();
            check(shop.Activities.CoffeeSupplies == 10 && shop.Activities.MilkSupplies == 10
                && shop.Activities.BrewRemaining == 0 && shop.Devices.FocusKind == ShopDeviceKind.Coffee,
                "Repeated empty-handed use opens the menu without duplicate refill or brewing");
            return new { passed, failed = 0 };
        }
        finally { player.CancelHeld(); shop.ClosePanel(); shop.ResetDemo(); shop.ManualTick = false; }
    }
}
