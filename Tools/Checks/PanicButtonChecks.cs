using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PanicButtonChecks
{
    private static int passed;
    private static void Check(bool valid,string label) { if(!valid)throw new InvalidOperationException(label);passed++; }
    // Fixture funding keeps the ledger and physical drawer equal without changing runtime starting funds.
    private static void Fund(MvpShopController game)
    {
        if(!game.Ledger.TrySell(2000,0,2000,0))throw new InvalidOperationException("Fixture funding failed");
        game.Drawer.Settle(2000,new int[ShopCatalog.Denominations.Length]);
    }
    private static void ClickPurchase(MvpShopController game)
    {
        var control=Object.FindObjectsByType<ShopWorldControl>().Single(c=>c.Kind==WorldControlKind.PanicButtonUpgrade);
        var camera=Camera.main;var point=camera.WorldToScreenPoint(control.transform.position);Physics.SyncTransforms();
        Check(point.z>0&&point.x>0&&point.x<camera.pixelWidth&&point.y>0&&point.y<camera.pixelHeight,"Upgrade is inside the monitor frame");
        Check(Physics.Raycast(camera.ScreenPointToRay(point),out var hit,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<ShopWorldControl>()==control,"Real mouse ray reaches panic upgrade");
        control.Activate(game);
    }
    public static object Run()
    {
        passed=0;var game=MvpShopController.Instance;var security=game.Security;var player=Object.FindAnyObjectByType<PlayerInteractor>();
        var camera=Camera.main;var cameraPosition=camera.transform.localPosition;var cameraRotation=camera.transform.localRotation;
        var movement=player.GetComponent<FirstPersonController>();bool movementEnabled=movement.enabled;movement.enabled=false;
        var counterPosition=game.Counter.transform.position;var counterRotation=game.Counter.transform.rotation;
        var actors=new System.Collections.Generic.List<GameObject>();
        game.ManualTick=true;
        try
        {
            game.ResetDemo();game.ClosePanel();
            Check(security!=null&&!security.Owned&&!security.Locked,"New game starts without security upgrade");
            Check(!security.Button.gameObject.activeSelf&&!security.EntranceCollider.gameObject.activeInHierarchy,"Unpurchased fixtures do not appear or block the door");
            Check(!security.Toggle()&&!security.Locked,"Unpurchased lock cannot be activated through the API");
            int opening=game.Ledger.Cash;
            Check(!game.BuyPanicButton()&&game.Ledger.Cash==opening&&game.Drawer.Total==opening&&game.Ledger.Improvements==0,"Insufficient funds never charge either balance");
            Fund(game);game.OpenShop();
            Check(!game.BuyPanicButton()&&game.Ledger.Cash==opening+2000,"Purchase rejected during open hours");
            game.RequestClose();game.Tick(.1f);Check(game.Phase==ShopPhase.Summary,"Empty shop closes normally");
            Check(!game.BuyPanicButton(),"Purchase rejected in day summary");
            game.ResetDemo();game.ClosePanel();Fund(game);
            game.Devices.Focus(ShopDeviceKind.Monitor);game.Devices.OpenApp(ShopComputerApp.Furnishing);
            Check(Object.FindObjectsByType<TextMesh>().Any(t=>t.text=="VÉSZGOMB • "+ShopCatalog.Money(ShopSecurity.PurchasePrice)),"Upgrade has a visible price");
            int before=game.Ledger.Cash;ClickPurchase(game);
            Check(security.Owned&&!security.Locked&&!game.Layout.Expanded,"Panic button can be purchased without expanding shop");
            Check(game.Ledger.Cash==before-ShopSecurity.PurchasePrice&&game.Drawer.Total==game.Ledger.Cash&&game.Ledger.Improvements==ShopSecurity.PurchasePrice,"Upgrade charged exactly once and booked as improvement");
            Check(security.Button.gameObject.activeInHierarchy&&!security.EntranceCollider.gameObject.activeInHierarchy,"Purchased button is present and entrance remains open");
            Check(Object.FindObjectsByType<TextMesh>().Any(t=>t.text=="VÉSZGOMB • MEGVÁSÁROLVA"),"Monitor acknowledges ownership");
            before=game.Ledger.Cash;ClickPurchase(game);
            Check(game.Ledger.Cash==before&&game.Drawer.Total==before,"Repeated purchase cannot charge again");
            Check(!security.Toggle()&&!security.Locked,"Lock cannot be engaged before opening");
            game.Devices.Release();game.OpenShop();
            var station=security.Button.GetComponent<ShopStation>();
            Vector3 eye=security.Button.position+security.Button.forward*.7f+Vector3.up*.45f;
            Physics.SyncTransforms();
            Check(Physics.Raycast(eye,(security.Button.position-eye).normalized,out var buttonHit,2.5f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&buttonHit.collider.GetComponentInParent<ShopStation>()==station,"Button is reachable from staff side at standing height");
            station.Use(player);
            Check(security.Locked&&security.EntranceCollider.gameObject.activeInHierarchy,"E interaction activates a physical shutter");
            Check(station.Prompt.Contains("feloldása"),"Interaction changes to unlock prompt");
            var entrance=security.Entrance;var inside=entrance.TransformPoint(new Vector3(0,0,1));var outside=entrance.TransformPoint(new Vector3(0,0,-1));
            Vector3 stopped=security.ConstrainMovement(inside,outside);
            Check(entrance.InverseTransformPoint(stopped).z>.25f,"Large outgoing step cannot tunnel through locked entrance");
            Check(entrance.InverseTransformPoint(security.ConstrainMovement(outside,inside)).z<-.25f,"Large incoming step is blocked too");
            var away=inside+entrance.forward;Check(security.ConstrainMovement(inside,away)==away,"Movement away from entrance remains free");
            var remoteA=inside+entrance.right*3;var remoteB=outside+entrance.right*3;
            Check(security.ConstrainMovement(remoteA,remoteB)==remoteB,"Lock does not freeze unrelated movement");
            Physics.SyncTransforms();
            Check(Physics.RaycastAll(inside+Vector3.up,-entrance.forward,2).Any(h=>h.collider==security.EntranceCollider),"Doorway has real blocking geometry");
            game.Tick(60);Check(game.Spawned==0&&game.Phase==ShopPhase.Open,"Locked entrance prevents new arrivals while business continues");
            var leavingObject=new GameObject("Security check departing customer");actors.Add(leavingObject);
            var leaving=leavingObject.AddComponent<MvpCustomerActor>();leaving.Initialize(0,0,0);leaving.transform.position=game.Layout.EntryInside;leaving.Leave();
            for(int i=0;i<15;i++)leaving.Advance(1,Vector3.zero);
            Check(!leaving.Gone&&entrance.InverseTransformPoint(leaving.transform.position).z>.25f,"Real departing NPC waits inside the shutter");
            var enteringObject=new GameObject("Security check arriving customer");actors.Add(enteringObject);
            var entering=enteringObject.AddComponent<MvpCustomerActor>();entering.Initialize(0,0,0);
            for(int i=0;i<15;i++)entering.Advance(1,game.Counter.ServicePoint);
            Check(!entering.Entered&&entrance.InverseTransformPoint(entering.transform.position).z<-.25f,"Real arriving NPC waits outside the shutter");
            station.Use(player);Check(!security.Locked,"Same physical button unlocks entrance");
            for(int i=0;i<15;i++){leaving.Advance(1,Vector3.zero);entering.Advance(1,game.Counter.ServicePoint);}
            Check(leaving.Gone&&entering.Entered,"Waiting NPCs resume and pass through after unlocking");
            Check(security.ConstrainMovement(inside,outside)==outside,"Unlock restores unrestricted traversal");
            var product=Object.FindObjectsByType<ShopProduct>().First(p=>!p.IsHeld);
            Check(player.TryHold(product),"Hold product before operating button");station.Use(player);
            Check(security.Locked&&player.HeldProduct==product,"Panic button works while holding stock without consuming it");player.CancelHeld();
            Fund(game);Check(game.OrderSupply(0,1),"Order a carton for held-item interaction check");game.Tick(8.1f);
            var carton=game.Warehouse.Carton(0);Check(player.TryHoldCarton(carton),"Hold carton before operating button");station.Use(player);
            Check(!security.Locked&&player.HeldCarton==carton&&carton.Quantity==1,"Panic button works while holding a filled carton");player.ReturnCarton();station.Use(player);
            var relative=game.Counter.transform.InverseTransformPoint(security.Button.position);
            game.Counter.transform.position+=Vector3.right*.15f;game.Counter.transform.rotation*=Quaternion.Euler(0,8,0);
            Check(Vector3.Distance(game.Counter.transform.InverseTransformPoint(security.Button.position),relative)<.0001f,"Button follows moved and rotated counter");
            game.Counter.transform.SetPositionAndRotation(counterPosition,counterRotation);
            game.RequestClose();Check(!security.Locked&&!security.EntranceCollider.gameObject.activeInHierarchy,"Closing automatically releases entrance");
            Check(!security.Toggle(),"Lock cannot trap customers during closing");
            game.Tick(.1f);game.NextDay();
            Check(game.Day==2&&security.Owned&&!security.Locked&&security.Button.gameObject.activeInHierarchy,"Next day retains upgrade with entrance open");
            game.ClosePanel();game.OpenShop();game.Tick(.01f);
            var outsideCustomer=Object.FindObjectsByType<MvpCustomerActor>().Single(a=>!actors.Contains(a.gameObject));
            Check(!outsideCustomer.Entered,"Real queued customer starts outside entrance");station.Use(player);game.Tick(200);
            Check(!outsideCustomer.Entered&&outsideCustomer.WaitTime==0,"A queued customer does not lose patience behind locked entrance");
            game.RequestClose();
            for(int i=0;i<100&&game.Phase!=ShopPhase.Summary;i++){game.Tick(1);if(game.ActiveCustomer!=null)game.SkipCurrent();}
            Check(game.Phase==ShopPhase.Summary&&!security.Locked,"Closing releases an actual waiting customer and reaches summary");
            game.ResetDemo();Check(!security.Owned&&!security.Locked&&!security.Button.gameObject.activeSelf,"New game removes purchased upgrade");
            return new {passed,failed=0};
        }
        finally
        {
            foreach(var actor in actors)Object.Destroy(actor);
            game.Counter.transform.SetPositionAndRotation(counterPosition,counterRotation);
            game.Devices.Release();player.CancelHeld();camera.transform.localPosition=cameraPosition;camera.transform.localRotation=cameraRotation;
            game.ResetDemo();movement.enabled=movementEnabled;game.ManualTick=false;
        }
    }
    public static object Capture(string mode)
    {
        var game=MvpShopController.Instance;var camera=Camera.main;var originalPosition=camera.transform.localPosition;var originalRotation=camera.transform.localRotation;
        var previous=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1500,1000,24);var image=new Texture2D(1500,1000,TextureFormat.RGB24,false);
        game.ManualTick=true;
        try
        {
            game.ResetDemo();game.ClosePanel();Fund(game);game.BuyPanicButton();camera.targetTexture=rt;
            if(mode=="shop"){game.Devices.Focus(ShopDeviceKind.Monitor);game.Devices.OpenApp(ShopComputerApp.Furnishing);}
            else if(mode=="button")
            {
                var button=game.Security.Button;camera.transform.position=button.position+button.forward*.62f+Vector3.up*.26f;camera.transform.LookAt(button.position+Vector3.up*.015f);
            }
            else
            {
                game.OpenShop();game.Security.Toggle();var entrance=game.Security.Entrance;
                camera.transform.position=entrance.TransformPoint(new Vector3(.1f,1.5f,2.3f));camera.transform.LookAt(entrance.TransformPoint(new Vector3(0,1.2f,0)));
            }
            Physics.SyncTransforms();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1500,1000),0,0);image.Apply();
            Directory.CreateDirectory("Builds/PanicButtonPreview");string path="Builds/PanicButtonPreview/"+mode+".png";File.WriteAllBytes(path,image.EncodeToPNG());return path;
        }
        finally
        {
            game.Devices.Release();camera.targetTexture=previous;RenderTexture.active=active;
            camera.transform.localPosition=originalPosition;camera.transform.localRotation=originalRotation;
            Object.Destroy(rt);Object.Destroy(image);game.ResetDemo();game.ManualTick=false;
        }
    }
}
