using System;
using System.Linq;
using UnityEngine;

public static class RegisterFixtureChecks
{
    private static int checks;
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);checks++;}
    public static object Run()
    {
        checks=0;var game=MvpShopController.Instance;
        game.ManualTick=true;game.ResetDemo();game.ClosePanel();
        var devices=game.Devices;var mechanism=devices.RegisterDrawer;
        var assembly=game.Counter.transform;var top=GameObject.Find("Counter top").transform;
        var monitor=GameObject.Find("Order Monitor").transform;
        var cabinet=GameObject.Find("Cabinet bottom").transform;
        Vector3 rootPosition=assembly.position;Quaternion rootRotation=assembly.rotation;
        Vector3 oldService=game.Counter.ServicePoint;
        try
        {
            Require(monitor.localScale.x/top.lossyScale.x<.25f,"Monitor is less than a quarter of counter width");
            Require(cabinet.localScale.x/top.lossyScale.x<.25f,"Register is less than a quarter of counter width");
            Require(top.IsChildOf(assembly)&&devices.TillAssembly.IsChildOf(assembly)&&game.DeliverySlot.transform.IsChildOf(assembly),"Countertop, cash register and handover tray belong to one movable assembly");
            Require(Mathf.Abs(devices.TillAssembly.position.y-(top.position.y+top.lossyScale.y*.5f))<.001f,"Register rests on the moved countertop");
            mechanism.SetOpen(false);mechanism.Advance(1f);Vector3 closed=mechanism.MovingDrawer.localPosition;
            Require(!mechanism.IsOpen&&mechanism.Progress==0,"Drawer starts fully closed");
            Require(!mechanism.MovingDrawer.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled),"Closed drawer money cannot intercept clicks");
            devices.Focus(ShopDeviceKind.Till);mechanism.Advance(.1f);
            float extension=Vector3.Dot(mechanism.MovingDrawer.localPosition-closed,Vector3.forward);
            Require(extension>0&&extension<.44f&&mechanism.Progress<1,"Opening has an intermediate sliding position");
            Require(!mechanism.MovingDrawer.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled),"Money interaction waits for full opening");
            mechanism.Advance(1f);
            Require(mechanism.IsOpen&&Mathf.Abs(Vector3.Distance(mechanism.MovingDrawer.localPosition,closed)-.44f)<.001f,"Cash insert extends 44 cm toward cashier");
            Require(mechanism.MovingDrawer.GetComponentsInChildren<Collider>().Any(c=>c.enabled),"Open money compartments are clickable");
            devices.Release();mechanism.Advance(.1f);
            Require(!mechanism.TargetOpen&&mechanism.Progress>0&&mechanism.Progress<1,"Leaving register animates drawer closed");
            mechanism.Advance(1f);Require(Vector3.Distance(mechanism.MovingDrawer.localPosition,closed)<.001f,"Drawer returns exactly to cabinet");
            // A rigid move and rotation prove that the scene references replace old world coordinates.
            Vector3 localService=assembly.InverseTransformPoint(oldService);
            assembly.position+=new Vector3(.16f,0,.10f);assembly.rotation=Quaternion.Euler(0,17,0)*rootRotation;
            Require(Vector3.Distance(game.Counter.ServicePoint,assembly.TransformPoint(localService))<.001f,"Customer service point follows a moved and rotated counter");
            devices.Focus(ShopDeviceKind.Monitor);devices.OpenApp(ShopComputerApp.Orders);
            var order=UnityEngine.Object.FindObjectsByType<ShopWorldControl>().First(c=>c.Kind==WorldControlKind.Order&&c.Value==0);
            Physics.SyncTransforms();Vector3 orderScreen=Camera.main.WorldToScreenPoint(order.transform.position);
            Require(orderScreen.z>0&&orderScreen.x>0&&orderScreen.x<Camera.main.pixelWidth&&orderScreen.y>0&&orderScreen.y<Camera.main.pixelHeight,"Monitor remains in frame after moving assembly");
            Require(Physics.Raycast(Camera.main.ScreenPointToRay(orderScreen),out var monitorHit,4f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&monitorHit.collider.GetComponentInParent<ShopWorldControl>()?.Kind==WorldControlKind.Order,"Monitor buttons follow counter rotation");
            devices.Release();devices.Focus(ShopDeviceKind.Till);mechanism.Advance(1f);Physics.SyncTransforms();
            var pay=UnityEngine.Object.FindObjectsByType<ShopWorldControl>().First(c=>c.Kind==WorldControlKind.Pay);
            Ray ray=Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(pay.transform.position));
            Require(Physics.Raycast(ray,out var hit,4f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<ShopWorldControl>()?.Kind==WorldControlKind.Pay,"Register camera and pay button follow counter rotation");
            return new{passed=checks,failed=0};
        }
        finally
        {
            devices.Release();mechanism.SetOpen(false);mechanism.Advance(1f);
            assembly.SetPositionAndRotation(rootPosition,rootRotation);game.ResetDemo();game.ManualTick=false;
        }
    }
}
