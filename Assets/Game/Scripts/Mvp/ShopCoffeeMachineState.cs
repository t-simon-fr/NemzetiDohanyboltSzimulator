using System;

public enum ShopBrewResult { Started, MilkMissing, Unavailable, IngredientsMissing }

// State and time are independent of cameras, cups, displays and MonoBehaviour lifetime.
public sealed class ShopCoffeeMachineState
{
    private readonly int[] loaded = new int[4], pending = new int[4];
    private readonly float[] delivery = new float[4];
    public bool Owned { get; internal set; }
    public int Coffee { get => loaded[0]; internal set => loaded[0] = value; }
    public int Milk { get => loaded[1]; internal set => loaded[1] = value; }
    public int Tea { get => loaded[2]; internal set => loaded[2] = value; }
    public int Cocoa { get => loaded[3]; internal set => loaded[3] = value; }
    public int PendingCoffee => pending[0];
    public int PendingMilk => pending[1];
    public int Pending(ShopDrinkIngredient ingredient) => pending[(int)ingredient];
    public int Loaded(ShopDrinkIngredient ingredient) => loaded[(int)ingredient];
    public int SelectedRecipe { get; private set; } = ShopCatalog.Coffee;
    public float BrewRemaining { get; private set; }
    private int brewingRecipe;
    public void Install() => Owned = true;
    public bool CanMake(int type, int quantity = 1) => Owned && ShopHotDrinkRecipes.Get(type)?.CanMake(loaded, quantity) == true;
    public string MissingIngredients => ShopHotDrinkRecipes.Missing(SelectedRecipe, loaded);

    public bool SelectRecipe(int type)
    {
        if (!ShopCatalog.IsCoffee(type) || BrewRemaining > 0) return false;
        SelectedRecipe = type;
        return true;
    }
    public bool TryOrder(bool milk, int ready, bool closed, Func<bool> debit)
        => TryOrder(milk ? ShopDrinkIngredient.Milk : ShopDrinkIngredient.Coffee, ready, closed, debit);
    public bool TryOrder(ShopDrinkIngredient ingredient, int ready, bool closed, Func<bool> debit)
    {
        int i = (int)ingredient;
        if (!Owned || closed || loaded[i] + pending[i] + ready + 10 > 60 || !debit()) return false;
        pending[i] += 10;
        delivery[i] = 8;
        return true;
    }
    private int TickDelivery(int i, float dt)
    {
        if (pending[i] <= 0 || (delivery[i] -= Math.Max(0, dt)) > 0) return 0;
        int amount = pending[i];
        pending[i] = 0;
        return amount;
    }
    public (int coffee, int milk) TickDeliveries(float dt) => (TickDelivery(0, dt), TickDelivery(1, dt));
    public int TickDelivery(ShopDrinkIngredient ingredient, float dt) => TickDelivery((int)ingredient, dt);
    public ShopBrewResult TryBrew(bool closed, bool trayOccupied)
    {
        if (!Owned || closed || BrewRemaining > 0 || trayOccupied) return ShopBrewResult.Unavailable;
        if (!CanMake(SelectedRecipe))
            return SelectedRecipe == ShopCatalog.Latte && Milk == 0 ? ShopBrewResult.MilkMissing : ShopBrewResult.IngredientsMissing;
        var recipe = ShopHotDrinkRecipes.Get(SelectedRecipe);
        recipe.Consume(loaded);
        brewingRecipe = SelectedRecipe;
        BrewRemaining = recipe.Seconds;
        return ShopBrewResult.Started;
    }
    public int TickBrew(float dt)
    {
        if (BrewRemaining <= 0) return -1;
        BrewRemaining = Math.Max(0, BrewRemaining - Math.Max(0, dt));
        return BrewRemaining == 0 ? brewingRecipe : -1;
    }
    public void Refill(int coffee, int milk) { Coffee += coffee; Milk += milk; }
    public void Refill(ShopDrinkIngredient ingredient, int amount) { loaded[(int)ingredient] += Math.Max(0, amount); }
    public void RefundBrew(int recipe) => ShopHotDrinkRecipes.Get(recipe).Consume(loaded, -1);
    public void Reset()
    {
        Owned = false;
        Array.Clear(loaded, 0, loaded.Length);
        Array.Clear(pending, 0, pending.Length);
        Array.Clear(delivery, 0, delivery.Length);
        BrewRemaining = 0;
        SelectedRecipe = ShopCatalog.Coffee;
    }
}
