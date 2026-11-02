using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class ProductSlot : MonoBehaviour
{
    [SerializeField] private Vector3 capacity = new Vector3(0.65f, 0.38f, 0.65f);
    [SerializeField] private ShopProduct occupant;
    [SerializeField] private string allowedProductId = "";
    public void RestrictTo(string id) => allowedProductId = id;
    private LineRenderer placementHint;
    private static Material hintMaterial;
    public Vector3 HintCenter => transform.TransformPoint(Vector3.up * .16f);
    public void ShowPlacementHint(bool show, bool selected, bool valid)
    {
        if (!show) { if (placementHint != null) placementHint.enabled=false; return; }
        if (placementHint == null)
        {
            if (hintMaterial == null) hintMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Placement guide"};
            var go=new GameObject("Placement guide");go.transform.SetParent(transform,false);
            placementHint=go.AddComponent<LineRenderer>();placementHint.sharedMaterial=hintMaterial;
            placementHint.useWorldSpace=false;placementHint.loop=true;placementHint.positionCount=4;
            placementHint.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;placementHint.receiveShadows=false;
            float x=capacity.x*.47f,z=capacity.z*.47f;
            placementHint.SetPositions(new[]{new Vector3(-x,.02f,-z),new Vector3(x,.02f,-z),new Vector3(x,.02f,z),new Vector3(-x,.02f,z)});
        }
        placementHint.enabled=true;placementHint.widthMultiplier=selected?.012f:.005f;
        var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",valid?new Color(.2f,1f,.45f):new Color(1f,.2f,.12f));placementHint.SetPropertyBlock(block);
    }
    public event System.Action<ShopProduct> ProductPlaced;
    public void NotifyProductPlaced(ShopProduct product) => ProductPlaced?.Invoke(product);
    public ShopProduct Occupant => occupant;
    public bool AcceptsType(ShopProduct product) => product != null && (string.IsNullOrEmpty(allowedProductId) || product.ProductId == allowedProductId);
    public bool CanAccept(ShopProduct product) => isActiveAndEnabled && AcceptsType(product) && (occupant == null || occupant == product);

    public bool CanAcceptStock(string id) => isActiveAndEnabled && occupant==null && !string.IsNullOrEmpty(allowedProductId) && allowedProductId==id;
    public bool CanStock(string id,Vector3 size,out Pose pose)
    {
        pose=GetPose(size*.5f,Vector3.zero);
        return CanAcceptStock(id) && Fits(size) && !Physics.CheckBox(pose.position,size*.49f,pose.rotation,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
    }

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
