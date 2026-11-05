using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

public static class PhysicalTerminalChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); passed++; };
        var shop = MvpShopController.Instance;
        var fixture = new GameObject("Terminal check fixture").AddComponent<ShopAcceptanceProbe>();
        var terminal = shop.Devices.Terminal;
        var camera = Camera.main;
        Action<WorldControlKind, int> click = (kind, value) => {
            Physics.SyncTransforms();
            Rect r = terminal.Bounds(camera, kind, value);
            var ray = camera.ScreenPointToRay(new Vector3(r.center.x, Screen.height - r.center.y, 0));
            if (!Physics.Raycast(ray, out var hit, 4, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) throw new Exception("Terminal ray missed " + kind + value);
            var control = hit.collider.GetComponentInParent<ShopWorldControl>();
            if (control != terminal.Find(kind, value)) throw new Exception("Terminal key occluded: " + kind + value + " by " + hit.collider.name);
            control.Activate(shop);
            terminal.Refresh();
        };
        try
        {
            fixture.Prepare("card");
            shop.Devices.Release();
            var originalPosition=camera.transform.localPosition;
            var originalRotation=camera.transform.localRotation;
            float originalFov=camera.fieldOfView;
            shop.Devices.Focus(ShopDeviceKind.Terminal);
            terminal.Refresh();
            check(shop.Devices.FocusKind == ShopDeviceKind.Terminal && !shop.Devices.RegisterDrawer.IsOpen, "Card checkout focuses physical terminal with closed drawer");
            check(terminal.Controls.Count == 14 && terminal.Controls.Count(c => c.Kind == WorldControlKind.CardDigit) == 10, "Ten physical numeric keys and four action keys");
            check(!terminal.GetComponentInChildren<BoxCollider>().bounds.Intersects(shop.FloorPlan.Find("Change plate").GetComponent<Collider>().bounds), "Terminal does not occupy the cash return tray");
            int balance = shop.Ledger.Cash;
            check(terminal.Bounds(camera, WorldControlKind.CardDigit, 1).center.x < terminal.Bounds(camera, WorldControlKind.CardDigit, 2).center.x && terminal.Bounds(camera, WorldControlKind.CardDigit, 2).center.x < terminal.Bounds(camera, WorldControlKind.CardDigit, 3).center.x && terminal.Bounds(camera, WorldControlKind.CardUndo).center.x > terminal.Bounds(camera, WorldControlKind.CardDigit, 3).center.x, "Numeric row reads 1-2-3 with action keys on the right");
            for (int digit = 0; digit < 10; digit++)
            {
                click(WorldControlKind.CardClear, 0);
                click(WorldControlKind.CardDigit, digit);
                check(shop.CardAmount == digit.ToString(), "Physical numeric key " + digit + " is visible and reachable");
            }
            click(WorldControlKind.CardClear, 0);
            foreach (int digit in new[] { 1, 2, 3 }) click(WorldControlKind.CardDigit, digit);
            check(shop.CardAmount == "123" && terminal.DisplayAmount == "123 Ft", "Ray-targeted numeric keys update actual screen and shared amount");
            click(WorldControlKind.CardConfirm, 0);
            check(shop.Ledger.Cash == balance && shop.Ledger.Sales == 0 && terminal.DisplayStatus == "HIBÁS ÖSSZEG", "Wrong physical submission shows error without money movement");
            check(shop.Devices.FocusKind == ShopDeviceKind.Terminal && ShopInputGate.Blocked, "Incorrect payment keeps terminal open for correction");
            click(WorldControlKind.CardUndo, 0);
            check(shop.CardAmount == "12", "Physical correction removes last digit");
            shop.Devices.HandleDeviceKey(Key.Digit3);
            terminal.Refresh();
            check(terminal.DisplayAmount == "123 Ft", "Keyboard and physical keypad share live display");
            click(WorldControlKind.CardClear, 0);
            check(shop.CardAmount == "" && terminal.DisplayAmount == "0 Ft", "Physical clear resets the complete entry");
            click(WorldControlKind.CardConfirm, 0);
            check(shop.Ledger.Cash == balance && shop.CardStatus == CardTerminalStatus.Error, "Empty terminal cannot settle");
            foreach (int digit in new[] { 2, 2, 0, 0 }) click(WorldControlKind.CardDigit, digit);
            check(terminal.DisplayDue.Contains(ShopCatalog.Money(2200)), "Terminal shows actual due amount");
            click(WorldControlKind.CardConfirm, 0);
            check(shop.Ledger.Cash == balance + 2200 && shop.Drawer.Total == shop.Ledger.Cash && shop.Ledger.Sales == 1, "Physical approval settles and reconciles exactly once");
            check(terminal.DisplayStatus == "SIKERES FIZETÉS" && terminal.DisplayAmount == ShopCatalog.Money(2200), "Approved amount remains visible on physical screen");
            check(!shop.Devices.Focused && !ShopInputGate.Blocked && Cursor.lockState == CursorLockMode.Locked, "Success automatically returns to normal world input");
            check(Vector3.Distance(camera.transform.localPosition,originalPosition)<.001f && Quaternion.Angle(camera.transform.localRotation,originalRotation)<.01f && Mathf.Abs(camera.fieldOfView-originalFov)<.01f, "Successful payment restores original camera position rotation and lens");
            check(ShopInputGate.WorldActionConsumed, "Payment input cannot click through into restored world view");
            terminal.Find(WorldControlKind.CardConfirm, 0).Activate(shop);
            check(shop.Ledger.Sales == 1 && shop.Ledger.Cash == balance + 2200, "Repeated physical approval cannot double charge");
            fixture.Prepare("card"); terminal.Refresh();
            check(shop.CardAmount == "" && terminal.DisplayAmount == "0 Ft" && shop.CardStatus != CardTerminalStatus.Approved, "Fresh transaction clears previous entry and approval");
            fixture.Prepare("pickup");
            shop.Devices.Focus(ShopDeviceKind.Terminal);
            click(WorldControlKind.CardDigit, 9); click(WorldControlKind.CardConfirm, 0);
            check(shop.CardAmount == "" && shop.Ledger.Sales == 0 && terminal.DisplayStatus == "KÉSZENLÉT", "Idle terminal cannot create a transaction");
            fixture.Prepare("cash");
            shop.Devices.RegisterDrawer.Advance(1);
            check(shop.Devices.FocusKind == ShopDeviceKind.Till && shop.Devices.RegisterDrawer.IsOpen, "Cash customer still uses actual cash drawer");
            check(shop.Devices.KeyboardTargets().All(t => !t.Id.StartsWith("Card")), "Cash selection excludes idle terminal keypad");
            return new { passed, failed = 0 };
        }
        finally
        {
            shop.ResetDemo(); shop.ManualTick = false;
            Object.Destroy(fixture.gameObject);
        }
    }
}
