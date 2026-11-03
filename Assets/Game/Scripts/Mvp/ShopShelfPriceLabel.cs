using System.Linq;
using UnityEngine;

// Shelf prices follow the actual contents when the player rearranges stock.
public sealed class ShopShelfPriceLabel : MonoBehaviour
{
    private TextMesh label;
    private ProductSlot[] slots;
    private float nextRefresh;
    private bool compact;
    public void Configure(TextMesh target,ProductSlot[] row,bool shortLabel=false){label=target;slots=row;compact=shortLabel;Refresh();}
    public void Refresh()
    {
        if(label==null||slots==null)return;
        var products=slots.Where(s=>s!=null&&s.Occupant!=null&&!s.Occupant.IsHeld).Select(s=>s.Occupant.ProductId).Distinct().ToArray();
        if(products.Length==0){label.text=compact?"ÜRES":"SZABAD POLC • BÁRMELY TERMÉK";return;}
        int type=products.Length==1?ShopCatalog.Index(products[0]):-1;
        label.text=type>=0?(compact?"":ShopCatalog.Names[type]+" • ")+ShopCatalog.Money(ShopCatalog.SalePrice(type)):compact?"VEGYES":"VEGYES POLC • NÉZZ A TERMÉKRE AZ ÁRÉRT";
    }
    private void Update(){if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.25f;Refresh();}
}
