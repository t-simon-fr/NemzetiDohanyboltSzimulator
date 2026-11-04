using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

public static class ModernRegisterChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); passed++; };
        var shop = MvpShopController.Instance;
        var devices = shop.Devices;
        var camera = Camera.main;
        var fixture = new GameObject("Register check fixture").AddComponent<ShopAcceptanceProbe>();
        Action<WorldControlKind, int> click = (kind, value) => {
            Physics.SyncTransforms();
            var target = devices.KeyboardTargets().First(t => t.Id == kind + ":" + value);
            var point = target.Bounds.center;
            var ray = camera.ScreenPointToRay(new Vector3(point.x, Screen.height - point.y, 0));
            if (!Physics.Raycast(ray, out var hit, 4, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) throw new Exception("Register ray missed " + target.Id);
            var control = hit.collider.GetComponentInParent<ShopWorldControl>();
            if (control == null || control.Kind != kind || control.Value != value) throw new Exception("Register control obstructed: " + target.Id + " by " + hit.collider.name);
            control.Activate(shop);
            devices.RegisterVisuals.Refresh(shop);
        };
        try
        {
            fixture.Prepare("pickup");
            float originalFov = camera.fieldOfView;
            Vector3 originalPosition = camera.transform.localPosition;
            Quaternion originalRotation = camera.transform.localRotation;
            fixture.Prepare("cash");
            devices.RegisterDrawer.Advance(1);
            var register = devices.RegisterVisuals;
            register.Refresh(shop);
            check(register.Model != null && register.Model.GetComponentsInChildren<MeshFilter>().Length >= 20, "Original register mesh prefab is installed");
            check(!devices.TillAssembly.Find("Sloped register housing").gameObject.activeSelf && !devices.TillAssembly.Find("Register keypad/Numeric key 1").gameObject.activeSelf, "Old shell and decorative keypad are hidden");
            var shell = register.Model.Find("Register_Shell").GetComponent<MeshFilter>().sharedMesh;
            var v = shell.vertices; var n = shell.normals; var triangles = shell.triangles;
            check(Vector3.Dot(Vector3.Cross(v[triangles[1]] - v[triangles[0]], v[triangles[2]] - v[triangles[0]]).normalized, n[triangles[0]]) > .9f, "Exported surface winding agrees with outward normals");
            check(devices.RegisterDrawer.MovingDrawer.Find("Drawer_Fascia") != null, "Own drawer model moves with actual insert");
            check(camera.fieldOfView == 42 && originalFov > camera.fieldOfView && devices.FocusKind == ShopDeviceKind.Till, "Cash focus uses the dedicated closer lens");
            var font = Resources.Load<TMP_FontAsset>(ShopRegisterVisuals.Resource + "RegisterFont");
            check(font.atlasPopulationMode == AtlasPopulationMode.Static && font.HasCharacters("FIZETENDŐ VISSZAJÁRÓ Űőű"), "Static SDF includes Hungarian glyphs");
            check(register.DisplayDue == ShopCatalog.Money(shop.ActiveCustomer.TotalPrice) && register.DisplayReceived == ShopCatalog.Money(shop.ActiveCustomer.Tender), "Register displays actual price and received tender");
            check(register.DisplayChange == ShopCatalog.Money(shop.ActiveCustomer.Tender - shop.ActiveCustomer.TotalPrice) && register.DisplaySelected == "0 Ft", "Register separates required and selected change");
            foreach (float aspect in new[] { 16f / 9, 4f / 3 })
            {
                camera.aspect = aspect; register.Focus(camera);
                foreach (var target in devices.KeyboardTargets())
                    check(target.Bounds.xMin > 0 && target.Bounds.yMin > 0 && target.Bounds.xMax < Screen.width && target.Bounds.yMax < Screen.height - 48, "Visible cash control at aspect " + aspect + ": " + target.Id);
            }
            camera.ResetAspect(); register.Focus(camera);
            int balance = shop.Ledger.Cash, price = shop.ActiveCustomer.TotalPrice;
            click(WorldControlKind.Pay, 0);
            check(shop.Ledger.Sales == 0 && shop.Ledger.Cash == balance, "Empty physical approval cannot settle sale");
            click(WorldControlKind.Denomination, 500); click(WorldControlKind.Undo, 0);
            check(shop.ChangeSelected == 0 && register.DisplaySelected == "0 Ft", "Physical undo updates amount display");
            click(WorldControlKind.Denomination, 500); click(WorldControlKind.Clear, 0);
            check(shop.ChangeSelected == 0, "Physical clear resets selection");
            devices.HandleDeviceKey(Key.Digit4);
            register.Refresh(shop);
            check(shop.ChangeSelected == 2000 && register.DisplaySelected == ShopCatalog.Money(2000), "Keyboard and physical display share selection");
            int remaining = shop.ActiveCustomer.Tender - price - shop.ChangeSelected;
            foreach (int value in ShopCatalog.Denominations)
                while (remaining >= value && shop.Drawer.CanSelect(value, shop.SelectedPieces(value))) { click(WorldControlKind.Denomination, value); remaining -= value; }
            check(remaining == 0 && register.DisplaySelected == register.DisplayChange, "Real denomination controls produce exact change");
            click(WorldControlKind.Pay, 0);
            check(shop.Ledger.Sales == 1 && shop.Ledger.Cash == balance + price && shop.Drawer.Total == shop.Ledger.Cash, "Physical approval settles exactly once with conserved drawer");
            shop.SubmitChange(shop.ChangeSelected);
            check(shop.Ledger.Sales == 1 && shop.Ledger.Cash == balance + price, "Repeated approval cannot double charge");
            check(devices.FocusKind == ShopDeviceKind.None && camera.fieldOfView == originalFov, "Completing payment restores normal lens");
            fixture.Prepare("pickup");
            originalPosition = camera.transform.localPosition; originalRotation = camera.transform.localRotation;
            devices.Focus(ShopDeviceKind.Till); shop.ClosePanel();
            check(camera.transform.localPosition == originalPosition && Quaternion.Angle(camera.transform.localRotation, originalRotation) < .01f && camera.fieldOfView == originalFov, "Leaving close view restores complete original camera pose");
            check(!devices.RegisterDrawer.TargetOpen, "Leaving register closes drawer");
            fixture.Prepare("card");
            check(devices.FocusKind == ShopDeviceKind.Terminal && camera.fieldOfView == originalFov, "Card payment keeps its own device view");
            return new { passed, failed = 0 };
        }
        finally
        {
            camera.ResetAspect(); shop.ClosePanel(); shop.ResetDemo(); shop.ManualTick = false;
            Object.Destroy(fixture.gameObject);
        }
    }
}
