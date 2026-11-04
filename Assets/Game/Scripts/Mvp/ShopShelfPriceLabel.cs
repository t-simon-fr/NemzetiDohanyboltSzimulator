using System.Linq;
using UnityEngine;

// Labels describe physical stock, never the shelf's original preferred product.
public sealed class ShopShelfPriceLabel : MonoBehaviour
{
    private TextMesh label;
    private ProductSlot[] slots;
    private float nextRefresh;
    private bool compact;

    public void Configure(TextMesh target, ProductSlot[] row, bool shortLabel = false)
    {
        label = target;
        slots = row;
        compact = shortLabel;
        Refresh();
    }

    public void Refresh()
    {
        if (label == null || slots == null) return;
        var types = slots.Where(s => s != null && s.Occupant != null && !s.Occupant.IsHeld)
            .Select(s => ShopCatalog.Index(s.Occupant.ProductId)).Where(type => type >= 0).Distinct().ToArray();
        if (types.Length == 0) { label.text = ""; return; }
        if (types.Length == 1)
        {
            int type = types[0];
            label.text = (compact ? ShopCatalog.ShortNames[type] : ShopCatalog.Names[type]) + " • " + ShopCatalog.Money(ShopCatalog.SalePrice(type));
            return;
        }
        label.text = (compact ? "" : "VEGYES POLC • ") + string.Join(" / ", types.Select(type => ShopCatalog.ShortNames[type]));
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .25f;
        Refresh();
    }
}
