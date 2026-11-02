using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class CigarettePackagingChecks
{
    public static object Run()
    {
        var game = MvpShopController.Instance;
        if (!Application.isPlaying || game == null) throw new InvalidOperationException("Enter Play mode first.");
        int passed = 0;
        Action<bool,string> check = (valid,label) => { if (!valid) throw new InvalidOperationException(label); passed++; };
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();
        var hand = new GameObject("Packaging test hand").transform; hand.position = Vector3.one * 1000;
        game.ManualTick = true;
        try
        {
            game.ResetDemo(); game.ClosePanel();
            check(ShopCatalog.Names.Take(4).SequenceEqual(new[] { "Lucky Stroke", "Marlbroke", "Downhill", "Davidon" }), "Approved names in catalog");
            check(game.AvailableProducts == 4, "All four cigarette brands available before expansion");
            for (int type = 0; type < 4; type++)
            {
                check(game.StockCount(type) == 8, "Eight packs for brand " + type);
                var product = UnityEngine.Object.FindObjectsByType<ShopProduct>().First(p => p.ProductId == ShopCatalog.Ids[type]);
                check(product.DisplayName == ShopCatalog.Names[type], "Correct interaction name");
                check(!product.GetComponent<MeshRenderer>().enabled, "Blockout renderer replaced");
                var renderer = product.GetComponentsInChildren<MeshRenderer>().First(r => r.enabled && r.sharedMaterials.Length == 2);
                check(renderer.sharedMaterials[0].mainTexture.name == "PackagingAtlas", "Production atlas assigned");
                check(renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount < 200, "Lightweight beveled mesh");
                check(product.GetComponentsInChildren<Collider>().Length == 1, "Visuals do not add colliders");
                var originalSlot = product.CurrentSlot;
                check(product.PickUp(hand) && product.IsHeld, "Pack can be held");
                check(renderer.enabled && renderer.transform.IsChildOf(product.transform), "Model follows held product");
                Physics.SyncTransforms();
                check(product.TryPlace(originalSlot) && !product.IsHeld, "Pack returns to its shelf");
                check(game.Warehouse.Dock(type).GetComponent<ShopStation>().ProductType == type, "Matching warehouse dock");
            }
            check(!game.OrderSupply(ShopCatalog.Index("water")), "Convenience goods remain locked");
            check(game.OrderSupply(3), "Davidon can be ordered before expansion"); game.Tick(8.1f);
            var carton = game.Warehouse.Carton(3);
            check(carton != null && carton.Quantity == 3, "Separate Davidon shipment carton");
            check(carton.GetComponentsInChildren<MeshRenderer>().Any(r => r.sharedMaterials.Length == 2 && r.sharedMaterials[0].mainTexture != null), "Carton contains textured pack samples");
            var david = UnityEngine.Object.FindObjectsByType<ShopProduct>().First(p => p.ProductId == ShopCatalog.Ids[3]);
            var target = david.CurrentSlot;
            check(player.TryHold(david) && game.ReturnSupply(player,3), "Davidon can return from shelf to carton");
            check(player.TryHoldCarton(carton), "Davidon carton is carryable"); Physics.SyncTransforms();
            check(carton.TryPlaceOne(target), "Single textured Davidon can be stocked from held carton");
            check(game.StockCount(3) == 8 && game.WarehouseCount(3) == 3 && carton.Quantity == 3, "Stock conservation across new brand workflow");
            player.ReturnCarton();
            return new { passed, failed = 0, brands = 4, initialPacks = 32 };
        }
        finally { player.CancelHeld(); game.ResetDemo(); game.ManualTick = false; UnityEngine.Object.Destroy(hand.gameObject); }
    }
    public static object CaptureRack()
    {
        var camera = Camera.main; var rack = GameObject.Find("Cigarette rack").transform;
        var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation; float oldFov = camera.fieldOfView;
        var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
        var rt = new RenderTexture(1600,900,24); var image = new Texture2D(1600,900,TextureFormat.RGB24,false);
        try
        {
            camera.transform.position = rack.TransformPoint(new Vector3(.10f,1.25f,-2.0f)); camera.transform.LookAt(rack.TransformPoint(new Vector3(0,1.08f,0))); camera.fieldOfView = 58;
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0,0,1600,900),0,0); image.Apply(); Directory.CreateDirectory("Builds/PackagingPreview");
            File.WriteAllBytes("Builds/PackagingPreview/shop-shelf.png",image.EncodeToPNG());
            return "Builds/PackagingPreview/shop-shelf.png";
        }
        finally { camera.targetTexture = oldTarget; RenderTexture.active = oldActive; camera.transform.SetPositionAndRotation(oldPosition,oldRotation); camera.fieldOfView = oldFov; UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(image); }
    }
}
