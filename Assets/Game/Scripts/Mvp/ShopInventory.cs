using System;
using System.Collections.Generic;
using System.Linq;

// Sole writer of stored quantities and pending deliveries. Physical products stay in Unity adapters.
public sealed class ShopInventory
{
    public const int Capacity = 24;
    private readonly int[] stored;
    private readonly List<int>[] unitCosts;
    private readonly List<Shipment> shipments = new List<Shipment>();
    public readonly struct Arrival
    {
        public int Type { get; }
        public int Quantity { get; }
        public Arrival(int type, int quantity) { Type = type; Quantity = quantity; }
    }
    private sealed class Shipment
    {
        public int Type, Quantity, UnitCost;
        public float Remaining;
    }

    public ShopInventory(int types)
    {
        stored = new int[types];
        unitCosts = Enumerable.Range(0, types).Select(_ => new List<int>()).ToArray();
    }
    public int NextUnitCost(int type) => unitCosts[type].Count > 0 ? unitCosts[type][0] : ShopCatalog.Costs[type];
    public int Stored(int type) => type >= 0 && type < stored.Length ? stored[type] : 0;
    public int Pending(int type) => shipments.Where(s => s.Type == type).Sum(s => s.Quantity);
    public float NextDelivery => shipments.Count == 0 ? 0 : shipments.Min(s => s.Remaining);
    public int FirstStoredType(int availableTypes) => Enumerable.Range(0, Math.Min(availableTypes, stored.Length)).Where(t => stored[t] > 0).DefaultIfEmpty(-1).First();

    public ShopPurchaseQuote Quote(int[] quantities, IReadOnlyList<int> costs, int availableTypes, int funds, bool closed, int maxQuantity, IReadOnlyList<bool> allowed = null, int paymentStep = 1)
        => ShopPurchaseRules.Quote(quantities, costs, availableTypes, stored,
            Enumerable.Range(0, stored.Length).Select(Pending).ToArray(), funds, closed, maxQuantity, Capacity, allowed, paymentStep);

    public bool TryPurchase(ShopPurchaseQuote quote, Func<int, bool> debit)
    {
        // Revalidate capacity immediately before the only mutating application step.
        if (!quote.Accepted || quote.Count != stored.Length) return false;
        for (int type = 0; type < stored.Length; type++)
            if ((long)Stored(type) + Pending(type) + quote.Quantity(type) > Capacity) return false;
        if (!debit(quote.Total)) return false;
        for (int type = 0; type < quote.Count; type++)
            if (quote.Quantity(type) > 0)
                shipments.Add(new Shipment { Type = type, Quantity = quote.Quantity(type), UnitCost = quote.UnitCost(type), Remaining = 8f });
        return true;
    }

    public bool TryWithdraw(int type)
    {
        if (Stored(type) <= 0) return false;
        stored[type]--;
        if (unitCosts[type].Count > 0) unitCosts[type].RemoveAt(0);
        return true;
    }

    public void RestoreWithdrawal(int type, int cost = -1)
    {
        stored[type]++;
        unitCosts[type].Insert(0, cost < 0 ? ShopCatalog.Costs[type] : cost);
    }
    public bool CanReturn(int type) => type >= 0 && type < stored.Length && Stored(type) + Pending(type) < Capacity;
    public bool TryReturn(int type, int cost = -1)
    {
        if (!CanReturn(type)) return false;
        stored[type]++;
        unitCosts[type].Add(cost < 0 ? ShopCatalog.Costs[type] : cost);
        return true;
    }

    public IReadOnlyList<Arrival> Tick(float dt)
    {
        if (shipments.Count == 0) return Array.Empty<Arrival>();
        var arrivals = new List<Arrival>();
        for (int i = 0; i < shipments.Count;)
        {
            var shipment = shipments[i];
            shipment.Remaining -= Math.Max(0, dt);
            if (shipment.Remaining > 0) { i++; continue; }
            stored[shipment.Type] += shipment.Quantity;
            unitCosts[shipment.Type].AddRange(Enumerable.Repeat(shipment.UnitCost, shipment.Quantity));
            arrivals.Add(new Arrival(shipment.Type, shipment.Quantity));
            shipments.RemoveAt(i);
        }
        return arrivals;
    }

    public void CompleteDeliveries()
    {
        foreach (var shipment in shipments)
        {
            stored[shipment.Type] += shipment.Quantity;
            unitCosts[shipment.Type].AddRange(Enumerable.Repeat(shipment.UnitCost, shipment.Quantity));
        }
        shipments.Clear();
    }

    public void Reset()
    {
        shipments.Clear();
        Array.Clear(stored, 0, stored.Length);
        foreach (var costs in unitCosts) costs.Clear();
    }
}
