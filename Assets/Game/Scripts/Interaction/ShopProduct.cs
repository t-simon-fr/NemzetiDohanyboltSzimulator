using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class ShopProduct : MonoBehaviour
{
    [SerializeField] private string productId = "sample";
    [SerializeField] private string displayName = "Próbaáru";
    [SerializeField] private ProductSlot currentSlot;
    private BoxCollider productCollider;
    private Collider[] colliders;
    private bool[] colliderStates;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Rigidbody body;
    private bool originalKinematic;
    private bool originalGravity;

    public int UnitCost { get; private set; } = -1;
    public void SetUnitCost(int value) { if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value)); UnitCost = value; }
    public string ProductId => productId;
    public string DisplayName => displayName;
    public bool IsHeld { get; private set; }
    public ProductSlot CurrentSlot => currentSlot;
    public void Configure(string productName) => displayName = productName;
    public void ConfigureProduct(string id, string productName)
    {
        productId = id;
        displayName = productName;
    }

    public void RemoveFromWorld()
    {
        if (currentSlot != null) currentSlot.Release(this);
        currentSlot = null;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public bool AcceptDelivery()
    {
        if (IsHeld || currentSlot == null) return false;
        currentSlot.Release(this);
        currentSlot = null;
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }

    public bool AssignInitialSlot(ProductSlot slot)
    {
        if (IsHeld || slot == null || !slot.TryOccupy(this)) return false;
        if (currentSlot != null && currentSlot != slot) currentSlot.Release(this);
        currentSlot = slot;
        GetShape(out Vector3 halfSize, out Vector3 centerOffset);
        Pose pose = slot.GetPose(halfSize, centerOffset);
        transform.SetPositionAndRotation(pose.position, pose.rotation);
        ShopLoosePhysics.Freeze(gameObject);
        return true;
    }

    public bool PickUp(Transform holdPoint)
    {
        if (IsHeld || holdPoint == null) return false;
        productCollider = GetComponent<BoxCollider>();
        colliders = GetComponentsInChildren<Collider>();
        colliderStates = new bool[colliders.Length];
        originalParent = transform.parent;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        body = GetComponent<Rigidbody>();
        if (body != null)
        {
            originalKinematic = body.isKinematic;
            originalGravity = body.useGravity;
            body.isKinematic = true;
            body.useGravity = false;
        }
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderStates[i] = colliders[i].enabled;
            colliders[i].enabled = false;
        }
        // Keep the source slot reserved until placement, so cancellation is always safe.
        transform.SetParent(holdPoint, true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        IsHeld = true;
        return true;
    }

    public bool TryGetPlacement(ProductSlot slot, out Pose pose)
    {
        pose = default;
        if (!IsHeld || slot == null || !slot.CanAccept(this)) return false;
        GetShape(out Vector3 halfSize, out Vector3 centerOffset);
        if (!slot.Fits(halfSize * 2f)) return false;
        pose = slot.GetPose(halfSize, centerOffset);
        Vector3 center = pose.position + pose.rotation * centerOffset;
        return !Physics.CheckBox(center, halfSize * 0.98f, pose.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    public bool TryPlace(ProductSlot slot)
    {
        if (!TryGetPlacement(slot, out Pose pose) || !slot.TryOccupy(this)) return false;
        if (currentSlot != null && currentSlot != slot) currentSlot.Release(this);
        currentSlot = slot;
        transform.SetParent(slot.transform.parent, true);
        transform.SetPositionAndRotation(pose.position, pose.rotation);
        RestorePhysics();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
        slot.NotifyProductPlaced(this);
        return true;
    }

    public void Drop(Vector3 position,Vector3 velocity)
    {
        if(!IsHeld)return;
        if(currentSlot!=null)currentSlot.Release(this);currentSlot=null;
        RestorePhysics();ShopLoosePhysics.Throw(gameObject,position,velocity);
    }
    public void ReturnToPickupPosition()
    {
        if (!IsHeld) return;
        transform.SetParent(originalParent, true);
        transform.SetPositionAndRotation(originalPosition, originalRotation);
        RestorePhysics();
    }

    private void GetShape(out Vector3 halfSize, out Vector3 centerOffset)
    {
        if (productCollider == null) productCollider = GetComponent<BoxCollider>();
        Vector3 scale = transform.lossyScale;
        Vector3 size = Vector3.Scale(productCollider.size, scale);
        halfSize = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)) * 0.5f;
        centerOffset = Vector3.Scale(productCollider.center, scale);
    }

    private void RestorePhysics()
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = colliderStates[i];
        }
        if (body != null)
        {
            body.isKinematic = originalKinematic;
            body.useGravity = originalGravity;
        }
        IsHeld = false;
    }

    private void OnDestroy()
    {
        if (currentSlot != null) currentSlot.Release(this);
    }
}
