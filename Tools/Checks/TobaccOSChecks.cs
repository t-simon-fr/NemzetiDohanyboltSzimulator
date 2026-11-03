using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class TobaccOSChecks
{
    static int passed;
    static void Check(bool valid,string label){if(!valid)throw new InvalidOperationException(label);passed++;}
    static TextMesh[] MonitorText()=>GameObject.Find("Order screen").GetComponentsInChildren<TextMesh>();
    static ShopWorldControl[] ActiveOrders()=>UnityEngine.Object.FindObjectsByType<ShopWorldControl>().Where(c=>c.Kind==WorldControlKind.Order).ToArray();
    static void Click(WorldControlKind kind,int value=0)
    {
        var control=UnityEngine.Object.FindObjectsByType<ShopWorldControl>().First(c=>c.Kind==kind&&c.Value==value);Physics.SyncTransforms();
        var camera=Camera.main;var screen=camera.WorldToScreenPoint(control.transform.position);
        Check(screen.z>0&&screen.x>0&&screen.x<camera.pixelWidth&&screen.y>0&&screen.y<camera.pixelHeight,"Visible TobaccOS control: "+kind+" / "+value);
        Check(Physics.Raycast(camera.ScreenPointToRay(screen),out var hit,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<ShopWorldControl>()==control,"Unobstructed TobaccOS control: "+kind+" / "+value);
        if(kind==WorldControlKind.Expand&&!MvpShopController.Instance.Layout.Expanded)ShopCheckFixtures.Fund(MvpShopController.Instance,ShopLayout.ExpansionPrice-2000);
        control.Activate(MvpShopController.Instance);
    }
    public static object Run()
    {
        var game=MvpShopController.Instance;var devices=game.Devices;game.ManualTick=true;passed=0;
        try
        {
            ShopCheckFixtures.Reset(game);game.ClosePanel();devices.Focus(ShopDeviceKind.Monitor);
            Check(devices.ComputerApp==ShopComputerApp.Desktop,"Boot to desktop");
            Check(MonitorText().Any(t=>t.text=="TobaccOS"),"Exact operating-system name");
            Check(ActiveOrders().Length==0,"Desktop contains no hidden order click targets");
            Click(WorldControlKind.OpenApp,(int)ShopComputerApp.Orders);
            Check(ActiveOrders().Select(c=>c.Value).OrderBy(i=>i).SequenceEqual(new[]{0,1,2,3}),"Only cigarette products before expansion");
            foreach(int type in Enumerable.Range(4,3))
                Check(!MonitorText().Any(t=>t.text.Contains(ShopCatalog.ShortNames[type])),"Locked product text is entirely absent: "+type);
            Check(!MonitorText().Any(t=>t.text.Contains("BŐVÍTÉS UTÁN")),"No locked product placeholder rows");
            Check(devices.OrderQuantity==3,"Initial quantity preserves the original three-unit flow");
            Click(WorldControlKind.ChangeOrderQuantity,-1);Check(devices.OrderQuantity==2,"Minus selects two units");
            Check(MonitorText().Any(t=>t.text=="2 db • "+ShopCatalog.Money(3000)),"Price follows selected quantity");
            Click(WorldControlKind.ChangeOrderQuantity,1);Check(devices.OrderQuantity==3,"Plus increases quantity");
            devices.ChangeOrderQuantity(-100);Check(devices.OrderQuantity==1,"Quantity lower bound");
            devices.ChangeOrderQuantity(100);Check(devices.OrderQuantity==12,"Quantity upper bound");
            int cash=game.Ledger.Cash;
            Check(!devices.OrderProduct(0)&&game.Ledger.Cash==cash&&game.PendingQuantity(0)==0,"Unaffordable order does not charge or ship");
            foreach(int quantity in new[]{0,-1,13,int.MaxValue})Check(!game.OrderSupply(0,quantity)&&game.Ledger.Cash==cash,"Invalid quantity rejected without charging");
            Check(!game.OrderSupply(4,1)&&!game.OrderSupply(-1,1)&&!game.OrderSupply(ShopCatalog.Count,1),"Backend enforces locked and invalid product rules");
            devices.ChangeOrderQuantity(-10);Check(devices.OrderQuantity==2,"Return to two units");
            Click(WorldControlKind.Order,0);
            Check(game.PendingQuantity(0)==2&&game.Ledger.Cash==3000&&game.Drawer.Total==3000,"Exact two-unit wholesale purchase");
            ShopCheckFixtures.Tick(game,7.9f);Check(game.WarehouseCount(0)==0&&game.PendingQuantity(0)==2,"No premature shipment");
            ShopCheckFixtures.Tick(game,.2f);devices.RefreshMonitor();Check(game.WarehouseCount(0)==2&&game.Warehouse.Carton(0).Quantity==2,"Selected quantity arrives in the correct carton");
            Check(MonitorText().Any(t=>t.text.StartsWith("Megérkezett:")),"Arrival feedback appears on monitor");
            Click(WorldControlKind.Desktop);Click(WorldControlKind.OpenApp,(int)ShopComputerApp.Furnishing);
            Check(ActiveOrders().Length==0,"Orders cannot intercept development clicks");
            Click(WorldControlKind.Expand);
            Click(WorldControlKind.Desktop);Click(WorldControlKind.OpenApp,(int)ShopComputerApp.Orders);
            Check(ActiveOrders().Length==ShopCatalog.Count,"Drinks appear after purchased expansion");
            foreach(var control in ActiveOrders())
            {
                var camera=Camera.main;var pixel=camera.WorldToScreenPoint(control.transform.position);Physics.SyncTransforms();
                Check(pixel.z>0&&pixel.x>0&&pixel.x<camera.pixelWidth&&pixel.y>0&&pixel.y<camera.pixelHeight,"Expanded catalog fits monitor");
                Check(Physics.Raycast(camera.ScreenPointToRay(pixel),out var hit,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<ShopWorldControl>()==control,"Expanded catalog has no overlapping controls");
            }
            Click(WorldControlKind.Order,4);Check(game.PendingQuantity(4)==2&&game.Ledger.Cash==700,"Unlocked water order uses chosen quantity");
            foreach(float aspect in new[]{4f/3f,16f/9f})
            {
                Camera.main.aspect=aspect;devices.Release();devices.Focus(ShopDeviceKind.Monitor);devices.OpenApp(ShopComputerApp.Orders);
                foreach(var control in ActiveOrders()){var pixel=Camera.main.WorldToScreenPoint(control.transform.position);Check(pixel.x>0&&pixel.x<Camera.main.pixelWidth&&pixel.y>0&&pixel.y<Camera.main.pixelHeight,"Monitor framing fits aspect "+aspect);}
            }
            Camera.main.ResetAspect();
            ShopCheckFixtures.Reset(game);game.ClosePanel();Check(ShopCheckFixtures.Expand(game),"Expand for warehouse capacity check");
            Check(game.OrderSupply(4,12)&&game.OrderSupply(4,12),"Two maximum-quantity shipments accepted");
            Check(game.PendingQuantity(4)==24&&game.Ledger.Cash==400,"Pending stock and wholesale total agree");
            Check(!game.OrderSupply(4,1)&&game.PendingQuantity(4)==24&&game.Ledger.Cash==400,"Combined stock limit rejects extra units without charging");
            ShopCheckFixtures.Tick(game,8.1f);Check(game.WarehouseCount(4)==24&&game.Warehouse.Carton(4).Quantity==24,"Multiple shipments preserve selected quantities");
            ShopCheckFixtures.Reset(game);game.ClosePanel();devices.Focus(ShopDeviceKind.Monitor);devices.OpenApp(ShopComputerApp.Orders);
            Check(devices.OrderQuantity==3&&ActiveOrders().Length==4,"New game restores quantity and hides drinks again");
            Check(!MonitorText().Any(t=>t.text.Contains("ReturAqua")||t.text.Contains("Loca Cola")||t.text.Contains("Hamster Energy")),"No unlocked catalog text leaks after reset");
            return new{passed,failed=0};
        }
        finally{Camera.main.ResetAspect();devices.Release();ShopCheckFixtures.Reset(game);game.ManualTick=false;}
    }
    public static object Capture(string mode)
    {
        var game=MvpShopController.Instance;var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1500,1000,24);var image=new Texture2D(1500,1000,TextureFormat.RGB24,false);game.ManualTick=true;
        try
        {
            ShopCheckFixtures.Reset(game);game.ClosePanel();if(mode=="expanded")ShopCheckFixtures.Expand(game);camera.targetTexture=rt;
            game.Devices.Focus(ShopDeviceKind.Monitor);if(mode!="desktop")game.Devices.OpenApp(ShopComputerApp.Orders);
            camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1500,1000),0,0);image.Apply();Directory.CreateDirectory("Builds/TobaccOSPreview");
            File.WriteAllBytes("Builds/TobaccOSPreview/"+mode+".png",image.EncodeToPNG());return "Builds/TobaccOSPreview/"+mode+".png";
        }
        finally{game.Devices.Release();camera.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);ShopCheckFixtures.Reset(game);game.ManualTick=false;}
    }
}
