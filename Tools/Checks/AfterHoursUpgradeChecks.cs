using System;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

public static class AfterHoursUpgradeChecks
{
    public static object Run()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var shop=MvpShopController.Instance;
        var player=Object.FindAnyObjectByType<PlayerInteractor>();
        var upgrades=shop.GetComponent<ShopNextUpgrades>();
        var fixture=new GameObject("Closing upgrade fixture").AddComponent<ShopAcceptanceProbe>();
        Func<bool>[] buy={shop.TryExpand,shop.BuyExtraShelf,shop.Activities.BuyCoffeeMachine,upgrades.BuyFridge,shop.BuyPanicButton};
        Action reset=()=>{player.CancelHeld();shop.ClosePanel();shop.ResetDemo();shop.ManualTick=true;};
        try
        {
            reset(); ShopCheckFixtures.Fund(shop,400000);
            check(shop.CanDevelopShop,"Preparing still allows development");
            int cash=shop.Ledger.Cash;
            shop.OpenShop();
            check(!shop.CanDevelopShop&&buy.All(action=>!action()),"Open shop rejects every development");
            check(shop.Ledger.Cash==cash&&shop.Drawer.Total==cash,"Rejected purchases consume no money");
            shop.RequestClose();shop.Tick(.1f);
            check(shop.Phase==ShopPhase.AfterHours&&shop.CanDevelopShop,"Closed empty shop permits development before leaving");
            int stock=Enumerable.Range(0,ShopCatalog.Ids.Length).Sum(shop.StockCount);
            int[] costs={ShopLayout.ExpansionPrice,ShopLayout.ExtraShelfPrice,ShopActivities.CoffeeMachinePrice,ShopNextUpgrades.FridgePrice,ShopSecurity.PurchasePrice};
            for(int i=0;i<buy.Length;i++)
            {
                int before=shop.Ledger.Cash;
                check(buy[i](),"Buy development after closing: "+i);
                check(shop.Ledger.Cash==before-costs[i]&&shop.Drawer.Total==shop.Ledger.Cash,"Development exact price and drawer reconciliation: "+i);
                if(i==0) {
                    check(shop.TryExpand()&&shop.TryExpand()&&shop.Layout.ShopLevel==3,"Further shop levels are independently purchasable");
                    int maximumCash=shop.Ledger.Cash;check(!shop.TryExpand()&&shop.Ledger.Cash==maximumCash,"Maximum shop level cannot be purchased again");
                } else check(!buy[i]()&&shop.Ledger.Cash==before-costs[i],"Already owned development cannot be bought twice: "+i);
            }
            check(Enumerable.Range(0,ShopCatalog.Ids.Length).Sum(shop.StockCount)==stock,"Development never creates or deletes merchandise");
            var body=player.GetComponent<CharacterController>();body.enabled=false;player.transform.position=shop.Layout.EntryInside;body.enabled=true;
            check(shop.TryEndDay()&&shop.Ledger.Closed&&shop.Phase==ShopPhase.Summary,"Leaving normally finalizes the upgraded day");
            check(!shop.CanDevelopShop&&buy.All(action=>!action()),"Finalized day cannot purchase developments");
            shop.NextDay();
            check(shop.CanDevelopShop&&shop.Layout.Expanded&&shop.Layout.ExtraShelfOwned&&shop.Activities.CoffeeOwned&&upgrades.FridgeOwned&&shop.Security.Owned,"Every closing-time development persists into next preparation");
            fixture.Prepare("ui-order");ShopCheckFixtures.Fund(shop,100000);shop.RequestClose();cash=shop.Ledger.Cash;
            check(shop.Phase==ShopPhase.Closing&&!shop.CanDevelopShop&&buy.All(action=>!action()),"Closing sign with waiting customers does not permit construction");
            check(shop.Ledger.Cash==cash,"Occupied closing shop preserves funds");
            shop.SkipCurrent();
            check(!shop.CanDevelopShop&&!shop.TryExpand(),"Customers still departing also block layout changes");
            reset();shop.OpenShop();shop.RequestClose();shop.Tick(.1f);cash=shop.Ledger.Cash;
            check(shop.CanDevelopShop&&!shop.TryExpand()&&shop.Ledger.Cash==cash,"After-hours eligibility does not bypass insufficient funds");
            return new{passed,failed=0};
        }
        finally{reset();shop.ManualTick=false;Object.Destroy(fixture.gameObject);}
    }
}
