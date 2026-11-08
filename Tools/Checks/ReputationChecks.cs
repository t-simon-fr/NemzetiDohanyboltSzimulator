using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ReputationChecks
{
    private static readonly BindingFlags Private = BindingFlags.Instance|BindingFlags.NonPublic;
    private static void Day(ShopReputationState s, int day, ShopVisitOutcome result=ShopVisitOutcome.Served,
        double availability=1, double cleanliness=100, double price=90, double seconds=40)
    {
        s.BeginDay(day);
        for(int id=0;id<3;id++)
        {
            s.Enter(id,45);s.Observe(seconds,availability,cleanliness,price);s.Complete(id,result);
        }
    }
    public static object State()
    {
        int passed=0;
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);passed++;};
        var s=new ShopReputationState();s.BeginDay(1);
        check(s.Value==50 && s.Preview().Scores.All(x=>x==50) && s.Preview().Evidence.All(x=>!x),"No evidence is neutral, not a perfect day");
        s.Observe(double.NaN,1,100,90);s.Observe(1,double.PositiveInfinity,100,90);
        check(s.OpenSeconds==0,"Invalid samples cannot create non-finite scores");
        check(s.CloseDay().Change==0 && s.Value==50,"Empty day causes no automatic gain or loss");
        s.BeginDay(2);s.Enter(1,30);s.Observe(10,1,100,90);s.Complete(1,ShopVisitOutcome.Served);
        check(!s.Enter(1,30) && !s.Complete(1,ShopVisitOutcome.Cancelled),"A completed visitor cannot be reopened or counted twice");
        check(s.CloseDay().Change==0,"One quick visit remains insufficient evidence");
        s=new ShopReputationState();Day(s,1);
        check(s.Value==50 && s.Preview().Scores.Count==4,"Reading live ratings does not apply daily reputation");
        var report=s.CloseDay();double value=s.Value;
        check(value>50 && report.Change>0 && report.Change<=4,"Good operation improves reputation gradually within the daily cap");
        check(ReferenceEquals(report,s.CloseDay()) && s.Value==value,"Repeated settlement returns the same result without another gain");
        s.Observe(300,0,0,0);check(s.OpenSeconds==120,"A settled day accepts no more observations");
        s.BeginDay(2);
        check(s.Value==value && s.LastReport==report && s.OpenSeconds==0 && s.Preview().Visits==0,"New day retains reputation and last result but clears daily evidence");
        check(s.CloseDay().Change==0 && s.Value==value,"Idle next day does not undo or inflate the reputation");
        var skipped=new ShopReputationState();Day(skipped,1,ShopVisitOutcome.Cancelled);
        check(skipped.Preview().ServiceVisits==3 && skipped.Preview().Scores[3]==0 && skipped.CloseDay().Change<0,"Skipping slow or fast available orders cannot manufacture an excellent service average");
        var refused=new ShopReputationState();Day(refused,1,ShopVisitOutcome.PriceRefusal);
        check(refused.Preview().Refusals==3 && refused.Preview().ServiceVisits==0 && refused.Preview().Missing==0,"Price refusals affect price only, not service and stock again");
        check(refused.CloseDay().Change<0,"Uniform refusals are not masked by clean floors and full shelves");
        var shortRefusal=new ShopReputationState();Day(shortRefusal,1,ShopVisitOutcome.PriceRefusal,seconds:10);
        check(shortRefusal.CloseDay().Change<=0,"Short all-refusal day cannot earn a confidence-weighted free reputation gain");
        var missing=new ShopReputationState();missing.BeginDay(1);missing.Enter(1,40);missing.MarkMissing(1);missing.Observe(120,1,100,90);missing.Complete(1,ShopVisitOutcome.Served);
        check(missing.Preview().Missing==1 && missing.Preview().ServiceVisits==0 && missing.Preview().Refusals==0,"Substitution marks one stock issue and excludes its delay from service scoring");
        var dirty=new ShopReputationState();Day(dirty,1,cleanliness:0);
        check(dirty.CloseDay().Change<0,"Persistent dirt cannot hide behind excellent other factors");
        var config=new ShopReputationConfig();
        check(config.PriceScore(1)==90 && config.PriceScore(.5)-config.PriceScore(1)<=5 && config.PriceScore(3)==0,"Reference prices score well; deep discounts give only a small benefit");
        check(config.DirtWeight(120,1)>config.DirtWeight(0,1) && config.DirtWeight(0,1)<.12,"Fresh dirt is mild; old dirt has greater exposure");
        double chunks=Enumerable.Range(0,4).Sum(i=>config.DirtWeight(i*30,30)*30);
        check(Math.Abs(chunks-config.DirtWeight(0,120)*120)<1e-7,"Dirt aging integration is independent of tick chunk size");
        check(ShopReputationConfig.Allowance(new[]{9})>ShopReputationConfig.Allowance(new[]{0}) && ShopReputationConfig.Allowance(new[]{13})>ShopReputationConfig.Allowance(new[]{0}) && ShopReputationConfig.Allowance(new int[0],true)>60,"Coffee, lottery and redemption allow appropriate service time");
        var low=new ShopReputationState(0);var neutral=new ShopReputationState(50);var high=new ShopReputationState(100);
        check(low.ArrivalScale==1.2 && neutral.ArrivalScale==1 && high.ArrivalScale==.8,"Arrival changes stay bounded with positive idle intervals at both extremes");
        double lowB=0,baseB=0,highB=0;
        for(int i=0;i<6000;i++)
        {
            double sample=ShopReputationState.Sample(i,3,907);
            lowB+=low.BasketSize(i,sample);baseB+=neutral.BasketSize(i,sample);highB+=high.BasketSize(i,sample);
        }
        check(highB>baseB && baseB>lowB && highB/6000<2.3 && lowB/6000>1.7,"Same seeded samples modestly increase basket sizes, not beyond the shared one-to-three limit");
        check(Enumerable.Range(0,100).All(i=>neutral.BasketSize(i,.1)==1+i%3),"Neutral reputation preserves legacy basket determinism");
        check(high.RegularChance>neutral.RegularChance && high.RegularChance<=.45 && low.RegularChance==0,"Regular probability grows gradually above neutral and stays capped");
        check(ShopReputationState.Sample(12,2,101)==ShopReputationState.Sample(12,2,101),"Visit selection is repeatable without disturbing the arrival RNG");
        for(int day=1;day<100;day++){Day(high,day);high.CloseDay();Day(low,day,ShopVisitOutcome.Cancelled,0,0,0);low.CloseDay();}
        check(high.Value<=100 && high.Value>=0 && low.Value<=100 && low.Value>=0,"Long runs remain inside zero-to-one-hundred bounds");
        return new {passed,failed=0};
    }

    private static void Spawn(MvpShopController g,int type) => typeof(MvpShopController).GetMethod("SpawnCustomer",Private).Invoke(g,new object[]{type,false});
    public static object Integration()
    {
        int passed=0;
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);passed++;};
        var g=MvpShopController.Instance;var player=Object.FindAnyObjectByType<PlayerInteractor>();
        try
        {
            g.GetComponent<ShopMenu>().Resume();g.ResetDemo();g.ClosePanel();g.ManualTick=true;g.AutomaticArrivals=false;
            g.Tick(60);check(g.Reputation.State.Value==50 && g.Reputation.State.OpenSeconds==0,"Preparation does not age reputation");
            g.OpenShop();g.Devices.Focus(ShopDeviceKind.Monitor);g.Devices.OpenApp(ShopComputerApp.Reputation);g.Tick(60);
            check(g.Reputation.State.OpenSeconds==60,"In-world device focus counts real open-shop waiting time, including payment and monitor use");
            var bounds=g.Devices.ComputerScreenRect;
            check(g.Devices.KeyboardTargets().All(c=>bounds.Contains(c.Bounds.min)&&bounds.Contains(c.Bounds.max)),"All reputation controls fit the existing screen");
            g.ClosePanel();g.SetModal(ShopModal.Help);g.Tick(60);check(g.Reputation.State.OpenSeconds==60,"Help pause does not produce service or cleanliness penalties");g.ClosePanel();
            g.GetComponent<ShopMenu>().Open();g.Tick(60);check(g.Reputation.State.OpenSeconds==60,"Actual pause menu freezes reputation observations");g.GetComponent<ShopMenu>().Resume();
            g.Tick(.5f);check(g.Reputation.EligibleKinds==4 && g.Reputation.AvailableKinds==4,"Starter choice counts four unlocked kinds, not their duplicate shelf slots");
            ShopCheckFixtures.Fund(g,300000);g.Layout.SetLevels(1,0);check(g.OrderSupply(6,1),"Queue a real energy delivery");
            int cash=g.Ledger.Cash,stock=Enumerable.Range(0,23).Sum(g.StockCount);g.Tick(1);
            check(g.Reputation.EligibleKinds==9 && g.Reputation.AvailableKinds==4 && g.PendingQuantity(6)==1,"Never-acquired base goods are missing choice; pending shipment is not available");
            check(g.Ledger.Cash==cash && Enumerable.Range(0,23).Sum(g.StockCount)==stock,"Reputation sampling does not spend cash or create stock");
            g.Partners.State.Activate(ShopPartnerKind.Hamster);g.Tick(.1f);
            check(g.Reputation.EligibleKinds==11 && g.Reputation.AvailableKinds==4,"Only purchased partner variants join the denominator");
            g.RequestClose();double elapsed=g.Reputation.State.OpenSeconds;g.Tick(30);
            check(g.Reputation.State.OpenSeconds==elapsed,"Closing and after-hours never age reputation evidence");
            g.ResetDemo();g.ClosePanel();g.AutomaticArrivals=false;ShopCheckFixtures.Fund(g,300000);
            check(g.Activities.BuyCoffeeMachine() && g.Lottery.TryBuy(),"Owned service fixtures integrate with existing purchase gates");
            g.Reputation.State.Config.TimeConfidence=.01;g.OpenShop();g.Tick(.1f);
            check(!g.Reputation.IsAvailable(9) && !g.Reputation.IsAvailable(11),"Empty coffee machine contributes no fictional available drinks");
            typeof(ShopActivities).GetProperty("CoffeeSupplies").SetValue(g.Activities,1);
            check(g.Reputation.IsAvailable(9) && g.Reputation.IsAvailable(10) && !g.Reputation.IsAvailable(11),"Shared coffee resource enables two recipes; latte still needs milk");
            typeof(ShopActivities).GetProperty("MilkSupplies").SetValue(g.Activities,1);
            check(g.Reputation.IsAvailable(11),"Real milk enables latte availability");
            g.Lottery.Order(13);g.Tick(1);
            check(!g.Reputation.IsAvailable(13) && !g.Pricing.TrySetPrice(13,2000),"Pending fixed-price lottery stock neither counts as available nor becomes editable");
            check(Math.Abs(g.Reputation.State.Preview().Scores[2]-90)<.01,"Fixed-price lottery does not lower the ordinary reference-price score");
            g.Tick(8.1f);g.Tick(.1f);check(g.Reputation.IsAvailable(13),"Only arrived lottery stock counts in choice");
            g.ResetDemo();g.ClosePanel();g.AutomaticArrivals=false;g.SingleProductVisits=true;g.OpenShop();
            for(int visit=0;visit<3;visit++)
            {
                Spawn(g,0);
                for(int i=0;i<1000 && g.ActiveCustomer==null;i++)g.Tick(.1f);
                check(g.ActiveCustomer!=null,"Real reputation visit reaches checkout "+visit);
                typeof(MvpCustomerActor).GetProperty("UsesCard").SetValue(g.ActiveCustomer,true);
                typeof(MvpCustomerActor).GetProperty("UsesCoins").SetValue(g.ActiveCustomer,false);
                foreach(var spill in g.Activities.ActiveSpills)spill.gameObject.SetActive(false);
                g.Tick(35);
                var item=Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId=="cig_red" && p.CurrentSlot!=null && p.CurrentSlot.IsStockShelf);
                check(item.AssignInitialSlot(g.DeliverySlot),"Move a real stock item to the existing scanner "+visit);g.DeliverySlot.NotifyProductPlaced(item);
                foreach(char digit in g.ActiveCustomer.TotalPrice.ToString())g.EnterCardDigit(digit-'0');
                check(g.ConfirmCardPayment(),"Real card settlement completes one measured visit "+visit);
                g.Tick(3);
            }
            var preview=g.Reputation.State.Preview();
            check(preview.Visits==3 && preview.ServiceVisits==3 && preview.EnoughData,"Actual visits include entry, queue and payment and supply sufficient daily evidence");
            g.RequestClose();for(int i=0;i<300 && !g.CanEndDay;i++)g.Tick(.1f);
            var body=player.GetComponent<CharacterController>();body.enabled=false;player.transform.position=g.Layout.EntryInside;body.enabled=true;
            check(g.TryEndDay(),"Existing door end-of-day settles reputation");
            double reputation=g.Reputation.State.Value;var last=g.Reputation.State.LastReport;
            check(last!=null && last.Change>0 && last.Change<=4 && reputation>50,"Good real service applies one bounded daily improvement");
            check(!g.TryEndDay() && g.Reputation.State.Value==reputation,"Repeated day exit cannot apply another update");
            g.NextDay();check(g.Reputation.State.Value==reputation && g.Reputation.State.LastReport==last && g.Reputation.State.OpenSeconds==0,"Day transition retains reputation and clears samples");
            g.Devices.Focus(ShopDeviceKind.Monitor);g.Devices.OpenApp(ShopComputerApp.Reputation);
            g.Devices.KeyboardTargets().Single(t=>t.Id=="Reputation:Last").Activate();
            check(g.Devices.KeyboardTargets().Single(t=>t.Id=="Reputation:Last").Enabled && g.Reputation.VisibleReport==last,"Last daily report is accessible through actual TobaccOS controls");g.ClosePanel();
            g.ResetDemo();g.ClosePanel();g.AutomaticArrivals=false;
            var high=new ShopReputationState(100);high.BeginDay(g.Day);high.Remember(2,0);
            typeof(ShopReputation).GetProperty("State").SetValue(g.Reputation,high);
            int regularVisit=Enumerable.Range(0,200).First(v=>g.Reputation.ChooseRegular(v,out var person,out var favourite) && v%4!=3);
            typeof(MvpShopController).GetProperty("Spawned").SetValue(g,regularVisit);g.OpenShop();Spawn(g,-1);
            for(int i=0;i<1000 && g.ActiveCustomer==null;i++)g.Tick(.1f);
            check(g.ActiveCustomer!=null && g.ActiveCustomer.IsRegular && g.ActiveCustomer.Person==2 && g.ActiveCustomer.Product==0,"Higher reputation returns a previously served existing person with an available favourite");
            check(g.Dialogue.Contains("tegnapi sietség") && g.Dialogue.Contains("Lucky Stroke"),"Regular flavour remains absurd while the real order stays readable");
            check(g.ActiveCustomer.OrderTypes.All(t=>g.AvailableInventory(t)>0),"Larger regular basket cannot bypass real stock rules");
            var random=UnityEngine.Random.state;
            int Traffic(double score)
            {
                g.ResetDemo();g.ClosePanel();g.ManualTick=true;
                var state=new ShopReputationState(score);state.BeginDay(g.Day);
                typeof(ShopReputation).GetProperty("State").SetValue(g.Reputation,state);
                UnityEngine.Random.InitState(317);g.OpenShop();
                for(int i=0;i<1000;i++)
                {
                    g.Tick(.5f);
                    if(g.ActiveCustomer!=null)g.SkipCurrent();
                    if(g.QueueCount>3)throw new Exception("Reputation exceeded queue capacity");
                }
                return g.Spawned;
            }
            int lowTraffic,highTraffic;
            try {lowTraffic=Traffic(0);highTraffic=Traffic(100);}finally{UnityEngine.Random.state=random;}
            check(highTraffic>lowTraffic && lowTraffic>5,"Same actual Unity arrival seed produces more visitors at higher reputation while low reputation still receives trade");
            g.ResetDemo();check(g.Reputation.State.Value==50 && g.Reputation.State.LastReport==null && g.Reputation.State.PaidVisits(2)==0,"New game clears reputation, observations and regular habits");
            return new {passed,failed=0};
        }
        finally {player.CancelHeld();g.ClosePanel();g.ResetDemo();g.ManualTick=false;}
    }
}
