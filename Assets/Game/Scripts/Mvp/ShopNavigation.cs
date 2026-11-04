using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ShopNavigation : MonoBehaviour
{
    public const float Clearance = .29f;
    private MvpShopController shop;
    private ShopRouteGrid grid;
    public int Version { get; private set; }

    public void Configure(MvpShopController owner) { shop = owner; Rebuild(); }
    public void Rebuild()
    {
        if (shop == null) return;
        grid = Build(shop.Arrangement == null ? new Dictionary<ShopFurniture, ShopFurniturePose>() : shop.Arrangement.Current());
        Version++;
    }
    public bool TryPath(Vector3 from, Vector3 to, out List<Vector3> path) => grid.TryPath(from, to, out path);

    public List<Rect> StaticObstacles(bool placement = false)
    {
        Physics.SyncTransforms();
        var result = new List<Rect>();
        foreach (var collider in FindObjectsByType<Collider>())
        {
            if (!collider.enabled || collider.isTrigger || collider == shop.Entrance.PlayerBoundary ||
                collider == shop.Security.EntranceCollider || collider.gameObject.layer == 2) continue;
            if (collider.transform.IsChildOf(shop.Entrance.DoorLeaf)) continue;
            if (collider.GetComponentInParent<MvpCustomerActor>() != null || collider.GetComponentInParent<ShopHandTool>() != null ||
                collider.GetComponentInParent<WarehouseCarton>() != null || collider.GetComponentInParent<ShopFurniture>() != null) continue;
            var product = collider.GetComponentInParent<ShopProduct>();
            if (product != null && (!placement || product.CurrentSlot != null)) continue;
            var feature = collider.GetComponentInParent<ShopFeatureTarget>();
            if (feature != null && feature.Kind == ShopFeatureKind.Spill) continue;
            Bounds bounds = collider.bounds;
            if (bounds.max.y < .14f || bounds.min.y > 1.58f) continue;
            result.Add(Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z));
        }
        return result;
    }

    public ShopRouteGrid Build(Dictionary<ShopFurniture, ShopFurniturePose> poses)
    {
        var obstacles = StaticObstacles();
        obstacles.AddRange(poses.Select(p => p.Key.BoundsAt(p.Value)));
        bool expanded = shop.Layout.Expanded;
        return new ShopRouteGrid(Rect.MinMaxRect(-2.25f, -5.25f, expanded ? 6.25f : 2.25f, 1.5f), point =>
        {
            // Only the entrance corridor extends beyond the purchased customer floor.
            if (point.z < -4.05f)
            {
                if (Mathf.Abs(point.x - ShopFloorPlan.EntranceX) > .45f) return false;
            }
            else if (point.x < .45f) return false; // staff aisle is never an NPC shortcut
            return !obstacles.Any(r => Expanded(r, Clearance).Contains(new Vector2(point.x, point.z)));
        });
    }

    public static Rect Expanded(Rect rect, float margin) => Rect.MinMaxRect(rect.xMin - margin, rect.yMin - margin, rect.xMax + margin, rect.yMax + margin);
}
