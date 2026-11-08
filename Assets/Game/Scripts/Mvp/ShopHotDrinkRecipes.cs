using System;
using System.Collections.Generic;

public enum ShopDrinkIngredient { Coffee, Milk, Tea, Cocoa }

public sealed class ShopHotDrinkRecipe
{
    public int Type { get; }
    public float Seconds { get; }
    private readonly int[] amounts;
    public int Amount(ShopDrinkIngredient ingredient) => amounts[(int)ingredient];
    public ShopHotDrinkRecipe(int type, float seconds, int coffee = 0, int milk = 0, int tea = 0, int cocoa = 0)
    {
        Type = type;
        Seconds = seconds;
        amounts = new[] { coffee, milk, tea, cocoa };
    }
    public bool CanMake(IReadOnlyList<int> available, int quantity = 1)
    {
        for (int i = 0; i < amounts.Length; i++)
            if (available[i] < amounts[i] * quantity) return false;
        return true;
    }
    public void Consume(int[] available, int direction = 1)
    {
        for (int i = 0; i < amounts.Length; i++) available[i] -= direction * amounts[i];
    }
}

public static class ShopHotDrinkRecipes
{
    public static readonly int[] Types = { ShopCatalog.Coffee, ShopCatalog.LongCoffee, ShopCatalog.Latte, ShopCatalog.Tea, ShopCatalog.HotChocolate };
    public static readonly ShopDrinkIngredient[] Ingredients = (ShopDrinkIngredient[])Enum.GetValues(typeof(ShopDrinkIngredient));
    public static readonly string[] IngredientNames = { "Kávéalapanyag", "Tej", "Teaalapanyag", "Kakaópor" };
    public static readonly string[] Brands = { "La Vazze", "Albro", "Levélke", "Kakaóka" };
    public static readonly int[] UnitCosts = { 200, 150, 120, 100 };
    private static readonly ShopHotDrinkRecipe[] recipes = {
        new ShopHotDrinkRecipe(ShopCatalog.Coffee, 3.5f, coffee: 1),
        new ShopHotDrinkRecipe(ShopCatalog.LongCoffee, 3.5f, coffee: 1),
        new ShopHotDrinkRecipe(ShopCatalog.Latte, 3.5f, coffee: 1, milk: 1),
        new ShopHotDrinkRecipe(ShopCatalog.Tea, 3.5f, tea: 1),
        new ShopHotDrinkRecipe(ShopCatalog.HotChocolate, 4f, milk: 1, cocoa: 1)
    };
    public static ShopHotDrinkRecipe Get(int type)
    {
        int index = Array.IndexOf(Types, type);
        return index < 0 ? null : recipes[index];
    }
    public static string Missing(int type, IReadOnlyList<int> available)
    {
        var recipe = Get(type);
        var missing = new List<string>();
        foreach (var ingredient in Ingredients)
            if (available[(int)ingredient] < recipe.Amount(ingredient))
                missing.Add(IngredientNames[(int)ingredient].ToUpperInvariant());
        return missing.Count == 0 ? "" : "NINCS " + missing[0];
    }
}
