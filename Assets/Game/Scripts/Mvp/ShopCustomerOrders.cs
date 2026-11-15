using System;
using System.Collections.Generic;
using System.Linq;

public sealed partial class MvpShopController
{
    // A disposable budget: choosing an order never moves or consumes real inventory.
    // Order lines scan EVERY type with available stock, starting from the preferred type and
    // wrapping around the whole catalog: a stocked type can appear as an extra line in any order.
    // Special families need an explicit guard in Take() (see the lottery rule below); without one,
    // any stocked type joins mixed baskets. Only-tested-stock gating ("request only what was
    // procured") falls out of remaining[] > 0, never from a separate unlock flag.
    public int[] CreateCustomerOrder(int preferred, int visit, int person = -1)
    {
        int types = ShopCatalog.Ids.Length;
        var remaining = Enumerable.Range(0, types)
            .Select(AvailableInventory).ToArray();
        int[] ingredients = Activities.IngredientBudget();
        if (Activities.BrewRemaining > 0) remaining[Activities.SelectedRecipe]++;
        int drift = person <= 0 ? 0 : (int)(ShopReputationState.Sample(visit, Day, (uint)(71 + person)) * 3);

        bool Take(int type)
        {
            if (ShopLotteryCatalog.IsTicket(type) && !ShopLotteryCatalog.IsTicket(preferred)) return false;
            if (ShopIceCreamCatalog.IsIceCream(type) && !FreezerOwned) return false;
            if (remaining[type] > 0) { remaining[type]--; return true; }
            var recipe = ShopHotDrinkRecipes.Get(type);
            if (!Activities.CoffeeOwned || recipe == null || !recipe.CanMake(ingredients)) return false;
            recipe.Consume(ingredients);
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
        int count = SingleProductVisits || ShopLotteryCatalog.IsTicket(preferred) ? 1 :
            Reputation?.State.BasketSize(visit,ShopReputationState.Sample(visit,Day,907)) ?? 1+visit%3;
        for (int line = 0; line < count; line++)
        {
            // Laci stocks up for work: a second unit of the first item when it is really there.
            // Extras must sit on a public shelf: the NPC shops them alone, without the player.
            // The preferred line keeps the legacy contract (spawn logic and player-served recipes rely on it).
            if (line > 0 && person == 3 && ShopReputationState.Sample(visit, Day, 509) < .5 && Layout.FindPublicStock(order[0]) != null && Take(order[0])) { order.Add(order[0]); continue; }
            int start = line == 0 ? preferred : (order[0] + line + drift) % AvailableProducts;
            bool found = false;
            for (int offset = 0; offset < types; offset++)
            {
                int type = (start + offset) % types;
                if (line > 0 && person >= 0 && Layout.FindPublicStock(type) == null) continue;
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
