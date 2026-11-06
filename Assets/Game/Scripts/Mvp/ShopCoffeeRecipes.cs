using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public sealed class ShopIngredientPackage:MonoBehaviour{public int Coffee,Milk;}
public sealed partial class ShopActivities
{
    public int ReadyCoffee=>ingredientPackages.Where(p=>p!=null).Sum(p=>p.GetComponent<ShopIngredientPackage>().Coffee);
    public int ReadyMilk=>ingredientPackages.Where(p=>p!=null).Sum(p=>p.GetComponent<ShopIngredientPackage>().Milk);
    private int pendingMilk => coffeeState.PendingMilk;
    private int selectedRecipe => coffeeState.SelectedRecipe;
    public bool OrderMilkSupplies()
    {
        if (!coffeeState.TryOrder(true, ReadyMilk, shop.Phase == ShopPhase.Summary, () => shop.Ledger.TryBuy(150, 10))) return false;
        shop.SyncCashDrawer();
        return true;
    }
    public int SelectedRecipe=>selectedRecipe;
    public void CycleRecipe()=>SelectRecipe(ShopCatalog.Coffee+(selectedRecipe-ShopCatalog.Coffee+1)%3);
    private void DrawMachineRecipeHint()
    {
        if(ShopMenu.Active||ShopInputGate.Blocked||player==null||player.TargetFeature==null||player.TargetFeature.Kind!=ShopFeatureKind.CoffeeMachine)return;
        using (ShopUi.Frame())
        {
            ShopUi.Panel(new Rect(380, 474, 520, 104));
            ShopUi.Label(new Rect(396, 490, 488, 28), "Kávé: " + ShopCatalog.ShortNames[selectedRecipe], 19, true);
            ShopUi.Label(new Rect(396, 524, 488, 42), ShopUi.BoundText("1: rövid • 2: hosszú • 3: latte • Q: következő\nE / bal klikk: elkészítés vagy átvétel"), 15);
        }
    }
    public int MilkSupplies { get => coffeeState.Milk; private set => coffeeState.Milk = value; }
    private readonly List<GameObject> ingredientPackages=new List<GameObject>();
    public void SelectRecipe(int type){if(coffeeState.SelectRecipe(type))CoffeeDisplay?.Refresh();}
    private void SpawnIngredientPackage(int servings,bool milk=false)
    {
        var root=new GameObject(milk?"Milk ingredient delivery":"Coffee ingredient delivery");root.transform.SetParent(worldRoot,false);root.transform.position=shop.Warehouse.Dock(4).position+Vector3.up*.55f+shop.Warehouse.Dock(4).right*((ingredientPackages.Count%2-.5f)*.32f);
        var body=Shape(root.transform,"Coffee and milk supply bag",Vector3.up*.12f,new Vector3(.26f,.24f,.20f),ivory,PrimitiveType.Cube,true);
        ProductVisuals.Text(root.transform,"Supply label",(milk?"TEJ\n":"KÁVÉ\n")+servings+" ADAG",new Vector3(0,.13f,-.103f),Quaternion.identity,.035f,Color.black);
        var tool=root.AddComponent<ShopHandTool>();tool.Configure(ShopToolKind.CoffeeRefill);var package=root.AddComponent<ShopIngredientPackage>();package.Coffee=milk?0:servings;package.Milk=milk?servings:0;
        ingredientPackages.Add(root);shop.Notify("Az alapanyag a raktári italpolc felső szintjén vár. E: felvétel, majd E a kávégépen: betöltés.");
    }
    private bool RefillFromHand()
    {
        if(player.HeldTool==null||player.HeldTool.Kind!=ShopToolKind.CoffeeRefill)return false;
        var package=player.HeldTool.GetComponent<ShopIngredientPackage>();if(package==null)return false;
        coffeeState.Refill(package.Coffee, package.Milk);ingredientPackages.Remove(package.gameObject);int coffeeAmount=package.Coffee,milkAmount=package.Milk;player.ConsumeHeldTool();shop.Notify("Betöltve: "+coffeeAmount+" kávé, "+milkAmount+" tej.");return true;
    }
    private void ResetIngredientPackages(){foreach(var p in ingredientPackages)if(p!=null){p.SetActive(false);Destroy(p);}ingredientPackages.Clear();}
    private void DrawRecipeControls(float w,float h)
    {
        if(!CoffeeOwned)return;
        for (int i = 0; i < 3; i++)
        {
            int type = ShopCatalog.Coffee + i;
            bool selected = selectedRecipe == type;
            string caption = (selected ? "Aktív: " : "") + ShopCatalog.ShortNames[type] + " • " + ShopCatalog.Money(ShopCatalog.SalePrice(type));
            var style = ShopUi.ButtonStyle(Mathf.Clamp((int)(w / 65), 14, 18), selected);
            if (GUI.Button(new Rect(12 + i * (w - 24) / 3, h - 88, (w - 30) / 3, 32), caption, style)) SelectRecipe(type);
        }
    }
}
