using System.Linq;
using UnityEngine;

// Labels describe physical stock, never the shelf's original preferred product.
public sealed class ShopShelfPriceLabel : MonoBehaviour
{
    private TextMesh label;
    private ProductSlot[] slots;
    private float nextRefresh;
    private bool compact;
    private float maximumWidth, originalSize;

    public void Configure(TextMesh target, ProductSlot[] row, bool shortLabel = false, float width = 0)
    {
        label = target;
        maximumWidth = width;
        originalSize = target.characterSize;
        slots = row;
        compact = shortLabel;
        Refresh();
    }

    public void Refresh()
    {
        if (label == null || slots == null) return;
        var types = slots.Where(s => s != null && s.Occupant != null && !s.Occupant.IsHeld)
            .Select(s => ShopCatalog.Index(s.Occupant.ProductId)).Where(type => type >= 0).Distinct().ToArray();
        if (types.Length == 0) { SetText(""); return; }
        if (types.Length == 1)
        {
            int type = types[0];
            SetText((compact ? ShopCatalog.ShortNames[type] : ShopCatalog.Names[type]) + " • " + ShopCatalog.Money(ShopCatalog.SalePrice(type)));
            return;
        }
        SetText((compact ? "" : "VEGYES POLC • ") + string.Join(" / ", types.Select(type => ShopCatalog.ShortNames[type])));
    }

    private void SetText(string value)
    {
        label.text = value;
        if (maximumWidth > 0) ProductVisuals.FitText(label, maximumWidth, originalSize);
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .25f;
        Refresh();
    }
}
