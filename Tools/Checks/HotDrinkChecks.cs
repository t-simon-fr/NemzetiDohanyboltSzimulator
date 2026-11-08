using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class HotDrinkChecks
{
    public static object State()
    {
        int passed = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; }
        Check(ShopCatalog.Index("tea") == 23 && ShopCatalog.Index("hot_chocolate") == 24 && ShopCatalog.Index("muszak_mixed") == 22, "New recipes append without moving existing ids");
        Check(!ShopCatalog.IsPackaged(23) && !ShopCatalog.IsPackaged(24), "Prepared drinks never become packaged procurement stock");
        foreach (int type in ShopHotDrinkRecipes.Types)
        {
            var machine = new ShopCoffeeMachineState();
            Check(machine.SelectRecipe(type) && machine.TryBrew(false, false) == ShopBrewResult.Unavailable, "Unowned machine rejects recipe " + type);
            machine.Install();
            foreach (var ingredient in ShopHotDrinkRecipes.Ingredients)
            {
                int debits = 0;
                Check(machine.TryOrder(ingredient, 0, false, () => { debits++; return true; }) && debits == 1, "One debit per ingredient order " + ingredient);
                Check(machine.TickDelivery(ingredient, -5) == 0 && machine.TickDelivery(ingredient, 7) == 0, "Delivery cannot arrive early " + ingredient);
                Check(machine.TickDelivery(ingredient, 1) == 10 && machine.TickDelivery(ingredient, 100) == 0 && machine.Loaded(ingredient) == 0, "Delivery emits one physical package without loading " + ingredient);
                machine.Refill(ingredient, 2);
            }
            Check(machine.TryBrew(true, false) == ShopBrewResult.Unavailable && machine.TryBrew(false, true) == ShopBrewResult.Unavailable, "Summary and output occupancy reject brew " + type);
            Check(machine.TryBrew(false, false) == ShopBrewResult.Started, "Start configured recipe " + type);
            var recipe = ShopHotDrinkRecipes.Get(type);
            foreach (var ingredient in ShopHotDrinkRecipes.Ingredients)
                Check(machine.Loaded(ingredient) == 2 - recipe.Amount(ingredient), "Exact recipe consumption " + type + " / " + ingredient);
            Check(!machine.SelectRecipe(ShopCatalog.Tea) && machine.TryBrew(false, false) != ShopBrewResult.Started, "Busy input cannot switch or double-consume " + type);
            Check(machine.TickBrew(recipe.Seconds - .1f) == -1 && machine.TickBrew(.2f) == type && machine.TickBrew(50) == -1, "One completion at configured time " + type);
            machine.RefundBrew(type);
            Check(ShopHotDrinkRecipes.Ingredients.All(i => machine.Loaded(i) == 2), "Blocked output refunds all and only spent ingredients " + type);
            Check(!machine.TryOrder(ShopDrinkIngredient.Tea, 50, false, () => throw new Exception("Capacity before debit")), "Ready packages count against capacity");
            Check(!machine.TryOrder(ShopDrinkIngredient.Cocoa, 0, false, () => false) && machine.Pending(ShopDrinkIngredient.Cocoa) == 0, "No delivery from failed payment");
            machine.Reset();
            Check(!machine.Owned && ShopHotDrinkRecipes.Ingredients.All(i => machine.Loaded(i) == 0 && machine.Pending(i) == 0), "Reset clears every ingredient");
        }
        var shared = new ShopCoffeeMachineState(); shared.Install();
        shared.Refill(1, 1); shared.Refill(ShopDrinkIngredient.Cocoa, 1); shared.SelectRecipe(ShopCatalog.HotChocolate);
        Check(shared.TryBrew(false, false) == ShopBrewResult.Started && shared.Coffee == 1 && shared.Milk == 0, "Cocoa shares milk with latte but never consumes coffee");
        shared.TickBrew(10); shared.SelectRecipe(ShopCatalog.Latte);
        Check(shared.TryBrew(false, false) == ShopBrewResult.MilkMissing && shared.Coffee == 1, "Shared milk exhaustion blocks latte without consumption");
        shared.SelectRecipe(ShopCatalog.Tea);
        Check(shared.MissingIngredients == "NINCS TEAALAPANYAG" && shared.TryBrew(false, false) == ShopBrewResult.IngredientsMissing, "Precise tea shortage");
        return new { passed, failed = 0 };
    }

    public static object Integration()
    {
        int passed = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; }
        var shop = MvpShopController.Instance;
        var player = Object.FindAnyObjectByType<PlayerInteractor>();
        var fixture = new GameObject("Hot drinks fixture").AddComponent<ShopAcceptanceProbe>();
        var a = shop.Activities;
        try
        {
            fixture.Prepare("hot-drinks"); shop.ManualTick = true;
            Check(shop.CreateCustomerOrder(ShopCatalog.Tea, 0).All(t => t != ShopCatalog.Tea && t != ShopCatalog.HotChocolate), "Never-bought tea/cocoa cannot enter orders");
            int cash = shop.Ledger.Cash;
            foreach (var ingredient in new[] { ShopDrinkIngredient.Tea, ShopDrinkIngredient.Cocoa, ShopDrinkIngredient.Milk })
                Check(a.OrderIngredient(ingredient), "Procure real " + ingredient);
            Check(shop.Ledger.Cash == cash - 3700 && shop.Drawer.Total == shop.Ledger.Cash, "Three exact procurement debits update drawer");
            Check(shop.CreateCustomerOrder(ShopCatalog.Tea, 0).All(t => t != ShopCatalog.Tea && t != ShopCatalog.HotChocolate), "Pending ingredients cannot enter orders");
            shop.Tick(8.1f);
            Check(a.ReadyIngredient(ShopDrinkIngredient.Tea) == 10 && a.ReadyIngredient(ShopDrinkIngredient.Cocoa) == 10 && a.MilkSupplies == 0, "Deliveries remain physical, unloaded stock");
            Check(shop.CreateCustomerOrder(ShopCatalog.Tea, 0)[0] == ShopCatalog.Tea && shop.CreateCustomerOrder(ShopCatalog.HotChocolate, 0)[0] == ShopCatalog.HotChocolate, "Delivered refillable ingredients enable matching customer orders");
            foreach (var package in Object.FindObjectsByType<ShopIngredientPackage>())
            {
                int amount = package.Amount; var ingredient = package.Ingredient;
                Check(package.GetComponentsInChildren<Collider>().Count(c => c.enabled) == 1, "One interaction collider per package " + ingredient);
                Check(player.TryHoldTool(package.GetComponent<ShopHandTool>()), "Pick up package " + ingredient);
                a.UseCoffeeMachine();
                Check(!player.HasHeldItem && a.LoadedIngredient(ingredient) == amount && a.ReadyIngredient(ingredient) == 0, "Hand refill transfers exact quantity " + ingredient);
            }
            Check(!shop.Reputation.IsAvailable(ShopCatalog.Coffee) && shop.Reputation.IsAvailable(ShopCatalog.Tea) && shop.Reputation.IsAvailable(ShopCatalog.HotChocolate), "Reputation uses actual per-recipe ingredients");
            foreach (int type in new[] { ShopCatalog.Tea, ShopCatalog.HotChocolate })
            {
                a.SelectRecipe(type); int[] before = a.IngredientBudget(false);
                Check(a.BrewCoffee(), "Brew new drink " + type);
                shop.ClosePanel(); a.UseCoffeeMachine(); a.SelectRecipe(ShopCatalog.Coffee);
                Check(!a.BrewCoffee() && a.SelectedRecipe == type, "Reopen and repeated input cannot change running brew " + type);
                a.Tick(5);
                var cup = a.CoffeeOutput.Occupant;
                Check(cup != null && cup.ProductId == ShopCatalog.Ids[type], "Real matching output cup " + type);
                Check(!a.BrewCoffee(), "Occupied output blocks another cup " + type);
                foreach (var ingredient in ShopHotDrinkRecipes.Ingredients)
                    Check(a.LoadedIngredient(ingredient) == before[(int)ingredient] - ShopHotDrinkRecipes.Get(type).Amount(ingredient), "World stock conservation " + type + "/" + ingredient);
                Check(a.TakeCoffeeFromDisplay() && player.HeldProduct == cup, "Take real drink from existing display " + type);
                player.CancelHeld(); cup.RemoveFromWorld();
            }
            Check(a.CoffeeSupplies == 0 && a.LoadedIngredient(ShopDrinkIngredient.Tea) == 9 && a.LoadedIngredient(ShopDrinkIngredient.Cocoa) == 9 && a.MilkSupplies == 9, "New drinks consume zero coffee and exact tea/cocoa/milk");
            Check(shop.Pricing.TrySetPrice(ShopCatalog.Tea, 500) && shop.Pricing.TrySetPrice(ShopCatalog.HotChocolate, 950), "New drink prices use existing price editor");
            fixture.Action("hot-drinks-customer"); shop.ManualTick = true;
            var wrong = shop.CreateCoffee(ShopCatalog.Coffee);
            wrong.AssignInitialSlot(a.CustomerHandoverSlots[0]); a.CustomerHandoverSlots[0].NotifyProductPlaced(wrong);
            Check(shop.MissingOrderTypes.Length == 2 && shop.Stage == ServiceStage.Order, "Wrong drink cannot satisfy tea or cocoa");
            wrong.RemoveFromWorld();
            for (int i = 0; i < 2; i++)
            {
                int type = shop.ActiveCustomer.OrderTypes[i]; a.SelectRecipe(type); a.BrewCoffee(); a.Tick(5);
                var cup = a.CoffeeOutput.Occupant; Check(a.TakeCoffeeFromDisplay(), "Take checkout cup " + type);
                fixture.Action("aim-coffee-handover-" + i);
                Physics.SyncTransforms(); player.RefreshTarget(Camera.main.ViewportPointToRay(new Vector3(.5f, .5f, 0)));
                Check(player.TryPlaceHeld(), "Place exact cup " + type + " target=" + player.TargetSlot?.name + " can=" + player.CanPlaceHeld + " held=" + player.HeldProduct?.ProductId + " hits=" + string.Join(",", Physics.RaycastAll(Camera.main.ViewportPointToRay(new Vector3(.5f,.5f,0)), 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide).OrderBy(h=>h.distance).Select(h=>h.collider.name)));
            }
            Check(shop.Stage == ServiceStage.Payment && shop.MissingOrderTypes.Length == 0 && shop.ActiveCustomer.TotalPrice == 1450, "Both exact new drinks reach existing checkout at player prices");
            int balance = shop.Ledger.Cash, sales = shop.Ledger.Sales;
            foreach (char c in "1450") shop.EnterCardDigit(c - '0');
            Check(shop.ConfirmCardPayment(), "Existing card checkout accepts new drinks");
            Check(shop.Ledger.Cash == balance + 1450 && shop.Ledger.Sales == sales + 1 && shop.Ledger.CostOfGoods == 370, "One real card posting records tea + cocoa + milk cost");
            shop.ConfirmCardPayment();
            Check(shop.Ledger.Cash == balance + 1450, "Repeated card approval cannot double-post");
            shop.ResetDemo();
            Check(ShopHotDrinkRecipes.Ingredients.All(i => a.LoadedIngredient(i) == 0 && a.ReadyIngredient(i) == 0 && a.PendingIngredient(i) == 0), "New game removes all ingredient ownership and packages");
            return new { passed, failed = 0 };
        }
        finally { player.CancelHeld(); shop.ClosePanel(); shop.ResetDemo(); shop.ManualTick = false; Object.Destroy(fixture.gameObject); }
    }
}
