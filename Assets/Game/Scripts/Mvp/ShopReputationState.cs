using System;
using System.Collections.Generic;
using System.Linq;

public enum ShopVisitOutcome { Served, Cancelled, MissingStock, PriceRefusal }

// All tuning lives here. Reputation never owns money, products or Unity objects.
public sealed class ShopReputationConfig
{
    public double InitialValue = 50, MinimumSeconds = 30, MinimumVisits = 3;
    public double TimeConfidence = 120, ServiceConfidence = 3;
    public double Smoothing = .18, MaximumDailyChange = 4, WeakestFactorAllowance = 35;
    public double[] Weights = { .30, .20, .20, .30 };
    public double ArrivalInfluence = .20, BasketInfluence = .20, RegularInfluence = .45;
    public double FreshDirtWeight = .10, DirtAgeSeconds = 60, DirtPenalty = 25;

    public double PriceScore(double ratio) => Clamp(ratio <= 1 ? 90 + Math.Min(5, (1-ratio)*10) : 90-(ratio-1)*150, 0, 100);
    public double ServiceScore(double seconds, double allowance) => Clamp(100-Math.Max(0, seconds/Math.Max(1,allowance)-1)*60,0,100);
    public static double Allowance(IEnumerable<int> types, bool claim = false)
        => claim ? 75 : 30 + types.Sum(t => ShopCatalog.IsCoffee(t) ? 25 : ShopLotteryCatalog.IsTicket(t) ? 30 : 8);
    public double DirtWeight(double age, double seconds)
    {
        double Integral(double t) => t <= DirtAgeSeconds ? FreshDirtWeight*t+(1-FreshDirtWeight)*t*t/(2*DirtAgeSeconds) :
            FreshDirtWeight*DirtAgeSeconds+(1-FreshDirtWeight)*DirtAgeSeconds/2+t-DirtAgeSeconds;
        return seconds > 0 ? (Integral(age+seconds)-Integral(age))/seconds : FreshDirtWeight;
    }
    public static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max,value));
}

public sealed class ShopReputationReport
{
    public int Day { get; internal set; }
    public double Value { get; internal set; }
    public double Change { get; internal set; }
    public double Seconds { get; internal set; }
    public int Visits { get; internal set; }
    public int ServiceVisits { get; internal set; }
    public int ServedVisits { get; internal set; }
    public double MeanServiceSeconds { get; internal set; }
    public int Missing { get; internal set; }
    public int Refusals { get; internal set; }
    public int Cancelled { get; internal set; }
    public bool EnoughData { get; internal set; }
    public IReadOnlyList<double> Scores { get; internal set; }
    public IReadOnlyList<bool> Evidence { get; internal set; }
    public IReadOnlyList<string> Reasons { get; internal set; }
}

public sealed class ShopReputationState
{
    private sealed class Visit
    {
        public double Started, Allowance;
        public bool Missing;
    }
    private readonly Dictionary<int,Visit> visits = new Dictionary<int,Visit>();
    private readonly HashSet<int> seen = new HashSet<int>();
    private readonly HashSet<int> closed = new HashSet<int>();
    private readonly int[] favourites = Enumerable.Repeat(-1,4).ToArray();
    private readonly int[] paidVisits = new int[4];
    private readonly double[] timeTotals = new double[3];
    private double serviceTotal, serviceSeconds;
    private int serviceCount, served, missing, refusals, cancelled, completed;
    public ShopReputationConfig Config { get; }
    public double Value { get; private set; }
    public double OpenSeconds { get; private set; }
    public int Day { get; private set; }
    public int RegularVisits { get; private set; }
    public ShopReputationReport LastReport { get; private set; }
    public double ArrivalScale => 1-(Value-50)/50*Config.ArrivalInfluence;
    public double RegularChance => Math.Max(0,(Value-50)/50)*Config.RegularInfluence;
    public int Favourite(int person) => favourites[person];
    public int PaidVisits(int person) => paidVisits[person];

    public ShopReputationState(double? initial = null, ShopReputationConfig config = null)
    {
        Config = config ?? new ShopReputationConfig();
        Value = ShopReputationConfig.Clamp(initial ?? Config.InitialValue,0,100);
    }
    public void BeginDay(int day)
    {
        if (day == Day) return;
        if (day < Day || closed.Contains(day)) throw new InvalidOperationException("A completed day cannot restart.");
        Day = day; OpenSeconds = serviceTotal = serviceSeconds = 0;
        serviceCount = served = missing = refusals = cancelled = completed = 0;
        Array.Clear(timeTotals,0,timeTotals.Length);
        visits.Clear(); seen.Clear();
    }
    public void Observe(double seconds, double availability, double cleanliness, double priceScore)
    {
        if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds) || closed.Contains(Day)) return;
        if (new[]{availability,cleanliness,priceScore}.Any(x=>double.IsNaN(x)||double.IsInfinity(x))) return;
        OpenSeconds += seconds;
        timeTotals[0] += ShopReputationConfig.Clamp(availability,0,1)*100*seconds;
        timeTotals[1] += ShopReputationConfig.Clamp(cleanliness,0,100)*seconds;
        timeTotals[2] += ShopReputationConfig.Clamp(priceScore,0,100)*seconds;
    }
    public bool Enter(int id, double allowance)
    {
        if (closed.Contains(Day) || !seen.Add(id)) return false;
        visits.Add(id,new Visit {Started=OpenSeconds,Allowance=Math.Max(1,allowance)});
        return true;
    }
    public void MarkMissing(int id) { if (visits.TryGetValue(id,out var visit)) visit.Missing = true; }
    public bool Complete(int id, ShopVisitOutcome outcome)
    {
        if (!visits.TryGetValue(id,out var visit) || closed.Contains(Day)) return false;
        visits.Remove(id); completed++;
        if (outcome == ShopVisitOutcome.Served) served++;
        if (outcome == ShopVisitOutcome.PriceRefusal) { refusals++; return true; }
        if (outcome == ShopVisitOutcome.MissingStock || visit.Missing) { missing++; return true; }
        double duration = Math.Max(0,OpenSeconds-visit.Started);
        double score = Config.ServiceScore(duration,visit.Allowance);
        if (outcome == ShopVisitOutcome.Cancelled) { cancelled++; score=0; }
        serviceTotal += score; serviceSeconds += duration; serviceCount++;
        return true;
    }
    public void Remember(int person, int product)
    {
        if (person < 0 || person >= favourites.Length || ShopLotteryCatalog.IsTicket(product) || product < 0) return;
        favourites[person] = product; paidVisits[person]++;
    }
    public void RecordRegular() => RegularVisits++;
    public int BasketSize(int visit, double sample)
    {
        int size = 1 + visit%3;
        double influence = (Value-50)/50*Config.BasketInfluence;
        if (sample < Math.Abs(influence)) size += Math.Sign(influence);
        return Math.Max(1,Math.Min(3,size));
    }
    // Independent of Unity's global RNG: arrival comparisons keep the same random stream.
    public static double Sample(int visit, int day, uint salt)
    {
        uint x = unchecked((uint)visit*747796405u+(uint)day*2891336453u+salt);
        x = ((x >> (int)((x >> 28)+4)) ^ x)*277803737u;
        return ((x >> 22)^x)/(double)uint.MaxValue;
    }
    public ShopReputationReport Preview()
    {
        double confidence = Math.Min(1,OpenSeconds/Config.TimeConfidence);
        var raw = timeTotals.Select(t=>OpenSeconds > 0 ? t/OpenSeconds : 50).ToArray();
        if (completed > 0)
        {
            raw[0] = Math.Min(raw[0],100*(1-missing/(double)completed));
            raw[2] = Math.Min(raw[2],100*(1-refusals/(double)completed));
        }
        var scores = raw.Select(x=>50+(x-50)*confidence).ToList();
        scores.Add(50+((serviceCount>0?serviceTotal/serviceCount:50)-50)*Math.Min(1,serviceCount/Config.ServiceConfidence));
        bool enough = OpenSeconds >= Config.MinimumSeconds && completed >= Config.MinimumVisits;
        var reasons = new List<string>();
        if (!enough) reasons.Add("Még kevés az adat: legalább "+Config.MinimumSeconds.ToString("0")+" nyitott másodperc és "+Config.MinimumVisits.ToString("0")+" lezárt látogatás kell.");
        if (missing>0) reasons.Add(missing+" rendelésnél hiányzott a kért áru.");
        if (refusals>0) reasons.Add(refusals+" vevő az árak miatt távozott.");
        if (cancelled>0) reasons.Add(cancelled+" vevő kiszolgálás nélkül távozott.");
        if (OpenSeconds>0 && raw[1]<75) reasons.Add("Sokáig maradt kosz a padlón.");
        if (OpenSeconds>0 && raw[0]<80 && missing==0) reasons.Add("A feloldott választék egy része nem volt készleten.");
        if (serviceCount>0 && serviceTotal/serviceCount<70 && cancelled==0) reasons.Add("A vevők túl sokat vártak a rendelésükhöz képest.");
        if (OpenSeconds>=Config.MinimumSeconds && raw[1]>=95) reasons.Add("Nyitvatartás alatt tiszta volt a bolt.");
        if (serviceCount>=Config.MinimumVisits && serviceTotal/serviceCount>=85) reasons.Add("A várakozás belefért a rendelések idejébe.");
        if (reasons.Count==0) reasons.Add("A mai megfigyelések gyűjtése folyamatban van.");
        return new ShopReputationReport {
            Day=Day,Value=Value,Seconds=OpenSeconds,Visits=completed,ServiceVisits=serviceCount,ServedVisits=served,
            MeanServiceSeconds=serviceCount>0?serviceSeconds/serviceCount:0,Missing=missing,Refusals=refusals,Cancelled=cancelled,
            EnoughData=enough,Scores=Array.AsReadOnly(scores.ToArray()),
            Evidence=Array.AsReadOnly(new[]{OpenSeconds>0,OpenSeconds>0,OpenSeconds>0,serviceCount>0}),Reasons=reasons.AsReadOnly()
        };
    }
    public ShopReputationReport CloseDay()
    {
        if (!closed.Add(Day)) return LastReport;
        var report = Preview();
        if (report.EnoughData)
        {
            double target = report.Scores.Select((s,i)=>s*Config.Weights[i]).Sum()/Config.Weights.Sum();
            double weakest = report.Scores.Where((s,i)=>report.Evidence[i]).DefaultIfEmpty(50).Min();
            target = Math.Min(target,weakest+Config.WeakestFactorAllowance);
            if (served==0) target = Math.Min(target,Value);
            double delta = ShopReputationConfig.Clamp((target-Value)*Config.Smoothing,-Config.MaximumDailyChange,Config.MaximumDailyChange);
            double next = ShopReputationConfig.Clamp(Value+delta,0,100);
            report.Change = next-Value; Value = next;
        }
        report.Value = Value; LastReport = report;
        return report;
    }
}
