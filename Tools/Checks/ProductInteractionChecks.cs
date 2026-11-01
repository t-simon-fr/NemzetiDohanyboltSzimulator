using System;
using UnityEngine;

// Runs in the Editor through Unity Pipeline without modifying the game scene.
public static class ProductInteractionChecks
{
    public static object Run()
    {
        int checks = 0;
        GameObject root = new GameObject("Temporary Fixed Slot Checks");
        root.transform.position = new Vector3(1000f, 0f, 1000f);
        try
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(12f, 0.2f, 12f);
            ProductSlot source = MakeSlot(root.transform, Vector3.zero, Quaternion.identity);
            ProductSlot destination = MakeSlot(root.transform, Vector3.right * 2f, Quaternion.Euler(0f, 90f, 0f));
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.transform.SetParent(root.transform, false);
            item.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
            Rigidbody body = item.AddComponent<Rigidbody>();
            ShopProduct product = item.AddComponent<ShopProduct>();
            GameObject child = new GameObject("Disabled extra collider");
            child.transform.SetParent(item.transform, false);
            BoxCollider extra = child.AddComponent<BoxCollider>();
            extra.enabled = false;
            Require(product.AssignInitialSlot(source), "Initial slot assignment", ref checks);
            Require(source.Occupant == product && product.CurrentSlot == source, "Initial occupancy", ref checks);
            Transform hand = new GameObject("Test Hand").transform;
            hand.SetParent(root.transform, false);
            hand.localPosition = Vector3.up * 2f;
            Vector3 size = item.transform.lossyScale;
            Vector3 originalPosition = item.transform.position;
            Require(product.PickUp(hand), "Pickup succeeds", ref checks);
            Require(product.IsHeld && item.transform.parent == hand, "Held parent", ref checks);
            Require(!item.GetComponent<BoxCollider>().enabled && body.isKinematic && !body.useGravity, "Held physics disabled", ref checks);
            Require(!product.PickUp(hand), "Duplicate pickup rejected", ref checks);
            Require(source.Occupant == product, "Origin stays reserved while carrying", ref checks);
            Require(!product.TryGetPlacement(null, out _), "Free placement outside slots rejected", ref checks);
            Physics.SyncTransforms();
            Require(product.TryGetPlacement(destination, out Pose pose), "Empty slot accepts product", ref checks);
            hand.rotation = Quaternion.Euler(30f, 135f, 20f);
            Require(product.TryGetPlacement(destination, out Pose rotatedPose) && Vector3.Distance(pose.position, rotatedPose.position) < 0.001f && Quaternion.Angle(pose.rotation, rotatedPose.rotation) < 0.001f, "Hand rotation cannot affect placement", ref checks);
            Require(Quaternion.Angle(pose.rotation, destination.transform.rotation) < 0.001f, "Slot controls rotation", ref checks);
            GameObject otherObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            otherObject.transform.SetParent(root.transform, false);
            otherObject.transform.localPosition = Vector3.right * -3f + Vector3.up;
            ShopProduct other = otherObject.AddComponent<ShopProduct>();
            Require(!source.TryOccupy(other), "Reserved slot rejects another product", ref checks);
            Require(destination.TryOccupy(other), "Empty slot can be occupied", ref checks);
            Require(!product.TryPlace(destination) && product.IsHeld, "Occupied slot rejects placement without losing held item", ref checks);
            destination.Release(other);
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.SetParent(root.transform, true);
            blocker.transform.position = pose.position;
            blocker.transform.localScale = Vector3.one * 0.5f;
            Physics.SyncTransforms();
            Require(!product.TryPlace(destination), "Physically obstructed slot rejected", ref checks);
            UnityEngine.Object.DestroyImmediate(blocker);
            ProductSlot small = MakeSlot(root.transform, Vector3.right * 4f, Quaternion.identity);
            small.Configure(Vector3.one * 0.05f);
            Physics.SyncTransforms();
            Require(!product.TryGetPlacement(small, out _), "Oversized product rejected", ref checks);
            Require(product.TryPlace(destination), "Placement succeeds", ref checks);
            Require(source.Occupant == null && destination.Occupant == product && product.CurrentSlot == destination, "Occupancy transferred", ref checks);
            Require(Vector3.Distance(item.transform.position, pose.position) < 0.001f && Quaternion.Angle(item.transform.rotation, pose.rotation) < 0.001f, "Exact fixed pose restored", ref checks);
            Require(item.GetComponent<BoxCollider>().enabled && !extra.enabled && body.isKinematic, "Slot keeps physical item stable", ref checks);
            Require(product.PickUp(hand), "Placed product is pickable again", ref checks);
            hand.localPosition += Vector3.right;
            product.ReturnToPickupPosition();
            Require(!product.IsHeld && destination.Occupant == product && Vector3.Distance(item.transform.position, pose.position) < 0.001f, "Cancelled pickup returns to reserved slot", ref checks);
            Require(Vector3.Distance(size, item.transform.lossyScale) < 0.001f, "Repeated movement preserves world size", ref checks);
            Physics.SyncTransforms();
            Require(Physics.Raycast(root.transform.position + new Vector3(0f, 0.2f, -1f), Vector3.forward, out RaycastHit hit, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide) && hit.collider.GetComponent<ProductSlot>() == source, "Empty slot is targetable", ref checks);
            return new { passed = checks, failed = 0 };
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            Physics.SyncTransforms();
        }
    }

    private static ProductSlot MakeSlot(Transform parent, Vector3 position, Quaternion rotation)
    {
        GameObject slot = new GameObject("Test Slot");
        slot.transform.SetParent(parent, false);
        slot.transform.localPosition = position;
        slot.transform.localRotation = rotation;
        ProductSlot result = slot.AddComponent<ProductSlot>();
        result.Configure(new Vector3(0.8f, 0.5f, 0.8f));
        return result;
    }

    private static void Require(bool condition, string message, ref int checks)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++;
    }
}
