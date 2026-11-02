using UnityEngine;

public enum ShopStationKind { Till, Supplies, Door, Monitor }
public sealed class ShopStation : MonoBehaviour
{
    [SerializeField] private ShopStationKind kind;
    [SerializeField] private MvpShopController shop;
    public void Configure(MvpShopController controller, ShopStationKind stationKind) { shop = controller; kind = stationKind; }
    public string Prompt => kind == ShopStationKind.Monitor ? "E – Rendelések és boltbővítés a monitoron" : kind == ShopStationKind.Till ? "E – Kassza / fizetés" : kind == ShopStationKind.Door ? "E – Nyitás / zárás" : "E – Áru a dobozból  •  Q – Másik termék";
    public void Use(PlayerInteractor player)
    {
        if (shop == null) return;
        if (kind == ShopStationKind.Monitor) shop.OpenSupplier();
        else if (kind == ShopStationKind.Till) shop.OpenCash();
        else if (kind == ShopStationKind.Door) { if (shop.Phase == ShopPhase.Preparing) shop.OpenShop(); else shop.RequestClose(); }
        else shop.TakeSupply(player);
    }
    public void Cycle() { if (kind == ShopStationKind.Supplies && shop != null) shop.CycleSupply(); }
}
