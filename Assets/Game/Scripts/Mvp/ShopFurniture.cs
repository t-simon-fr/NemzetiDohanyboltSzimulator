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
    public string Id { get; private set; }
    public string Title { get; private set; }
    public Vector2 Size { get; private set; }
    public ShopFurniturePose DefaultPose { get; private set; }
    public ShopFurniturePose Pose => new ShopFurniturePose(transform.position, Mathf.RoundToInt(transform.eulerAngles.y / 90));
    public ProductSlot[] Slots { get; private set; }
    private Vector3[] access;

    public void Configure(string id, string title, Vector2 size)
    {
        Id = id;
        Title = title;
        Size = size;
        DefaultPose = Pose;
        Slots = GetComponentsInChildren<ProductSlot>(true);
        access = Slots.Select(s => transform.InverseTransformPoint(s.transform.position - s.transform.forward * .8f))
            .Select(p => new Vector3(p.x, 0, p.z)).Distinct().ToArray();
    }

    public Rect BoundsAt(ShopFurniturePose pose)
    {
        Vector2 size = pose.QuarterTurns % 2 == 0 ? Size : new Vector2(Size.y, Size.x);
        return new Rect(pose.Position.x - size.x * .5f, pose.Position.z - size.y * .5f, size.x, size.y);
    }

    public IEnumerable<Vector3> AccessAt(ShopFurniturePose pose)
    {
        foreach (var point in access)
        {
            Vector3 value = pose.Position + pose.Rotation * point;
            value.y = 0;
            yield return value;
        }
    }

    public void Apply(ShopFurniturePose pose)
    {
        transform.SetPositionAndRotation(pose.Position, pose.Rotation);
        // Products live in the stock container, so re-pose occupied slots explicitly.
        foreach (var slot in Slots)
            if (slot.Occupant != null) slot.Occupant.AssignInitialSlot(slot);
    }
}
