using System;
using System.Linq;
using UnityEngine;

public static class ExpansionChecks
{
    public static object Run()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var shop=MvpShopController.Instance;
        var layout=shop.Layout;
        shop.GetComponent<ShopMenu>().Resume();shop.ResetDemo();shop.ManualTick=true;
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();player.CancelHeld();
        ShopCheckFixtures.Fund(shop,500000);
        int cash=shop.Ledger.Cash;
        check(shop.TryExpand()&&layout.ShopLevel==1&&layout.WarehouseLevel==0,"First shop step is seven metres and independent of warehouse");
        check(shop.Ledger.Cash==cash-24000,"Shop step debits once");
        var plans=new System.Collections.Generic.List<object>();
        var original=shop.Arrangement.Current().ToDictionary(p=>p.Key,p=>p.Value);
        for(int s=0;s<4;s++)for(int w=0;w<4;w++)
        {
            layout.SetLevels(s,w);
            check(Mathf.Abs(layout.ShopArea.width-(4.8f+s*2))<.01f,"Purchased shop dimensions");
            check(Mathf.Abs(layout.WarehouseArea.width-(w<2?3.3f:4.8f))<.01f,"Warehouse dimensions");
            check(shop.Arrangement.Validate(shop.Arrangement.Current(),out var why),"Default circulation "+s+"/"+w+": "+why);
            check(shop.FloorPlan.Find("Door lintel").position.x==ShopFloorPlan.EntranceX,"Door remains in starter shop");
            if(w==0)
            {
                var a=shop.Arrangement.Current();
                var b=a.ToDictionary(p=>p.Key,p=>p.Value);
                int index=0;
                foreach(var item in a.Keys.Where(f=>f.Id=="public-shelf"||f.Id.StartsWith("starter-shelf")).OrderBy(f=>f.Id))
                {
                    b[item]=s>=2?new ShopFurniturePose(new Vector3(3.25f+index*1.25f,0,-1.5f),0):
                        new ShopFurniturePose(a[item].Position+Vector3.left*.2f,a[item].QuarterTurns);
                    index++;
                }
                check(shop.Arrangement.Validate(b,out var planReason),"Alternative floor plan "+s+": "+planReason);
                foreach(var example in new[]{a,b})plans.Add(new{level=s,furniture=example.Select(p=>new{id=p.Key.Id,x=p.Value.Position.x,z=p.Value.Position.z,width=p.Key.BoundsAt(p.Value).width,depth=p.Key.BoundsAt(p.Value).height}).ToArray()});
            }
        }
        layout.SetLevels(0,0);cash=shop.Ledger.Cash;
        check(shop.GetComponent<ShopExpansionGeometry>().BuyWarehouse()&&layout.ShopLevel==0&&layout.WarehouseLevel==1,"Warehouse can expand before shop");
        check(shop.Ledger.Cash==cash-12000,"Warehouse exact cost");
        layout.SetLevels(3,3);
        check(!shop.TryExpand()&&!shop.GetComponent<ShopExpansionGeometry>().BuyWarehouse(),"Maximum levels cannot be bought again");
        foreach(ShopModuleKind kind in Enum.GetValues(typeof(ShopModuleKind)))
        {
            cash=shop.Ledger.Cash;
            check(shop.GetComponent<ShopModularFurniture>().Buy(kind),"Buy usable module "+kind);
            check(shop.Ledger.Cash==cash-ShopModularFurniture.Prices[(int)kind],"Exact module debit "+kind);
        }
        check(shop.Arrangement.Validate(shop.Arrangement.Current(),out var reason),"Purchased modules retain circulation: "+reason);
        var warehouse=shop.Arrangement.Furniture.First(f=>f.InWarehouse);
        check(shop.Warehouse.StorageDocks.Any(d=>d.IsChildOf(warehouse.transform)),"Warehouse module has usable carton docks");
        check(shop.OrderSupply(0,1),"Order actual stock for modular rack");shop.Tick(8.1f);
        var carton=shop.Warehouse.Carton(0);int quantity=carton.Quantity;
        var dock=shop.Warehouse.StorageDocks.First(d=>d.IsChildOf(warehouse.transform));
        check(player.TryHoldCarton(carton)&&player.TryPlaceCarton(dock),"Place actual carton on purchased warehouse rack");
        check(carton.Quantity==quantity&&shop.Warehouse.CartonAt(dock)==carton,"New rack retains exact carton identity and contents");
        check(shop.Arrangement.Begin(),"Arrangement opens with warehouse");
        var before=warehouse.Pose;
        check(!shop.Arrangement.TryPlace(warehouse,new ShopFurniturePose(new Vector3(99,0,99),0)),"Outside warehouse rejected");
        shop.Arrangement.Cancel();check(warehouse.Pose.Position==before.Position,"Cancel retains warehouse placement");
        foreach(var pair in original)check(pair.Key.Pose.Position==pair.Value.Position,"Expansion preserves existing furniture");
        shop.ResetDemo();
        check(layout.ShopLevel==0&&layout.WarehouseLevel==0&&!shop.Arrangement.Furniture.Any(f=>f.InWarehouse),"New game resets levels and purchased modules");
        return new{passed,failed=0,plans};
    }
}
