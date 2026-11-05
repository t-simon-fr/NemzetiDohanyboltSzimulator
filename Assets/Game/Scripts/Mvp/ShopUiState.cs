using System;
using System.Linq;
using UnityEngine;

// Small view model shared by rendering and acceptance checks. No gameplay writes.
public sealed class ShopUiState
{
    private string message;
    private int revision = -1;
    private float messageUntil;
    public bool MovementLearned { get; set; }
    public void Observe(string value, int version, float now)
    {
        if (version == revision) return;
        revision = version;
        message = value;
        messageUntil = now + 6f;
    }
    public string Notice(float now) => now < messageUntil ? message ?? "" : "";

    public static string Clock(float elapsed)
    {
        int minutes = Mathf.Min(23 * 60 + 59, 8 * 60 + Mathf.FloorToInt(Mathf.Max(0, elapsed)));
        return (minutes / 60).ToString("00") + ":" + (minutes % 60).ToString("00");
    }
    public static ShopOrderLine[] Order(MvpShopController shop)
    {
        if (shop.ActiveCustomer == null) return Array.Empty<ShopOrderLine>();
        int[] missing = shop.MissingOrderTypes;
        return shop.ActiveCustomer.OrderTypes.GroupBy(t => t).Select(group => new ShopOrderLine {
            Type = group.Key, Required = group.Count(),
            Delivered = shop.Stage == ServiceStage.Thanks ? group.Count() : group.Count() - missing.Count(t => t == group.Key),
            Unavailable = shop.IsRequestedTypeUnavailable(group.Key)
        }).ToArray();
    }
}

[Serializable]
public struct ShopOrderLine
{
    public int Type, Required, Delivered;
    public bool Unavailable;
    public string Description => ShopCatalog.ShortNames[Type] + "  —  " + Delivered + " / " + Required + " átadva";
}
