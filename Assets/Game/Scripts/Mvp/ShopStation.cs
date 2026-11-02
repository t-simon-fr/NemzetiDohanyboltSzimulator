using UnityEngine;

public enum ShopStationKind { Till, Supplies, Door, Monitor }
public sealed class ShopStation : MonoBehaviour
{
    [SerializeField] private ShopStationKind kind;
    [SerializeField] private MvpShopController shop;
    public void Configure(MvpShopController controller, ShopStationKind stationKind) { shop = controller; kind = stationKind; }
    public bool IsSupplies => kind == ShopStationKind.Supplies;
    public string Contents
    {
        get
        {
            if (shop == null || !IsSupplies) return "";
            var lines = new System.Text.StringBuilder("SZÁLLÍTÓDOBOZ\n");
            for (int i=0; i<shop.AvailableProducts; i++)
            {
                lines.Append(i==shop.SelectedSupply ? "► " : "   ").Append(ShopCatalog.Names[i]).Append(": ").Append(shop.WarehouseCount(i)).Append(" db");
                if (shop.PendingQuantity(i)>0) lines.Append(" • úton: ").Append(shop.PendingQuantity(i));
                lines.AppendLine();
            }
            return lines.ToString();
        }
    }
    public string Prompt => IsSupplies && shop != null
        ? ShopCatalog.Names[shop.SelectedSupply] + " • " + shop.WarehouseCount(shop.SelectedSupply) + " db\nE – Kivétel  •  Q – Másik termék"
        : kind == ShopStationKind.Monitor ? "E – Rendelések és boltbővítés a monitoron" : kind == ShopStationKind.Till ? "E – Kassza / fizetés" : kind == ShopStationKind.Door ? "E – Nyitás / zárás" : "E – Áru a dobozból  •  Q – Másik termék";
    public void Use(PlayerInteractor player)
    {
        if (shop == null) return;
        if (kind == ShopStationKind.Monitor) shop.OpenSupplier();
        else if (kind == ShopStationKind.Till) shop.OpenCash();
        else if (kind == ShopStationKind.Door) { if (shop.Phase == ShopPhase.Preparing) shop.OpenShop(); else shop.RequestClose(); }
        else if (player.HasHeldProduct) shop.ReturnSupply(player);
        else shop.TakeSupply(player);
    }
    public void Cycle() { if (kind == ShopStationKind.Supplies && shop != null) shop.CycleSupply(); }
}
