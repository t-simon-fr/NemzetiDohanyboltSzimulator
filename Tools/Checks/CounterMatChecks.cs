using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CounterMatChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool,string> check = (ok,name) => { if (!ok) throw new Exception(name); passed++; };
        var shop = MvpShopController.Instance;
        var player = Object.FindAnyObjectByType<PlayerInteractor>();
        var fixture = new GameObject("Counter mat check").AddComponent<ShopAcceptanceProbe>();
        try
        {
            shop.ClosePanel(); shop.ResetDemo(); shop.ManualTick = true;
            int cash = shop.Ledger.Cash;
            var basket = shop.Activities.BasketSlot;
            var mat = basket.transform.Find("Customer basket mat").GetComponent<Renderer>();
            var blue = shop.FloorPlan.Find("Blue handover tray").GetComponent<Renderer>();
            var top = shop.FloorPlan.Find("Counter top");
            var a = mat.bounds; var b = blue.bounds;
            check(a.max.x < b.min.x || b.max.x < a.min.x || a.max.z < b.min.z || b.max.z < a.min.z, "Customer and blue mats have disjoint footprints");
            var localCenter = basket.transform.InverseTransformPoint(a.center);
            check(Mathf.Abs(localCenter.x) < .001f && Mathf.Abs(localCenter.z) < .001f, "Visible customer mat is centered on the real product slot");
            var trigger = basket.GetComponent<BoxCollider>().bounds;
            check(trigger.min.x >= a.min.x && trigger.max.x <= a.max.x && trigger.min.z >= a.min.z && trigger.max.z <= a.max.z, "Placement footprint stays inside its visible mat");
            check(shop.Activities.CustomerHandoverSlots.All(slot=>{var bounds=slot.GetComponent<BoxCollider>().bounds;return bounds.min.x>=a.min.x&&bounds.max.x<=a.max.x&&bounds.min.z>=a.min.z&&bounds.max.z<=a.max.z;}),"All customer handover places stay within the mat");
            var mesh = top.GetComponent<MeshFilter>().sharedMesh.bounds;
            bool inside = true;
            foreach (float x in new[] { a.min.x, a.max.x })
                foreach (float z in new[] { a.min.z, a.max.z })
                {
                    var point = top.InverseTransformPoint(new Vector3(x,top.position.y,z));
                    inside &= point.x > mesh.min.x + .025f && point.x < mesh.max.x - .025f && point.z > mesh.min.z + .025f && point.z < mesh.max.z - .025f;
                }
            check(inside, "Customer mat stays inside the beveled countertop edges");
            var product = shop.CreateCoffee(ShopCatalog.Coffee);
            var input = shop.Activities.CustomerHandoverSlots[0];
            check(player.TryHold(product), "Actual prepared coffee can be held for placement");
            Physics.SyncTransforms();
            player.RefreshTarget(new Ray(basket.HintCenter + Vector3.up*.65f,Vector3.down));
            check(player.TargetSlot == input && player.CanPlaceHeld, "Real physical ray targets a coffee place on the visible customer mat");
            check(player.TryPlaceHeld() && input.Occupant == product, "Existing use action places prepared coffee onto the customer mat");
            check(product.CurrentSlot == input && !player.HasHeldItem && shop.Ledger.Cash == cash, "Placement conserves actual product and cash");
            check(player.TryHold(product), "Placed product remains retrievable");
            player.CancelHeld();
            check(product.CurrentSlot == input && !product.IsHeld && input.Occupant == product, "Cancellation safely restores the same mat and product");
            product.RemoveFromWorld();
            fixture.Prepare("ui-order");
            fixture.Action("deliver-ui-one");
            fixture.Action("deliver-ui-one");
            check(ShopUiState.Order(shop).Sum(l=>l.Delivered) == 2, "Multiple identical goods remain independently delivered");
            return new { passed, failed = 0 };
        }
        finally { player.CancelHeld(); shop.ClosePanel(); shop.ResetDemo(); shop.ManualTick = false; Object.Destroy(fixture.gameObject); }
    }
}
