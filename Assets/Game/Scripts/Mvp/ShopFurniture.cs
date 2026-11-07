using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct ShopFurniturePose
{
    public Vector3 Position;
    public int QuarterTurns;
    public Quaternion Rotation => Quaternion.Euler(0, QuarterTurns * 90, 0);
    public ShopFurniturePose(Vector3 position, int turns)
    {
        Position = position;
        QuarterTurns = (turns % 4 + 4) % 4;
    }
}

public sealed class ShopFurniture : MonoBehaviour
{
    public int CatalogKind { get; set; } = -1;
    public bool InitiallyOwned { get; set; }
    public float Height { get; private set; }
    public string Dimensions => Size.x.ToString("0.##") + " × " + Size.y.ToString("0.##") + " × " + Height.ToString("0.##") + " m";
    public string Id { get; private set; }
    public string Title { get; private set; }
    public Vector2 Size { get; private set; }
    public bool InWarehouse { get; set; }
    public ShopFurniturePose DefaultPose { get; private set; }
    public ShopFurniturePose Pose => new ShopFurniturePose(transform.position, Mathf.RoundToInt(transform.eulerAngles.y / 90));
    public ProductSlot[] Slots { get; private set; }
    private Vector3[] access;
    public float ApproachDistance => CatalogKind == 5 ? 1.2f : .8f;

    public void Configure(string id, string title, Vector2 size)
    {
        Id = id;
        Title = title;
        Size = size;
        DefaultPose = Pose;
        Height = GetComponentsInChildren<Renderer>(true).Select(r => r.bounds.max.y - transform.position.y).DefaultIfEmpty(2f).Max();
        Slots = GetComponentsInChildren<ProductSlot>(true);
        access = Slots.Select(s => transform.InverseTransformPoint(s.transform.position - s.transform.forward * ApproachDistance))
            .Select(p => new Vector3(p.x, 0, p.z)).Distinct().ToArray();
    }

    public Rect BoundsAt(ShopFurniturePose pose)
    {
        Vector2 size = pose.QuarterTurns % 2 == 0 ? Size : new Vector2(Size.y, Size.x);
        return new Rect(pose.Position.x - size.x * .5f, pose.Position.z - size.y * .5f, size.x, size.y);
    }

    public IEnumerable<Vector3> AccessAt(ShopFurniturePose pose)
    {
        if(InWarehouse)yield return pose.Position+pose.Rotation*new Vector3(0,0,-.85f);
        foreach (var point in access)
        {
            yield return ApproachAt(point, pose);
        }
    }

    public Vector3 ApproachAt(Vector3 localPoint, ShopFurniturePose pose)
    {
        Vector3 point = pose.Position + pose.Rotation * localPoint;
        point.y = 0;
        var layout = MvpShopController.Instance?.Layout;
        if (layout == null || InWarehouse || CatalogKind == 5) return point;
        // A customer can reach a corner column slightly sideways. Keep the approach
        // away from a side wall without moving it through the shelf or its back wall.
        var area = layout.ShopArea;
        if (pose.QuarterTurns % 2 == 0) point.x = Mathf.Clamp(point.x, area.xMin + .35f, area.xMax - .35f);
        else point.z = Mathf.Clamp(point.z, area.yMin + .35f, area.yMax - .35f);
        return point;
    }

    public void Apply(ShopFurniturePose pose)
    {
        transform.SetPositionAndRotation(pose.Position, pose.Rotation);
        // Products live in the stock container, so re-pose occupied slots explicitly.
        foreach (var slot in Slots)
            if (slot.Occupant != null) slot.Occupant.AssignInitialSlot(slot);
    }
}
