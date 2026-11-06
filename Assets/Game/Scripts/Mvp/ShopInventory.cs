using System;
using System.Collections.Generic;
using System.Linq;

// Sole writer of stored quantities and pending deliveries. Physical products stay in Unity adapters.
public sealed class ShopInventory
{
    public const int Capacity = 24;
    private readonly int[] stored;
    private readonly List<Shipment> shipments = new List<Shipment>();
    public readonly struct Arrival
    {
        public int Type { get; }
        public int Quantity { get; }
        public Arrival(int type, int quantity) { Type = type; Quantity = quantity; }
    }
    private sealed class Shipment
    {
        public int Type, Quantity;
        public float Remaining;
    }

    public ShopInventory(int types) { stored = new int[types]; }
    public int Stored(int type) => type >= 0 && type < stored.Length ? stored[type] : 0;
    public int Pending(int type) => shipments.Where(s => s.Type == type).Sum(s => s.Quantity);
    public float NextDelivery => shipments.Count == 0 ? 0 : shipments.Min(s => s.Remaining);
    public int FirstStoredType(int availableTypes) => Enumerable.Range(0, Math.Min(availableTypes, stored.Length)).Where(t => stored[t] > 0).DefaultIfEmpty(-1).First();

    public ShopPurchaseQuote Quote(int[] quantities, IReadOnlyList<int> costs, int availableTypes, int funds, bool closed, int maxQuantity)
        => ShopPurchaseRules.Quote(quantities, costs, availableTypes, stored,
            Enumerable.Range(0, stored.Length).Select(Pending).ToArray(), funds, closed, maxQuantity, Capacity);

    public bool TryPurchase(ShopPurchaseQuote quote, Func<int, bool> debit)
    {
        // Revalidate capacity immediately before the only mutating application step.
        if (!quote.Accepted || quote.Count != stored.Length) return false;
        for (int type = 0; type < stored.Length; type++)
            if ((long)Stored(type) + Pending(type) + quote.Quantity(type) > Capacity) return false;
        if (!debit(quote.Total)) return false;
        for (int type = 0; type < quote.Count; type++)
            if (quote.Quantity(type) > 0)
                shipments.Add(new Shipment { Type = type, Quantity = quote.Quantity(type), Remaining = 8f });
        return true;
    }

    public bool TryWithdraw(int type)
    {
        if (Stored(type) <= 0) return false;
        stored[type]--;
        return true;
    }

    public void RestoreWithdrawal(int type) { stored[type]++; }
    public bool CanReturn(int type) => type >= 0 && type < stored.Length && Stored(type) + Pending(type) < Capacity;
    public bool TryReturn(int type)
    {
        if (!CanReturn(type)) return false;
        stored[type]++;
        return true;
    }

    public IReadOnlyList<Arrival> Tick(float dt)
    {
        if (shipments.Count == 0) return Array.Empty<Arrival>();
        var arrivals = new List<Arrival>();
        for (int i = shipments.Count - 1; i >= 0; i--)
        {
            var shipment = shipments[i];
            shipment.Remaining -= Math.Max(0, dt);
            if (shipment.Remaining > 0) continue;
            stored[shipment.Type] += shipment.Quantity;
            arrivals.Add(new Arrival(shipment.Type, shipment.Quantity));
            shipments.RemoveAt(i);
        }
        return arrivals;
    }

    public void CompleteDeliveries()
    {
        foreach (var shipment in shipments) stored[shipment.Type] += shipment.Quantity;
        shipments.Clear();
    }

    public void Reset() { shipments.Clear(); Array.Clear(stored, 0, stored.Length); }
}
