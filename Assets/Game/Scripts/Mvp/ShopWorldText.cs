using UnityEngine;

// Rebind dynamic font atlases after loading an editor-saved scene or entering Play Mode.
[ExecuteAlways]
[RequireComponent(typeof(TextMesh))]
public sealed class ShopWorldText : MonoBehaviour
{
    private void OnEnable()=>GetComponent<MeshRenderer>().sharedMaterial=ProductVisuals.WorldFontMaterial();
}
