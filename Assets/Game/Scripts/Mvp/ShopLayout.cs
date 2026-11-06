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
    public int ShopLevel { get; private set; }
    public int WarehouseLevel { get; private set; }
    public static readonly int[] ShopPrices = { 24000, 38000, 55000 };
    public static readonly int[] WarehousePrices = { 12000, 20000, 32000 };
    public int NextShopPrice => ShopLevel < 3 ? ShopPrices[ShopLevel] : 0;
    public int NextWarehousePrice => WarehouseLevel < 3 ? WarehousePrices[WarehouseLevel] : 0;
    public Rect ShopArea => Rect.MinMaxRect(-2.4f,-4.3f,2.4f+ShopLevel*2,1.8f);
    public Rect WarehouseArea => Rect.MinMaxRect(WarehouseLevel < 2 ? -5.9f : -7.4f,
        WarehouseLevel == 0 ? -1.9f : WarehouseLevel < 3 ? -2.9f : -3.9f,-2.6f,1.9f);
    public void Configure(GameObject wall,GameObject room,ProductSlot[] slots,int[] types)
    { starterWall=wall;extension=room;publicSlots=slots;publicTypes=types;SetExpanded(false); }
    public void SetExpanded(bool expanded)
        => SetLevels(expanded ? 2 : 0, expanded ? WarehouseLevel : 0);
    public void SetLevels(int shopLevel, int warehouseLevel)
    {
        ShopLevel=Mathf.Clamp(shopLevel,0,3); WarehouseLevel=Mathf.Clamp(warehouseLevel,0,3);
        bool expanded=ShopLevel>0;
        Expanded=expanded;GetComponent<ShopNextUpgrades>()?.RefreshLayout();starterWall.SetActive(!expanded);extension.SetActive(expanded);
        // Existing scenes keep the second divider beside the starter partition.
        var sideWall=starterWall.transform.parent.Find("Staff side wall");
        if(sideWall!=null)sideWall.gameObject.SetActive(!expanded);
        GetComponent<ShopExpansionGeometry>()?.Refresh();
    }
    private void Start()
    {
        foreach(var label in GetComponentsInScene<TextMesh>())
        {
            if(label.name.StartsWith("Shelf price ")&&label.transform.parent!=null)
            {
                var parts=label.name.Substring("Shelf price ".Length).Split('-');
                if(parts.Length==2&&int.TryParse(parts[0],out int rowIndex)&&int.TryParse(parts[1],out int lane))
                {
                    var rack=label.transform.parent.parent;
                    var pair=new[]{rack.Find("Cigarette slot "+rowIndex+"-"+(lane*2))?.GetComponent<ProductSlot>(),rack.Find("Cigarette slot "+rowIndex+"-"+(lane*2+1))?.GetComponent<ProductSlot>()};
                    if(pair[0]!=null&&pair[1]!=null)label.gameObject.AddComponent<ShopShelfPriceLabel>().Configure(label,pair,true);
                }
                continue;
            }
            if(label.name!="Árcímke"||label.transform.parent==null)continue;
            var row=System.Array.FindAll(publicSlots,s=>s!=null&&s.transform.parent==label.transform.parent&&Mathf.Abs(s.transform.localPosition.y-label.transform.localPosition.y-.09f)<.02f);
            if(row.Length==0)continue;
            label.gameObject.AddComponent<ShopShelfPriceLabel>().Configure(label,row);
        }
    }
    private static T[] GetComponentsInScene<T>() where T:Component
    {
        var roots=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var found=new System.Collections.Generic.List<T>();foreach(var root in roots)found.AddRange(root.GetComponentsInChildren<T>(true));return found.ToArray();
    }
    public void RegisterPublicSlots(ProductSlot[] slots){publicSlots=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(publicSlots,slots));}
    public bool IsPublicProduct(int type)=>Expanded && type>=0 && (type<ShopCatalog.Count||type==ShopCatalog.Contraband) && (type>=ShopCatalog.CigaretteCount || FindPublicStock(type)!=null);
    public ShopProduct FindPublicStock(int type)
    {
        if(!Expanded||type<0||(type>=ShopCatalog.Count&&type!=ShopCatalog.Contraband))return null;
        for(int i=0;i<publicSlots.Length;i++)
            if(publicSlots[i]!=null && publicSlots[i].isActiveAndEnabled && publicSlots[i].Occupant!=null && publicSlots[i].Occupant.ProductId==ShopCatalog.Ids[type] && !publicSlots[i].Occupant.IsHeld)return publicSlots[i].Occupant;
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
