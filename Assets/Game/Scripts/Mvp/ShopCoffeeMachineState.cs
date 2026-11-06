using System;

public enum ShopBrewResult { Started, MilkMissing, Unavailable }

// State and time are independent of cameras, cups, displays and MonoBehaviour lifetime.
public sealed class ShopCoffeeMachineState
{
    public bool Owned { get; internal set; }
    public int Coffee { get; internal set; }
    public int Milk { get; internal set; }
    public int PendingCoffee { get; private set; }
    public int PendingMilk { get; private set; }
    public int SelectedRecipe { get; private set; } = ShopCatalog.Coffee;
    public float BrewRemaining { get; private set; }
    private float coffeeDelivery, milkDelivery;
    private int brewingRecipe;
    public void Install() => Owned = true;

    public bool SelectRecipe(int type)
    {
        if (!ShopCatalog.IsCoffee(type) || BrewRemaining > 0) return false;
        SelectedRecipe = type;
        return true;
    }

    public bool TryOrder(bool milk, int ready, bool closed, Func<bool> debit)
    {
        int loaded = milk ? Milk : Coffee;
        int pending = milk ? PendingMilk : PendingCoffee;
        if (!Owned || closed || loaded + pending + ready + 10 > 60 || !debit()) return false;
        if (milk) { PendingMilk += 10; milkDelivery = 8; }
        else { PendingCoffee += 10; coffeeDelivery = 8; }
        return true;
    }

    public (int coffee, int milk) TickDeliveries(float dt)
    {
        dt = Math.Max(0, dt);
        int coffee = 0, milk = 0;
        if (PendingCoffee > 0 && (coffeeDelivery -= dt) <= 0) { coffee = PendingCoffee; PendingCoffee = 0; }
        if (PendingMilk > 0 && (milkDelivery -= dt) <= 0) { milk = PendingMilk; PendingMilk = 0; }
        return (coffee, milk);
    }

    public ShopBrewResult TryBrew(bool closed, bool trayOccupied)
    {
        if (SelectedRecipe == ShopCatalog.Latte && Milk == 0) return ShopBrewResult.MilkMissing;
        if (!Owned || closed || Coffee == 0 || BrewRemaining > 0 || trayOccupied) return ShopBrewResult.Unavailable;
        Coffee--;
        if (SelectedRecipe == ShopCatalog.Latte) Milk--;
        brewingRecipe = SelectedRecipe;
        BrewRemaining = 3.5f;
        return ShopBrewResult.Started;
    }

    public int TickBrew(float dt)
    {
        if (BrewRemaining <= 0) return -1;
        BrewRemaining = Math.Max(0, BrewRemaining - Math.Max(0, dt));
        return BrewRemaining == 0 ? brewingRecipe : -1;
    }

    public void Refill(int coffee, int milk) { Coffee += coffee; Milk += milk; }
    public void RefundBrew(int recipe) { Coffee++; if (recipe == ShopCatalog.Latte) Milk++; }
    public void Reset()
    {
        Owned = false;
        Coffee = Milk = PendingCoffee = PendingMilk = 0;
        coffeeDelivery = milkDelivery = BrewRemaining = 0;
        SelectedRecipe = ShopCatalog.Coffee;
    }
}
