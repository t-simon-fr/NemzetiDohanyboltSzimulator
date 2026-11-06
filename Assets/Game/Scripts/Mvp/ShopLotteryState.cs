using System;
using System.Collections.Generic;
using System.Linq;

public enum LotteryTicketStatus { Invalid, Unpaid, AwaitingDraw, Expired, Redeemed, Losing, Winning, OperatorPayout }

public static class ShopLotteryCatalog
{
    public const int FirstType = 13, NumberType = 15, Types = 3;
    public const int UpgradePrice = 12000, Capacity = 30, ClaimDays = 7, PayoutLimit = 10000;
    public const string Operator = "Szerecsenjáték Zrt.";
    public static readonly string[] Names = { "Falusi Fortuna", "Nyugdíj Plusz", "Ötöske" };
    public static readonly int[] Prices = { 500, 1000, 500 };
    public static bool IsTicket(int type) => type >= FirstType && type < FirstType + Types;
    public static int Index(int type) => IsTicket(type) ? type - FirstType : -1;
    public static int Commission(int type) => Prices[Index(type)] / 10;

    public static bool TryNumbers(string text, out int[] numbers)
    {
        numbers = Array.Empty<int>();
        var fields = (text ?? "").Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 5) return false;
        var parsed = new int[5];
        for (int i = 0; i < parsed.Length; i++)
            if (!int.TryParse(fields[i], out parsed[i]) || parsed[i] < 1 || parsed[i] > 20) return false;
        if (parsed.Distinct().Count() != 5) return false;
        numbers = parsed.OrderBy(n => n).ToArray();
        return true;
    }
}

public sealed class LotteryTicket
{
    public string Id { get; internal set; }
    public int Type { get; internal set; }
    public int CreatedDay { get; internal set; }
    public int SoldDay { get; internal set; }
    public int Owner { get; internal set; } = -1;
    public int DrawDay { get; internal set; }
    public int Prize { get; internal set; }
    public bool Sold { get; internal set; }
    public bool Redeemed { get; internal set; }
    public bool Voided { get; internal set; }
    public bool ReturnComplete { get; internal set; }
    public int LastVisitDay { get; internal set; }
    private int[] numbers = Array.Empty<int>();
    public int[] Numbers => (int[])numbers.Clone();
    internal void SetNumbers(int[] value) => numbers = (int[])value.Clone();
}

// Pure, per-run retail book. Consignment stock is not bought at face value.
// The ledger callback is the only place where cash changes; failed callbacks leave this book unchanged.
public sealed class ShopLotteryState
{
    private readonly int[] stock = new int[ShopLotteryCatalog.Types];
    private readonly int[] pending = new int[ShopLotteryCatalog.Types];
    private readonly float[] arrival = new float[ShopLotteryCatalog.Types];
    private readonly Dictionary<string, LotteryTicket> tickets = new Dictionary<string, LotteryTicket>(StringComparer.Ordinal);
    private readonly Dictionary<int, int[]> draws = new Dictionary<int, int[]>();
    private readonly Random random;
    private int nextId, day = 1;
    private bool transaction;
    public bool Owned { get; private set; }
    public int Sales { get; private set; }
    public int Payouts { get; private set; }
    public int Commission { get; private set; }
    public int Settled { get; private set; }
    public int DailySales { get; private set; }
    public int DailyPayouts { get; private set; }
    public int DailyCommission { get; private set; }
    public int Outstanding => Sales - Payouts - Commission - Settled;
    public int Reserve => Math.Max(0, Outstanding);
    public int DrawCount => draws.Count;
    public IEnumerable<LotteryTicket> Tickets => tickets.Values;
    public ShopLotteryState(int seed = 106) { random = new Random(seed); }
    public LotteryTicket Find(string id) => id != null && tickets.TryGetValue(id, out var ticket) ? ticket : null;
    public int Stock(int type) { int i = ShopLotteryCatalog.Index(type); return i < 0 ? 0 : stock[i]; }
    public int Pending(int type) { int i = ShopLotteryCatalog.Index(type); return i < 0 ? 0 : pending[i]; }

    public bool TryBuy(Func<bool> debit)
    {
        if (Owned || transaction) return false;
        transaction = true;
        try { if (!debit()) return false; Owned = true; return true; }
        finally { transaction = false; }
    }

    public bool Order(int type, int count)
    {
        int i = ShopLotteryCatalog.Index(type);
        int printed = tickets.Values.Count(t => t.Type == type && !t.Sold && !t.Voided);
        if (!Owned || i < 0 || count < 1 || count > 10 || stock[i] + pending[i] + printed + count > ShopLotteryCatalog.Capacity) return false;
        pending[i] += count;
        arrival[i] = 8;
        return true;
    }

    public void Tick(float seconds)
    {
        for (int i = 0; i < stock.Length; i++)
        {
            if (pending[i] == 0) continue;
            arrival[i] -= Math.Max(0, seconds);
            if (arrival[i] > 0) continue;
            stock[i] += pending[i]; pending[i] = 0; arrival[i] = 0;
        }
    }

    public int[] QuickPick() => Enumerable.Range(1, 20).OrderBy(_ => random.Next()).Take(5).OrderBy(n => n).ToArray();

    public LotteryTicket Print(int type, int currentDay, int[] numbers = null)
    {
        int i = ShopLotteryCatalog.Index(type);
        if (!Owned || i < 0 || stock[i] == 0 || currentDay != day || transaction) return null;
        if (type == ShopLotteryCatalog.NumberType &&
            !ShopLotteryCatalog.TryNumbers(string.Join(" ", numbers ?? Array.Empty<int>()), out numbers)) return null;
        var ticket = new LotteryTicket {
            Id = "SJ-" + (++nextId).ToString("000000"), Type = type, CreatedDay = currentDay,
            DrawDay = type == ShopLotteryCatalog.NumberType ? currentDay + 1 : 0,
            Prize = type == ShopLotteryCatalog.NumberType ? 0 : ScratchPrize(type)
        };
        if (type == ShopLotteryCatalog.NumberType) ticket.SetNumbers(numbers);
        stock[i]--;
        tickets.Add(ticket.Id, ticket);
        return ticket;
    }

    private int ScratchPrize(int type)
    {
        int roll = random.Next(100);
        int price = ShopLotteryCatalog.Prices[ShopLotteryCatalog.Index(type)];
        return roll < 4 ? price * 20 : roll < 12 ? price * 5 : roll < 35 ? price : 0;
    }

    public bool CanSell(string id, int currentDay)
    {
        var t = Find(id);
        return Owned && t != null && !t.Sold && !t.Voided && currentDay == day
            && (t.DrawDay == 0 || t.DrawDay > currentDay);
    }

    public bool TrySell(string id, int owner, int currentDay, Func<bool> payment)
    {
        if (transaction || owner < 0 || !CanSell(id, currentDay)) return false;
        transaction = true;
        try
        {
            if (!payment()) return false;
            var t = Find(id);
            t.Sold = true; t.Owner = owner; t.SoldDay = currentDay;
            int price = ShopLotteryCatalog.Prices[ShopLotteryCatalog.Index(t.Type)];
            int fee = ShopLotteryCatalog.Commission(t.Type);
            Sales += price; DailySales += price; Commission += fee; DailyCommission += fee;
            return true;
        }
        finally { transaction = false; }
    }

    public bool VoidUnpaid(string id)
    {
        var t = Find(id);
        if (transaction || t == null || t.Sold || t.Voided) return false;
        t.Voided = true;
        stock[ShopLotteryCatalog.Index(t.Type)]++;
        return true;
    }

    public void AdvanceDay(int currentDay)
    {
        if (currentDay <= day) return;
        for (int d = day + 1; d <= currentDay; d++)
        {
            if (!tickets.Values.Any(t => t.DrawDay == d && t.Sold)) continue;
            if (!draws.ContainsKey(d)) draws.Add(d, QuickPick());
            foreach (var t in tickets.Values.Where(t => t.DrawDay == d && t.Sold))
            {
                int hits = t.Numbers.Intersect(draws[d]).Count();
                t.Prize = hits == 5 ? 25000 : hits == 4 ? 5000 : hits == 3 ? 1500 : hits == 2 ? 500 : 0;
            }
        }
        day = currentDay;
        DailySales = DailyPayouts = DailyCommission = 0;
    }

    public int[] DrawNumbers(int drawDay) => draws.TryGetValue(drawDay, out var value) ? (int[])value.Clone() : Array.Empty<int>();

    public LotteryTicketStatus Scan(string id, int currentDay)
    {
        var t = Find(id);
        if (!Owned || t == null || t.Voided) return LotteryTicketStatus.Invalid;
        if (!t.Sold) return LotteryTicketStatus.Unpaid;
        if (t.Redeemed) return LotteryTicketStatus.Redeemed;
        int validFrom = t.DrawDay > 0 ? t.DrawDay : t.SoldDay;
        if (currentDay > validFrom + ShopLotteryCatalog.ClaimDays) return LotteryTicketStatus.Expired;
        if (t.DrawDay > 0 && !draws.ContainsKey(t.DrawDay)) return LotteryTicketStatus.AwaitingDraw;
        if (t.Prize == 0) return LotteryTicketStatus.Losing;
        return t.Prize > ShopLotteryCatalog.PayoutLimit ? LotteryTicketStatus.OperatorPayout : LotteryTicketStatus.Winning;
    }

    public bool TryRedeem(string id, int currentDay, Func<int, bool> payout)
    {
        if (transaction || Scan(id, currentDay) != LotteryTicketStatus.Winning) return false;
        transaction = true;
        try
        {
            var t = Find(id);
            if (!payout(t.Prize)) return false;
            t.Redeemed = true; Payouts += t.Prize; DailyPayouts += t.Prize;
            return true;
        }
        finally { transaction = false; }
    }

    public bool TrySettle(Func<int, bool> transfer)
    {
        int amount = Outstanding;
        if (!Owned || transaction || amount == 0) return false;
        transaction = true;
        try { if (!transfer(-amount)) return false; Settled += amount; return true; }
        finally { transaction = false; }
    }

    public LotteryTicket ScheduleReturn(int currentDay)
    {
        var t = tickets.Values.FirstOrDefault(ticket => ticket.Sold && !ticket.Redeemed && !ticket.Voided && !ticket.ReturnComplete
            && ticket.SoldDay < currentDay && ticket.LastVisitDay < currentDay
            && (ticket.DrawDay == 0 || draws.ContainsKey(ticket.DrawDay)));
        if (t != null) t.LastVisitDay = currentDay;
        return t;
    }

    public void FinishReturn(string id, int currentDay)
    {
        var t = Find(id);
        var status = Scan(id, currentDay);
        if (t != null && status != LotteryTicketStatus.Winning && status != LotteryTicketStatus.AwaitingDraw)
            t.ReturnComplete = true;
    }
}
