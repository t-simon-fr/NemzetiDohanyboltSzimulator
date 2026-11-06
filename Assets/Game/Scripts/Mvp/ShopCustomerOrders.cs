using System;
using System.Collections.Generic;
using System.Linq;

public sealed partial class MvpShopController
{
    // A disposable budget: choosing an order never moves or consumes real inventory.
    public int[] CreateCustomerOrder(int preferred, int visit)
    {
        int types = ShopCatalog.Ids.Length;
        var remaining = Enumerable.Range(0, types)
            .Select(AvailableInventory).ToArray();
        int coffee = Activities.CoffeeOwned ? Activities.CoffeeSupplies + Activities.ReadyCoffee : 0;
        int milk = Activities.CoffeeOwned ? Activities.MilkSupplies + Activities.ReadyMilk : 0;
        if (Activities.BrewRemaining > 0) remaining[Activities.SelectedRecipe]++;

        bool Take(int type)
        {
            if (remaining[type] > 0) { remaining[type]--; return true; }
            if (!ShopCatalog.IsCoffee(type) || coffee == 0 || type == ShopCatalog.Latte && milk == 0) return false;
            coffee--;
            if (type == ShopCatalog.Latte) milk--;
            return true;
        }

        // Orders do not reserve stock: a product may run out while someone is waiting.
        // Real items in queued customers' baskets still belong to the shop until paid.
        foreach (var thief in fleeing)
            foreach (var item in new[] { thief.BasketItem }.Concat(thief.ExtraItems).Where(p => p != null))
            {
                int type = ShopCatalog.Index(item.ProductId);
                if (type >= 0) remaining[type] = Math.Max(0, remaining[type] - 1);
            }

        var order = new List<int>();
        int count = SingleProductVisits ? 1 : 1 + visit % 3;
        for (int line = 0; line < count; line++)
        {
            int start = line == 0 ? preferred : (order[0] + line) % AvailableProducts;
            bool found = false;
            for (int offset = 0; offset < types; offset++)
            {
                int type = (start + offset) % types;
                if (!Take(type)) continue;
                order.Add(type);
                found = true;
                break;
            }
            if (!found) break;
        }
        return order.ToArray();
    }
}
