using System;
using System.Linq;
using UnityEngine;

public sealed partial class ShopDevices
{
    private readonly string[] cartInputs = Enumerable.Repeat("0", ShopCatalog.Count).ToArray();
    private Vector2 cartScroll;
    private int cartSelectedType;
    private bool cartEditing;
    private bool HandleCartKey(UnityEngine.InputSystem.Key key)
    {
        if (cartEditing) return false;
        int digit = Digit(key);
        if (digit >= 1 && digit <= shop.AvailableProducts) { cartSelectedType = digit-1; return true; }
        if (key == UnityEngine.InputSystem.Key.Backspace) { OpenApp(ShopComputerApp.Desktop); return true; }
        if (key == UnityEngine.InputSystem.Key.Enter || key == UnityEngine.InputSystem.Key.NumpadEnter) { SubmitCart(); return true; }
        if (key == UnityEngine.InputSystem.Key.Minus || key == UnityEngine.InputSystem.Key.NumpadMinus) { SetCartQuantity(cartSelectedType,CartQuantity(cartSelectedType)-1); return true; }
        if (key == UnityEngine.InputSystem.Key.Equals || key == UnityEngine.InputSystem.Key.NumpadPlus) { SetCartQuantity(cartSelectedType,CartQuantity(cartSelectedType)+1); return true; }
        return false;
    }
    public int CartQuantity(int type) => int.TryParse(cartInputs[type],out int quantity) ? quantity : -1;
    public void SetCartQuantity(int type, int quantity)
    {
        if (type >= 0 && type < shop.AvailableProducts)
            cartInputs[type] = Mathf.Clamp(quantity,0,ShopCatalog.MaxOrderQuantity).ToString();
    }
    public bool SubmitCart()
    {
        var quantities = Enumerable.Range(0,ShopCatalog.Count).Select(CartQuantity).ToArray();
        bool result = shop.OrderBasket(quantities);
        orderFeedback = shop.Message;
        if (result) Array.Fill(cartInputs,"0");
        return result;
    }
    private void DrawOrderCart()
    {
        if (FocusKind != ShopDeviceKind.Monitor || ComputerApp != ShopComputerApp.Orders) return;
        Rect area = AppRect;
        ShopUi.Panel(area);
        GUI.BeginGroup(area);
        float width = area.width;
        var label = ShopUi.Text(15);
        var button = ShopUi.ButtonStyle(15);
        cartScroll = GUI.BeginScrollView(new Rect(6,4,width-12,area.height-85),cartScroll,
            new Rect(0,0,width-35,shop.AvailableProducts*43));
        long total = 0;
        bool valid = true;
        for (int type=0;type<shop.AvailableProducts;type++)
        {
            float y = type*43;
            int quantity = CartQuantity(type);
            valid &= quantity >= 0 && quantity <= ShopCatalog.MaxOrderQuantity;
            if (quantity > 0) total += (long)ShopCatalog.Costs[type]*quantity;
            GUI.Label(new Rect(2,y,width-235,40),ShopCatalog.ShortNames[type]+"\n"+ShopCatalog.Money(ShopCatalog.Costs[type])+" / db",label);
            if (GUI.Button(new Rect(width-230,y+4,32,30),"-",button)) SetCartQuantity(type,Mathf.Max(0,quantity)-1);
            GUI.SetNextControlName("CartQuantity"+type);
            cartInputs[type] = GUI.TextField(new Rect(width-193,y+4,48,30),cartInputs[type],3);
            if (GUI.Button(new Rect(width-140,y+4,32,30),"+",button)) SetCartQuantity(type,Mathf.Max(0,quantity)+1);
            GUI.Label(new Rect(width-103,y+4,65,30),"db",label);
        }
        GUI.EndScrollView();
        cartEditing = GUI.GetNameOfFocusedControl().StartsWith("CartQuantity",StringComparison.Ordinal);
        GUI.Label(new Rect(10,area.height-78,width*.47f,32),"Összesen: "+ShopCatalog.Money((int)Math.Min(total,int.MaxValue)),label);
        GUI.enabled = valid && total > 0 && total <= shop.Ledger.AvailableFunds;
        if (GUI.Button(new Rect(width*.50f,area.height-78,width*.48f-10,32),"Megrendelem",button)) SubmitCart();
        GUI.enabled = true;
        GUI.Label(new Rect(10,area.height-42,width-20,38),valid ? orderFeedback : "0–12 közötti egész darabszámot adj meg.",ShopUi.Text(13));
        GUI.EndGroup();
    }
}

public sealed partial class MvpShopController
{
    public bool OrderBasket(int[] quantities)
    {
        if (quantities == null || quantities.Length != ShopCatalog.Count || Ledger.Closed || Phase == ShopPhase.Summary) return false;
        int total = 0;
        for (int type=0;type<quantities.Length;type++)
        {
            int quantity = quantities[type];
            if (quantity < 0 || quantity > ShopCatalog.MaxOrderQuantity || type >= AvailableProducts && quantity > 0 || warehouse[type]+PendingQuantity(type)+quantity > 24)
            { Notify("A kosár nem rendelhető: ellenőrizd a mennyiségeket és a raktárkapacitást."); return false; }
            total += ShopCatalog.Costs[type]*quantity;
        }
        if (total == 0 || !Ledger.TryBuy(total,1))
        { Notify(total == 0 ? "A rendelési kosár üres." : "Nincs elég pénz a teljes rendeléshez."); return false; }
        for (int type=0;type<quantities.Length;type++)
            if (quantities[type] > 0) shipments.Add(new Shipment { Type=type, Quantity=quantities[type], Remaining=8f });
        SyncCashDrawer();
        Notify("A teljes rendelés elküldve: "+ShopCatalog.Money(total)+". Szállítás: 8 másodperc.");
        return true;
    }
}
