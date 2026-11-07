using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class ShopDevices
{
    private sealed class FurnishingOffer
    {
        public string Id, Title, Detail;
        public int Price;
        public bool Owned, Enabled;
        public Action Buy;
    }

    private List<FurnishingOffer> FurnishingOffers()
    {
        var offers = new List<FurnishingOffer>();
        if (furnishingPage == 1)
        {
            for (int i = 0; i < ShopArrangement.CatalogNames.Length; i++)
            {
                int kind = i;
                string reason = shop.Arrangement.PurchaseUnavailable(kind);
                offers.Add(new FurnishingOffer {
                    Id = "Module:Plan:" + kind, Title = ShopArrangement.CatalogNames[kind],
                    Detail = reason == "" ? ShopArrangement.CatalogDetails[kind] : reason,
                    Price = ShopArrangement.CatalogPrice(kind), Enabled = reason == "",
                    Owned = false,
                    Buy = () => { if (BeginArrangement()) shop.Arrangement.StartPurchase(kind); }
                });
            }
            return offers;
        }
        if (furnishingPage == 2)
        {
            for (int i = 0; i < ShopPartnerCatalog.Names.Length; i++)
            {
                var kind = (ShopPartnerKind)i;
                string reason = shop.Partners.Unavailable(kind);
                offers.Add(new FurnishingOffer {
                    Id = "Partner:Buy:" + i, Title = ShopPartnerCatalog.Names[i],
                    Detail = reason == "" || shop.Partners.State.Owned(kind) ? ShopPartnerCatalog.Details[i] : reason,
                    Price = ShopPartnerCatalog.PackagePrice, Owned = shop.Partners.State.Owned(kind), Enabled = reason == "",
                    Buy = () => {
                        if (kind == ShopPartnerKind.Muszakkezdo) shop.Partners.BuyCounterDisplay();
                        else if (BeginArrangement()) shop.Arrangement.StartPurchase(ShopPartnerCatalog.FirstFurnitureKind + (int)kind);
                    }
                });
            }
            return offers;
        }
        var layout = shop.Layout;
        offers.Add(new FurnishingOffer { Id = "Expand:0", Title = "Üzlet bővítése", Detail = (5 + layout.ShopLevel * 2) + " × 6,3 m · " + (layout.ShopLevel + 1) + "/4 szint", Price = layout.NextShopPrice, Owned = layout.ShopLevel == 3, Buy = () => shop.TryExpand() });
        offers.Add(new FurnishingOffer { Id = "Warehouse:Expand", Title = "Raktár bővítése", Detail = (layout.WarehouseLevel + 1) + "/4 szint", Price = layout.NextWarehousePrice, Owned = layout.WarehouseLevel == 3, Buy = () => shop.GetComponent<ShopExpansionGeometry>().BuyWarehouse() });
        offers.Add(new FurnishingOffer { Id = "CoffeeMachineUpgrade:0", Title = "Kávégép", Detail = "Friss kávé a vásárlóknak", Price = ShopActivities.CoffeeMachinePrice, Owned = shop.Activities.CoffeeOwned, Buy = () => shop.Activities.BuyCoffeeMachine() });
        offers.Add(new FurnishingOffer { Id = "Lottery:Upgrade", Title = "Lottópont", Detail = "Szerecsenjáték Zrt.", Price = ShopLotteryCatalog.UpgradePrice, Owned = shop.Lottery.Owned, Buy = () => shop.Lottery.TryBuy() });
        offers.Add(new FurnishingOffer { Id = "PanicButtonUpgrade:0", Title = "Vészgomb", Detail = "Segítség veszély esetén", Price = ShopSecurity.PurchasePrice, Owned = shop.Security.Owned, Buy = () => PurchasePanicButton() });
        foreach (var offer in offers) offer.Enabled = !offer.Owned && shop.CanDevelopShop && shop.Ledger.AvailableFunds >= offer.Price;
        return offers;
    }

    private void AddFurnishingControls(List<ShopKeyboardTarget> controls)
    {
        controls.Add(new ShopKeyboardTarget("Furnishing:Developments", "Fejlesztések", new Rect(ComputerBody.x + 12, ComputerBody.y + 8, 240, 44), () => furnishingPage = 0));
        controls.Add(new ShopKeyboardTarget("Furnishing:Furniture", "Bútorok", new Rect(ComputerBody.x + 264, ComputerBody.y + 8, 240, 44), () => furnishingPage = 1));
        controls.Add(new ShopKeyboardTarget("Furnishing:Partners", "Partnerségek", new Rect(ComputerBody.x + 516, ComputerBody.y + 8, 240, 44), () => furnishingPage = 2));
        var offers = FurnishingOffers();
        for (int i = 0; i < offers.Count; i++)
        {
            var offer = offers[i];
            controls.Add(new ShopKeyboardTarget(offer.Id, offer.Owned ? "Megvásárolva" : furnishingPage == 1 ? "Elhelyezés" : "Megveszem",
                new Rect(ComputerBody.xMax - 226, ComputerBody.y + 64 + i * FurnishingRowHeight, 216, furnishingPage == 1 ? 50 : 54), offer.Buy, offer.Enabled));
        }
        controls.Add(new ShopKeyboardTarget("Arrange:0", "Üzlet átrendezése", new Rect(ComputerBody.x + 12, ComputerBody.yMax - 54, 340, 44), () => BeginArrangement(), shop.CanDevelopShop));
    }

    private int FurnishingRowHeight => furnishingPage == 2 ? 112 : furnishingPage == 1 ? 56 : 64;
    private void DrawFurnishingRows()
    {
        var offers = FurnishingOffers();
        for (int i = 0; i < offers.Count; i++)
        {
            var offer = offers[i];
            float y = ComputerBody.y + 64 + i * FurnishingRowHeight;
            ShopComputerTheme.Label(new Rect(ComputerBody.x + 18, y, 570, 28), offer.Title, 23, true);
            ShopComputerTheme.Label(new Rect(ComputerBody.x + 18, y + 29, 570, furnishingPage == 2 ? 58 : 25), offer.Detail, 17);
            ShopComputerTheme.Label(new Rect(ComputerBody.x + 620, y, 230, 54), offer.Owned ? "Kész" : ShopCatalog.Money(offer.Price), 22, true);
            ShopUi.Fill(new Rect(ComputerBody.x + 18, y + (furnishingPage == 2 ? 102 : furnishingPage == 1 ? 54 : 60), ComputerBody.width - 36, 1), ShopComputerTheme.Rule);
        }
    }
}
