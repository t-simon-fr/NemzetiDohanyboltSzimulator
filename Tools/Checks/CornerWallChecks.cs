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
            var cornerPose=new ShopFurniturePose(new Vector3(area.xMax-.6f,0,area.yMax-.6f),0);
            check(e.PlacePurchase(cornerPose),"Corner fits two walls with inward diagonal customer side: "+e.Feedback);
            var frontCorner=e.Draft;
            var next=e.PurchasePreview;
            frontCorner[next]=new ShopFurniturePose(new Vector3(area.xMax-.6f,0,area.yMin+.6f),1);
            check(e.Validate(frontCorner,out var frontReason),"Front-right corner fits with inward diagonal face: "+frontReason);
            var mesh = corner.GetComponentsInChildren<MeshFilter>(true).First(f => f.name == "Corner shelf board").sharedMesh;
            var profile = mesh.vertices.Take(5).ToArray();
            var west = profile.Where(v => Mathf.Abs(v.x + .6f) < .0001f).OrderBy(v => v.z).ToArray();
            var south = profile.Where(v => Mathf.Abs(v.z + .6f) < .0001f).OrderBy(v => v.x).ToArray();
            check(west.Length == 2 && south.Length == 2 && Mathf.Abs(west[1].z - west[0].z - .5f) < .0001f && Mathf.Abs(south[1].x - south[0].x - .5f) < .0001f,
                "Both actual corner mesh joining edges match normal 50cm depth");
            cornerPose = e.Draft[corner];
            check(e.StartPurchase(0), "Start first connecting normal shelf");
            var normalA = e.PurchasePreview;
            var joinA = new ShopFurniturePose(cornerPose.Position + new Vector3(-1.2f, 0, .35f), 0);
            check(e.PlacePurchase(new ShopFurniturePose(joinA.Position + new Vector3(-.06f, 0, -.04f), 0)),
                "First normal shelf snaps beside corner with reachable product faces: " + e.Feedback);
            var normalB = e.PurchasePreview;
            var joinB = new ShopFurniturePose(cornerPose.Position + new Vector3(.35f, 0, -1.2f), 1);
            check(e.PlacePurchase(new ShopFurniturePose(joinB.Position + new Vector3(-.04f, 0, -.06f), 1)),
                "Perpendicular normal shelf snaps beside corner with reachable product faces: " + e.Feedback);
            check(Mathf.Abs(corner.Size.x - 1.2f) < .0001f && Mathf.Abs(corner.Size.y - 1.2f) < .0001f,
                "Corner fills exactly one normal shelf width along both walls");
            var normalRows = normalA.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Shelf ")).OrderBy(t => t.localPosition.y).ToArray();
            var cornerRows = corner.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Corner shelf board").OrderBy(t => t.localPosition.y).ToArray();
            check(normalRows.Length == 5 && normalRows.Zip(cornerRows, (a, b) => Mathf.Abs(a.localPosition.y - b.localPosition.y) < .0001f && Mathf.Abs(a.localScale.y - b.GetComponent<MeshFilter>().sharedMesh.bounds.size.y) < .0001f).All(v => v),
                "All five shelf levels and board thicknesses continue across joints");
            var placedA = e.Draft[normalA]; var placedB = e.Draft[normalB];
            check(Vector3.Distance(placedA.Position, joinA.Position) < .001f && Vector3.Distance(placedB.Position, joinB.Position) < .001f,
                "Wall and neighbour guides join both normal rows without a gap");
            for (int turns = 0; turns < 4; turns++)
            {
                var rotation = Quaternion.Euler(0, turns * 90, 0);
                Vector3 Transform(Vector3 point) => rotation * new Vector3(point.x, 0, point.z);
                var westBack = Transform(west[1]); var westFront = Transform(west[0]);
                var southBack = Transform(south[1]); var southFront = Transform(south[0]);
                check(Vector3.Distance(westBack, Transform(new Vector3(-.6f, 0, .6f))) < .0001f &&
                    Vector3.Distance(westFront, Transform(new Vector3(-.6f, 0, .1f))) < .0001f &&
                    Vector3.Distance(southBack, Transform(new Vector3(.6f, 0, -.6f))) < .0001f &&
                    Vector3.Distance(southFront, Transform(new Vector3(.1f, 0, -.6f))) < .0001f,
                    "Corner profile joins both 50cm normal shelf ends after rotation " + turns);
            }
            check(e.Save()&&g.Ledger.Cash==cash-18500&&g.Drawer.Total==g.Ledger.Cash,"Joined corner and two normals debit exactly 18500 once");
            var product=g.CreateCoffee(ShopCatalog.Index("water"));product.AssignInitialSlot(corner.Slots[1]);
            check(g.Navigation.TryPath(g.Layout.EntryInside,g.Layout.ShelfApproach(product),out var path),"Actual diagonal NPC pickup target is reachable");
            foreach (var shelf in new[] { normalA, normalB })
            {
                var endProduct = g.CreateCoffee(ShopCatalog.Index("water"));
                endProduct.AssignInitialSlot(shelf.Slots.Last());
                check(g.Navigation.TryPath(g.Layout.EntryInside, g.Layout.ShelfApproach(endProduct), out var endPath),
                    "Actual normal shelf end product remains reachable beside connected corner");
                endProduct.RemoveFromWorld();
            }
            var blocked = corner.Pose.Position + corner.Pose.Rotation * new Vector3(-.3f, 0, -.3f);
            check(!g.Navigation.TryPath(g.Layout.EntryInside, blocked, out var blockedPath),
                "Diagonal clearance still rejects an NPC centre too near the real shelf front");
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
            check(targets.Count(t=>t.Id.StartsWith("Module:Plan:"))==ShopModularFurniture.Prices.Length&&targets.All(t=>screen.Contains(t.Bounds.min)&&screen.Contains(t.Bounds.max)),"Every catalog entry fits monitor and keyboard navigation");
            return new{passed,failed=0};
        }
        finally{e.Cancel();g.ClosePanel();g.ResetDemo();g.ManualTick=false;body.enabled=false;player.transform.position=home;body.enabled=true;}
    }
}
