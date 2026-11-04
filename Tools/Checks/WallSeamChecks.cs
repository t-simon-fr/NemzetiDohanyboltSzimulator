using System;
using UnityEngine;

public static class WallSeamChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool,string> check = (ok,name) => { if (!ok) throw new Exception(name); passed++; };
        var shop = MvpShopController.Instance;
        shop.ClosePanel(); shop.ResetDemo(); shop.ManualTick = true;
        var partition = shop.FloorPlan.Find("Expansion partition").gameObject;
        var side = shop.FloorPlan.Find("Staff side wall").gameObject;
        int cash = shop.Ledger.Cash;
        try
        {
            Physics.SyncTransforms();
            var bounds = partition.GetComponent<Renderer>().bounds;
            float join = bounds.max.z;
            foreach (float height in new[] { .2f, 1.5f, 2.7f })
                foreach (float offset in new[] { -.01f, .02f, .05f, .09f })
                {
                    var origin = new Vector3(bounds.min.x - .4f, height, join + offset);
                    bool hit = Physics.Raycast(origin, Vector3.right, out var wall, .8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    check(hit && (wall.collider.gameObject == partition || wall.collider.gameObject == side), "Closed wall blocks seam ray at " + height + "/" + offset);
                }
            check(Mathf.Abs(side.GetComponent<Renderer>().bounds.max.z - 1.9f) < .001f, "Rear boundary remains fixed");
            shop.Layout.SetExpanded(true); Physics.SyncTransforms();
            check(!partition.activeInHierarchy && !side.activeInHierarchy, "Expansion removes both complete divider sections");
            check(!Physics.Raycast(new Vector3(bounds.min.x-.4f,1.5f,join+.05f),Vector3.right,.8f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore), "Expansion leaves no invisible seam blocker");
            shop.ResetDemo(); Physics.SyncTransforms();
            check(partition.activeInHierarchy && side.activeInHierarchy, "New game restores both wall sections");
            check(Physics.Raycast(new Vector3(bounds.min.x-.4f,1.5f,join+.05f),Vector3.right,out var restored,.8f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore) && restored.collider.gameObject == side, "Restored wall remains gap-free");
            check(shop.Ledger.Cash == cash, "Wall geometry never changes money");
            return new { passed, failed = 0 };
        }
        finally { shop.ResetDemo(); shop.ManualTick = false; }
    }
}
