using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public static class FloorPlanChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, message) => { if (!ok) throw new Exception(message); passed++; };
        var shop = MvpShopController.Instance;
        var player = Object.FindAnyObjectByType<PlayerInteractor>();
        var body = player.GetComponent<CharacterController>();
        Vector3 original = player.transform.position;
        shop.ManualTick = true;
        try
        {
            shop.ResetDemo();
            check(Vector3.Dot(shop.Counter.transform.forward, Vector3.left) > .99f, "Cashier faces right from left-wall counter");
            Vector3 entrance = shop.Entrance.Position;
            check(entrance.x - .8f > -2.4f && entrance.x + .8f < 2.4f, "Whole entrance stays inside starter floor");
            Vector3 counter = shop.Counter.transform.position;
            shop.OrderSupply(0, 1);
            shop.Tick(8.1f);
            var carton = Object.FindObjectsByType<WarehouseCarton>().First(c => c.Quantity > 0);
            check(player.TryHoldCarton(carton), "Carry actual delivery carton through staff route");
            body.enabled = false;
            player.transform.position = ShopFloorPlan.StaffSpawn;
            body.enabled = true;
            Physics.SyncTransforms();
            foreach (var destination in new[] {
                ShopFloorPlan.StaffExit, new Vector3(-2.05f, 0, 0), new Vector3(-3, 0, 0),
                new Vector3(-2.05f, 0, 0), ShopFloorPlan.StaffExit,
                ShopFloorPlan.CustomerPassage, new Vector3(1.25f, 0, -2.5f),
                ShopFloorPlan.CustomerPassage, ShopFloorPlan.StaffExit, ShopFloorPlan.StaffSpawn })
            {
                for (int i = 0; i < 300; i++)
                {
                    Vector3 delta = destination - player.transform.position;
                    delta.y = 0;
                    if (delta.magnitude < .08f) break;
                    body.Move(Vector3.ClampMagnitude(delta, .04f));
                }
                Vector3 distance = destination - player.transform.position;
                distance.y = 0;
                check(distance.magnitude < .1f, "Carton route reaches " + destination + "; actual=" + player.transform.position + "; nearby=" + string.Join(",", Physics.OverlapBox(player.transform.position + Vector3.up * .9f, new Vector3(.4f,.8f,.4f)).Select(c=>c.name)));
            }
            player.CancelHeld();
            ShopCheckFixtures.Fund(shop, 60000);
            check(shop.TryExpand(), "Buy expansion");
            check(shop.Counter.transform.position == counter && shop.Entrance.Position == entrance,
                "Expansion preserves counter and entrance");
            check(GameObject.Find("Expansion L Counter") == null, "No automatic L-shaped counter");
            return new { passed, failed = 0 };
        }
        finally
        {
            player.CancelHeld();
            body.enabled = false;
            player.transform.position = original;
            body.enabled = true;
            shop.ResetDemo();
            shop.ManualTick = false;
        }
    }
}

