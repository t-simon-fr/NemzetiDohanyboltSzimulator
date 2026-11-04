using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ShelfLabelChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); passed++; };
        var shop = MvpShopController.Instance;
        var player = Object.FindAnyObjectByType<PlayerInteractor>();
        var fixture = new GameObject("Shelf labels fixture").AddComponent<ShopAcceptanceProbe>();
        Action refresh = () => {
            foreach (var label in Object.FindObjectsByType<ShopShelfPriceLabel>(FindObjectsInactive.Include)) label.Refresh();
            shop.Warehouse.RefreshLabels();
            Physics.SyncTransforms();
        };
        Func<int, string> warehouseLabel = type => shop.Warehouse.Dock(type).Find("Brand dock label").GetComponent<TextMesh>().text;
        try
        {
            fixture.Prepare("shelf-labels");
            refresh();
            var controller = player.GetComponent<FirstPersonController>();
            for (int step = 0; step < 20; step++) controller.StepMovement(Vector2.zero, false, false, .02f);
            check(player.transform.position.y > -.1f, "Warehouse fixture stands on the actual aisle floor");
            var aim = Camera.main.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            check(Physics.Raycast(aim, out var hit, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<ShopStation>()?.IsSupplies == true, "Warehouse fixture targets its actual shelf without obstruction");
            var labels = Object.FindObjectsByType<ShopShelfPriceLabel>(FindObjectsInactive.Include);
            check(labels.All(label => label.GetComponent<TextMesh>().text == ""), "All empty retail and private shelf labels are blank");
            check(Enumerable.Range(0, ShopCatalog.Count).All(type => warehouseLabel(type) == ""), "All warehouse labels start blank");
            check(shop.Warehouse.Dock(0).GetComponent<ShopStation>().Prompt == "", "Empty warehouse shelf has no pickup prompt");
            check(typeof(ShopStation).GetProperty("Contents") == null, "Supply station no longer exposes a popup inventory list");
            int before = shop.Ledger.Cash;
            check(shop.OrderSupply(0, 2), "Order real delivery");
            check(warehouseLabel(0) == "", "Pending order does not label an empty shelf");
            shop.Tick(8.1f); refresh();
            check(warehouseLabel(0) == ShopCatalog.Names[0] && shop.WarehouseCount(0) == 2, "Arrived carton labels its physical shelf");
            check(shop.Ledger.Cash == before - 2 * ShopCatalog.Costs[0], "Delivery preserves exact purchase cost");
            int paid = shop.Ledger.Cash;
            var carton = shop.Warehouse.Carton(0);
            check(player.TryHoldCarton(carton), "Pick up actual carton");
            check(warehouseLabel(0) == "" && shop.WarehouseCount(0) == 2, "Carried goods clear shelf label without losing inventory");
            var label = labels.First(item => item.name == "Shelf price 0-0");
            var slots = (ProductSlot[])typeof(ShopShelfPriceLabel).GetField("slots", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(label);
            check(carton.TryPlaceOne(slots[0]), "Stock one real product from carton");
            refresh();
            var text = label.GetComponent<TextMesh>();
            check(text.text.Contains(ShopCatalog.ShortNames[0]) && text.text.Contains(ShopCatalog.Money(ShopCatalog.SalePrice(0))), "Retail label shows actual product and current price");
            check(shop.WarehouseCount(0) == 1 && shop.StockCount(0) == 1 && carton.Quantity == 1, "Stocking conserves warehouse and retail quantities");
            player.ReturnCarton(); refresh();
            check(warehouseLabel(0) == ShopCatalog.Names[0], "Putting carton back restores its shelf label");
            var product = slots[0].Occupant;
            check(player.TryHold(product), "Pick product up from retail shelf");
            refresh();
            check(text.text == "", "Held product does not leave a phantom shelf label");
            player.CancelHeld(); refresh();
            check(text.text.Contains(ShopCatalog.ShortNames[0]), "Cancelling pickup restores actual source label");
            product.RemoveFromWorld(); refresh();
            check(text.text == "", "Removing last product clears shelf label");
            var replacement = shop.CreateCoffee(1);
            check(replacement.AssignInitialSlot(slots[0]), "Place a different product in the former brand lane");
            refresh();
            check(text.text.Contains(ShopCatalog.ShortNames[1]) && !text.text.Contains(ShopCatalog.ShortNames[0]), "Replacement label follows contents instead of preferred brand");
            check(shop.CreateCoffee(0).AssignInitialSlot(slots[1]), "Put two different products in one labelled lane");
            refresh();
            check(text.text.Contains(ShopCatalog.ShortNames[0]) && text.text.Contains(ShopCatalog.ShortNames[1]) && !text.text.Contains("Ft"), "Mixed lane names both goods without a misleading common price");
            check(shop.Pricing.TrySetPrice(1, ShopCatalog.Prices[1] + 100), "Change actual retail price");
            slots[1].Occupant.RemoveFromWorld(); refresh();
            check(text.text.Contains(ShopCatalog.Money(ShopCatalog.SalePrice(1))), "Single-product price follows current pricing after mixed stock removal");
            check(player.TryHoldCarton(carton), "Take remaining warehouse carton");
            player.ReturnCarton();
            carton.DropLoose(shop.Warehouse.Dock(1), shop.Warehouse.Dock(1).TransformPoint(Vector3.up * .055f));
            refresh();
            check(warehouseLabel(0) == "" && warehouseLabel(1) == ShopCatalog.Names[0], "Warehouse label follows actual carton position rather than original dock brand");
            carton.AdjustQuantity(-1); refresh();
            check(warehouseLabel(1) == "", "Empty cardboard does not count as shelf goods");
            check(shop.Ledger.Cash == paid, "Labels and manual handling never change money");
            shop.CreateNightProduct().AssignInitialSlot(shop.CounterStorage.Slots[0]); refresh();
            var privateLabel = labels.First(item => item.name == "Private storage label");
            check(privateLabel.GetComponent<TextMesh>().text.Contains("Dnyipro"), "Private storage label appears only with actual stock");
            shop.ResetDemo(); refresh();
            check(Enumerable.Range(0, ShopCatalog.Count).All(type => warehouseLabel(type) == "") && privateLabel.GetComponent<TextMesh>().text == "", "New game clears warehouse and private labels");
            return new { passed, failed = 0 };
        }
        finally
        {
            player.CancelHeld(); shop.ResetDemo(); shop.ManualTick = false;
            Object.Destroy(fixture.gameObject);
        }
    }
}
