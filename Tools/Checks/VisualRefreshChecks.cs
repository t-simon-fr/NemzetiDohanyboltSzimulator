using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

public static class VisualRefreshChecks
{
    public static object Prices()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var g=MvpShopController.Instance;var p=g.Pricing;
        try
        {
            g.ResetDemo();g.ClosePanel();g.ManualTick=true;
            int cash=g.Ledger.Cash,stock=g.StockCount(0);
            p.SetDraft("2400");check(p.Price(0)==2200,"Typing is a draft, not a price mutation");
            check(p.CommitEdit()&&p.Price(0)==2400,"Valid typed price is committed");
            check(g.Ledger.Cash==cash&&g.Drawer.Total==cash&&g.StockCount(0)==stock,"Price editing changes neither stock nor cash");
            foreach(string bad in new[]{"","abc","2301","999999999999","-5","0","6605"})
            {
                p.SetDraft(bad);check(!p.CommitEdit()&&p.Price(0)==2400&&p.Error.Length>0,"Invalid draft cannot mutate price: "+bad);
                check(p.CancelEdit()&&p.Draft=="2400","Escape restores current price after invalid draft");
            }
            p.SetDraft("2500");check(p.HandleEditorKey(Key.Escape,false)&&p.Price(0)==2400&&!p.Editing,"Escape cancels valid uncommitted draft");
            p.StepEditor(1,false);check(p.Price(0)==2500,"Plus uses 100 Ft");
            p.StepEditor(-1,true);check(p.Price(0)==2495,"Shift minus uses 5 Ft");
            p.SetDraft("2600");p.HandleEditorKey(Key.Tab,false);check(p.Price(0)==2600&&!p.Editing,"Leaving field with Tab commits once");
            p.ChooseProduct(1);check(p.SelectedType==1&&p.Draft=="2300","Selecting another product shows its own price");
            p.ChooseCategory(ShopPriceCategory.Alcohol);check(p.VisibleTypes.Length==0,"Locked stock has no phantom category products");
            int unchanged=p.Price(p.SelectedType);p.StepEditor(1,false);
            check(p.Price(p.SelectedType)==unchanged,"Empty category cannot change a hidden product through shortcuts");
            g.Devices.Focus(ShopDeviceKind.Monitor);g.Devices.OpenApp(ShopComputerApp.Prices);p.ChooseCategory(ShopPriceCategory.Alcohol);
            check(!g.Devices.KeyboardTargets().Any(t=>t.Id=="Price:Edit"||t.Id=="Price:Plus"),"Empty category exposes no hidden editor targets");
            g.ClosePanel();
            ShopCheckFixtures.Fund(g,100000);g.TryExpand();g.Activities.BuyCoffeeMachine();
            foreach(ShopPriceCategory category in Enum.GetValues(typeof(ShopPriceCategory)))
            {
                p.ChooseCategory(category);
                check(p.VisibleTypes.Length>0&&p.VisibleTypes.All(t=>ShopPricing.CategoryOf(t)==category)&&p.VisibleTypes.Contains(p.SelectedType),"Available category isolates its own products: "+category);
            }
            p.ChooseCategory(ShopPriceCategory.Coffee);p.SetDraft("700");check(p.CommitEdit()&&p.Price(ShopCatalog.Coffee)==700,"Coffee price editable without treating it as packaged stock");
            check(ShopPricing.CategoryOf(ShopCatalog.Contraband)==ShopPriceCategory.Tobacco,"Contraband stays in tobacco category");
            g.ResetDemo();check(!p.Editing&&p.Category==ShopPriceCategory.Tobacco&&p.Price(0)==2200,"New game clears draft and category");
            return new{passed,failed=0};
        }
        finally{g.ClosePanel();g.ResetDemo();g.ManualTick=false;}
    }

    public static object Computer()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var g=MvpShopController.Instance;
        try
        {
            g.ResetDemo();g.ClosePanel();g.ManualTick=true;ShopCheckFixtures.Fund(g,100000);g.TryExpand();g.Activities.BuyCoffeeMachine();
            g.Devices.Focus(ShopDeviceKind.Monitor);
            var screen=g.Devices.ComputerScreenRect;
            for(int app=0;app<=6;app++)
            {
                g.Devices.OpenApp((ShopComputerApp)app);
                var targets=g.Devices.KeyboardTargets();
                check(targets.Count>=2&&targets.All(t=>screen.Contains(t.Bounds.min)&&screen.Contains(t.Bounds.max)),"App controls fit projected physical screen: "+app);
            }
            g.Devices.OpenApp(ShopComputerApp.Desktop);
            var order=g.Devices.KeyboardTargets().Single(t=>t.Id=="OpenApp:1");order.Activate();
            check(g.Devices.ComputerApp==ShopComputerApp.Orders,"Desktop action opens actual orders");
            g.Devices.SetCartQuantity(0,1);int before=g.Ledger.Cash;
            g.Devices.KeyboardTargets().Single(t=>t.Id=="Cart:Submit").Activate();
            check(g.Ledger.Cash==before-1500&&g.PendingQuantity(0)==1,"Computer cart action buys actual shipment exactly once");
            g.Devices.OpenApp(ShopComputerApp.Coffee);before=g.Ledger.Cash;
            g.Devices.KeyboardTargets().Single(t=>t.Id=="Coffee:Milk").Activate();
            check(g.Ledger.Cash==before-1500&&g.Drawer.Total==g.Ledger.Cash,"Computer coffee supply action debits the shared till");
            g.Devices.OpenApp(ShopComputerApp.Games);g.Devices.KeyboardTargets().Single(t=>t.Id=="Game:Memory").Activate();
            check(g.Devices.KeyboardTargets().Count(t=>t.Id.StartsWith("Memory:"))==16,"Memory game exposes sixteen accessible cards");
            g.Devices.KeyboardTargets().Single(t=>t.Id=="Game:Menu").Activate();
            check(g.Devices.KeyboardTargets().Any(t=>t.Id=="Game:Snake"),"Minigame back restores game launcher");
            return new{passed,failed=0};
        }
        finally{g.ClosePanel();g.ResetDemo();g.ManualTick=false;}
    }

    public static object Identity()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var g=MvpShopController.Instance;var identity=g.GetComponent<ShopTrafikIdentity>();
        check(identity!=null&&identity.Facade!=null,"Runtime identity assembled without scene regeneration");
        check(identity.Facade.parent.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),"Decorations add no blocking entrance or shelf colliders");
        var panels = identity.Facade.parent.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Trafik left wainscot ")).ToArray();
        var frontWall = g.FloorPlan.Find("Shop side wall front").GetComponent<Renderer>().bounds;
        var rearWall = g.FloorPlan.Find("Shop side wall rear").GetComponent<Renderer>().bounds;
        check(panels.Length == 2, "Both solid side-wall sections retain their lower trim");
        check(panels.All(p => p.bounds.max.z <= frontWall.max.z + .001f || p.bounds.min.z >= rearWall.min.z - .001f),
            "No decorative panel spans the stockroom doorway");
        var decorations = identity.Facade.parent.GetComponentsInChildren<Renderer>();
        check(!decorations.Any(r => r.name == "Trafik lighter case glass"), "Lighter display has no glass cover");
        check(!decorations.Any(r => r.name == "Trafik display lighter" || r.name == "Trafik lighter case base"),
            "Counter has neither decorative lighters nor their display base");
        check(g.FloorPlan.Find("Counter top").GetComponent<Renderer>().sharedMaterial.name=="Trafik countertop","Counter has owned muted surface material");
        check(g.FloorPlan.Find("Exterior shop sign")==null||!g.FloorPlan.Find("Exterior shop sign").GetComponent<Renderer>().enabled,"Old competing exterior sign hidden");
        check(identity.Facade.GetComponentsInChildren<TextMesh>().Any(t=>t.text=="NEMZETI DOHÁNYBOLT"),"Facade title is actual Hungarian sign text");
        check(identity.Facade.parent.GetComponentsInChildren<MeshFilter>().Any(m=>m.sharedMesh!=null&&m.sharedMesh.name=="Trafik pitched roof"),"Stylized roof is actual runtime mesh");
        var roof=identity.Facade.parent.GetComponentsInChildren<MeshFilter>().First(m=>m.sharedMesh.name=="Trafik pitched roof");
        g.Layout.SetExpanded(false);check(Mathf.Abs(roof.sharedMesh.bounds.max.x-2.75f)<.01f,"Starter roof ends at starter wall");
        g.Layout.SetExpanded(true);check(Mathf.Abs(roof.sharedMesh.bounds.max.x-6.75f)<.01f,"Expanded roof follows purchased room");
        g.Layout.SetExpanded(false);
        var cigarette=g.CreateCoffee(0); check(g.Entrance.PlayerBoundary.isTrigger&&g.DeliverySlot.CanPlaceManually(cigarette),"Entrance and cigarette scanner policies preserved"); cigarette.RemoveFromWorld();
        return new{passed,failed=0};
    }
}
