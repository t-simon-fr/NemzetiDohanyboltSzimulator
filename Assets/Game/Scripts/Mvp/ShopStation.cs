using UnityEngine;

public enum ShopStationKind { Till, Supplies, Door, Monitor }
public sealed class ShopStation : MonoBehaviour
{
    [SerializeField] private ShopStationKind kind;
    [SerializeField] private MvpShopController shop;
    [SerializeField] private int productType=-1;
    public int ProductType=>productType;
    public void Configure(MvpShopController controller,ShopStationKind stationKind){shop=controller;kind=stationKind;}
    public void ConfigureBrand(MvpShopController controller,int type){Configure(controller,ShopStationKind.Supplies);productType=type;}
    public bool IsSupplies=>kind==ShopStationKind.Supplies;
    public string Contents
    {
        get
        {
            if(shop==null||!IsSupplies)return "";
            var lines=new System.Text.StringBuilder("RAKTÁR\n");
            var ownCarton=GetComponent<WarehouseCarton>();
            for(int i=0;i<shop.AvailableProducts;i++)
            {
                if(productType>=0&&i!=productType)continue;
                lines.Append(i==productType?"► ":"   ").Append(ShopCatalog.Names[i]).Append(": ").Append(ownCarton!=null ? ownCarton.Quantity : shop.WarehouseCount(i)).Append(" db");
                if(shop.PendingQuantity(i)>0)lines.Append(" • úton: ").Append(shop.PendingQuantity(i));
                lines.AppendLine();
            }
            if(GetComponent<WarehouseCarton>()!=null&&GetComponent<WarehouseCarton>().Quantity==0)lines.Append("Üres karton: E – felvétel\nHátul a papírgyűjtőbe viheted.");
            return lines.ToString();
        }
    }
    public string Prompt
    {
        get
        {
            if(kind==ShopStationKind.Monitor)return "E – Rendelések és boltbővítés a monitoron";
            if(kind==ShopStationKind.Till)return "E – Kassza / fizetés";
            if(kind==ShopStationKind.Door)return "E – Nyitás / zárás";
            var carton=GetComponent<WarehouseCarton>();
            if(carton!=null&&carton.Quantity==0)return "Üres "+ShopCatalog.Names[productType]+" karton\nE – Felvétel és kivitel a papírgyűjtőhöz";
            int type=productType>=0?productType:shop.SelectedSupply;
            return ShopCatalog.Names[type]+" • "+shop.WarehouseCount(type)+" db\nE – Termék kivétele / visszarakása";
        }
    }
    public void Use(PlayerInteractor player)
    {
        if(shop==null)return;
        if(kind==ShopStationKind.Monitor)shop.OpenSupplier();
        else if(kind==ShopStationKind.Till)shop.OpenCash();
        else if(kind==ShopStationKind.Door){if(shop.Phase==ShopPhase.Preparing)shop.OpenShop();else shop.RequestClose();}
        else if(player.HeldCarton!=null)
        {
            if(productType<0 || player.HeldCarton.ProductType==productType)player.ReturnCarton();
            else shop.Notify("Ez másik márka dobozhelye. Keresd a "+ShopCatalog.Names[player.HeldCarton.ProductType]+" feliratot.");
        }
        else if(player.HasHeldProduct)shop.ReturnSupply(player,productType);
        else
        {
            var carton=GetComponent<WarehouseCarton>();
            if(carton!=null&&carton.Quantity==0)player.TryHoldCarton(carton);
            else shop.TakeSupply(player,productType);
        }
    }
    public void Cycle(){if(IsSupplies&&productType<0)shop.CycleSupply();}
}
