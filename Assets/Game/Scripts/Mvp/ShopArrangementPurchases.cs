using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class ShopArrangement
{
    // Index 4 is the existing, once-only fridge; other entries are repeatable modules.
    public static readonly string[] CatalogNames = { "Normál polc", "Keskeny polc", "Kétoldalas polc", "Raktári állvány", "Hűtő" };
    public static readonly string[] CatalogDetails = { "1,2 × 0,5 m", "0,6 × 0,5 m", "1,2 × 0,9 m · kétoldalas", "1,2 × 0,6 m · raktár", "1,65 × 0,8 m" };
    private readonly Dictionary<ShopFurniture, int> plannedPurchases = new Dictionary<ShopFurniture, int>();
    private ShopFurniture purchaseGhost;
    private int purchaseKind = -1;
    private bool shopping;
    public int PlannedCount => plannedPurchases.Count;
    public ShopFurniture PurchasePreview => purchaseGhost;
    public int PlannedCost => plannedPurchases.Values.Sum(CatalogPrice);
    public int RemainingFunds => shop.Ledger.AvailableFunds - PlannedCost;
    public static int CatalogPrice(int kind) => kind == 4 ? ShopNextUpgrades.FridgePrice : ShopModularFurniture.Prices[kind];
    public bool IsPlanned(ShopFurniture item) => plannedPurchases.ContainsKey(item);

    public string PurchaseUnavailable(int kind)
    {
        if (kind < 0 || kind >= CatalogNames.Length) return "Ismeretlen bútor.";
        if (!shop.CanDevelopShop) return "Vásárolni nyitás előtt vagy zárás után, üres boltban lehet.";
        if (kind == 4)
        {
            if (shop.GetComponent<ShopNextUpgrades>().FridgeOwned || plannedPurchases.ContainsValue(4)) return "A hűtő már megvan a boltban vagy a tervben.";
            if (!shop.Layout.Expanded) return "A hűtőhöz előbb bővítsd az üzletet.";
        }
        return CatalogPrice(kind) > RemainingFunds ? "Nincs elég elkölthető pénz a következő darabra." : "";
    }

    public bool StartPurchase(int kind)
    {
        if (!Active) return false;
        string reason = PurchaseUnavailable(kind);
        if (reason != "") { Feedback = reason; return false; }
        DiscardPurchaseGhost();
        purchaseKind = kind;
        purchaseGhost = kind == 4 ? shop.GetComponent<ShopNextUpgrades>().FridgeFurniture :
            shop.GetComponent<ShopModularFurniture>().CreatePreview((ShopModuleKind)kind);
        selected = purchaseGhost;
        preview = new ShopFurniturePose(new Vector3(99, 0, 99), 0);
        dragging = true;
        shopping = true;
        ValidatePreview();
        return true;
    }

    private void DiscardPurchaseGhost()
    {
        if (purchaseGhost == null) return;
        if (purchaseKind != 4) Destroy(purchaseGhost.gameObject);
        if (selected == purchaseGhost) { selected = null; dragging = false; }
        purchaseGhost = null;
        purchaseKind = -1;
    }

    public bool PlacePurchase(ShopFurniturePose pose)
    {
        if (!Active || purchaseGhost == null) return false;
        string reason = PurchaseUnavailable(purchaseKind);
        if (reason != "") { Feedback = reason; return false; }
        pose = SnapPose(purchaseGhost, pose);
        var candidate = new Dictionary<ShopFurniture, ShopFurniturePose>(draft) { [purchaseGhost] = pose };
        if (!Validate(candidate, out reason)) { Feedback = reason; return false; }
        int kind = purchaseKind;
        draft = candidate;
        plannedPurchases.Add(purchaseGhost, kind);
        purchaseGhost = null;
        purchaseKind = -1;
        dragging = false;
        Feedback = "A bútor a tervben van. Fizetés a mentéskor.";
        if (PurchaseUnavailable(kind) == "") StartPurchase(kind);
        return true;
    }

    public bool RemovePlanned(ShopFurniture item)
    {
        if (!Active || !plannedPurchases.TryGetValue(item, out int kind)) return false;
        DiscardPurchaseGhost();
        plannedPurchases.Remove(item);
        draft.Remove(item);
        if (selected == item) { selected = null; dragging = false; }
        if (kind != 4) Destroy(item.gameObject);
        Feedback = "Bútor törölve a tervből. Nem történt pénzlevonás.";
        return true;
    }

    private void ClearPlannedPurchases()
    {
        DiscardPurchaseGhost();
        foreach (var pair in plannedPurchases)
            if (pair.Value != 4 && pair.Key != null) Destroy(pair.Key.gameObject);
        plannedPurchases.Clear();
    }

    private void CommitPlannedPurchases()
    {
        foreach (var pair in plannedPurchases)
        {
            pair.Key.gameObject.SetActive(true);
            if (pair.Value == 4) shop.GetComponent<ShopNextUpgrades>().CommitPlannedFridge();
            else shop.GetComponent<ShopModularFurniture>().CommitPlannedPurchase(pair.Key);
        }
        plannedPurchases.Clear();
    }
}
