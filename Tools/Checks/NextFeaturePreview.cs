using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;
public static class NextFeaturePreview
{
    public static object Capture()
    {
        var g=MvpShopController.Instance;var camera=Camera.main;var player=Object.FindAnyObjectByType<PlayerInteractor>();var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
        var target=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1500,1000,24);var image=new Texture2D(1500,1000,TextureFormat.RGB24,false);var props=new GameObject("Preview cup slots");g.ManualTick=true;
        try
        {
            g.ResetDemo();g.ClosePanel();ShopCheckFixtures.Fund(g,150000);g.TryExpand();g.BuyExtraShelf();g.GetComponent<ShopNextUpgrades>().BuyFridge();g.Activities.BuyCoffeeMachine();g.Activities.OrderCoffeeSupplies();g.Activities.OrderMilkSupplies();g.Tick(8.1f);
            foreach(var pack in Object.FindObjectsByType<ShopIngredientPackage>()){player.TryHoldTool(pack.GetComponent<ShopHandTool>());g.Activities.UseCoffeeMachine();}
            for(int type=ShopCatalog.Coffee;type<=ShopCatalog.Latte;type++)
            {
                g.Activities.SelectRecipe(type);g.Activities.BrewCoffee();g.Tick(3.6f);var cup=g.Activities.CoffeeOutput.Occupant;
                var slot=new GameObject("Preview cup "+type).AddComponent<ProductSlot>();slot.transform.SetParent(props.transform,false);slot.transform.position=new Vector3(2.85f,1.18f,-1f+(type-ShopCatalog.Coffee)*.42f);slot.Configure(new Vector3(.3f,.4f,.3f));
                player.TryHold(cup);Physics.SyncTransforms();if(!cup.TryPlace(slot))throw new InvalidOperationException("Preview cup cannot be placed");player.CancelHeld();
            }
            foreach(int type in new[]{4,5,6,7,8}){g.OrderSupply(type,2);g.Tick(8.1f);for(int n=0;n<2;n++){g.TryCreateSupply(type,out var item);var slot=Object.FindObjectsByType<ProductSlot>().First(s=>s.name.StartsWith("Fridge slot")&&s.CanAcceptStock(item.ProductId));item.AssignInitialSlot(slot);}}
            Directory.CreateDirectory("Builds/NextFeatureRelease");camera.targetTexture=rt;
            camera.fieldOfView=55;camera.transform.position=new Vector3(7,3.5f,-10);camera.transform.LookAt(new Vector3(0,1.4f,-4.4f));Save(camera,rt,image,"exterior");
            camera.transform.position=new Vector3(5.3f,2.8f,-3.8f);camera.transform.LookAt(new Vector3(2.15f,1.1f,-.25f));Save(camera,rt,image,"l-counter");
            camera.transform.position=new Vector3(4.5f,1.3f,-1.1f);camera.transform.LookAt(new Vector3(4.5f,1,1.35f));Save(camera,rt,image,"fridge");
            camera.fieldOfView=40;camera.transform.position=new Vector3(1.55f,1.9f,-1.5f);camera.transform.LookAt(new Vector3(2.85f,1.4f,-.05f));Save(camera,rt,image,"coffee-recipes");
            g.OpenShop();g.DebugSpawnVisit(false);g.DebugSpawnVisit(false);var person=Object.FindObjectsByType<MvpCustomerActor>().First();camera.fieldOfView=35;camera.transform.position=person.transform.position+new Vector3(.8f,1.65f,2);camera.transform.LookAt(person.transform.position+Vector3.up*1.3f);Save(camera,rt,image,"character-detail");
            return "Builds/NextFeatureRelease: exterior, l-counter, fridge, coffee-recipes, character-detail";
        }
        finally{camera.targetTexture=target;RenderTexture.active=active;camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;player.CancelHeld();g.ResetDemo();g.ManualTick=false;Object.Destroy(props);Object.Destroy(rt);Object.Destroy(image);}
    }
    static void Save(Camera camera,RenderTexture rt,Texture2D image,string name){camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1500,1000),0,0);image.Apply();File.WriteAllBytes("Builds/NextFeatureRelease/"+name+".png",image.EncodeToPNG());}
}
