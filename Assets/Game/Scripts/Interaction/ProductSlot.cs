using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class ProductSlot : MonoBehaviour
{
    [SerializeField] private Vector3 capacity = new Vector3(0.65f, 0.38f, 0.65f);
    [SerializeField] private ShopProduct occupant;
    public event System.Action<ShopProduct> ProductPlaced;
    public void NotifyProductPlaced(ShopProduct product) => ProductPlaced?.Invoke(product);
    public ShopProduct Occupant => occupant;
    public bool CanAccept(ShopProduct product) => isActiveAndEnabled && product != null && (occupant == null || occupant == product);

    public void Configure(Vector3 availableSize)
    {
        capacity = availableSize;
        BoxCollider selection = GetComponent<BoxCollider>();
        selection.isTrigger = true;
        selection.size = capacity;
        selection.center = Vector3.up * (capacity.y * 0.5f + 0.015f);
    }

    public bool TryOccupy(ShopProduct product)
    {
        if (!CanAccept(product)) return false;
        occupant = product;
        return true;
    }

    public void Release(ShopProduct product)
    {
        if (occupant == product) occupant = null;
    }

    public bool Fits(Vector3 size) => size.x <= capacity.x && size.y <= capacity.y && size.z <= capacity.z;

    public Pose GetPose(Vector3 halfSize, Vector3 colliderCenterOffset)
    {
        Quaternion rotation = transform.rotation;
        Vector3 center = transform.position + Vector3.up * (halfSize.y + 0.015f);
        return new Pose(center - rotation * colliderCenterOffset, rotation);
    }

    private void Reset() => Configure(capacity);
    private void OnValidate() => Configure(capacity);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = occupant == null ? Color.green : Color.yellow;
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.up * (capacity.y * 0.5f + 0.015f), capacity);
        Gizmos.matrix = previous;
    }
}
