using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Samples existing systems; the pure state owns scoring and daily settlement.
public sealed partial class ShopReputation : MonoBehaviour
{
    private MvpShopController shop;
    private readonly Dictionary<ShopFeatureTarget,double> dirtAges = new Dictionary<ShopFeatureTarget,double>();
    private readonly Dictionary<MvpCustomerActor,int> visitIds = new Dictionary<MvpCustomerActor,int>();
    private int nextVisitId;
    public ShopReputationState State { get; private set; } = new ShopReputationState();
    public int AvailableKinds { get; private set; }
    public int EligibleKinds { get; private set; }
    public void Configure(MvpShopController owner) { shop=owner; State.BeginDay(owner.Day); }
    public void ResetAll()
    {
        State = new ShopReputationState(); dirtAges.Clear(); visitIds.Clear(); nextVisitId=0;
        AvailableKinds = EligibleKinds = 0; showingLast=false;
    }
    public void BeginDay() { State.BeginDay(shop.Day); visitIds.Clear(); nextVisitId=0; showingLast=false; }
    public void Tick(float dt)
    {
        if (shop.Phase != ShopPhase.Open || dt<=0) return;
        var kinds = shop.Pricing.AvailableTypes.Concat(shop.Lottery.Owned?Enumerable.Range(13,3):Enumerable.Empty<int>()).Distinct().ToArray();
        EligibleKinds = kinds.Length; AvailableKinds = kinds.Count(IsAvailable);
        var spills = shop.Activities.ActiveSpills.ToArray();
        var live = new HashSet<ShopFeatureTarget>(spills);
        foreach(var stale in dirtAges.Keys.Where(id=>!live.Contains(id)).ToArray()) dirtAges.Remove(stale);
        double penalty=0;
        foreach(var spill in spills)
        {
            double age=dirtAges.TryGetValue(spill,out var old)?old:0;
            penalty += State.Config.DirtPenalty*State.Config.DirtWeight(age,dt);
            dirtAges[spill]=age+dt;
        }
        var prices = shop.Pricing.AvailableTypes.Where(t=>!ShopLotteryCatalog.IsTicket(t)).ToArray();
        double price = prices.Length>0 ? prices.Average(t=>State.Config.PriceScore(shop.Pricing.Price(t)/(double)ShopCatalog.Prices[t])) : 50;
        State.Observe(dt,EligibleKinds>0?AvailableKinds/(double)EligibleKinds:0,
            System.Math.Max(0,100-penalty),price);
    }
    public bool IsAvailable(int type)
    {
        if (ShopCatalog.IsCoffee(type))
            return shop.StockCount(type)>0 || shop.Activities.CanPrepareDrink(type) ||
                shop.Activities.BrewRemaining>0 && shop.Activities.SelectedRecipe==type;
        return shop.AvailableInventory(type)>0;
    }
    public void Observe(MvpCustomerActor actor)
    {
        if (!actor.Entered || actor.WillSteal) return;
        if (!visitIds.TryGetValue(actor,out int id)) { id=++nextVisitId; visitIds.Add(actor,id); }
        State.Enter(id,ShopReputationConfig.Allowance(actor.OrderTypes,actor.LotteryClaimId!=null || actor.MobileRequest!=null));
    }
    public void MarkMissing(MvpCustomerActor actor) { if(actor!=null && visitIds.TryGetValue(actor,out int id)) State.MarkMissing(id); }
    public void Complete(MvpCustomerActor actor, ShopVisitOutcome outcome)
    {
        if (actor==null || actor.WillSteal) return;
        if (!visitIds.TryGetValue(actor,out int id) || !State.Complete(id,outcome)) return;
        if (outcome==ShopVisitOutcome.Served && actor.OrderTypes.Length>0)
            State.Remember(actor.Person,actor.Product);
    }
    public bool ChooseRegular(int visit, out int person, out int favourite)
    {
        person=favourite=-1;
        if (ShopReputationState.Sample(visit,shop.Day,101) >= State.RegularChance) return false;
        var people=Enumerable.Range(0,4).Where(p=>State.PaidVisits(p)>0 && IsAvailable(State.Favourite(p))).ToArray();
        if (people.Length==0) return false;
        person=people[System.Math.Min(people.Length-1,(int)(ShopReputationState.Sample(visit,shop.Day,503)*people.Length))];
        favourite=State.Favourite(person);
        return true;
    }
    public string RegularOrder(MvpCustomerActor actor)
    {
        string[] lines = {
            "A múltkori blokkot bekereteztem. Most a kerethez vásárolok.",
            "A pletykát hoztam, nem kell becsomagolni.",
            "Megint sietek. A tegnapi sietség még nincs kész.",
            "A munkakedvet továbbra is külön keresem."
        };
        return ShopPeople.Order(actor.Person,actor.OrderTypes)+" "+lines[actor.Person];
    }
}
