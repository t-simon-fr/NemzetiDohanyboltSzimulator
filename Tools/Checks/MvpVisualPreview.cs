using System;
using System.Linq;
using UnityEngine;

public static class MvpVisualPreview
{
    public static object Cash()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<MvpShopController>();
        game.ManualTick=true;game.ResetDemo();game.OpenShop();
        for(int i=0;i<100&&game.Stage!=ServiceStage.Order;i++)game.Tick(.1f);
        var product=UnityEngine.Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId=="water");
        Transform hand=new GameObject("Temporary Payment Preview Hand").transform;hand.position=Vector3.one*1000;
        try
        {
            if(!product.PickUp(hand))throw new InvalidOperationException("Preview pickup failed.");
            Physics.SyncTransforms();
            if(!product.TryPlace(game.DeliverySlot))throw new InvalidOperationException("Preview placement failed.");
            game.AddChange(100);game.AddChange(50);
            return new { stage=game.Stage.ToString(),change=game.ChangeSelected };
        }
        finally { if(product!=null&&product.IsHeld)product.ReturnToPickupPosition();UnityEngine.Object.DestroyImmediate(hand.gameObject);game.ManualTick=false; }
    }
}
