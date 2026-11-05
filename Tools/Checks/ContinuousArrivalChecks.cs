using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ContinuousArrivalChecks
{
    public static object Run()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var g=MvpShopController.Instance;
        var random=UnityEngine.Random.state;
        try
        {
            g.ResetDemo();g.ClosePanel();g.ManualTick=true;
            check(g.AutomaticArrivals,"New game enables live arrivals");
            g.OpenShop();g.AutomaticArrivals=false;g.Tick(60);
            check(g.Spawned==0,"Explicit-visit fixtures can suspend automatic arrivals");
            g.AutomaticArrivals=true;UnityEngine.Random.InitState(741);
            int stock=Enumerable.Range(0,ShopCatalog.Ids.Length).Sum(g.StockCount),cash=g.Ledger.Cash;
            int previous=0,peak=0;
            float last=0;
            var intervals=new List<float>();
            for(int step=0;step<1000;step++)
            {
                g.Tick(.5f);peak=Math.Max(peak,g.QueueCount);
                if(g.Spawned!=previous)
                {
                    if(previous>0)intervals.Add(step*.5f-last);
                    last=step*.5f;previous=g.Spawned;
                }
                if(g.ActiveCustomer!=null)g.SkipCurrent();
            }
            check(g.Spawned>8 && g.Phase==ShopPhase.Open,"Open shop keeps receiving visits beyond the old eight-visitor day");
            check(peak<=3 && intervals.Distinct().Count()>2,"Randomly spaced arrivals preserve the three-person queue limit");
            check(g.Ledger.Cash==cash && Enumerable.Range(0,ShopCatalog.Ids.Length).Sum(g.StockCount)==stock,"Arrival and rejection do not spend money or consume stock");
            g.RequestClose();int closedCount=g.Spawned;
            for(int step=0;step<600;step++){g.Tick(.5f);if(g.ActiveCustomer!=null)g.SkipCurrent();}
            check(g.Spawned==closedCount && g.CanEndDay && !g.Ledger.Closed,"Closed shop drains existing visitors and waits for manual day end");
            return new{passed,failed=0};
        }
        finally {g.ResetDemo();g.ManualTick=false;UnityEngine.Random.state=random;}
    }
}
