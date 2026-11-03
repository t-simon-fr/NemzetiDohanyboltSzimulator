using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class DrinkPackagingChecks
{
    public static object Run()
    {
        var game=MvpShopController.Instance; if(game==null||!Application.isPlaying)throw new InvalidOperationException("Enter Play mode first.");
        int passed=0; Action<bool,string> check=(valid,name)=>{if(!valid)throw new InvalidOperationException(name);passed++;};
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();
        var hand=new GameObject("Drink check hand").transform;hand.position=Vector3.one*1000;game.ManualTick=true;
        try
        {
            check(ShopCatalog.Ids.Skip(4).Take(ShopCatalog.Count-4).SequenceEqual(new[]{"water","cola","energy","kevert","beer"}),"Approved drink IDs");
            check(ShopCatalog.Names[6]=="Hamster Energy 500 ml","Half-litre energy drink in catalog");
            check(ProductVisuals.Size(6).y>ProductVisuals.Size(5).y&&ProductVisuals.Size(6).x>ProductVisuals.Size(5).x,"500 ml can is taller and wider than 330 ml cola");
            for(int type=4;type<ShopCatalog.Count;type++)
            {
                game.ResetDemo();game.ClosePanel();check(!game.OrderSupply(type),"Drink locked until shop expansion");
                check(ShopCheckFixtures.Expand(game),"Unlock public drink shelves");
                check(game.OrderSupply(type),"Drink can be purchased");game.Tick(8.1f);
                var carton=game.Warehouse.Carton(type);
                check(carton!=null&&carton.Quantity==3,"Delivery has a brand-specific carton");
                check(game.Warehouse.Dock(type).GetComponent<ShopStation>().ProductType==type,"Correct warehouse dock");
                check(carton.GetComponentsInChildren<MeshRenderer>().Any(r=>r.sharedMaterial.mainTexture!=null&&(r.sharedMaterial.mainTexture.name=="DrinkLabels"||r.sharedMaterial.mainTexture.name=="KevertLabel"||r.sharedMaterial.mainTexture.name=="KofejtoiLabel")),"Carton samples use real drink models");
                check(player.TryHoldCarton(carton),"Whole drink carton can be carried");
                var slot=UnityEngine.Object.FindObjectsByType<ProductSlot>().First(s=>s.name.StartsWith("Public slot")&&s.PreferredProductId==ShopCatalog.Ids[type]&&s.CanAcceptStock(ShopCatalog.Ids[type]));
                Physics.SyncTransforms();check(carton.TryPlaceOne(slot),"Place one bottle/can on its public shelf");
                var product=slot.Occupant;
                check(product.DisplayName==ShopCatalog.Names[type]&&!product.GetComponent<MeshRenderer>().enabled,"Correct name and no blockout renderer");
                check(product.GetComponentsInChildren<Collider>().Length==1,"Only interaction root has a collider");
                check(product.transform.localScale==ProductVisuals.Size(type),"Physical size matches carton fitting calculation");
                check(game.StockCount(type)==1&&game.WarehouseCount(type)==2&&carton.Quantity==2,"Single unit stocking conserves inventory");
                player.ReturnCarton();check(product.PickUp(hand),"Textured drink can be held individually");
                check(product.GetComponentsInChildren<MeshRenderer>().Any(r=>r.enabled&&r.sharedMaterial.mainTexture!=null),"Label remains visible in hand");
                Physics.SyncTransforms();check(product.TryPlace(slot),"Drink can be placed back on its shelf");
                check(player.TryHold(product)&&game.ReturnSupply(player,type),"Drink can return to its own carton");
                check(game.StockCount(type)==0&&game.WarehouseCount(type)==3&&carton.Quantity==3,"Return conserves the paid shipment");
            }
            return new{passed,failed=0,drinks=ShopCatalog.Count-4,energyVolumeMl=500};
        }
        finally{player.CancelHeld();game.ResetDemo();game.ManualTick=false;UnityEngine.Object.Destroy(hand.gameObject);}
    }
    public static object CaptureShelf()
    {
        var game=MvpShopController.Instance;var player=UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();
        var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
        var active=RenderTexture.active;var target=camera.targetTexture;var rt=new RenderTexture(1500,1000,24);var image=new Texture2D(1500,1000,TextureFormat.RGB24,false);
        game.ManualTick=true;
        try
        {
            game.ResetDemo();game.ClosePanel();ShopCheckFixtures.Expand(game);
            for(int type=4;type<ShopCatalog.Count;type++)
            {
                if(!game.OrderSupply(type))throw new Exception("Cannot order shelf preview stock");game.Tick(8.1f);
                var carton=game.Warehouse.Carton(type);player.TryHoldCarton(carton);
                for(int i=0;i<3;i++)
                {
                    var slot=UnityEngine.Object.FindObjectsByType<ProductSlot>().First(s=>s.name.StartsWith("Public slot")&&s.PreferredProductId==ShopCatalog.Ids[type]&&s.CanAcceptStock(ShopCatalog.Ids[type]));Physics.SyncTransforms();
                    if(!carton.TryPlaceOne(slot))throw new Exception("Cannot stock drink preview");
                }
                player.ReturnCarton();
            }
            foreach(var label in UnityEngine.Object.FindObjectsByType<ShopShelfPriceLabel>())label.Refresh();
            var shelf=GameObject.Find("Public shelf").transform;
            camera.transform.position=shelf.TransformPoint(new Vector3(-4.0f,1.65f,-.15f));camera.transform.LookAt(shelf.TransformPoint(new Vector3(0,1.35f,0)));camera.fieldOfView=48;
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1500,1000),0,0);image.Apply();
            Directory.CreateDirectory("Builds/DrinkPreview");File.WriteAllBytes("Builds/DrinkPreview/shop-drinks.png",image.EncodeToPNG());return "Builds/DrinkPreview/shop-drinks.png";
        }
        finally{camera.targetTexture=target;RenderTexture.active=active;camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);player.CancelHeld();game.ResetDemo();game.ManualTick=false;}
    }
}
