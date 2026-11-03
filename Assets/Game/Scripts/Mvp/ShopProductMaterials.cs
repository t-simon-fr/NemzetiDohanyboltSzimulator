using UnityEngine;
public sealed class ShopProductMaterials : MonoBehaviour
{
    public Material[] Materials;
    private void OnDestroy(){if(Materials!=null)foreach(var material in Materials)if(material!=null)Destroy(material);}
}
