using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

public static class FeatureReleasePreview
{
    public static object Setup(string mode)
    {
        var game=MvpShopController.Instance;game.ManualTick=true;ShopCheckFixtures.Reset(game);game.ClosePanel();
        var movement=Object.FindAnyObjectByType<FirstPersonController>();movement.enabled=false;
        ShopCheckFixtures.Fund(game,80000);game.TryExpand();game.Activities.BuyCoffeeMachine();game.BuyPanicButton();game.BuyExtraShelf();
        for(int type=4;type<ShopCatalog.Count;type++)
        {
            game.OrderSupply(type);ShopCheckFixtures.Tick(game,8.1f);
            for(int unit=0;unit<3;unit++)
            {
                game.TryCreateSupply(type,out var product);var slot=Object.FindObjectsByType<ProductSlot>().First(s=>s.PreferredProductId==ShopCatalog.Ids[type]&&s.CanAcceptStock(ShopCatalog.Ids[type]));product.AssignInitialSlot(slot);
            }
        }
        game.Activities.OrderCoffeeSupplies();ShopCheckFixtures.Tick(game,8.1f);game.Activities.BrewCoffee();ShopCheckFixtures.Tick(game,3.6f);
        var camera=Camera.main;
        if(mode=="machine"){camera.transform.position=new Vector3(1.38f,1.60f,-.15f);camera.transform.LookAt(game.Activities.CoffeeOutput.transform.position+Vector3.up*.24f);}
        else if(mode=="store"){camera.transform.position=new Vector3(-1.7f,1.65f,.9f);camera.transform.LookAt(new Vector3(3.2f,1.2f,-1.5f));}
        else if(mode=="cleaning")
        {
            var spill=game.Activities.SpawnSpill();var player=movement.GetComponent<PlayerInteractor>();player.TryHoldTool(game.Activities.Mop);
            camera.transform.position=spill.transform.position+new Vector3(0,1.5f,1.1f);camera.transform.LookAt(spill.transform.position);
        }
        else if(mode=="coins")
        {
            var hand=new GameObject("Preview service hand").transform;hand.position=Vector3.one*1000;game.OpenShop();
            for(int tick=0;tick<3000;tick++)
            {
                ShopCheckFixtures.Tick(game,.1f);var active=game.ActiveCustomer;if(active==null||game.Stage==ServiceStage.Thanks)continue;
                if(game.Stage==ServiceStage.Order&&active.Product<4)
                {var product=Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId==ShopCatalog.Ids[active.Product]&&!p.IsHeld);product.PickUp(hand);Physics.SyncTransforms();product.TryPlace(game.DeliverySlot);}
                if(game.Stage!=ServiceStage.Payment)continue;
                if(game.CoinPayment){game.OpenCash();break;}
                int due=active.Tender-ShopCatalog.Prices[active.Product];
                for(int attempt=0;attempt<100;attempt++)
                {
                    game.ClearChange();int rest=due;
                    foreach(int value in ShopCatalog.Denominations){int count=Math.Min(rest/value,game.Drawer.Count(value));for(int i=0;i<count;i++)game.AddChange(value);rest-=count*value;}
                    if(rest==0)break;game.ExchangeCash();
                }
                game.SubmitChange(game.ChangeSelected);
            }
            Object.Destroy(hand.gameObject);
            if(game.Devices.FocusKind!=ShopDeviceKind.Coins)throw new InvalidOperationException("Coin preview failed to reach a real customer payment.");
        }
        else
        {
            game.Devices.Focus(ShopDeviceKind.Monitor);
            var app=mode=="desktop"?ShopComputerApp.Desktop:mode=="coffee"?ShopComputerApp.Coffee:mode=="cameras"?ShopComputerApp.Cameras:mode=="furnishing"?ShopComputerApp.Furnishing:ShopComputerApp.Games;
            game.Devices.OpenApp(app);
            if(mode=="cameras")game.Activities.RenderCamera(0);
            if(mode=="snake")game.Activities.StartSnake();
            if(mode=="memory")game.Activities.StartMemory();
        }
        var windowType=Type.GetType("UnityEditor.GameView,UnityEditor");if(windowType!=null)UnityEditor.EditorWindow.GetWindow(windowType).Show();
        Physics.SyncTransforms();Directory.CreateDirectory("Builds/FeatureRelease/Preview");return mode;
    }
    public static object Capture(string mode)
    {string path="Builds/FeatureRelease/Preview/"+mode+".png";ScreenCapture.CaptureScreenshot(path,2);return path;}
    public static object Restore()
    {
        var game=MvpShopController.Instance;game.Devices.Release();Object.FindAnyObjectByType<PlayerInteractor>().CancelHeld();
        Camera.main.transform.localPosition=new Vector3(0,1.62f,0);Camera.main.transform.localRotation=Quaternion.identity;
        ShopCheckFixtures.Reset(game);game.ManualTick=false;Object.FindAnyObjectByType<FirstPersonController>().enabled=true;return true;
    }
}
