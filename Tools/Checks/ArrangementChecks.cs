using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ArrangementChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); passed++; };
        var grid = new ShopRouteGrid(new Rect(0, 0, 3, 3), p => p.x < 1 || p.x > 1.5f || p.z > 2);
        check(grid.TryPath(new Vector3(.25f,0,.25f), new Vector3(2.75f,0,.25f), out var detour) && detour.Max(p=>p.z) > 2,
            "A* takes real detour around obstructing furniture");
        grid = new ShopRouteGrid(new Rect(0,0,3,3), p => p.x < 1 || p.x > 1.5f);
        check(!grid.TryPath(new Vector3(.25f,0,.25f), new Vector3(2.75f,0,.25f), out var blocked), "Disconnected floor is rejected");
        var openGrid = new ShopRouteGrid(new Rect(0,0,4,4), p => true);
        check(openGrid.TryPath(new Vector3(.25f,0,.25f), new Vector3(3.75f,0,3.75f), out var raw) && raw.Count > 10, "Raw grid path staircases across open floor");
        var smooth = openGrid.SmoothPath(raw);
        check(smooth.Count <= 3 && smooth.First() == raw.First() && smooth.Last() == raw.Last(), "Smoothing shortcuts the open diagonal to its endpoints");
        Func<Vector3,bool> walkable = p => p.x < 1 || p.x > 1.5f || p.z > 2;
        var wallGrid = new ShopRouteGrid(new Rect(0,0,3,3), walkable);
        check(wallGrid.TryPath(new Vector3(.25f,0,.25f), new Vector3(2.75f,0,.25f), out var wallRaw), "Walled floor stays connected");
        var wallSmooth = wallGrid.SmoothPath(wallRaw);
        bool clear = true;
        for (int i = 0; clear && i < wallSmooth.Count - 1; i++)
        {
            float distance = Vector3.Distance(wallSmooth[i], wallSmooth[i + 1]);
            int steps = Math.Max(1, (int)Math.Ceiling(distance / .05f));
            for (int s = 0; s <= steps; s++)
                if (!walkable(Vector3.Lerp(wallSmooth[i], wallSmooth[i + 1], (float)s / steps))) { clear = false; break; }
        }
        check(clear && wallSmooth.First() == wallRaw.First() && wallSmooth.Last() == wallRaw.Last(), "Smoothed detour keeps every segment walkable with identical endpoints");
        var shop = MvpShopController.Instance;
        var layout = shop.Arrangement;
        var player = Object.FindAnyObjectByType<PlayerInteractor>();
        shop.ManualTick = true;
        try
        {
            shop.ResetDemo();
            ShopCheckFixtures.Fund(shop, 150000);
            check(shop.TryExpand() && shop.GetComponent<ShopNextUpgrades>().BuyFridge(), "Purchased default furniture has usable approaches");
            var shelf = layout.Furniture.First(f=>f.Id=="public-shelf");
            var fridge = layout.Furniture.First(f=>f.Id=="drinks-fridge");
            var slot = shelf.Slots.First(s=>s.CanAcceptStock("water"));
            var product = shop.CreateCoffee(ShopCatalog.Index("water"));
            product.AssignInitialSlot(slot);
            Vector3 stockPosition = product.transform.position;
            Vector3 original = shelf.transform.position;
            int money = shop.Ledger.Cash;
            check(player.TryHold(product) && !layout.Begin(), "Held product blocks arrangement");
            player.CancelHeld();
            product.AssignInitialSlot(slot);
            check(layout.Begin() && Time.timeScale == 0 && ShopInputGate.Blocked, "Editor freezes world and blocks first-person input");
            float elapsed = shop.Elapsed;
            shop.Tick(60);
            check(shop.Elapsed == elapsed && shop.Spawned == 0, "Manual and runtime world ticks cannot advance while editing");
            check(!layout.TryPlace(shelf, new ShopFurniturePose(new Vector3(8,0,-2),0)), "Reject unpurchased floor");
            check(!layout.TryPlace(shelf, new ShopFurniturePose(new Vector3(1.2f,0,-3.5f),1)), "Reject entrance obstruction");
            check(!layout.TryPlace(shelf, new ShopFurniturePose(new Vector3(-1.3f,0,-1),0)), "Reject staff aisle obstruction");
            check(!layout.TryPlace(shelf, new ShopFurniturePose(fridge.transform.position,1)), "Reject overlap with fridge");
            check(!layout.TryPlace(shelf, new ShopFurniturePose(new Vector3(5.95f,0,-1),2)), "Reject shelf with its usable face against wall");
            var moved = layout.SnapToGuides(shelf, new ShopFurniturePose(new Vector3(3.75f,0,-2.9f),1));
            check(layout.TryPlace(shelf, moved), "Move and rotate stocked shelf in preview");
            check(shelf.transform.position == original && product.transform.position == stockPosition && product.CurrentSlot == slot,
                "Preview does not mutate furniture or stock");
            layout.Cancel();
            check(shelf.transform.position == original && Time.timeScale == 1 && !layout.Active && !Camera.main.orthographic, "Cancel restores full pre-edit state, camera and clock");
            check(layout.Begin() && layout.TryPlace(shelf, moved) && layout.Save(), "Atomic layout save");
            check(shelf.transform.position == moved.Position && shelf.Pose.QuarterTurns == 1 && product.CurrentSlot == slot &&
                product.transform.position != stockPosition && shop.Ledger.Cash == money, "Stock moves with slot, same item and money preserved");
            shop.GetComponent<ShopNextUpgrades>().RefreshLayout();
            check(shelf.transform.position == moved.Position, "Refresh does not overwrite saved placement");
            foreach (var point in shelf.AccessAt(shelf.Pose))
                check(shop.Navigation.TryPath(shop.Layout.EntryInside, point, out var route), "Every moved shelf approach remains reachable");
            check(shop.Activities.BuyCoffeeMachine(), "Purchase coffee machine after rearrangement");
            typeof(ShopActivities).GetProperty("CoffeeSupplies").SetValue(shop.Activities, 1);
            check(shop.Activities.BrewCoffee() && !layout.Begin(), "Busy machine blocks editing");
            shop.Activities.Tick(4);
            shop.OpenShop();
            check(!shop.Devices.BeginArrangement() && shop.Message.Contains("Átrendezni"), "Monitor explains why open shop blocks editing");
            shop.SingleProductVisits = true;
            typeof(MvpShopController).GetMethod("SpawnCustomer",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
                .Invoke(shop,new object[]{ShopCatalog.Index("water"),false});
            shop.AutomaticArrivals=false;
            for (int i = 0; i < 3000 && shop.ActiveCustomer == null; i++) shop.Tick(.1f);
            check(shop.ActiveCustomer != null && shop.ActiveCustomer.BasketItem == product,
                "Real NPC navigates to rotated shelf, picks its product and reaches counter");
            shop.SkipCurrent();
            shop.RequestClose();
            for (int i = 0; i < 3000 && !shop.CanEndDay; i++) shop.Tick(.1f);
            check(product.CurrentSlot == slot, "Cancelled purchase returns exact product to moved shelf");
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.position = shop.Layout.EntryInside;
            body.enabled = true;
            check(shop.TryEndDay(), "Explicit door exit after rearrangement");
            shop.NextDay();
            check(shelf.transform.position == moved.Position && fridge.gameObject.activeInHierarchy && product.CurrentSlot == slot,
                "Next day retains layout, upgrades and exact stocked product");
            shop.ResetDemo();
            check(shelf.transform.position == original && !fridge.gameObject.activeInHierarchy, "New game restores defaults and ownership");
            return new { passed, failed = 0 };
        }
        finally
        {
            layout.Cancel();
            player.CancelHeld();
            shop.ResetDemo();
            shop.ManualTick = false;
        }
    }
}
