using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Application session owns receipt membership and service state; physical slots are Unity adapters.
public sealed class ShopCheckoutSession
{
    private readonly List<ShopProduct> items = new List<ShopProduct>();
    private readonly HashSet<ProductSlot> receipts = new HashSet<ProductSlot>();
    private ProductSlot basket;
    private Transform receiptParent;
    private Func<int, Vector3> position;
    public IReadOnlyList<ShopProduct> Items => items;
    public ServiceStage Stage { get; set; }

    public void Configure(ProductSlot basket, Transform receiptParent, Func<int, Vector3> position)
    {
        this.basket = basket;
        this.receiptParent = receiptParent;
        this.position = position;
    }

    public bool OwnsReceipt(ProductSlot slot) => slot != null && receipts.Contains(slot);
    public bool IsHandover(ProductSlot slot) => slot != null && (slot == basket || OwnsReceipt(slot));
    private bool ValidItem(ShopProduct item) => item != null && item.gameObject.activeSelf && !item.IsHeld && IsHandover(item.CurrentSlot);
    public bool IsComplete(int[] requested) => requested != null && items.Count == requested.Length && items.All(ValidItem);
    public int[] Missing(int[] requested) => requested == null ? Array.Empty<int>() : requested.GroupBy(t => t)
        .SelectMany(group => Enumerable.Repeat(group.Key, Math.Max(0, group.Count() - items.Count(item =>
            ValidItem(item) && item.ProductId == ShopCatalog.Ids[group.Key])))).ToArray();

    public bool Collect(ShopProduct product, int[] requested, out string rejection)
    {
        rejection = null;
        items.RemoveAll(item => !ValidItem(item));
        if (product != null && !items.Contains(product))
        {
            int type = ShopCatalog.Index(product.ProductId);
            if (requested.Count(t => t == type) <= items.Count(item => item.ProductId == product.ProductId))
            { rejection = "Ez a termék már a kosárban van."; return false; }
            if (product.CurrentSlot == basket)
            { items.Add(product); return IsComplete(requested); }
            int freeIndex = Enumerable.Range(0, requested.Length).Where(index =>
                (index != 0 || basket.Occupant == null) && !items.Any(item =>
                    item.CurrentSlot != null && Vector3.Distance(item.CurrentSlot.transform.position, position(index)) < .02f))
                .DefaultIfEmpty(-1).First();
            if (freeIndex < 0) { rejection = "Tedd szabaddá a kosár pulthelyeit."; return false; }
            ProductSlot destination = basket;
            if (freeIndex != 0)
            {
                destination = new GameObject("Order receipt " + freeIndex).AddComponent<ProductSlot>();
                destination.transform.SetParent(receiptParent, false);
                destination.transform.position = position(freeIndex);
                destination.Configure(new Vector3(.22f, .4f, .33f));
                destination.SetManualPlacementRule(_ => false);
                receipts.Add(destination);
            }
            if (!(product.IsHeld ? product.TryPlace(destination) : product.AssignInitialSlot(destination)))
            { if (destination != basket) RemoveReceipt(destination); return false; }
            if (destination != basket) destination.RestrictTo("__receipt_locked");
            if (!items.Contains(product)) items.Add(product);
        }
        return IsComplete(requested);
    }

    public bool TrySettle(Func<bool> settle)
    {
        if (Stage != ServiceStage.Payment || !settle()) return false;
        Stage = ServiceStage.Thanks;
        return true;
    }

    public void RemoveReceipt(ProductSlot slot)
    {
        if (slot == null || !receipts.Remove(slot)) return;
        slot.gameObject.SetActive(false);
        UnityEngine.Object.Destroy(slot.gameObject);
    }

    public void CleanupEmptyReceipts()
    {
        foreach (var slot in receipts.ToArray())
            if (slot == null) receipts.Remove(slot);
            else if (slot.Occupant == null) RemoveReceipt(slot);
    }

    public void ClearItems() => items.Clear();
}
