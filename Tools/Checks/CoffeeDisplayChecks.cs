using System;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CoffeeDisplayChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); passed++; };
        var shop = MvpShopController.Instance;
        var player = Object.FindAnyObjectByType<PlayerInteractor>();
        var fixture = new GameObject("Coffee display fixture").AddComponent<ShopAcceptanceProbe>();
        var camera = Camera.main;
        var activities = shop.Activities;
        var display = activities.CoffeeDisplay;
        Action<int> click = index => {
            Physics.SyncTransforms();
            Rect r = display.Bounds(camera, index);
            var ray = camera.ScreenPointToRay(new Vector3(r.center.x, Screen.height - r.center.y, 0));
            if (!Physics.Raycast(ray, out var hit, 4, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) throw new Exception("Coffee display ray missed key " + index);
            var control = hit.collider.GetComponentInParent<ShopWorldControl>();
            if (control != display.Controls[index]) throw new Exception("Coffee display key occluded: " + index + " by " + hit.collider.name);
            control.Activate(shop);
            display.Refresh();
        };
        try
        {
            fixture.Prepare("coffee-display"); shop.ManualTick = true;
            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            check(Physics.Raycast(ray, out var hit, 2.5f), "Machine display is within normal player reach");
            var station = hit.collider.GetComponentInParent<ShopStation>();
            check(station != null && station.IsCoffeeControl, "Display has its own reachable interaction target");
            station.Use(player);
            check(shop.Devices.FocusKind == ShopDeviceKind.Coffee && ShopInputGate.Blocked, "E target opens machine control without a modal");
            int balance = shop.Ledger.Cash;
            int initialRecipe = activities.SelectedRecipe;
            shop.Devices.HandleDeviceKey(UnityEngine.InputSystem.Key.Digit2);
            shop.Devices.HandleDeviceKey(UnityEngine.InputSystem.Key.Digit3);
            check(activities.SelectedRecipe == initialRecipe && activities.BrewRemaining == 0, "Number keys do not select recipes or brew");
            shop.ClosePanel();
            activities.UseCoffeeMachine();
            check(shop.Devices.FocusKind == ShopDeviceKind.Coffee && activities.BrewRemaining == 0, "Machine body and screen both open the menu without brewing");
            for (int index = 0; index < 3; index++)
            {
                shop.Devices.Focus(ShopDeviceKind.Coffee);
                click(index);
                int type = ShopCatalog.Coffee + index;
                check(activities.SelectedRecipe == type && display.DisplayRecipe == ShopCatalog.ShortNames[type], "Physical selection updates shared recipe " + type);
                int coffee = activities.CoffeeSupplies, milk = activities.MilkSupplies;
                click(3);
                check(activities.BrewRemaining > 0 && activities.CoffeeSupplies == coffee - 1 && activities.MilkSupplies == milk - (type == ShopCatalog.Latte ? 1 : 0), "Brew consumes exact ingredients once for " + type);
                float remaining = activities.BrewRemaining;
                click((index + 1) % 3); click(3);
                check(activities.SelectedRecipe == type && activities.BrewRemaining == remaining && activities.CoffeeSupplies == coffee - 1, "Busy machine ignores recipe changes and duplicate brew");
                activities.Tick(.4f);
                check(display.DisplayStatus.StartsWith("FŐZÉS") && activities.BrewRemaining < remaining, "Machine screen follows live brew progress");
                shop.ClosePanel(); activities.Tick(4);
                Physics.SyncTransforms();
                check(activities.CoffeeOutput.Occupant != null && activities.CoffeeOutput.Occupant.ProductId == ShopCatalog.Ids[type] && display.DisplayStatus.StartsWith("ELKÉSZÜLT"), "Brew completes after leaving machine view");
                check(!activities.CoffeeOutput.Occupant.GetComponent<Collider>().bounds.Intersects(display.transform.parent.Find("Coffee machine body").GetComponent<Collider>().bounds), "Brewed cup clears coffee machine housing");
                shop.Devices.Focus(ShopDeviceKind.Coffee);
                check(display.ActionControl.Kind == WorldControlKind.CoffeeTake && display.ActionControl.Interactable, "Completed drink enables real pickup");
                click(3);
                check(player.HeldProduct != null && player.HeldProduct.ProductId == ShopCatalog.Ids[type] && shop.Devices.FocusKind == ShopDeviceKind.None && ShopInputGate.WorldActionConsumed, "Physical take holds real cup and consumes world click");
                player.CancelHeld(); activities.CoffeeOutput.Occupant.RemoveFromWorld();
                Physics.SyncTransforms(); display.Refresh();
            }
            check(activities.CoffeeSupplies == 2 && activities.MilkSupplies == 1 && shop.Ledger.Cash == balance, "Three recipes preserve exact stock and do not change money");
            var menu = shop.GetComponent<ShopMenu>();
            Cursor.lockState = CursorLockMode.None;
            menu.Open(); menu.Resume();
            check(!ShopInputGate.Blocked && Cursor.lockState == CursorLockMode.Locked && Time.timeScale == 1, "Resuming world restores player control after an unlocked cursor");
            shop.Devices.Focus(ShopDeviceKind.Coffee);
            menu.Open(); menu.Resume();
            check(ShopInputGate.Blocked && shop.Devices.FocusKind == ShopDeviceKind.Coffee && Cursor.lockState == CursorLockMode.None, "Resuming focused coffee screen preserves device input context");
            activities.SelectRecipe(ShopCatalog.Latte);
            typeof(ShopActivities).GetProperty("MilkSupplies").SetValue(activities, 0);
            display.Refresh();
            check(display.DisplayStatus == "NINCS TEJ" && !display.ActionControl.Interactable, "Screen explains unavailable latte and disables brew");
            click(3);
            check(activities.CoffeeSupplies == 2 && activities.BrewRemaining == 0, "Disabled milk-shortage action consumes nothing");
            typeof(ShopActivities).GetProperty("CoffeeSupplies").SetValue(activities, 0); display.Refresh();
            check(display.DisplayStatus == "NINCS KÁVÉALAPANYAG" && !display.ActionControl.Interactable, "Screen continuously shows coffee shortage");
            check(display.Bounds(camera, 0).center.x < display.Bounds(camera, 1).center.x && display.Bounds(camera, 1).center.x < display.Bounds(camera, 2).center.x, "Recipe choices read left to right");
            shop.ResetDemo(); display.Refresh();
            check(!activities.CoffeeOwned && activities.BrewRemaining == 0 && activities.CoffeeSupplies == 0 && activities.MilkSupplies == 0, "New game clears machine and inventory state");
            return new { passed, failed = 0 };
        }
        finally
        {
            player.CancelHeld(); shop.ResetDemo(); shop.ManualTick = false;
            Object.Destroy(fixture.gameObject);
        }
    }
}
