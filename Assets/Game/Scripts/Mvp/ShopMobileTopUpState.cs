using System;
using System.Collections.Generic;
using System.Linq;

public sealed class ShopMobileConfig
{
    public const int UnlockDay = 6;
    public int CommissionPercent { get; }
    public static readonly string[] Providers = { "Üreskom", "Notyet tel", "Two" };
    public static readonly int[] Amounts = { 1000, 2000, 5000 };
    public ShopMobileConfig(int commissionPercent = 5)
    {
        if (commissionPercent < 0 || commissionPercent > 100) throw new ArgumentOutOfRangeException();
        CommissionPercent = commissionPercent;
    }
    public int Commission(int amount) => amount * CommissionPercent / 100;
    public static string Normalize(string number)
    {
        if (number == null || number.Any(c => !(c >= '0' && c <= '9') && c != ' ' && c != '-')) return "";
        string digits = new string(number.Where(c => c >= '0' && c <= '9').ToArray());
        return digits.Length == 11 && digits.StartsWith("000", StringComparison.Ordinal) ? digits : "";
    }
}

public sealed class ShopMobileRequest
{
    public int VisitId { get; }
    public int Provider { get; }
    public string Number { get; }
    public int Amount { get; }
    public ShopMobileRequest(int visitId, int provider, string number, int amount)
    {
        if (visitId < 0 || provider < 0 || provider >= ShopMobileConfig.Providers.Length || ShopMobileConfig.Normalize(number) == "" || !ShopMobileConfig.Amounts.Contains(amount))
            throw new ArgumentException("Invalid simulated mobile request.");
        VisitId = visitId; Provider = provider; Number = ShopMobileConfig.Normalize(number); Amount = amount;
    }
    public string Description => ShopMobileConfig.Providers[Provider] + " · " + Number + " · " + ShopCatalog.Money(Amount);
}

public sealed class ShopMobileReceipt
{
    public string Id { get; }
    public ShopMobileRequest Request { get; }
    public int Day { get; }
    public int Commission { get; }
    public int OperatorShare => Request.Amount - Commission;
    public ShopMobileReceipt(string id, ShopMobileRequest request, int day, int commission)
    { Id = id; Request = request; Day = day; Commission = commission; }
}

// Immutable requests/receipts; UI drafts cannot change a prepared or paid transaction.
public sealed class ShopMobileTopUpState
{
    public ShopMobileConfig Config { get; }
    public bool Owned { get; private set; }
    public ShopMobileRequest Prepared { get; private set; }
    public ShopMobileReceipt LastReceipt { get; private set; }
    private readonly HashSet<int> paidVisits = new HashSet<int>();
    private bool posting;
    public int DailySales { get; private set; }
    public int DailyCommission { get; private set; }
    public int DailyCount { get; private set; }
    public int Outstanding { get; private set; }
    private int receiptSequence, visitSequence;
    public ShopMobileTopUpState(ShopMobileConfig config = null) { Config = config ?? new ShopMobileConfig(); }
    public bool ActivateForDay(int day)
    {
        if (Owned || day < ShopMobileConfig.UnlockDay) return false;
        Owned = true; return true;
    }
    public ShopMobileRequest CreateRequest(int visit)
    {
        if (!Owned) return null;
        int id = ++visitSequence;
        return new ShopMobileRequest(id, (id - 1) % ShopMobileConfig.Providers.Length, "000" + id.ToString("D8"), ShopMobileConfig.Amounts[(visit / 3) % 3]);
    }
    public string Validate(ShopMobileRequest request, int provider, string number, string amount)
    {
        if (!Owned || request == null || paidVisits.Contains(request.VisitId)) return "Nincs aktív feltöltési kérés.";
        if (provider < 0 || provider >= ShopMobileConfig.Providers.Length) return "Válassz szolgáltatót.";
        string normalized = ShopMobileConfig.Normalize(number);
        if (normalized == "") return "11 számjegyű, 000 kezdetű játékbeli tesztszám szükséges.";
        if (!int.TryParse(amount, out int value) || !ShopMobileConfig.Amounts.Contains(value)) return "Válassz 1000, 2000 vagy 5000 Ft-ot.";
        if (provider != request.Provider || normalized != request.Number || value != request.Amount) return "A vevő szerint nem ezeket az adatokat kérte. Ellenőrizd a számot, szolgáltatót és összeget.";
        return "";
    }
    public bool Prepare(ShopMobileRequest request, int provider, string number, string amount)
    {
        if (posting || Validate(request, provider, number, amount) != "") return false;
        Prepared = request; return true;
    }
    public bool IsPrepared(ShopMobileRequest request) => request != null && Prepared == request && !paidVisits.Contains(request.VisitId);
    public void Cancel() { if (!posting) Prepared = null; }
    public bool TryComplete(ShopMobileRequest request, int day, Func<bool> payment)
    {
        if (posting || !IsPrepared(request)) return false;
        posting = true;
        try
        {
            if (!payment()) return false;
            var receipt = new ShopMobileReceipt("MOB-" + (++receiptSequence).ToString("D5"), request, day, Config.Commission(request.Amount));
            paidVisits.Add(request.VisitId); LastReceipt = receipt; Prepared = null;
            DailySales += request.Amount; DailyCommission += receipt.Commission; DailyCount++; Outstanding += receipt.OperatorShare;
            return true;
        }
        finally { posting = false; }
    }
    public bool TrySettle(Func<int, bool> transfer)
    {
        if (posting || Outstanding <= 0) return false;
        posting = true;
        try
        {
            if (!transfer(Outstanding)) return false;
            Outstanding = 0; return true;
        }
        finally { posting = false; }
    }
    public void BeginDay() { Cancel(); DailySales = DailyCommission = DailyCount = 0; }
}
