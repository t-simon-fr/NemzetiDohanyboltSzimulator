using System;
using UnityEngine;

public static class ShopCatalog
{
    public const int Count = 6;
    public static readonly string[] Ids = { "cig_red", "cig_blue", "cig_gold", "water", "gum", "beer" };
    public static readonly string[] ShortNames = { "Piros cigi", "Kék cigi", "Arany cigi", "Víz", "Rágó", "Sör" };
    public static readonly string[] Names = { "Pannon Piros", "Duna Kék", "Alföld Arany", "Buborék ásványvíz", "Menta rágó", "Pirkadat sör" };
    public static readonly int[] Prices = { 2200, 2300, 2400, 350, 300, 550 };
    public static readonly int[] Costs = { 1500, 1600, 1700, 150, 120, 260 };
    public static readonly int[] Denominations = { 20000, 10000, 5000, 2000, 1000, 500, 200, 100, 50, 20, 10, 5 };
    public static readonly int[] VisitProducts = { 0, 1, 2, 0, 1, 0, 2, 0 };
    public static int Index(string id) => Array.IndexOf(Ids, id);
    public static string Money(int value) => value.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("hu-HU")) + " Ft";
}

public sealed class ShopLedger
{
    public int OpeningCash { get; private set; }
    public int Cash { get; private set; }
    public int Revenue { get; private set; }
    public int Purchases { get; private set; }
    public int Improvements { get; private set; }
    public int CostOfGoods { get; private set; }
    public int OperatingCosts { get; private set; }
    public int Sales { get; private set; }
    public bool Closed { get; private set; }
    public int Profit => Revenue - CostOfGoods - OperatingCosts;
    public ShopLedger(int openingCash) { OpeningCash = openingCash; Cash = openingCash; }
    public bool TryBuy(int unitCost, int quantity)
    {
        if (Closed || unitCost <= 0 || quantity <= 0 || quantity > 100 || (long)unitCost * quantity > Cash) return false;
        int cost = unitCost * quantity;
        Cash -= cost; Purchases += cost; return true;
    }
    public bool TryImprove(int amount)
    {
        if(Closed || amount<=0 || amount>Cash)return false;
        Cash-=amount;Improvements+=amount;return true;
    }
    public bool TrySell(int price, int cost, int tender, int change)
    {
        if (Closed || price <= 0 || cost < 0 || tender < price || change < 0 || tender - price != change) return false;
        Cash += price; Revenue += price; CostOfGoods += cost; Sales++; return true;
    }
    public void Close(int operatingCost)
    {
        if (Closed) return;
        OperatingCosts = Math.Max(0, operatingCost); Cash -= OperatingCosts; Closed = true;
    }
}

public static class ShopInputGate
{
    public static bool Blocked { get; set; }
}

public static class ShopPeople
{
    public static readonly string[] ShortNames = { "Piros cigi", "Kék cigi", "Arany cigi", "Víz", "Rágó", "Sör" };
    public static readonly string[] Names = { "Józsi", "Marika", "Pisti", "Laci" };
    public static readonly Color[] Colors = { new Color(.22f,.35f,.29f), new Color(.60f,.23f,.35f), new Color(.18f,.33f,.65f), new Color(.63f,.42f,.13f) };
    public static readonly string[] Greetings = { "A szokásosat kérem. Most először vagyok itt, de nekem ez a szokásos.", "Csak egy percre ugrottam be. A történet hosszabb lesz.", "Sietek. Csak azt nem tudom még, hová.", "Munkába viszem. A munkát sajnos itt nem árulják." };
    public static readonly string[] Thanks = { "Na, látja! Máris törzsvendég vagyok.", "Köszönöm. Na és azt hallotta, hogy… jó, majd holnap.", "Köszi! A visszajáró legalább nem sietett el.", "Köszönöm, ezzel már majdnem munkakedvem is van." };
    public static readonly string[] Wrong = { "Ez nem a szokásos. Ezt én is most látom először.", "Édesem, ezt a történetet nem így rendeltem.", "Sietni sietek, de azért a megfelelőt vinném.", "Ezzel legfeljebb a főnököt lehet megkínálni." };
}
