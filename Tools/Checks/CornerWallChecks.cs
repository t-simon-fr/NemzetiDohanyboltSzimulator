using System;
using System.Linq;
using UnityEngine;
public static class CornerWallChecks
{
    public static object Run()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var g=MvpShopController.Instance;var e=g.Arrangement;
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();var body=player.GetComponent<CharacterController>();var home=player.transform.position;
        try
        {
            g.GetComponent<ShopMenu>().Resume();g.ResetDemo();g.ClosePanel();g.ManualTick=true;ShopCheckFixtures.Fund(g,200000);g.Layout.SetLevels(3,3);
            int cash=g.Ledger.Cash;
            check(!g.GetComponent<ShopModularFurniture>().Buy((ShopModuleKind)4)&&g.Ledger.Cash==cash,"Reserved fridge catalog index is not a shelf module purchase");
            check(e.Begin()&&e.StartPurchase(0),"Start front-wall reproduction");
            var area=g.Layout.FurnitureArea(false);
            var front=new Vector3(5.4f,0,area.yMin+.25f);
            check(!e.PlacePurchase(new ShopFurniturePose(front,0))&&e.Feedback.Contains("Forgasd"),"Front-facing-wall shelf explains wrong customer side");
            check(e.PlacePurchase(new ShopFurniturePose(front,2)),"Inward-facing shelf at identical front-wall position fits");
            check(e.PurchasePreview!=null&&e.PreviewQuarterTurns==2,"Repeat placement retains inward rotation");
            e.Cancel();check(g.Ledger.Cash==cash,"Reproduction cancellation changes no money");
            for(int level=0;level<4;level++)
            {
                g.Layout.SetLevels(level,0);
                check(e.Begin()&&e.StartPurchase(0),"Open physical-wall level "+level);
                var item=e.PurchasePreview;area=g.Layout.FurnitureArea(false);
                var pose=e.SnapToGuides(item,new ShopFurniturePose(new Vector3(area.xMax-.65f,0,-1.2f),0));
                check(Mathf.Abs(item.BoundsAt(pose).xMax-area.xMax)<.0001f,"Actual footprint reaches drawn physical boundary "+level);
                check(Mathf.Abs(area.xMax-(g.Layout.ShopArea.xMax+.023f))<.001f,"Wall face replaces legacy 25mm gap "+level);
                e.Cancel();
            }
            check(!UnityEngine.Object.FindObjectsByType<Collider>().Any(c=>c.enabled&&c.name.Contains("wainscot")),"Decorative brown finish adds no collision");
            g.Layout.SetLevels(3,3);cash=g.Ledger.Cash;
            check(e.Begin()&&e.StartPurchase(5),"Corner catalog entry can be selected");
            var corner=e.PurchasePreview;
            check(corner.CatalogKind==5&&corner.Slots.Length==10&&Mathf.Abs(corner.Height-2.5f)<.01f,"Tall corner model has ten real product slots");
            check(corner.GetComponentsInChildren<MeshFilter>(true).Count(f=>f.name=="Corner shelf board"&&f.sharedMesh.vertexCount==10)==5,"Five actual chamfered shelf boards");
            area=g.Layout.FurnitureArea(false);
            var cornerPose=new ShopFurniturePose(new Vector3(area.xMax-.4f,0,area.yMax-.4f),0);
            check(e.PlacePurchase(cornerPose),"Corner fits two walls with inward diagonal customer side: "+e.Feedback);
            var frontCorner=e.Draft;
            var next=e.PurchasePreview;
            frontCorner[next]=new ShopFurniturePose(new Vector3(area.xMax-.4f,0,area.yMin+.4f),1);
            check(e.Validate(frontCorner,out var frontReason),"Front-right corner fits with inward diagonal face: "+frontReason);
            check(e.Save()&&g.Ledger.Cash==cash-6500&&g.Drawer.Total==g.Ledger.Cash,"Corner purchase debits exactly 6500 once");
            var product=g.CreateCoffee(ShopCatalog.Index("water"));product.AssignInitialSlot(corner.Slots[1]);
            check(g.Navigation.TryPath(g.Layout.EntryInside,g.Layout.ShelfApproach(product),out var path),"Actual diagonal NPC pickup target is reachable");
            var saved=corner.Pose;
            g.AutomaticArrivals=false;g.OpenShop();g.RequestClose();g.Tick(.2f);
            body.enabled=false;player.transform.position=g.Layout.EntryInside;body.enabled=true;
            check(g.TryEndDay(),"Leave normally after arranging corner");g.NextDay();
            check(corner.gameObject.activeSelf&&corner.Pose.Position==saved.Position&&product.CurrentSlot==corner.Slots[1],"Next day preserves corner placement and exact stock");
            check(e.Begin()&&!e.PlanSale(corner)&&product.CurrentSlot!=null,"Stocked corner cannot be sold");
            product.RemoveFromWorld();cash=g.Ledger.Cash;
            check(e.PlanSale(corner)&&e.SaleCredit==3250&&e.Save()&&g.Ledger.Cash==cash+3250,"Empty corner resale uses shared atomic 50 percent rule");
            g.ResetDemo();check(!e.Furniture.Any(f=>f.CatalogKind==5&&f.gameObject.activeSelf),"New game removes paid corner modules");
            g.Devices.Focus(ShopDeviceKind.Monitor);g.Devices.OpenApp(ShopComputerApp.Furnishing);
            g.Devices.KeyboardTargets().Single(t=>t.Id=="Furnishing:Furniture").Activate();
            var screen=g.Devices.ComputerScreenRect;var targets=g.Devices.KeyboardTargets();
            check(targets.Count(t=>t.Id.StartsWith("Module:Plan:"))==6&&targets.All(t=>screen.Contains(t.Bounds.min)&&screen.Contains(t.Bounds.max)),"Six catalog entries fit monitor and keyboard navigation");
            return new{passed,failed=0};
        }
        finally{e.Cancel();g.ClosePanel();g.ResetDemo();g.ManualTick=false;body.enabled=false;player.transform.position=home;body.enabled=true;}
    }
}
