using System.Collections.Generic;
using UnityEngine;

public sealed class ShopPartnerships : MonoBehaviour
{
    public ShopPartnerState State { get; } = new ShopPartnerState();
    private MvpShopController shop;
    private ShopPartnerVisuals visuals;
    private readonly List<GameObject> displays = new List<GameObject>();
    public GameObject CounterDisplay { get; private set; }
    public void Configure(MvpShopController owner)
    {
        shop = owner;
        visuals = new ShopPartnerVisuals();
    }
    public string Unavailable(ShopPartnerKind kind)
    {
        if (!ShopPartnerCatalog.Valid(kind)) return "Ismeretlen partnerség.";
        if (State.Owned(kind)) return "Aktív partnerség • 10% beszerzési kedvezmény";
        if (kind != ShopPartnerKind.Muszakkezdo && !shop.Layout.Expanded) return "Hűtős partnercsomaghoz bővítsd az üzletet.";
        if (!shop.CanDevelopShop) return "Nyitás előtt vagy zárás után, üres boltban vásárolható.";
        return shop.Ledger.AvailableFunds < ShopPartnerCatalog.PackagePrice ? "Nincs elég elkölthető pénz a partnercsomagra." : "";
    }
    public ShopFurniture CreateFridgePreview(ShopPartnerKind kind)
    {
        var item = shop.GetComponent<ShopNextUpgrades>().CreateFridgePreview();
        item.CatalogKind = ShopPartnerCatalog.FirstFurnitureKind + (int)kind;
        visuals.DecorateFridge(item, kind);
        item.Configure("partner-fridge-" + kind, ShopPartnerCatalog.Names[(int)kind] + " hűtő", new Vector2(1.65f, .8f));
        foreach (var slot in item.Slots)
            slot.SetStockFilter(id => ShopPartnerCatalog.PartnerFor(ShopCatalog.Index(id)) == (int)kind);
        return item;
    }
    public void CommitFridge(ShopFurniture item)
    {
        var kind = (ShopPartnerKind)(item.CatalogKind - ShopPartnerCatalog.FirstFurnitureKind);
        State.Activate(kind);
        displays.Add(item.gameObject);
        shop.Arrangement.Adopt(item);
        shop.Layout.RegisterPublicSlots(item.Slots);
    }
    public bool BuyCounterDisplay()
    {
        var kind = ShopPartnerKind.Muszakkezdo;
        string reason = Unavailable(kind);
        if (reason != "") { shop.Notify(reason); return false; }
        var display = visuals.CreateCounterDisplay(transform);
        display.SetActive(false);
        if (!shop.Ledger.TryImprove(ShopPartnerCatalog.PackagePrice))
        {
            Destroy(display);
            return false;
        }
        State.Activate(kind);
        display.SetActive(true);
        displays.Add(display);
        CounterDisplay = display;
        shop.SyncCashDrawer();
        shop.Navigation.Rebuild();
        shop.Notify("Műszakkezdő partnerség aktív. Három új pálinka és 10% beszerzési kedvezmény.");
        return true;
    }
    public void AddProductVisual(GameObject item, int type) => visuals.AddProduct(item, type);
    public void ResetAll()
    {
        foreach (var display in displays)
        {
            if (display == null) continue;
            var furniture = display.GetComponent<ShopFurniture>();
            if (furniture != null) shop.Arrangement.Forget(furniture);
            display.SetActive(false);
            Destroy(display);
        }
        displays.Clear();
        CounterDisplay = null;
        State.Reset();
    }
    private void OnDestroy() => visuals?.Dispose();
}
