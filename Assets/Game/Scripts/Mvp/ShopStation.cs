using UnityEngine;

public enum ShopStationKind { Till, Supplies, Door, Monitor, PanicButton, Smuggler, Exit, Terminal, CoffeeControl, CustomerHand, Lottery }
public sealed class ShopStation : MonoBehaviour
{
    [SerializeField] private ShopStationKind kind;
    [SerializeField] private MvpShopController shop;
    [SerializeField] private int productType=-1;
    public int ProductType=>productType;
    public void Configure(MvpShopController controller,ShopStationKind stationKind){shop=controller;kind=stationKind;}
    public void ConfigureBrand(MvpShopController controller,int type){Configure(controller,ShopStationKind.Supplies);productType=type;}
    public void ConfigureLottery(MvpShopController controller,int type){Configure(controller,ShopStationKind.Lottery);productType=type;}
    public bool IsLottery=>kind==ShopStationKind.Lottery;
    public bool IsSupplies=>kind==ShopStationKind.Supplies;
    public bool IsPanicButton=>kind==ShopStationKind.PanicButton;
    public bool IsCoffeeControl=>kind==ShopStationKind.CoffeeControl;
    public bool IsExit=>kind==ShopStationKind.Exit;
    private WarehouseCarton SupplyCarton => GetComponent<WarehouseCarton>() ?? shop.Warehouse.CartonAt(transform);
    private Transform StorageDock => shop.Warehouse.IsStorageDock(transform) ? transform :
        GetComponent<WarehouseCarton>() != null && shop.Warehouse.IsStorageDock(transform.parent) ? transform.parent : null;
    public string CartonPlacementPrompt => StorageDock != null && shop.Warehouse.CartonAt(StorageDock) == null
        ? "E / bal kattintás – Doboz lerakása az üres raktári helyre" : "Ez a raktári hely foglalt";
    public string Prompt
    {
        get
        {
            if(IsLottery)return productType>=13?"E / bal kattintás – "+ShopCatalog.ShortNames[productType]+" · felvétel / visszaadás":"E / bal kattintás – Lottópont";
            if(kind==ShopStationKind.Smuggler)return "E – Beszélgetés a gyanús alakkal";
            if(IsPanicButton)return shop.Security.Prompt;
            if(kind==ShopStationKind.Monitor)return "E / B – Számítógép: rendelések és berendezés";
            if(kind==ShopStationKind.Terminal)return "E – Bankkártyaterminál";
            if(IsCoffeeControl)return "E – Kávégép kijelzője: italválasztás és főzés";
            if(kind==ShopStationKind.CustomerHand)return "E / bal kattintás – Aprópénz kiválogatása";
            if(kind==ShopStationKind.Till)return "E – Kassza / fizetés";
            if(kind==ShopStationKind.Exit)return shop.CanEndDay ? "E – Hazamegyek: nap lezárása" : "E – Bejárati ajtó";
            if(kind==ShopStationKind.Door)return shop.Phase==ShopPhase.Preparing ? "E – Tábla megfordítása: NYITVA" : shop.Phase==ShopPhase.Open ? "E – Tábla megfordítása: ZÁRVA" : "ZÁRVA • A napot az ajtónál fejezheted be";
            var carton = SupplyCarton;
            if (carton == null) return "";
            return carton.Quantity > 0 ? "E – Doboz felvétele" : "E – Üres kartondoboz felvétele";
        }
    }
    public void Use(PlayerInteractor player)
    {
        if(shop==null)return;
        if(IsLottery){if(productType>=13)shop.Lottery.UseRack(productType,player);else shop.Lottery.Open();}
        else if(kind==ShopStationKind.Smuggler)shop.AfterHours.OpenTrade();
        else if(IsPanicButton)shop.Security.Toggle();
        else if(kind==ShopStationKind.Monitor)shop.OpenSupplier();
        else if(kind==ShopStationKind.Terminal)shop.Devices.Focus(ShopDeviceKind.Terminal);
        else if(IsCoffeeControl)shop.Devices.Focus(ShopDeviceKind.Coffee);
        else if(kind==ShopStationKind.CustomerHand)
        {
            if (!isActiveAndEnabled || player.HasHeldItem || !shop.CoinPayment ||
                shop.Stage != ServiceStage.Payment || shop.Activities.CustomerHand != transform) return;
            ShopInputGate.ConsumeWorldAction();
            shop.OpenCash();
        }
        else if(kind==ShopStationKind.Till)shop.OpenCash();
        else if(kind==ShopStationKind.Exit)shop.TryEndDay();
        else if(kind==ShopStationKind.Door){if(shop.Phase==ShopPhase.Preparing)shop.OpenShop();else shop.RequestClose();}
        else if(player.HeldCarton!=null)
        {
            if (!player.TryPlaceCarton(StorageDock)) shop.Notify("Ez a raktári hely foglalt. Keress egy üres helyet.");
        }
        else if(player.HasHeldProduct)
        {
            var carton = SupplyCarton;
            if (carton != null) shop.ReturnSupply(player, carton.ProductType, carton);
            else shop.Notify("Ide kartondobozt tehetsz. Az egyedi árut a saját termék dobozába rakhatod vissza.");
        }
        else
        {
            var carton=SupplyCarton;
            if(carton!=null)player.TryHoldCarton(carton);
            else shop.Notify("Ez a raktári hely üres. Ide bármelyik kartondobozt leteheted.");
        }
    }
    public void Cycle(){if(IsSupplies&&productType<0)shop.CycleSupply();}
}
