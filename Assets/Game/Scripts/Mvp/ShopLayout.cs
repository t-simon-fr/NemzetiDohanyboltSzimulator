using UnityEngine;

public sealed class ShopLayout : MonoBehaviour
{
    public const int ExpansionPrice=2000;
    [SerializeField] private GameObject starterWall;
    [SerializeField] private GameObject extension;
    [SerializeField] private ProductSlot[] publicSlots;
    [SerializeField] private int[] publicTypes;
    public bool Expanded { get; private set; }
    public void Configure(GameObject wall,GameObject room,ProductSlot[] slots,int[] types)
    { starterWall=wall;extension=room;publicSlots=slots;publicTypes=types;SetExpanded(false); }
    public void SetExpanded(bool expanded)
    { Expanded=expanded;starterWall.SetActive(!expanded);extension.SetActive(expanded); }
    public bool IsPublicProduct(int type)=>Expanded && type>=3;
    public ShopProduct FindPublicStock(int type)
    {
        if(!IsPublicProduct(type))return null;
        for(int i=0;i<publicSlots.Length;i++)
            if(publicTypes[i]==type && publicSlots[i].Occupant!=null && !publicSlots[i].Occupant.IsHeld)return publicSlots[i].Occupant;
        return null;
    }
    public Vector3 ShelfApproach(ShopProduct product)=>new Vector3(5.1f,0,product.CurrentSlot.transform.position.z);
    public Vector3 AisleGate=>new Vector3(3.2f,0,-3.25f);
}
