using System;
using System.Linq;
using UnityEngine;

public static class ShopCatalog
{
    // Packaged products that use shelves and warehouse docks. Coffee is prepared separately.
    public const int Count = 9;
    public const int SupplyCapacity = 30;
    public static bool IsPackaged(int type) => type >= 0 && type < Count || ShopPartnerCatalog.IsVariant(type);
    public const int Coffee = Count;
    public const int LongCoffee=Count+1,Latte=Count+2,Contraband=Count+3;
    public const int Tea = 23, HotChocolate = 24;
    public const int TicketSingle = 25, TicketBook = 26;
    public const int IceChoc = 27, IceStraw = 28, IceVan = 29;
    // Historical name retained for the prepared-drink pipeline (never packaged shelf stock).
    public static bool IsCoffee(int type) => type >= Coffee && type <= Latte || type == Tea || type == HotChocolate;
    public const int CigaretteCount = 4;
    public const int MaxOrderQuantity = 12;
    public static bool IsCigarette(int type) => type >= 0 && type < CigaretteCount || type==Contraband;
    public static readonly string[] Ids = { "cig_red", "cig_blue", "cig_gold", "cig_davidon", "water", "cola", "energy", "kevert", "beer", "coffee", "coffee_long", "latte", "dnyipro", "lottery_fortuna", "lottery_pension", "lottery_numbers", "energy_zero", "energy_mango", "fantom", "spritz", "muszak_plum", "muszak_pear", "muszak_mixed", "tea", "hot_chocolate", "ticket_single", "ticket_book", "ice_choc", "ice_straw", "ice_van" };
    public static readonly string[] ShortNames = { "Lucky Stroke", "Marlbroke", "Downhill", "Davidon", "ReturAqua", "Loca Cola", "Hamster Energy", "Kevert", "Kőfejtői", "Rövid kávé", "Hosszú kávé", "Latte", "Dnyipro", "Falusi Fortuna", "Nyugdíj Plusz", "Ötöske", "Hamster Zero", "Hamster Pacific Mango", "Fantom", "Spritz", "Műszakkezdő Szilva", "Műszakkezdő Körte", "Műszakkezdő Vegyes", "Tea", "Forró csoki", "Vonaljegy", "Gyűjtőjegy", "Hókristály", "Jeges Pötty", "Sarki Fény" };
    public static readonly string[] Names = { "Lucky Stroke", "Marlbroke", "Downhill", "Davidon", "ReturAqua 500 ml", "Loca Cola 330 ml", "Hamster Energy 500 ml", "Kevert 50 ml", "Kőfejtői 500 ml", "Rövid kávé", "Hosszú kávé", "Latte", "Dnyipro ukrán cigaretta", "Falusi Fortuna sorsjegy", "Nyugdíj Plusz sorsjegy", "Ötöske számszelvény", "Hamster Zero 500 ml", "Hamster Pacific Mango 500 ml", "Fantom narancs 330 ml", "Spritz citrus 330 ml", "Műszakkezdő Szilva 50 ml", "Műszakkezdő Körte 50 ml", "Műszakkezdő Vegyes 50 ml", "Tea", "Forró csoki", "Vonaljegy", "Gyűjtőjegy 10 utazás", "Hókristály csoki", "Jeges Pötty eper", "Sarki Fény vanília" };
    public static readonly int[] Prices = { 2200, 2300, 2400, 2600, 350, 450, 650, 450, 550, 650, 750, 1100, 1400, 500, 1000, 500, 650, 700, 450, 450, 550, 550, 550, 450, 900, 600, 5000, 450, 500, 550 };
    // Lottery cost is the operator's share, not a second procurement payment.
    public static readonly int[] Costs = { 1500, 1600, 1700, 1850, 150, 180, 300, 220, 280, 200, 200, 350, 700, 450, 900, 450, 300, 330, 180, 180, 260, 270, 250, 120, 250, 300, 2800, 200, 220, 240 };
    public static readonly int[] Denominations = { 20000, 10000, 5000, 2000, 1000, 500, 200, 100, 50, 20, 10, 5 };
    public static readonly int[] VisitProducts = { 0, 1, 2, 3, 1, 0, 2, 3 };
    public static int SalePrice(int type)=>MvpShopController.Instance?.Pricing?.Price(type)??Prices[type];
    public static int Index(string id) => Array.IndexOf(Ids, id);
    public static string Money(int value) => value.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("hu-HU")) + " Ft";
}

public static class ShopTicketCatalog
{
    public const int FirstType = 25, Types = 2;
    public const int RidesPerBook = 10;
    public static bool IsTicket(int type) => type >= FirstType && type < FirstType + Types;
    public static int Index(int type) => IsTicket(type) ? type - FirstType : -1;
}

public static class ShopIceCreamCatalog
{
    public const int FirstType = 27, Types = 3;
    public static bool IsIceCream(int type) => type >= FirstType && type < FirstType + Types;
    public static int Index(int type) => IsIceCream(type) ? type - FirstType : -1;
}

public sealed class ShopLedger
{
    public int BankBalance {get;private set;}
    public int CardRevenue {get;private set;}
    public int LotteryReserve { get; private set; }
    public int MobileReserve { get; private set; }
    public int AvailableFunds => Math.Max(0, Cash - LotteryReserve - MobileReserve);
    public void SetMobileReserve(int amount) => MobileReserve = Math.Max(0, amount);
    public bool TrySettleMobile(int amount)
    {
        if (Closed || amount <= 0 || amount != MobileReserve || (long)Cash - amount < LotteryReserve) return false;
        Cash -= amount; MobileReserve = 0; return true;
    }
    public void SetLotteryReserve(int amount) => LotteryReserve = Math.Max(0, amount);
    // Operator transfers are cash movements, never a second sale, profit or purchase.
    public bool TryOperatorTransfer(int delta)
    {
        long next = (long)Cash + delta;
        if (Closed || delta == 0 || delta % 5 != 0 || next < MobileReserve || next > int.MaxValue) return false;
        Cash = (int)next;
        return true;
    }
    public int OpeningCash { get; private set; }
    public int Cash { get; private set; }
    public int Revenue { get; private set; }
    public int Purchases { get; private set; }
    public int PurchaseRounding { get; private set; }
    public void RecordPurchaseRounding(int difference) { if (!Closed) PurchaseRounding += difference; }
    public int Improvements { get; private set; }
    public int CostOfGoods { get; private set; }
    public int OperatingCosts { get; private set; }
    public int StockLoss { get; private set; }
    public void RecordStockLoss(int cost) { if(!Closed && cost>0)StockLoss+=cost; }
    public int Sales { get; private set; }
    public bool Closed { get; private set; }
    public int Profit => Revenue - CostOfGoods - OperatingCosts - StockLoss - PurchaseRounding;
    public ShopLedger(int openingCash,int bankBalance=0) { OpeningCash = openingCash+Math.Max(0,bankBalance); Cash = OpeningCash; BankBalance=0; }
    private void SpendFunds(int amount){Cash-=amount;}
    public bool TrySellCard(int price,int cost)
    {
        if(Closed||price<=0||cost<0)return false;
        Cash+=price;CardRevenue+=price;Revenue+=price;CostOfGoods+=cost;Sales++;return true;
    }
    public bool TryBuy(int unitCost, int quantity)
    {
        if (Closed || unitCost <= 0 || quantity <= 0 || quantity > 100 || (long)unitCost * quantity > AvailableFunds) return false;
        int cost = unitCost * quantity;
        SpendFunds(cost); Purchases += cost; return true;
    }
    // Only the night vendor calls this, guarded by the AfterHours phase.
    // The ledger now remains open until the player leaves at the entrance.
    public bool TryBuyAfterHours(int cost,int quantity)
    {
        if(Closed||Cash<0||cost<=0||quantity<=0||quantity>10||(long)cost*quantity>AvailableFunds)return false;
        int amount=cost*quantity;SpendFunds(amount);Purchases+=amount;return true;
    }
    public int FurnitureResale { get; private set; }
    public bool TryFurnish(int purchases, int resale)
    {
        long next = (long)Cash + resale - purchases;
        if (Closed || purchases < 0 || resale < 0 || purchases > (long)AvailableFunds + resale || next < 0 || next > int.MaxValue || (long)Improvements + purchases > int.MaxValue || (long)FurnitureResale + resale > int.MaxValue) return false;
        Cash = (int)next;
        Improvements += purchases;
        FurnitureResale += resale;
        return true;
    }
    public bool TryImprove(int amount)
    {
        if(Closed || amount<=0 || amount>AvailableFunds)return false;
        SpendFunds(amount);Improvements+=amount;return true;
    }
    public bool TrySell(int price, int cost, int tender, int change)
    {
        if (Closed || price <= 0 || cost < 0 || tender < price || change < 0 || tender - price != change) return false;
        Cash += price; Revenue += price; CostOfGoods += cost; Sales++; return true;
    }
    public void Close(int operatingCost)
    {
        if (Closed) return;
        OperatingCosts = Math.Max(0, operatingCost); SpendFunds(OperatingCosts); Closed = true;
    }
}

public static class ShopInputGate
{
    private static int consumedFrame = -1;
    public static bool WorldActionConsumed => consumedFrame == Time.frameCount;
    public static void ConsumeWorldAction() => consumedFrame = Time.frameCount;
    public static bool Blocked { get; set; }
    public static void SetBlocked(bool blocked, bool updateCursor = true)
    {
        Blocked = blocked;
        if (!updateCursor) return;
        Cursor.lockState = blocked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = blocked;
    }
}

public static class ShopPeople
{
    public static string Order(int person, int[] types, int visit, int day)
    {
        string items = string.Join(", ", types.GroupBy(t => t).Select(g => g.Count() + " × " + ShopCatalog.ShortNames[g.Key]));
        return "Kérek " + items + ". " + Greet(person, visit, day);
    }
    private static int Pick(int person, int visit, int day, int salt)
    {
        return Math.Min(2, (int)(ShopReputationState.Sample(visit, day, (uint)(salt + person * 17)) * 3));
    }
    public static string Greet(int person, int visit, int day) => Greetings[person][Pick(person, visit, day, 311)];
    public static string Thank(int person, int visit, int day) => Thanks[person][Pick(person, visit, day, 317)];
    public static string Complain(int person, int visit, int day) => Wrong[person][Pick(person, visit, day, 313)];
    private static readonly string[][] Greetings = {
        new[]{"A szokásosat kérem. Most először vagyok itt, de nekem ez a szokásos.", "Ugyanazt, mint mindig. Tudja már fejből.", "A listám rövid és állandó."},
        new[]{"Csak egy percre ugrottam be. A történet hosszabb lesz.", "Na, mi újság? Közben kérem a csomagom.", "Mesélek, de előbb a szokásos."},
        new[]{"Sietek. Csak azt nem tudom még, hová.", "Gyorsan, gyorsan, lekésem a buszt.", "Egy percem van, de az az egy az enyém."},
        new[]{"Munkába viszem. A munkát sajnos itt nem árulják.", "A kollégáknak is hozok, ők fizetnek. Talán.", "Hétfő van. Ebből is látszik."}
    };
    private static readonly string[][] Thanks = {
        new[]{"Na, látja! Máris törzsvendég vagyok.", "Szokás szerint köszönöm.", "A szokásosat kaptam, szokás szerint köszönöm."},
        new[]{"Köszönöm. Na és azt hallotta, hogy… jó, majd holnap.", "Köszi! Holnap folytatom a történetet.", "Köszi, és ne felejtse: holnap ugyanitt."},
        new[]{"Köszi! A visszajáró legalább nem sietett el.", "Köszi, rohanok is!", "Köszi! Még a buszt is elérhetem."},
        new[]{"Köszönöm, ezzel már majdnem munkakedvem is van.", "Köszi, a kollégák nevében is.", "Köszi, hétfőhöz képest ez siker."}
    };
    private static readonly string[][] Wrong = {
        new[]{"Ez nem a szokásos. Ezt én is most látom először.", "Ez nem az én listámon volt.", "Ezt nem szoktam kérni."},
        new[]{"Édesem, ezt a történetet nem így rendeltem.", "Félreértett, de megbocsátok. Ezt nem kérem.", "Ez most nem az én csomagom."},
        new[]{"Sietni sietek, de azért a megfelelőt vinném.", "Nincs időm cserélgetni, ez nem az enyém.", "Rossz csomag, és sietek is. Szép napot."},
        new[]{"Ezzel legfeljebb a főnököt lehet megkínálni.", "A kollégák ezt nem rendelték. Visszaviszem.", "Ez nem a munkába való."}
    };
    public static readonly string[] ShortNames = { "Lucky Stroke", "Marlbroke", "Downhill", "Davidon", "ReturAqua", "Loca Cola", "Hamster Energy", "Kevert", "Kőfejtői", "Rövid kávé", "Hosszú kávé", "Latte", "Dnyipro" };
    public static readonly string[] Names = { "Józsi", "Marika", "Pisti", "Laci" };
    public static readonly Color[] Colors = { new Color(.22f,.35f,.29f), new Color(.60f,.23f,.35f), new Color(.18f,.33f,.65f), new Color(.63f,.42f,.13f) };
    private static readonly string[] Moods = { "RUTINOS", "BESZÉLGETŐS", "SIETŐS", "MUNKÁBA" };
    public static string Mood(int person) => Moods[Mathf.Clamp(person, 0, Moods.Length - 1)];
    public static string Monogram(int person) => Names[Mathf.Clamp(person, 0, Names.Length - 1)].Substring(0, 1);
    public static bool IsComplaintLine(int person, string text)
    {
        if (string.IsNullOrEmpty(text) || person < 0 || person >= Wrong.Length) return false;
        foreach (string line in Wrong[person]) if (line == text) return true;
        return false;
    }
}
