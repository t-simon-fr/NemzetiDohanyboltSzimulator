using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class FurniturePlanningChecks
{
    private static ShopFurniturePose Fit(MvpShopController shop, ShopFurniture item)
    {
        var room = item.InWarehouse ? shop.Layout.WarehouseArea : shop.Layout.ShopArea;
        var plan = shop.Arrangement.Draft;
        for (float z = room.yMin + .7f; z < room.yMax - .4f; z += .25f)
            for (float x = room.xMin + .7f; x < room.xMax - .4f; x += .25f)
                for (int turn = 0; turn < 4; turn++)
                {
                    var pose = shop.Arrangement.SnapToGuides(item, new ShopFurniturePose(new Vector3(x, 0, z), turn));
                    plan[item] = pose;
                    if (shop.Arrangement.Validate(plan, out var reason)) return pose;
                }
        throw new Exception("No placement for " + item.Title);
    }

    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); passed++; };
        var shop = MvpShopController.Instance;
        var editor = shop.Arrangement;
        try
        {
            shop.GetComponent<ShopMenu>().Resume(); shop.ResetDemo(); shop.ClosePanel(); shop.ManualTick = true;
            ShopCheckFixtures.Fund(shop, 200000);
            shop.Layout.SetLevels(3, 3);
            var owned = editor.Current();
            int funds = shop.Ledger.Cash, improvements = shop.Ledger.Improvements;
            int slots = ((ProductSlot[])typeof(ShopLayout).GetField("publicSlots", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(shop.Layout)).Length, docks = shop.Warehouse.StorageDocks.Count();
            check(editor.Begin(), "Open planning without a purchase");
            check(editor.StartPurchase(2), "Choose double-sided rack");
            check(editor.PlacePurchase(Fit(shop, editor.PurchasePreview)), "Place first planned island");
            check(editor.PurchasePreview != null && editor.PlannedCount == 1, "Repeat placement keeps same catalog selection active");
            check(editor.PlacePurchase(Fit(shop, editor.PurchasePreview)), "Place second planned island");
            check(editor.PlannedCost == 20000 && shop.Ledger.Cash == funds && shop.Ledger.Improvements == improvements, "Two planned islands cost nothing until confirmation");
            check(((ProductSlot[])typeof(ShopLayout).GetField("publicSlots", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(shop.Layout)).Length == slots && editor.Current().Count == owned.Count, "Draft stock slots never enter live inventory");
            var pending = editor.Draft.Keys.First(editor.IsPlanned);
            check(editor.RemovePlanned(pending) && editor.PlannedCost == 10000, "Remove planned item updates total without refund or charge");
            editor.Cancel();
            check(shop.Ledger.Cash == funds && editor.Current().Count == owned.Count && editor.PlannedCount == 0, "Cancel discards all purchases and leaves ownership and money intact");
            check(editor.Begin() && editor.StartPurchase(3) && editor.PlacePurchase(Fit(shop, editor.PurchasePreview)), "Plan actual warehouse rack");
            check(shop.Warehouse.StorageDocks.Count() == docks, "Unpaid warehouse docks are not usable");
            check(editor.StartPurchase(4) && editor.PlacePurchase(Fit(shop, editor.PurchasePreview)), "Plan once-only fridge");
            check(editor.StartPurchase(4), "Another independent fridge can be previewed");
            check(!shop.GetComponent<ShopNextUpgrades>().FridgeOwned, "Fridge stays unowned during planning");
            int total = editor.PlannedCost;
            shop.Ledger.SetLotteryReserve(shop.Ledger.Cash - total + 1);
            check(!editor.Save() && editor.Active && editor.PlannedCost == total && shop.Ledger.Cash == funds, "Reserved money blocks entire purchase without partial mutation");
            check(editor.Current().Count == owned.Count && shop.Warehouse.StorageDocks.Count() == docks, "Rejected save registers nothing");
            shop.Ledger.SetLotteryReserve(0);
            check(editor.Save(), "Save commits complete plan");
            check(shop.Ledger.Cash == funds - total && shop.Ledger.Improvements == improvements + total && shop.Drawer.Total == shop.Ledger.Cash, "One exact combined debit reconciles physical cash");
            check(!editor.Save() && shop.Ledger.Cash == funds - total, "Repeated save never debits twice");
            check(shop.GetComponent<ShopNextUpgrades>().FridgeOwned && editor.Current().Count == owned.Count + 2, "Fridge and warehouse rack become real owned furniture");
            check(shop.Warehouse.StorageDocks.Count() == docks + 4, "Paid rack registers its four carton docks once");
            check(editor.Begin() && editor.StartPurchase(4), "Owned fridge permits another instance after reopening");
            editor.Cancel();
            var shelf = owned.Keys.First();
            for (int turn = 0; turn < 4; turn++)
            {
                var pose = ShopArrangement.SnapPose(shelf, new ShopFurniturePose(new Vector3(3.123f, 5, -1.678f), turn));
                var bounds = shelf.BoundsAt(pose);
                Func<float,bool> aligned=v=>Mathf.Abs(v/.1f-Mathf.Round(v/.1f))<.001f;
                check((aligned(bounds.xMin)||aligned(bounds.xMax))&&(aligned(bounds.yMin)||aligned(bounds.yMax))&&pose.Position.y==shelf.DefaultPose.Position.y,"Both footprint axes align after rotation "+turn);
            }
            shop.ResetDemo(); shop.ClosePanel();
            check(!shop.GetComponent<ShopNextUpgrades>().FridgeOwned && editor.PlannedCount == 0 && !editor.Current().Keys.Any(f => f.InWarehouse), "New game clears owned and pending purchases");
            shop.Devices.Focus(ShopDeviceKind.Monitor); shop.Devices.OpenApp(ShopComputerApp.Furnishing);
            shop.Devices.KeyboardTargets().Single(t=>t.Id=="Furnishing:Developments").Activate();
            var controls = shop.Devices.KeyboardTargets();
            check(controls.Any(t => t.Id == "Expand:0") && controls.Any(t => t.Id == "Warehouse:Expand"), "Both room sizes share development tab");
            check(!controls.Any(t => t.Id == "ExtraShelf:0" || t.Id.StartsWith("Module:Plan:")), "Legacy cigarette expansion and furniture not in developments");
            controls.Single(t => t.Id == "Furnishing:Furniture").Activate();
            controls = shop.Devices.KeyboardTargets();
            check(controls.Count(t => t.Id.StartsWith("Module:Plan:")) == 6 && !controls.Any(t => t.Id == "Warehouse:Expand"), "Furniture catalog contains four racks and fridge, no building expansion");
            check(!controls.Single(t => t.Id == "Module:Plan:4").Enabled, "Starter shop still locks fridge");
            int before = shop.Ledger.Cash;
            controls.Single(t => t.Id == "Module:Plan:1").Activate();
            check(editor.Active && editor.PurchasePreview != null && shop.Ledger.Cash == before, "TobaccOS catalog enters placement without charging");
            editor.Cancel();
            return new { passed, failed = 0 };
        }
        finally { editor.Cancel(); shop.ClosePanel(); shop.ResetDemo(); shop.ManualTick = false; }
    }
}
