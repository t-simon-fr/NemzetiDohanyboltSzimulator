using UnityEngine;

// Composition root: creation order is explicit and separate from game commands and ticks.
public sealed class ShopBootstrap
{
    public void Initialize(MvpShopController shop, PlayerInteractor player)
    {
        shop.InitializeRuntimeRoots();
        shop.FloorPlan = shop.gameObject.AddComponent<ShopFloorPlan>();
        shop.FloorPlan.Configure(shop, player);
        shop.InteriorLighting = shop.gameObject.AddComponent<ShopInteriorLighting>();
        shop.InteriorLighting.Configure(shop);
        shop.CounterStorage = shop.gameObject.AddComponent<ShopCounterStorage>();
        shop.CounterStorage.Configure(shop);
        shop.Security = shop.GetComponent<ShopSecurity>();
        if (shop.Security == null) shop.Security = shop.gameObject.AddComponent<ShopSecurity>();
        shop.Security.Configure(shop);
        shop.Entrance = shop.gameObject.AddComponent<ShopEntrance>();
        shop.Entrance.Configure(shop);
        shop.Activities = shop.gameObject.AddComponent<ShopActivities>();
        shop.Activities.Configure(shop, player);
        shop.DeliverySlot.Configure(new Vector3(.4f, .4f, .4f));
        shop.gameObject.AddComponent<ShopNextUpgrades>().Configure(shop);
        shop.Lottery = shop.gameObject.AddComponent<ShopLotteryRetailer>();
        shop.Lottery.Configure(shop);
        shop.Pricing = shop.gameObject.AddComponent<ShopPricing>();
        shop.Pricing.Configure(shop);
        shop.AfterHours = shop.gameObject.AddComponent<ShopAfterHours>();
        shop.AfterHours.Configure(shop);
        shop.Navigation = shop.gameObject.AddComponent<ShopNavigation>();
        shop.Arrangement = shop.gameObject.AddComponent<ShopArrangement>();
        shop.Arrangement.Configure(shop, player);
        shop.gameObject.AddComponent<ShopModularFurniture>().Configure(shop);
        shop.Navigation.Configure(shop);
        shop.InitializeCheckout();
        shop.Scanner = new GameObject("Checkout barcode scanner").AddComponent<ShopBarcodeScanner>();
        shop.Scanner.Configure(shop);
        shop.gameObject.AddComponent<ShopTrafikIdentity>().Configure(shop);
        shop.gameObject.AddComponent<ShopExpansionGeometry>().Configure(shop);
        shop.ResetDemo();
        shop.gameObject.AddComponent<ShopMenu>().Configure(shop);
    }
}
