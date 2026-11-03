using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class InteriorArtChecks
{
    public static object Run()
    {
        var game=MvpShopController.Instance;if(!Application.isPlaying||game==null)throw new InvalidOperationException("Enter Play mode first.");
        int passed=0;Action<bool,string> check=(valid,label)=>{if(!valid)throw new InvalidOperationException(label);passed++;};
        game.ManualTick=true;ShopCheckFixtures.Reset(game);game.ClosePanel();
        try
        {
            foreach(string name in new[]{"Interior Art","Interior Counter Detail","Interior Countertop Props","Interior Rack Detail"})
            {
                var root=GameObject.Find(name);check(root!=null,"Owned art root exists: "+name);
                check(root.GetComponentsInChildren<Collider>(true).Length==0,"Visual art cannot intercept interactions: "+name);
            }
            var floor=GameObject.Find("Floor");check(floor.GetComponent<Renderer>().sharedMaterial.mainTexture.name=="FloorTiles","Ceramic floor texture assigned");
            check(floor.GetComponent<Renderer>().sharedMaterial.mainTexture.wrapMode==TextureWrapMode.Repeat,"Surface texture repeats");
            check(GameObject.Find("Service counter").GetComponent<Renderer>().sharedMaterial.mainTexture.name=="Walnut","Counter uses walnut material");
            check(GameObject.Find("Counter top").GetComponent<MeshFilter>().sharedMesh.vertexCount>24,"Countertop has modeled bevels");
            var rack=GameObject.Find("Cigarette rack");var slots=rack.GetComponentsInChildren<ProductSlot>();check(slots.Length==32,"All 32 stock slots retained");
            for(int brand=0;brand<4;brand++)
            {
                check(game.StockCount(brand)==8,"Eight actual packs per brand");
                check(UnityEngine.Object.FindObjectsByType<ShopProduct>().Where(p=>p.ProductId==ShopCatalog.Ids[brand]).All(p=>p.CurrentSlot!=null&&p.CurrentSlot.isActiveAndEnabled),"Starting products occupy live slots");
            }
            check(rack.transform.Find("Rack back").localScale.x==2.20f,"Compact rack geometry");
            check(Mathf.Abs(rack.transform.Find("Cigarette slot 0-1").localPosition.x-rack.transform.Find("Cigarette slot 0-0").localPosition.x-.25f)<.001f,"Readable dense pack spacing");
            check(rack.GetComponentsInChildren<TextMesh>().Count(t=>t.name.StartsWith("Shelf price "))==16,"Price tickets on all four shelves");
            check(!rack.GetComponentsInChildren<ShopProduct>().Any(),"Decor never creates fake stock");
            check(GameObject.Find("Interior Art").GetComponentsInChildren<Transform>().Count(t=>t.name=="Fluorescent fixture")==2,"Two visible ceiling fixtures");
            check(GameObject.Find("Interior light").GetComponent<Light>().type==LightType.Spot,"Staff light shines downwards");
            check(GameObject.Find("Customer area fill").GetComponent<Light>().type==LightType.Spot,"Customer light shines downwards");
            check(GameObject.Find("Survivor plant")!=null&&GameObject.Find("Cashier coffee mug")!=null,"Counter props present");
            check(GameObject.Find("Opening hours")!=null,"Hours displayed by entrance");
            var partition=GameObject.Find("Expansion partition");var trim=partition.transform.Find("Interior Wall Trim");check(trim!=null&&trim.gameObject.activeInHierarchy,"Starter wall has matching trim");
            check(ShopCheckFixtures.Expand(game)&&!trim.gameObject.activeInHierarchy,"Expansion hides decorative wall trim with its partition");
            var counter=game.Counter.transform;var prop=GameObject.Find("Cashier coffee mug").transform;var local=counter.InverseTransformPoint(prop.position);var position=counter.position;var rotation=counter.rotation;
            try{counter.position+=new Vector3(.12f,0,.08f);counter.rotation=Quaternion.Euler(0,12,0)*rotation;check(Vector3.Distance(prop.position,counter.TransformPoint(local))<.001f,"Counter decorations follow movement and rotation");}
            finally{counter.SetPositionAndRotation(position,rotation);}
            return new{passed,failed=0};
        }
        finally{ShopCheckFixtures.Reset(game);game.ManualTick=false;}
    }
    public static object Capture(string revision)
    {
        var game=MvpShopController.Instance;if(game==null||!Application.isPlaying)throw new InvalidOperationException("Play shop first.");
        var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
        var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
        var rt=new RenderTexture(1600,1000,24);var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        game.ManualTick=true;
        try
        {
            ShopCheckFixtures.Reset(game);game.ClosePanel();
            var counter=game.Counter.transform;var rack=GameObject.Find("Cigarette rack").transform;
            camera.transform.position=counter.TransformPoint(new Vector3(-1.6f,1.2f,-2.55f));
            camera.transform.LookAt(rack.TransformPoint(new Vector3(.25f,1.05f,0)));camera.fieldOfView=58;
            Save(camera,rt,image,revision+"-shop");
            camera.transform.position=rack.TransformPoint(new Vector3(.22f,1.25f,-1.95f));camera.transform.LookAt(rack.TransformPoint(new Vector3(0,1.08f,0)));camera.fieldOfView=54;
            Save(camera,rt,image,revision+"-rack");
            game.Devices.Focus(ShopDeviceKind.Monitor);game.Devices.OpenApp(ShopComputerApp.Orders);Save(camera,rt,image,revision+"-computer");game.Devices.Release();
            return "Builds/InteriorPreview/"+revision+"-shop.png";
        }
        finally
        {
            game.Devices.Release();camera.targetTexture=previousTarget;RenderTexture.active=previousActive;
            camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;
            UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);ShopCheckFixtures.Reset(game);game.ManualTick=false;
        }
    }
    static void Save(Camera camera,RenderTexture rt,Texture2D image,string name)
    {
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();
        Directory.CreateDirectory("Builds/InteriorPreview");File.WriteAllBytes("Builds/InteriorPreview/"+name+".png",image.EncodeToPNG());
    }
}
