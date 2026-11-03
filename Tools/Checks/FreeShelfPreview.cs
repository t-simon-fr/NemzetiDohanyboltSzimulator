using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

public static class FreeShelfPreview
{
    public static object Capture()
    {
        var game=MvpShopController.Instance;var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
        var target=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1500,1000,24);var image=new Texture2D(1500,1000,TextureFormat.RGB24,false);game.ManualTick=true;
        try
        {
            game.ResetDemo();game.ClosePanel();ShopCheckFixtures.Fund(game,100000);game.TryExpand();game.BuyExtraShelf();
            foreach(var item in Object.FindObjectsByType<ShopProduct>())item.RemoveFromWorld();
            foreach(int type in new[]{0,4,5,6,7,8})game.OrderSupply(type,4);game.Tick(8.1f);
            for(int row=0;row<5;row++)for(int col=0;col<6;col++)
            {
                int type=new[]{0,8,7,4,6,5}[col];if(row>2)continue;
                var slot=Object.FindObjectsByType<ProductSlot>().First(s=>s.name=="Public slot "+row+"-"+col);
                game.TryCreateSupply(type,out var item);item.AssignInitialSlot(slot);
            }
            for(int col=0;col<6;col++)
            {
                int type=new[]{0,8,7,4,6,5}[col];game.TryCreateSupply(type,out var item);
                item.AssignInitialSlot(Object.FindObjectsByType<ProductSlot>().First(s=>s.name=="Cigarette slot 0-"+col));
            }
            foreach(var label in Object.FindObjectsByType<ShopShelfPriceLabel>())label.Refresh();
            Directory.CreateDirectory("Builds/KeyboardAlcoholRelease");camera.targetTexture=rt;
            var shelf=GameObject.Find("Public shelf").transform;
            camera.transform.position=shelf.TransformPoint(new Vector3(-4,1.65f,-.15f));camera.transform.LookAt(shelf.TransformPoint(new Vector3(0,1.35f,0)));camera.fieldOfView=48;
            Save(camera,rt,image,"mixed-public-shelf");
            var staff=GameObject.Find("Cigarette rack").transform;
            camera.transform.position=staff.TransformPoint(new Vector3(0,1.4f,-2.0f));camera.transform.LookAt(staff.TransformPoint(new Vector3(0,1.28f,0)));camera.fieldOfView=65;
            Save(camera,rt,image,"mixed-staff-shelf");return new {publicShelf="Builds/KeyboardAlcoholRelease/mixed-public-shelf.png",staffShelf="Builds/KeyboardAlcoholRelease/mixed-staff-shelf.png"};
        }
        finally{camera.targetTexture=target;RenderTexture.active=active;camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;Object.Destroy(rt);Object.Destroy(image);game.ResetDemo();game.ManualTick=false;}
    }
    static void Save(Camera camera,RenderTexture rt,Texture2D image,string name){camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1500,1000),0,0);image.Apply();File.WriteAllBytes("Builds/KeyboardAlcoholRelease/"+name+".png",image.EncodeToPNG());}
}
