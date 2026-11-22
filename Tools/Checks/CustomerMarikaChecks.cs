using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

// #183: the MPFB Marika model replaces the capsule Marika (person 1).
// Covers model assembly, Animator takes, complaint/smile mouths and the
// untouched money/stock flow around her.
public static class CustomerMarikaChecks
{
    private static Transform DeepFind(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = DeepFind(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); passed++; };
        var shop = MvpShopController.Instance;
        shop.ManualTick = true;
        try
        {
            check(ShopMarikaRig.MoodFor(true, ServiceStage.Order, true, false) == ShopMarikaRig.Mood.Panic, "Stealing maps to panic");
            check(ShopMarikaRig.MoodFor(false, ServiceStage.Thanks, true, false) == ShopMarikaRig.Mood.Smile, "Thanks maps to smile");
            check(ShopMarikaRig.MoodFor(false, ServiceStage.Order, true, true) == ShopMarikaRig.Mood.Grim, "Complaint maps to grim");
            check(ShopMarikaRig.MoodFor(false, ServiceStage.Order, true, false) == ShopMarikaRig.Mood.Neutral, "Plain order stays neutral");
            check(!ShopPeople.IsComplaintLine(1, "Kérek 1 × Lucky Stroke."), "Plain order is not a complaint");
            shop.ResetDemo();
            shop.ClosePanel();
            shop.ManualTick = true;
            shop.AutomaticArrivals = false;
            shop.OpenShop();
            typeof(MvpShopController).GetProperty("Spawned").SetValue(shop, 1);
            typeof(MvpShopController).GetMethod("SpawnCustomer", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(shop, new object[] { 0, false });
            var actor = Object.FindObjectsByType<MvpCustomerActor>().First(a => a.Person == 1);
            var rig = actor.GetComponent<ShopMarikaRig>();
            check(rig != null, "Marika gets the Animator rig");
            var body = actor.transform.Find("Body");
            check(body != null && !body.GetComponent<Renderer>().enabled, "Capsule body hides under the new model");
            check(actor.transform.Find("Hair") == null && actor.transform.Find("Nose") == null, "Legacy primitive bits stay off for Marika");
            int skinned = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Count(r => r.sharedMesh != null);
            check(skinned >= 7, "MPFB model assembles all parts");
            var animator = actor.GetComponentInChildren<Animator>(true);
            check(animator != null && animator.avatar != null && animator.avatar.isHuman, "Humanoid avatar is valid");
            check(animator.runtimeAnimatorController != null && animator.runtimeAnimatorController.animationClips.Length >= 4, "Walk/Idle/Present/Complain takes are wired");
            var bounds = new Bounds(actor.transform.position, Vector3.zero);
            var baked = new Mesh();
            try
            {
                foreach (var smr in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.sharedMesh == null) continue;
                    smr.BakeMesh(baked);
                    var verts = baked.vertices;
                    for (int i = 0; i < verts.Length; i++)
                        bounds.Encapsulate(smr.transform.localToWorldMatrix.MultiplyPoint3x4(verts[i]));
                }
            }
            finally { Object.Destroy(baked); }
            float relativeTop = bounds.max.y - actor.transform.position.y;
            float relativeBottom = bounds.min.y - actor.transform.position.y;
            check(relativeTop > 1.7f && relativeTop < 1.9f, "Head keeps the original height");
            check(relativeBottom > -.15f, "Feet stay at ground level");
            var basket = actor.transform.Find("Kosár");
            check(basket != null && basket.localPosition == new Vector3(.3f, 1.05f, .25f), "Basket attach point is untouched");
            var hand = DeepFind(actor.transform, "mixamorig:RightHand");
            check(hand != null, "Mixamo hand bone is addressable");
            var anim = actor.GetComponentInChildren<Animator>(true);
            anim.enabled = true;
            anim.Play("Idle", 0, 0f);
            anim.Update(0f);
            actor.transform.rotation = Quaternion.identity;
            float idleHand = hand.position.y - actor.transform.position.y;
            check(idleHand > .85f && idleHand < 1.15f, "Right hand rides at basket height (" + idleHand.ToString("F2") + ")");
            var thigh = DeepFind(actor.transform, "mixamorig:LeftUpLeg");
            check(thigh != null, "Mixamo leg bone is addressable");
            Quaternion before = thigh.localRotation;
            for (int i = 0; i < 6; i++) shop.Tick(.1f);
            check(thigh.localRotation != before, "Legs swing while walking");
            int cash = shop.Ledger.Cash;
            int stock = shop.StockCount(0) + shop.StockCount(1) + shop.StockCount(2) + shop.StockCount(3);
            for (int n = 0; n < 2000 && shop.ActiveCustomer == null; n++) shop.Tick(.1f);
            check(shop.ActiveCustomer == actor && shop.Stage == ServiceStage.Order, "Marika reaches the order stage");
            check(DeepFind(actor.transform, "MarikaMouthNeutral").gameObject.activeSelf, "Neutral mouth shows on arrival");
            int wrongType = Enumerable.Range(0, 4).First(t => !actor.OrderTypes.Contains(t));
            var wrong = Object.FindObjectsByType<ShopProduct>().First(p => p.ProductId == ShopCatalog.Ids[wrongType] && p.CurrentSlot != null && p.CurrentSlot.IsStockShelf);
            wrong.AssignInitialSlot(shop.DeliverySlot);
            shop.DeliverySlot.NotifyProductPlaced(wrong);
            shop.Tick(.1f);
            check(ShopPeople.IsComplaintLine(actor.Person, shop.Dialogue), "Wrong delivery reads as a complaint line");
            check(DeepFind(actor.transform, "MarikaMouthGrim").gameObject.activeSelf, "Complaint switches the grim mouth");
            // The player would take back the wrong item by hand; the suite models the take-back directly.
            wrong.RemoveFromWorld();
            typeof(MvpCustomerActor).GetProperty("UsesCard").SetValue(actor, true);
            foreach (int type in actor.OrderTypes.Distinct())
            {
                int need = actor.OrderTypes.Count(t => t == type);
                var items = Object.FindObjectsByType<ShopProduct>().Where(p => p.ProductId == ShopCatalog.Ids[type] && p.CurrentSlot != null && p.CurrentSlot.IsStockShelf).Take(need).ToArray();
                check(items.Length == need, "Seeded shelf covers every order line");
                foreach (var item in items)
                {
                    item.AssignInitialSlot(shop.DeliverySlot);
                    shop.DeliverySlot.NotifyProductPlaced(item);
                }
            }
            check(shop.Stage == ServiceStage.Payment, "Full handover reaches payment on the new model");
            shop.Tick(.1f);
            check(!DeepFind(actor.transform, "MarikaMouthGrim").gameObject.activeSelf, "Payment clears the complaint face");
            shop.Ledger.TrySellCard(50000, 0);
            shop.SyncCashDrawer();
            int total = actor.TotalPrice;
            foreach (char digit in total.ToString()) shop.EnterCardDigit(digit - '0');
            check(shop.ConfirmCardPayment(), "Card settles once on the new model");
            shop.Tick(.1f);
            check(shop.Stage == ServiceStage.Thanks && DeepFind(actor.transform, "MarikaMouthSmile").gameObject.activeSelf, "Thanks switches the smile mouth");
            check(shop.Ledger.Cash == cash + 50000 + total, "Sale credits exactly once");
            check(shop.StockCount(0) + shop.StockCount(1) + shop.StockCount(2) + shop.StockCount(3) == stock - actor.OrderTypes.Length - 1, "Stock drops by exactly the sold items plus the taken-back wrong one");
            for (int i = 0; i < 30; i++) shop.Tick(.2f);
            check(shop.ActiveCustomer == null, "Thanks ends and the customer leaves");
            return new { passed, failed = 0 };
        }
        finally
        {
            shop.ResetDemo();
            shop.ManualTick = false;
            for (int i = 0; i < 10; i++) shop.Tick(.2f);
        }
    }
}
