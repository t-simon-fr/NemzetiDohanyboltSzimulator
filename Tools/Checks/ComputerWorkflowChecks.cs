using System;
using System.Linq;
using UnityEngine;

public static class ComputerWorkflowChecks
{
    private static int checks;
    private static int Water => ShopCatalog.Index("water");
    private static void Require(bool value,string label){if(!value)throw new InvalidOperationException(label);checks++;}
    private static ShopWorldControl Control(WorldControlKind kind,int value=0)=>UnityEngine.Object.FindObjectsByType<ShopWorldControl>().First(c=>c.Kind==kind&&c.Value==value);
    private static void Click(WorldControlKind kind,int value=0)
    {
        var control=Control(kind,value);Physics.SyncTransforms();var screen=Camera.main.WorldToScreenPoint(control.transform.position);
        Require(screen.z>0&&screen.x>0&&screen.x<Camera.main.pixelWidth&&screen.y>0&&screen.y<Camera.main.pixelHeight,"Computer app target is visible: "+control.name);
        Require(Physics.Raycast(Camera.main.ScreenPointToRay(screen),out var hit,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<ShopWorldControl>()==control,"Actual mouse ray reaches app control: "+control.name);
        control.Activate(MvpShopController.Instance);
    }
    public static object Run()
    {
        checks=0;var game=MvpShopController.Instance;var player=UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();
        var movement=player.GetComponent<FirstPersonController>();var body=player.GetComponent<CharacterController>();
        var original=player.transform.position;var rotation=player.transform.rotation;movement.enabled=false;
        game.ManualTick=true;game.ResetDemo();game.ClosePanel();var devices=game.Devices;var area=game.Warehouse;
        try
        {
            Require(Enumerable.Range(0,ShopCatalog.Count).All(i=>(area.Dock(i).Find("Warehouse shelf 0")!=null&&area.Dock(i).GetComponentsInChildren<Transform>().Count(t=>t.name=="Rack upright")==4)||(i==3&&area.Dock(2).Find("Warehouse shelf 0")!=null)),"All brand docks have a storage rack or a shared upper shelf");
            Require(Enumerable.Range(0,ShopCatalog.Count).All(i=>!area.Dock(i).GetComponentsInChildren<Transform>().Any(t=>t.name=="Dock leg"||t.name=="Dock top")),"Old tables removed from current scene");
            foreach(string name in new[]{"Screen glass","Monitor stand","Monitor base"})
                Require(GameObject.Find(name).GetComponentInParent<ShopStation>()!=null,"Whole monitor resolves a use station: "+name);
            body.enabled=false;player.transform.position=devices.MonitorPosition+Vector3.forward*2.8f-Vector3.up*1.62f;body.enabled=true;
            game.OpenSupplier();Require(devices.FocusKind==ShopDeviceKind.Monitor&&devices.ComputerApp==ShopComputerApp.Desktop,"B opens desktop from a forgiving distance without precise aiming");
            Require(!UnityEngine.Object.FindObjectsByType<ShopWorldControl>().Any(c=>c.Kind==WorldControlKind.Order||c.Kind==WorldControlKind.Expand),"Desktop only exposes app shortcuts, no hidden purchase clicks");
            Click(WorldControlKind.OpenApp,(int)ShopComputerApp.Orders);
            Require(devices.ComputerApp==ShopComputerApp.Orders&&UnityEngine.Object.FindObjectsByType<ShopWorldControl>().Count(c=>c.Kind==WorldControlKind.Order)==game.AvailableProducts,"Order app shows only available products");
            foreach (int brand in Enumerable.Range(0,game.AvailableProducts))
            {
                var control = Control(WorldControlKind.Order,brand); var screen = Camera.main.WorldToScreenPoint(control.transform.position); Physics.SyncTransforms();
                Require(screen.z>0&&screen.x>0&&screen.x<Camera.main.pixelWidth&&screen.y>0&&screen.y<Camera.main.pixelHeight,"Every order button visible");
                Require(Physics.Raycast(Camera.main.ScreenPointToRay(screen),out var hit,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<ShopWorldControl>()==control,"Every order button reachable without overlap");
            }
            int cash=game.Ledger.Cash;Click(WorldControlKind.Order,0);Require(game.PendingQuantity(0)==3&&game.Ledger.Cash==cash-4500,"Ordering application places one paid shipment");
            Click(WorldControlKind.Desktop);Require(devices.ComputerApp==ShopComputerApp.Desktop,"Taskbar returns to desktop");
            Click(WorldControlKind.OpenApp,(int)ShopComputerApp.Furnishing);Require(devices.ComputerApp==ShopComputerApp.Furnishing,"Furnishing app opens independently");
            Require(!UnityEngine.Object.FindObjectsByType<ShopWorldControl>().Any(c=>c.Kind==WorldControlKind.Order),"Hidden order app cannot intercept furnishing clicks");
            var counter=game.Counter.transform.position;
            Click(WorldControlKind.ExtraShelf);Require(game.Layout.ExtraShelfOwned&&game.Ledger.Cash==300&&game.Drawer.Total==300,"Purchase nine-place cigarette shelf through furnishing app");
            Require(GameObject.Find("Extra Cigarette Shelf").GetComponentsInChildren<ProductSlot>().Length==9,"Purchased furniture has nine usable fixed stock cells");
            Require(game.Counter.transform.position==counter&&!game.BuyExtraShelf()&&game.Ledger.Cash==300,"Buying furniture keeps counter fixed and cannot charge twice");
            Click(WorldControlKind.Exit);Require(!devices.Focused&&!ShopInputGate.Blocked,"Computer exit restores movement");
            game.Tick(8.1f);area.Dock(0).GetComponent<ShopStation>().Use(player);var carton=player.HeldCarton;
            var shelf=GameObject.Find("Extra Cigarette Shelf").GetComponentsInChildren<ProductSlot>().First(s=>s.CanAcceptStock("cig_red"));
            Physics.SyncTransforms();player.RefreshTarget(new Ray(shelf.HintCenter-shelf.transform.forward*1.1f,shelf.transform.forward));
            Require(player.TargetSlot==shelf&&player.TryPlaceHeld()&&shelf.Occupant.ProductId=="cig_red"&&carton.Quantity==2,"New purchased furniture accepts individual stock from carried carton");
            game.OpenSupplier();Require(!devices.Focused&&player.HeldCarton==carton,"Carrying carton prevents opening computer and losing held goods");player.ReturnCarton();
            game.OpenShop();Require(!game.BuyExtraShelf(),"Furniture purchases blocked during active business");game.RequestClose();game.Tick(.1f);game.NextDay();
            Require(game.Day==2&&game.Layout.ExtraShelfOwned&&shelf.Occupant!=null&&game.WarehouseCount(0)==2,"Next day retains furniture and both stocked and stored units");
            game.ResetDemo();game.ClosePanel();Require(!game.Layout.ExtraShelfOwned&&!UnityEngine.Object.FindObjectsByType<ProductSlot>().Any(s=>s.name.StartsWith("Extra cigarette")),"New game hides unpurchased furniture and clears its goods");
            Require(game.TryExpand()&&game.OrderSupply(Water),"Prepare inexpensive water shipment");game.Tick(8.1f);area.Dock(Water).GetComponent<ShopStation>().Use(player);var held=player.HeldCarton;
            Require(held.Quantity==3&&area.Carton(Water)==null,"Filled carried carton releases its delivery dock");
            Require(game.OrderSupply(Water),"Reorder same brand while filled carton is carried");game.Tick(8.1f);var next=area.Carton(Water);
            Require(next!=null&&next!=held&&next.Quantity==3&&held.Quantity==3&&game.WarehouseCount(Water)==6,"Arrival gets separate rack carton and never refills carried case");
            var bin=UnityEngine.Object.FindAnyObjectByType<WarehouseBin>();Require(!bin.Use(player)&&player.HeldCarton==held&&game.WarehouseCount(Water)==6,"Full case cannot be discarded");
            player.ReturnCarton();Require(!player.HasHeldItem&&held.Quantity==3&&!held.IsDocked&&next.Quantity==3,"Returning to occupied rack retains both full cartons");
            Require(player.TryHoldCarton(held),"Loose full carton can be picked again");player.CancelHeld();
            game.ResetDemo();game.ClosePanel();
            body.enabled=false;player.transform.position=devices.MonitorPosition+Vector3.forward*8;body.enabled=true;game.OpenSupplier();Require(!devices.Focused,"Computer remains a physical device, unavailable across store");
            return new{passed=checks,failed=0};
        }
        finally
        {
            game.ResetDemo();game.ManualTick=false;body.enabled=false;player.transform.SetPositionAndRotation(original,rotation);body.enabled=true;movement.enabled=true;
        }
    }
}
