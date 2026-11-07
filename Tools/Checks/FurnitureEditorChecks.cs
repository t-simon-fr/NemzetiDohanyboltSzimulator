using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
public static class FurnitureEditorChecks
{
    private static ShopFurniturePose Fit(MvpShopController shop, ShopFurniture item)
    {
        var room = item.InWarehouse ? shop.Layout.WarehouseArea : shop.Layout.ShopArea;
        var plan = shop.Arrangement.Draft;
        for (float z = room.yMin + .7f; z < room.yMax - .4f; z += .25f)
            for (float x = room.xMin + .7f; x < room.xMax - .4f; x += .25f)
                for (int turn = 0; turn < 4; turn++)
                {
                    var pose = shop.Arrangement.SnapToGuides(item, new ShopFurniturePose(new Vector3(x, 0, z), turn));
                    plan[item] = pose;
                    if (shop.Arrangement.Validate(plan, out var reason)) return pose;
                }
        throw new Exception("No placement for " + item.Title);
    }

    public static object Run()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var g=MvpShopController.Instance;var e=g.Arrangement;
        try
        {
            g.GetComponent<ShopMenu>().Resume();g.ResetDemo();g.ClosePanel();g.ManualTick=true;
            ShopCheckFixtures.Fund(g,200000);g.Layout.SetLevels(3,3);
            var starter=e.Current().Keys.First(f=>f.InitiallyOwned);
            check(starter.Height>2.4f,"Tall starter height is measured from actual model");
            int cash=g.Ledger.Cash;
            check(e.Begin()&&e.PlanSale(starter)&&e.SaleCredit==3000,"Plan starter sale at explicit 50% valuation");
            check(starter.gameObject.activeSelf&&g.Ledger.Cash==cash,"Sale preview changes neither object nor money");
            e.Cancel();check(starter.gameObject.activeSelf&&e.SaleCount==0&&g.Ledger.Cash==cash,"Cancel restores sale plan");
            check(e.Begin()&&e.PlanSale(starter)&&e.Save(),"Confirm starter sale");
            check(!starter.gameObject.activeSelf&&g.Ledger.Cash==cash+3000&&g.Ledger.FurnitureResale==3000&&g.Ledger.Sales==0,"Resale credited once, not counted as merchandise sale");
            check(!e.Save()&&g.Drawer.Total==g.Ledger.Cash,"No repeated credit and physical till reconciles");
            cash=g.Ledger.Cash;
            check(e.Begin(),"Reopen editor");
            for(int i=0;i<2;i++)check(e.StartPurchase(4)&&e.PlacePurchase(Fit(g,e.PurchasePreview)),"Place fridge instance "+i);
            check(e.PlannedCost==36000&&e.Save(),"One combined two-fridge purchase");
            var fridges=e.Current().Keys.Where(f=>f.CatalogKind==4).ToArray();
            check(fridges.Length==2&&fridges[0].Id!=fridges[1].Id&&fridges[0].Slots.Intersect(fridges[1].Slots).Count()==0,"Independent identities and stock slots for two fridges");
            check(g.Ledger.Cash==cash-36000,"Exact two-fridge debit");
            var product=g.CreateCoffee(ShopCatalog.Index("water"));product.AssignInitialSlot(fridges[0].Slots[0]);
            check(e.Begin()&&!e.PlanSale(fridges[0])&&product.CurrentSlot==fridges[0].Slots[0],"Stocked fridge rejects resale without inventory mutation");
            product.RemoveFromWorld();check(e.PlanSale(fridges[0])&&e.SaleCredit==9000,"Empty fridge resale available");
            check(e.Save()&&e.Current().Keys.Count(f=>f.CatalogKind==4)==1,"Selling one fridge retains the other");
            // Isolate wall placement from the deliberately dense fridge purchase fixture.
            g.ResetDemo();g.ClosePanel();g.Layout.SetLevels(3,3);ShopCheckFixtures.Fund(g,200000);
            check(e.Begin()&&e.StartPurchase(0),"Choose starter-model normal shelf");
            check(e.PurchasePreview.Height>2.4f && Mathf.Abs(e.PurchasePreview.Height-starter.Height)<.01f,"Purchased normal shelf matches the starter model height");
            var low=e.PurchasePreview;
            foreach(int turn in Enumerable.Range(0,4))
            {
                var pose=e.SnapToGuides(low,new ShopFurniturePose(new Vector3(g.Layout.ShopArea.xMax-.65f,0,-1.21f),turn));
                var bounds=low.BoundsAt(pose);
                if(turn%2==0)check(Mathf.Abs(bounds.xMax-g.Layout.ShopArea.xMax)<.001f&&e.SnapFeedback.Contains("Fal"),"Right wall attracts actual edge "+turn);
                check(Mathf.Abs(bounds.yMin/.1f-Mathf.Round(bounds.yMin/.1f))<.001f,"Visible grid aligns depth edge "+turn);
            }
            var wallPose=e.SnapToGuides(low,new ShopFurniturePose(new Vector3(g.Layout.ShopArea.xMax-.65f,0,-1.2f),0));
            var epsilonPlan=e.Draft;
            epsilonPlan[low]=new ShopFurniturePose(wallPose.Position+Vector3.right*.0000005f,0);
            check(e.Validate(epsilonPlan,out var epsilonReason),"Sub-millimetre rotation rounding is accepted at wall");
            epsilonPlan[low]=new ShopFurniturePose(wallPose.Position+Vector3.right*.01f,0);
            check(!e.Validate(epsilonPlan,out var outsideReason),"One centimetre of real wall overflow remains invalid");
            bool wallPlaced=e.PlacePurchase(wallPose);
            check(wallPlaced,"Wall-attracted pose is actually placeable, including wall collider margin: "+e.Feedback);
            check(e.ModelPreviewVisible,"Real mesh preview is available synchronously after selection");
            check(e.Save(),"Wall placement can be committed");
            var wallProduct=g.CreateCoffee(ShopCatalog.Index("water"));
            wallProduct.AssignInitialSlot(low.Slots.OrderByDescending(s=>s.transform.localPosition.x).First());
            var approach=g.Layout.ShelfApproach(wallProduct);
            check(g.Navigation.TryPath(g.Layout.EntryInside,approach,out var path),"Actual customer target for corner shelf column remains reachable beside wall");
            e.Cancel();g.ResetDemo();
            check(starter.gameObject.activeSelf&&!g.GetComponent<ShopNextUpgrades>().FridgeOwned&&e.SaleCredit==0,"New game restores starter ownership and removes purchased fridges");
            var ledger=new ShopLedger(100);ledger.SetLotteryReserve(90);
            check(!ledger.TryFurnish(100,50)&&ledger.Cash==100&&ledger.FurnitureResale==0,"Insufficient combined funds cannot partially credit a resale");
            check(ledger.TryFurnish(50,50)&&ledger.Cash==100&&ledger.AvailableFunds==10,"Sale can fund replacement without spending operator reserve");
            ledger.Close(0);check(!ledger.TryFurnish(0,50),"Closed ledger cannot sell furniture");
            return new{passed,failed=0};
        }
        finally{e.Cancel();g.ResetDemo();g.ClosePanel();g.ManualTick=false;}
    }
}
