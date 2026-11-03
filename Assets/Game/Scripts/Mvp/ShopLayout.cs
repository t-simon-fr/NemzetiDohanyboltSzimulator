using UnityEngine;

public sealed class ShopLayout : MonoBehaviour
{
    public const int ExpansionPrice=24000;
    public const int ExtraShelfPrice=9000;
    [SerializeField] private GameObject extraShelf;
    public bool ExtraShelfOwned { get; private set; }
    public void ConfigureExtraShelf(GameObject shelf){extraShelf=shelf;SetExtraShelf(false);}
    public void SetExtraShelf(bool enabled){ExtraShelfOwned=enabled;if(extraShelf!=null)extraShelf.SetActive(enabled);}
    [SerializeField] private GameObject starterWall;
    [SerializeField] private GameObject extension;
    [SerializeField] private ProductSlot[] publicSlots;
    [SerializeField] private int[] publicTypes;
    public bool Expanded { get; private set; }
    public void Configure(GameObject wall,GameObject room,ProductSlot[] slots,int[] types)
    { starterWall=wall;extension=room;publicSlots=slots;publicTypes=types;SetExpanded(false); }
    public void SetExpanded(bool expanded)
    {
        Expanded=expanded;starterWall.SetActive(!expanded);extension.SetActive(expanded);
        // Existing scenes keep the second divider beside the starter partition.
        var sideWall=starterWall.transform.parent.Find("Staff side wall");
        if(sideWall!=null)sideWall.gameObject.SetActive(!expanded);
    }
    public bool IsPublicProduct(int type)=>Expanded && type>=ShopCatalog.CigaretteCount && type<ShopCatalog.Count;
    public ShopProduct FindPublicStock(int type)
    {
        if(!IsPublicProduct(type))return null;
        for(int i=0;i<publicSlots.Length;i++)
            if(publicTypes[i]==type && publicSlots[i].Occupant!=null && !publicSlots[i].Occupant.IsHeld)return publicSlots[i].Occupant;
        return null;
    }
    [SerializeField] private Transform entryOutside, entryInside, aisleGate, exitDoorway;
    public Vector3 EntryOutside => entryOutside.position;
    public Vector3 EntryInside => entryInside.position;
    public Vector3 ExitDoorway => exitDoorway.position;
    public void ConfigureRoutes(Transform outside, Transform inside, Transform gate, Transform exit)
    { entryOutside=outside; entryInside=inside; aisleGate=gate; exitDoorway=exit; }
    public Vector3 ShelfApproach(ShopProduct product)
    {
        Vector3 point=product.CurrentSlot.transform.position-product.CurrentSlot.transform.forward*.8f;
        point.y=0;return point;
    }
    public Vector3 AisleGate=>aisleGate.position;
}
