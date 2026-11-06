using System;
using System.Collections.Generic;

public enum ShopPurchaseError { None, Closed, InvalidBasket, InvalidQuantity, Unavailable, Capacity, Empty, Funds }

public sealed class ShopPurchaseQuote
{
    private readonly int[] quantities;
    public ShopPurchaseError Error { get; }
    public int Total { get; }
    public bool Accepted => Error == ShopPurchaseError.None;
    public int Count => quantities.Length;
    public int Quantity(int type) => quantities[type];

    internal ShopPurchaseQuote(ShopPurchaseError error, int total, int[] quantities)
    {
        Error = error;
        Total = total;
        this.quantities = quantities == null ? Array.Empty<int>() : (int[])quantities.Clone();
    }
}

// Pure quote calculation: no ledger, deliveries or physical objects are mutated here.
public static class ShopPurchaseRules
{
    public static ShopPurchaseQuote Quote(int[] quantities, IReadOnlyList<int> costs, int availableTypes,
        IReadOnlyList<int> stored, IReadOnlyList<int> pending, int funds, bool closed, int maxQuantity, int capacity)
    {
        ShopPurchaseQuote Reject(ShopPurchaseError error) => new ShopPurchaseQuote(error, 0, null);
        if (closed) return Reject(ShopPurchaseError.Closed);
        if (quantities == null || quantities.Length != stored.Count || costs.Count < stored.Count || pending.Count != stored.Count)
            return Reject(ShopPurchaseError.InvalidBasket);
        long total = 0;
        for (int type = 0; type < quantities.Length; type++)
        {
            int quantity = quantities[type];
            if (quantity < 0 || quantity > maxQuantity) return Reject(ShopPurchaseError.InvalidQuantity);
            if (quantity > 0 && type >= availableTypes) return Reject(ShopPurchaseError.Unavailable);
            if ((long)stored[type] + pending[type] + quantity > capacity) return Reject(ShopPurchaseError.Capacity);
            total += (long)costs[type] * quantity;
        }
        if (total == 0) return Reject(ShopPurchaseError.Empty);
        if (total > funds || total > int.MaxValue) return Reject(ShopPurchaseError.Funds);
        return new ShopPurchaseQuote(ShopPurchaseError.None, (int)total, quantities);
    }
}
