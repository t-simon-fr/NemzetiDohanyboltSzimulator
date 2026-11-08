using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public sealed class ShopIngredientPackage : MonoBehaviour
{
    public int Coffee, Milk, Tea, Cocoa;
    public ShopDrinkIngredient Ingredient => Milk > 0 ? ShopDrinkIngredient.Milk : Tea > 0 ? ShopDrinkIngredient.Tea : Cocoa > 0 ? ShopDrinkIngredient.Cocoa : ShopDrinkIngredient.Coffee;
    public int Amount => Coffee + Milk + Tea + Cocoa;
    public int Quantity(ShopDrinkIngredient ingredient) => ingredient == ShopDrinkIngredient.Coffee ? Coffee : ingredient == ShopDrinkIngredient.Milk ? Milk : ingredient == ShopDrinkIngredient.Tea ? Tea : Cocoa;
    public string Brand => ShopHotDrinkRecipes.Brands[(int)Ingredient];
}
public sealed partial class ShopActivities
{
    public int ReadyCoffee=>ingredientPackages.Where(p=>p!=null).Sum(p=>p.GetComponent<ShopIngredientPackage>().Coffee);
    public int ReadyMilk=>ingredientPackages.Where(p=>p!=null).Sum(p=>p.GetComponent<ShopIngredientPackage>().Milk);
    private int pendingMilk => coffeeState.PendingMilk;
    private int selectedRecipe => coffeeState.SelectedRecipe;
    public int ReadyIngredient(ShopDrinkIngredient ingredient) => ingredientPackages.Where(p => p != null).Sum(p => p.GetComponent<ShopIngredientPackage>().Quantity(ingredient));
    public int LoadedIngredient(ShopDrinkIngredient ingredient) => coffeeState.Loaded(ingredient);
    public int PendingIngredient(ShopDrinkIngredient ingredient) => coffeeState.Pending(ingredient);
    public int[] IngredientBudget(bool includeReady = true) => ShopHotDrinkRecipes.Ingredients.Select(i => LoadedIngredient(i) + (includeReady ? ReadyIngredient(i) : 0)).ToArray();
    public bool CanPrepareDrink(int type, bool includeReady = false, int quantity = 1) => CoffeeOwned && ShopHotDrinkRecipes.Get(type)?.CanMake(IngredientBudget(includeReady), quantity) == true;
    public string MissingDrinkIngredients => coffeeState.MissingIngredients;
    public bool OrderMilkSupplies() => OrderIngredient(ShopDrinkIngredient.Milk);
    public bool OrderIngredient(ShopDrinkIngredient ingredient)
    {
        bool debitAttempted = false;
        bool ordered = coffeeState.TryOrder(ingredient, ReadyIngredient(ingredient), shop.Phase == ShopPhase.Summary, () => {
            debitAttempted = true;
            return shop.Ledger.TryBuy(ShopHotDrinkRecipes.UnitCosts[(int)ingredient], 10);
        });
        if (!ordered)
        {
            shop.Notify(debitAttempted ? "Nincs elég pénz az alapanyagra." : "Kávégép szükséges; alapanyagonként legfeljebb 60 adag tárolható, a szállítást is beleszámítva.");
            return false;
        }
        shop.SyncCashDrawer();
        shop.Notify("Megrendelve: 10 adag " + ShopHotDrinkRecipes.IngredientNames[(int)ingredient].ToLowerInvariant() + ". Szállítás: 8 másodperc.");
        return true;
    }
    public int SelectedRecipe=>selectedRecipe;
    public void CycleRecipe() => SelectRecipe(ShopHotDrinkRecipes.Types[(System.Array.IndexOf(ShopHotDrinkRecipes.Types, selectedRecipe) + 1) % ShopHotDrinkRecipes.Types.Length]);
    public int MilkSupplies { get => coffeeState.Milk; private set => coffeeState.Milk = value; }
    private readonly List<GameObject> ingredientPackages=new List<GameObject>();
    public void SelectRecipe(int type){if(coffeeState.SelectRecipe(type))CoffeeDisplay?.Refresh();}
    private void SpawnIngredientPackage(int servings, bool milk = false) => SpawnIngredientPackage(servings, milk ? ShopDrinkIngredient.Milk : ShopDrinkIngredient.Coffee);
    private void SpawnIngredientPackage(int servings, ShopDrinkIngredient ingredient)
    {
        bool milk = ingredient == ShopDrinkIngredient.Milk;
        var root = new GameObject(ShopHotDrinkRecipes.IngredientNames[(int)ingredient] + " ingredient delivery");
        root.transform.SetParent(worldRoot, false);
        root.transform.position = shop.Warehouse.Dock(4).position + Vector3.up * .55f
            + shop.Warehouse.Dock(4).right * (((int)ingredient % 2 - .5f) * .32f) + shop.Warehouse.Dock(4).forward * ((int)ingredient / 2 * .23f);
        // Printed fronts face the warehouse aisle, not the rack's back panel.
        root.transform.localRotation = Quaternion.Euler(0, -90, 0);
        if (ingredient == ShopDrinkIngredient.Tea || ingredient == ShopDrinkIngredient.Cocoa)
            BuildDryIngredientPackage(root.transform, ingredient, servings);
        else
        {
            var model = Resources.Load<GameObject>(milk ? "Ingredients/Albro" : "Ingredients/LaVazze");
            if (model == null) throw new System.InvalidOperationException("Missing ingredient packaging prefab.");
            Instantiate(model, root.transform, false);
        }
        var collider = root.AddComponent<BoxCollider>();
        collider.size = milk ? new Vector3(.16f, .35f, .12f) : new Vector3(.20f, .24f, .10f);
        collider.center = Vector3.up * collider.size.y * .5f;
        var package = root.AddComponent<ShopIngredientPackage>();
        package.Coffee = ingredient == ShopDrinkIngredient.Coffee ? servings : 0;
        package.Milk = milk ? servings : 0;
        package.Tea = ingredient == ShopDrinkIngredient.Tea ? servings : 0;
        package.Cocoa = ingredient == ShopDrinkIngredient.Cocoa ? servings : 0;
        var tool = root.AddComponent<ShopHandTool>();
        tool.Configure(ShopToolKind.CoffeeRefill);
        ingredientPackages.Add(root);shop.Notify("Az alapanyag a raktári italpolc felső szintjén vár. E / bal kattintás: felvétel és betöltés a gépnél.");
    }
    private bool RefillFromHand()
    {
        if(player.HeldTool==null||player.HeldTool.Kind!=ShopToolKind.CoffeeRefill)return false;
        var package=player.HeldTool.GetComponent<ShopIngredientPackage>();if(package==null)return false;
        coffeeState.Refill(package.Ingredient, package.Amount);
        string message = "Betöltve: " + package.Amount + " adag " + ShopHotDrinkRecipes.IngredientNames[(int)package.Ingredient].ToLowerInvariant() + ".";
        ingredientPackages.Remove(package.gameObject);
        player.ConsumeHeldTool();
        shop.Notify(message);
        return true;
    }
    private void BuildDryIngredientPackage(Transform parent, ShopDrinkIngredient ingredient, int servings)
    {
        bool tea = ingredient == ShopDrinkIngredient.Tea;
        var color = tea ? new Color(.17f, .34f, .23f) : new Color(.32f, .14f, .08f);
        var paper = Mat("Dry ingredient cream", ShopUi.Paper);
        Box(parent, "Ingredient carton", new Vector3(0, .12f, 0), new Vector3(.20f, .24f, .10f), paper);
        Box(parent, "Ingredient brand band", new Vector3(0, .16f, -.051f), new Vector3(.192f, .08f, .002f), Mat("Dry ingredient band", color));
        ProductVisuals.Text(parent, "Ingredient brand", tea ? "LEVÉLKE" : "KAKAÓKA", new Vector3(0, .165f, -.054f), Quaternion.identity, .022f, ShopUi.Paper);
        ProductVisuals.Text(parent, "Ingredient contents", tea ? "TEA" : "KAKAÓPOR", new Vector3(0, .08f, -.054f), Quaternion.identity, .019f, color);
        ProductVisuals.Text(parent, "Ingredient servings", servings + " ADAG", new Vector3(0, .035f, -.054f), Quaternion.identity, .013f, color);
    }
    private void ResetIngredientPackages(){foreach(var p in ingredientPackages)if(p!=null){p.SetActive(false);Destroy(p);}ingredientPackages.Clear();}
    private void DrawRecipeControls(float w,float h)
    {
        if(!CoffeeOwned)return;
        for (int i = 0; i < ShopHotDrinkRecipes.Types.Length; i++)
        {
            int type = ShopHotDrinkRecipes.Types[i];
            bool selected = selectedRecipe == type;
            string caption = (selected ? "Aktív: " : "") + ShopCatalog.ShortNames[type] + " • " + ShopCatalog.Money(ShopCatalog.SalePrice(type));
            var style = ShopUi.ButtonStyle(Mathf.Clamp((int)(w / 65), 14, 18), selected);
            if (GUI.Button(new Rect(12 + i * (w - 24) / 5, h - 88, (w - 30) / 5, 32), caption, style)) SelectRecipe(type);
        }
    }
}
