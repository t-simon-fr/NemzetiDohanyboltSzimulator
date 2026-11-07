using System;
using System.Collections.Generic;

public enum ShopPurchaseError { None, Closed, InvalidBasket, InvalidQuantity, Unavailable, Capacity, Empty, Funds }

public sealed class ShopPurchaseQuote
{
    private readonly int[] quantities, costs;
    public ShopPurchaseError Error { get; }
    public int Total { get; }
    public int Subtotal { get; }
    public int Rounding => Total - Subtotal;
    public int UnitCost(int type) => costs[type];
    public bool Accepted => Error == ShopPurchaseError.None;
    public int Count => quantities.Length;
    public int Quantity(int type) => quantities[type];

    internal ShopPurchaseQuote(ShopPurchaseError error, int total, int[] quantities, IReadOnlyList<int> costs = null, int subtotal = -1)
    {
        Error = error;
        Total = total;
        Subtotal = subtotal < 0 ? total : subtotal;
        this.costs = new int[costs?.Count ?? 0];
        for (int i = 0; i < this.costs.Length; i++) this.costs[i] = costs[i];
        this.quantities = quantities == null ? Array.Empty<int>() : (int[])quantities.Clone();
    }
}

// Pure quote calculation: no ledger, deliveries or physical objects are mutated here.
public static class ShopPurchaseRules
{
    public static long Payment(long subtotal, int step) => (subtotal + step / 2) / step * step;
    public static ShopPurchaseQuote Quote(int[] quantities, IReadOnlyList<int> costs, int availableTypes,
        IReadOnlyList<int> stored, IReadOnlyList<int> pending, int funds, bool closed, int maxQuantity, int capacity, IReadOnlyList<bool> allowed = null, int paymentStep = 1)
    {
        ShopPurchaseQuote Reject(ShopPurchaseError error) => new ShopPurchaseQuote(error, 0, null);
        if (closed) return Reject(ShopPurchaseError.Closed);
        if (quantities == null || quantities.Length != stored.Count || costs.Count < stored.Count || pending.Count != stored.Count || allowed != null && allowed.Count != stored.Count || paymentStep < 1)
            return Reject(ShopPurchaseError.InvalidBasket);
        long total = 0;
        for (int type = 0; type < quantities.Length; type++)
        {
            int quantity = quantities[type];
            if (quantity < 0 || quantity > maxQuantity) return Reject(ShopPurchaseError.InvalidQuantity);
            if (quantity > 0 && (allowed == null ? type >= availableTypes : !allowed[type])) return Reject(ShopPurchaseError.Unavailable);
            if ((long)stored[type] + pending[type] + quantity > capacity) return Reject(ShopPurchaseError.Capacity);
            total += (long)costs[type] * quantity;
        }
        if (total == 0) return Reject(ShopPurchaseError.Empty);
        long payment = Payment(total, paymentStep);
        if (payment > funds || payment > int.MaxValue || total > int.MaxValue) return Reject(ShopPurchaseError.Funds);
        return new ShopPurchaseQuote(ShopPurchaseError.None, (int)payment, quantities, costs, (int)total);
    }
}
