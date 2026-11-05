using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WarehouseStorageChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool,string> check = (ok,name) => { if (!ok) throw new Exception(name); passed++; };
        var shop = MvpShopController.Instance;
        var player = Object.FindAnyObjectByType<PlayerInteractor>();
        var warehouse = shop.Warehouse;
        Func<Transform,string> label = dock => dock.Find("Brand dock label").GetComponent<TextMesh>().text;
        try
        {
            player.CancelHeld(); shop.ClosePanel(); shop.ResetDemo(); shop.ManualTick = true;
            ShopCheckFixtures.Fund(shop, 30000);
            var docks = warehouse.StorageDocks.ToArray();
            check(docks.Length > ShopCatalog.Count, "Previously unassigned shelf rows have actual carton places");
            check(docks.All(d => warehouse.CartonAt(d) == null && label(d) == ""), "All empty warehouse places have blank labels");
            check(shop.OrderSupply(0,3) && shop.OrderSupply(1,2), "Buy two different finite cartons");
            shop.Tick(8.1f);
            int cash = shop.Ledger.Cash;
            var first = warehouse.Carton(0);
            var other = warehouse.Carton(1);
            var destination = warehouse.Dock(2);
            check(player.TryHoldCarton(first), "Pick up the real first carton");
            warehouse.Dock(1).GetComponent<ShopStation>().Use(player);
            check(player.HeldCarton == first && warehouse.CartonAt(warehouse.Dock(1)) == other, "Occupied place rejects return without overwriting either carton");
            destination.GetComponent<ShopStation>().Use(player);
            check(!player.HasHeldItem && first.IsDocked && warehouse.CartonAt(destination) == first, "Different former brand accepts carton at the targeted empty place");
            check(label(destination) == ShopCatalog.Names[0] && label(warehouse.Dock(0)) == "", "Labels follow actual carton and vacated source");
            warehouse.Dock(0).GetComponent<ShopStation>().Use(player);
            check(!player.HasHeldItem, "Vacant original place cannot pick up a remote carton");
            destination.GetComponent<ShopStation>().Use(player);
            check(player.HeldCarton == first, "New place picks up its actual carton");
            player.CancelHeld();
            check(warehouse.CartonAt(destination) == first && !player.HasHeldItem, "Cancellation returns to the new pickup place");

            var lower = docks.Where(d => d.parent == warehouse.Dock(0) && d.position.y < warehouse.Dock(0).position.y-.2f).First();
            check(player.TryHoldCarton(first), "Pick carton for formerly unassigned lower shelf");
            Physics.SyncTransforms();
            var target = lower.TransformPoint(Vector3.up*.18f);
            var eye = target - lower.forward*.9f;
            player.RefreshTarget(new Ray(eye, (target-eye).normalized));
            check(player.LookingAtSupplies, "Physical ray finds lower shelf storage target");
            lower.GetComponent<ShopStation>().Use(player);
            check(warehouse.CartonAt(lower) == first && label(lower) == ShopCatalog.Names[0], "Lower shelf accepts carton and labels its actual goods");
            check(shop.WarehouseCount(0) == 3 && shop.WarehouseCount(1) == 2 && shop.Ledger.Cash == cash, "Relocation preserves both stock totals and money");
            check(player.TryHoldCarton(other) && player.TryPlaceCarton(warehouse.Dock(0)), "Second brand can occupy vacated original first-brand dock");
            check(player.TryHoldCarton(first) && shop.OrderSupply(0,2), "Another same-brand delivery can arrive while old carton is held");
            shop.Tick(8.1f); cash = shop.Ledger.Cash;
            var delivered = warehouse.Carton(0);
            check(delivered != first && delivered.Quantity == 2 && warehouse.CartonAt(warehouse.Dock(0)) == other, "Delivery chooses free storage without overwriting relocated different-brand carton");
            check(delivered.transform.parent != lower && warehouse.CartonAt(delivered.transform.parent) == delivered, "Delivery remains separate from former carried source");
            player.CancelHeld();
            check(warehouse.CartonAt(lower) == first && first.Quantity == 3 && shop.WarehouseCount(0) == 5, "Delivery plus cancellation conserve both same-brand cartons");
            var returned = Object.FindObjectsByType<ShopProduct>().First(p => p.ProductId == ShopCatalog.Ids[0]);
            check(player.TryHold(returned), "Pick unit to return into one of two same-brand cartons");
            lower.GetComponent<ShopStation>().Use(player);
            check(!player.HasHeldItem && first.Quantity == 4 && delivered.Quantity == 2 && shop.WarehouseCount(0) == 6, "Return increments the targeted physical carton, never the other same-brand carton");

            var retail = Object.FindObjectsByType<ProductSlot>().Where(s => s.IsStockShelf && s.Occupant != null).Take(2).ToArray();
            foreach (var slot in retail) slot.Occupant.RemoveFromWorld();
            int stock = shop.StockCount(1);
            check(player.TryHoldCarton(other), "Pick relocated carton for normal retail restocking");
            foreach (var slot in retail) { Physics.SyncTransforms(); check(other.TryPlaceOne(slot), "Relocated carton still unpacks real product"); }
            check(other.Quantity == 0 && shop.WarehouseCount(1) == 0 && shop.StockCount(1) == stock+2, "Restocking conserves each paid unit");
            destination.GetComponent<ShopStation>().Use(player);
            check(warehouse.CartonAt(destination) == other && other.IsDocked && label(destination) == "", "Empty cardboard occupies a place but does not claim product label");
            check(player.TryHoldCarton(first) && !player.TryPlaceCarton(destination), "Empty carton cannot be overwritten even with blank label");
            player.CancelHeld();
            check(player.TryHold(retail[0].Occupant), "Pick retail unit for physical relocated carton return");
            destination.GetComponent<ShopStation>().Use(player);
            check(!player.HasHeldItem && other.Quantity == 1 && shop.WarehouseCount(1) == 1 && shop.StockCount(1) == stock+1, "Individual return uses physically present carton brand rather than original dock brand");
            check(label(destination) == ShopCatalog.Names[1] && shop.Ledger.Cash == cash, "Returned goods refresh actual label without changing money");
            var poses = new[] { first, other, delivered }.Select(c => c.transform.position).ToArray();
            shop.OpenShop(); shop.RequestClose(); shop.Tick(.1f); shop.NextDay();
            check(new[] { first, other, delivered }.Select((c,i) => c.transform.position == poses[i]).All(x => x), "Next day preserves relocated physical cartons");
            check(shop.WarehouseCount(0) == 6 && shop.WarehouseCount(1) == 1, "Next day preserves exact relocated inventory");
            shop.ResetDemo(); ShopCheckFixtures.Fund(shop, 10000);
            check(shop.OrderSupply(0,1), "Buy carton for occupied-source cancellation");
            shop.Tick(8.1f); var carried = warehouse.Carton(0);
            check(player.TryHoldCarton(carried) && shop.OrderSupply(0,1), "Order replacement while original source is vacated");
            shop.Tick(8.1f); var replacement = warehouse.Carton(0);
            check(replacement != carried && warehouse.CartonAt(warehouse.Dock(0)) == replacement, "Delivery may use the now vacant source");
            player.CancelHeld();
            check(!player.HasHeldItem && !carried.IsHeld && !carried.IsDocked && carried.Quantity == 1 && replacement.Quantity == 1 && shop.WarehouseCount(0) == 2, "Cancellation safely retains both cartons when source was occupied during carrying");
            shop.ResetDemo(); warehouse.RefreshLabels();
            check(docks.All(d => warehouse.CartonAt(d) == null && label(d) == ""), "New game clears every physical carton place and label");
            check(warehouse.StorageDocks.Count == docks.Length, "Reset does not duplicate runtime storage places");
            return new { passed, failed = 0, storagePlaces = docks.Length };
        }
        finally { player.CancelHeld(); shop.ClosePanel(); shop.ResetDemo(); shop.ManualTick = false; }
    }
}
