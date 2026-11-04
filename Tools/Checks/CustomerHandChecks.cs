using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CustomerHandChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); passed++; };
        var shop = MvpShopController.Instance;
        var player = Object.FindAnyObjectByType<PlayerInteractor>();
        var camera = Camera.main;
        var fixture = new GameObject("Hand check fixture").AddComponent<ShopAcceptanceProbe>();
        try
        {
            fixture.Prepare("hand-interaction");
            var hand = shop.Activities.CustomerHand;
            var station = hand.GetComponent<ShopStation>();
            int balance = shop.Ledger.Cash;
            float fov = camera.fieldOfView;
            Vector3 position = camera.transform.localPosition;
            Quaternion rotation = camera.transform.localRotation;
            Physics.SyncTransforms();
            var ray = camera.ViewportPointToRay(new Vector3(.5f,.5f,0));
            player.RefreshTarget(ray);
            var target = typeof(PlayerInteractor).GetField("station", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player);
            check(ReferenceEquals(target, station) && station.Prompt.Contains("Aprópénz"), "Normal targeting discovers the customer's palm and prompt");
            var distantOrigin = hand.position + hand.forward * 3.5f + Vector3.up * .2f;
            player.RefreshTarget(new Ray(distantOrigin, (hand.position-distantOrigin).normalized));
            var distantTarget = typeof(PlayerInteractor).GetField("station", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player);
            check(!ReferenceEquals(distantTarget,station), "Hand cannot be targeted beyond ordinary interaction reach");
            player.RefreshTarget(ray);
            Vector3 approachDirection = Vector3.ProjectOnPlane(camera.transform.position-hand.position,Vector3.up).normalized;
            station.Use(player);
            check(shop.Devices.FocusKind == ShopDeviceKind.Coins && shop.Activities.CoinOffer.Total == 0, "Palm opens existing coin checkout without selecting a piece");
            check(!hand.GetComponent<Collider>().enabled, "World hand target cannot obstruct individual coin clicks in close view");
            check(ShopInputGate.WorldActionConsumed, "Opening palm consumes the initiating world action");
            check(Vector3.Dot(camera.transform.up,Vector3.up) > .3f, "Close view uses natural world-up orientation");
            check(Vector3.Dot(Vector3.ProjectOnPlane(camera.transform.position-hand.position,Vector3.up).normalized,approachDirection) > .99f, "Camera stays on the player's approach side");
            shop.ClosePanel();
            check(hand.GetComponent<Collider>().enabled, "Leaving close view restores the world hand target");
            check(camera.transform.localPosition == position && Quaternion.Angle(camera.transform.localRotation,rotation) < .01f && camera.fieldOfView == fov, "Leaving hand restores complete player camera");
            foreach (float angle in new[] { -30f, 0f, 30f })
            {
                Vector3 direction = Quaternion.AngleAxis(angle,Vector3.up) * hand.forward;
                camera.transform.position = hand.position + direction + Vector3.up * .6f;
                shop.Devices.Focus(ShopDeviceKind.Coins);
                check(Vector3.Dot(Vector3.ProjectOnPlane(camera.transform.position-hand.position,Vector3.up).normalized,direction) > .99f, "Camera follows actual approach angle " + angle);
                foreach (float aspect in new[] { 16f/9, 4f/3 })
                {
                    camera.aspect = aspect;
                    var offer = shop.Activities.CoinOffer;
                    for (int i = 0; i < offer.Coins.Count; i++)
                    {
                        Physics.SyncTransforms();
                        var coin = shop.Devices.KeyboardTargets().First(t => t.Id == "CustomerCoin:" + i);
                        var b = coin.Bounds;
                        if (b.xMin < 0 || b.yMin < 0 || b.xMax > Screen.width || b.yMax > Screen.height - 170) throw new Exception("Coin clipped at " + angle + "/" + aspect);
                        var click = camera.ScreenPointToRay(new Vector3(b.center.x,Screen.height-b.center.y,0));
                        if (!Physics.Raycast(click,out var hit,4)) throw new Exception("Coin ray missed " + i);
                        var control = hit.collider.GetComponentInParent<ShopWorldControl>();
                        if (control == null || control.Kind != WorldControlKind.CustomerCoin || control.Value != i) throw new Exception("Coin obstructed by " + hit.collider.name);
                        control.Activate(shop);
                    }
                    check(offer.Total == offer.Coins.Sum() && shop.Ledger.Cash == balance, "All coins remain individually clickable after raising at " + angle + "/" + aspect);
                    offer.Clear(); shop.Activities.RefreshHandCoins();
                }
                shop.ClosePanel();
            }
            camera.ResetAspect();
            fixture.Action("aim-customer-hand");
            var product = shop.CreateCoffee(0);
            check(player.TryHold(product), "Held-item fixture is available");
            station.Use(player);
            check(!shop.Devices.Focused && shop.Activities.CoinOffer.Total == 0, "Occupied player hand cannot open coin checkout");
            player.CancelHeld(); product.RemoveFromWorld();
            shop.Activities.ClearHand();
            station.Use(player); shop.Devices.Focus(ShopDeviceKind.Coins);
            check(!shop.Devices.Focused && shop.Ledger.Cash == balance, "Stale hand and missing-offer focus are inert");
            return new { passed, failed = 0 };
        }
        finally
        {
            camera.ResetAspect(); shop.ClosePanel(); shop.ResetDemo(); shop.ManualTick = false;
            Object.Destroy(fixture.gameObject);
        }
    }
}
