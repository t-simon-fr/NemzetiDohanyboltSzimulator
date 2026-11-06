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
    public Rect CartFieldRect(int type)
    {
        float y=ComputerBody.y+48+type*62-cartScroll.y;
        return ComputerToScreen(new Rect(ComputerBody.x+858,y+6,90,44));
    }
    private void DrawOrderCart()
    {
        if(ComputerApp!=ShopComputerApp.Orders)return;
        GUI.BeginGroup(ComputerBody);
        try
        {
            float w=ComputerBody.width;
            ShopComputerTheme.Label(new Rect(16,0,390,36),"Termék",21,true);
            ShopComputerTheme.Label(new Rect(410,0,310,36),"Polc / Raktár / Úton",19,true);
            ShopComputerTheme.Label(new Rect(790,0,330,36),"Rendelendő",21,true);
            long total=0;bool valid=true;
            for(int type=0;type<shop.AvailableProducts;type++) {
                int q=CartQuantity(type);valid &= q>=0&&q<=ShopCatalog.MaxOrderQuantity;
                if(q>0)total+=(long)q*ShopCatalog.Costs[type];
            }
            cartScroll=GUI.BeginScrollView(new Rect(0,48,w,340),cartScroll,new Rect(0,0,w-24,shop.AvailableProducts*62));
            for(int type=0;type<shop.AvailableProducts;type++) {
                float y=type*62;int q=CartQuantity(type);
                ShopComputerTheme.Label(new Rect(16,y,380,30),ShopCatalog.Names[type],22,true);
                ShopComputerTheme.Label(new Rect(16,y+29,380,25),ShopCatalog.Money(ShopCatalog.Costs[type])+" / db",18);
                ShopComputerTheme.Label(new Rect(410,y,310,54),shop.StockCount(type)+" / "+shop.WarehouseCount(type)+" / "+shop.PendingQuantity(type),22);
                if(GUI.Button(new Rect(796,y+6,48,44),"-",ShopComputerTheme.Button()))SetCartQuantity(type,Mathf.Max(0,q)-1);
                GUI.SetNextControlName("CartQuantity"+type);
                cartInputs[type]=GUI.TextField(new Rect(858,y+6,90,44),cartInputs[type],3,ShopComputerTheme.Field(24));
                if(GUI.Button(new Rect(962,y+6,48,44),"+",ShopComputerTheme.Button()))SetCartQuantity(type,Mathf.Max(0,q)+1);
                ShopComputerTheme.Label(new Rect(1024,y+6,60,44),"db",19);
                ShopUi.Fill(new Rect(16,y+60,w-56,1),ShopComputerTheme.Rule);
            }
            GUI.EndScrollView();
            cartEditing=GUI.GetNameOfFocusedControl().StartsWith("CartQuantity",StringComparison.Ordinal);
            ShopComputerTheme.Label(new Rect(16,400,680,46),"Összesen: "+ShopCatalog.Money((int)Math.Min(total,int.MaxValue)),26,true);
            GUI.enabled=valid&&total>0&&total<=shop.Ledger.AvailableFunds;
            if(GUI.Button(new Rect(w-350,400,330,46),"Megrendelem",ShopComputerTheme.Button(true)))SubmitCart();
            GUI.enabled=true;
            string feedback=!valid?"0–12 közötti egész darabszám szükséges.":orderFeedback;
            if(feedback.Length>0)ShopComputerTheme.Label(new Rect(16,450,w-32,27),feedback,17);
        }
        finally{GUI.EndGroup();GUI.enabled=true;}
    }
}

public sealed partial class MvpShopController
{
    public bool OrderBasket(int[] quantities)
    {
        if (!TryPurchase(quantities, out var quote))
        {
            if (quote.Error == ShopPurchaseError.Closed || quote.Error == ShopPurchaseError.InvalidBasket) return false;
            Notify(quote.Error == ShopPurchaseError.Empty ? "A rendelési kosár üres." :
                quote.Error == ShopPurchaseError.Funds ? "Nincs elég pénz a teljes rendeléshez." :
                "A kosár nem rendelhető: ellenőrizd a mennyiségeket és a raktárkapacitást.");
            return false;
        }
        Notify("A teljes rendelés elküldve: "+ShopCatalog.Money(quote.Total)+". Szállítás: 8 másodperc.");
        return true;
    }

    private bool TryPurchase(int[] quantities, out ShopPurchaseQuote quote)
    {
        quote = inventory.Quote(quantities, ShopCatalog.Costs, AvailableProducts,
            Ledger.AvailableFunds, Ledger.Closed || Phase == ShopPhase.Summary, ShopCatalog.MaxOrderQuantity);
        if (!inventory.TryPurchase(quote, total => Ledger.TryBuy(total, 1))) return false;
        SyncCashDrawer();
        return true;
    }
}
