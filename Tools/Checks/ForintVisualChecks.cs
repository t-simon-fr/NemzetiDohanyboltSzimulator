using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ForintVisualChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); passed++; };
        var shop = MvpShopController.Instance;
        var camera = Camera.main;
        var holder = new GameObject("Forint verification");
        var fixture = holder.AddComponent<ShopAcceptanceProbe>();
        Action<int> clickCoin = index => {
            Physics.SyncTransforms();
            var target = shop.Devices.KeyboardTargets().First(t => t.Id == "CustomerCoin:" + index);
            var ray = camera.ScreenPointToRay(new Vector3(target.Bounds.center.x, Screen.height - target.Bounds.center.y, 0));
            if (!Physics.Raycast(ray, out var hit, 4, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) throw new Exception("Coin ray missed " + index);
            var control = hit.collider.GetComponentInParent<ShopWorldControl>();
            if (control == null || control.Kind != WorldControlKind.CustomerCoin || control.Value != index) throw new Exception("Offered coin obstructed: " + index + " by " + hit.collider.name);
            control.Activate(shop);
        };
        try
        {
            fixture.Prepare("cash");
            int balance = shop.Ledger.Cash;
            foreach (int value in ShopCatalog.Denominations)
            {
                var money = shop.Devices.MoneyVisuals.Create(holder.transform, value, Vector3.one * 100);
                var front = money.transform.Find("Front").GetComponent<MeshRenderer>();
                var back = money.transform.Find("Back").GetComponent<MeshRenderer>();
                bool note = value >= 500;
                string name = note ? "Banknote_" + value : "Coin_" + value;
                check(front.sharedMaterial.mainTexture.name == name + (note ? "" : "_Reverse") && back.sharedMaterial.mainTexture.name == name + (note ? "" : "_Obverse"), "Official two-sided imagery for " + value);
                var copy = shop.Devices.MoneyVisuals.Create(holder.transform, value, Vector3.one * 100);
                check(copy.transform.Find("Front").GetComponent<MeshRenderer>().sharedMaterial == front.sharedMaterial && copy.transform.Find("Front").GetComponent<MeshFilter>().sharedMesh == front.GetComponent<MeshFilter>().sharedMesh, "Money shares cached resources for " + value);
                var mesh = front.GetComponent<MeshFilter>().sharedMesh;
                var vertices = mesh.vertices; var indices = mesh.triangles;
                check(Vector3.Cross(vertices[indices[1]] - vertices[0], vertices[indices[2]] - vertices[0]).y > 0 && back.GetComponent<MeshFilter>().sharedMesh.normals[0].y < 0 && money.GetComponent<Collider>().enabled, "Front and back face outward with usable hitbox for " + value);
                if (note) check(front.sharedMaterial.mainTextureOffset.y >= .535f && front.sharedMaterial.mainTextureScale.y <= .465f && back.sharedMaterial.mainTextureOffset.y == 0, "Banknote faces exclude the composite's central gap for " + value);
                if (!note)
                {
                    var uv = mesh.uv;
                    var a = uv[indices[1]] - uv[0]; var b = uv[indices[2]] - uv[0];
                    check(a.x * b.y - a.y * b.x < 0, "Coin texture preserves readable unmirrored lettering for " + value);
                }
                money.SetActive(false); copy.SetActive(false);
            }
            check(shop.Ledger.Cash == balance && shop.ChangeSelected == 0, "Rendering money cannot mutate the cash ledger or selection");
            fixture.Prepare("coins");
            balance = shop.Ledger.Cash;
            var offer = shop.Activities.CoinOffer;
            check(offer != null && shop.CoinPayment && offer.Price == 385, "Real coin checkout is prepared");
            check(offer.Coins.Distinct().Count() == 6 && shop.Devices.KeyboardTargets().Count(t => t.Id.StartsWith("CustomerCoin:")) == offer.Coins.Count, "Every denomination in the palm remains individually selectable");
            check(!shop.ConfirmCustomerCoins() && shop.Ledger.Cash == balance, "Empty coin approval does not charge");
            clickCoin(0);
            check(offer.Total == offer.Coins[0] && shop.Ledger.Cash == balance, "Physical coin click selects the correct value without charging");
            clickCoin(0);
            check(offer.Total == 0, "Second physical click deselects the same coin");
            var taken = new List<int>();
            int remaining = offer.Price;
            foreach (int value in new[] { 200, 100, 50, 20, 10, 5 })
            {
                int index = Enumerable.Range(0, offer.Coins.Count).First(i => offer.Coins[i] == value);
                clickCoin(index); taken.Add(index); remaining -= value;
            }
            check(remaining == 0 && offer.Total == offer.Price && shop.Ledger.Sales == 0, "All six real coin faces produce exact unpaid selection");
            var counts = ShopCatalog.Denominations.Select(shop.Drawer.Count).ToArray();
            check(shop.ConfirmCustomerCoins(), "Exact physical coin payment is accepted");
            check(shop.Ledger.Sales == 1 && shop.Ledger.Cash == balance + 385 && shop.Drawer.Total == shop.Ledger.Cash, "Coin approval conserves cash and charges once");
            check(ShopCatalog.Denominations.Select((value, i) => shop.Drawer.Count(value) == counts[i] + taken.Count(index => offer.Coins[index] == value)).All(ok => ok), "Only the selected physical coin pieces enter the drawer");
            check(!shop.ConfirmCustomerCoins() && shop.Ledger.Sales == 1 && shop.Activities.CustomerHand == null, "Completed payment clears palm and rejects repeated approval");
            return new { passed, failed = 0 };
        }
        finally
        {
            holder.SetActive(false); Object.Destroy(holder);
            shop.ClosePanel(); shop.ResetDemo(); shop.ManualTick = false;
        }
    }
}
