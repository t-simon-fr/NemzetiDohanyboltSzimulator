using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ShopLotteryPaper : MonoBehaviour
{
    public string TicketId { get; internal set; }
}

// Unity adapter: physical stock, customer requests and the existing checkout share the pure book.
public sealed partial class ShopLotteryRetailer : MonoBehaviour
{
    private MvpShopController shop;
    private Transform terminal;
    private GameObject returnedPaper;
    private TextMesh screen, returnedLabel;
    private readonly TextMesh[] stockLabels = new TextMesh[3];
    private readonly ShopRuntimeResources resources = new ShopRuntimeResources();
    private Material dark, paper, gold;
    private Material[] ticketMaterials;
    public ShopLotteryState Book { get; private set; }
    public bool Owned => Book != null && Book.Owned;
    public Transform Terminal => terminal;
    public string ScannedId { get; private set; } = "";
    public string NumberEntry { get; set; } = "";
    public string Feedback { get; private set; } = "Válassz szolgáltatást.";
    public void Configure(MvpShopController owner)
    {
        shop = owner;
        dark = Mat("Lottery charcoal", new Color(.055f,.075f,.07f));
        paper = Mat("Lottery ivory", ShopComputerTheme.Paper);
        gold = Mat("Lottery brass", new Color(.61f,.44f,.15f));
        ticketMaterials = new[] { Mat("Fortuna green",new Color(.13f,.33f,.20f)),
            Mat("Pension burgundy",new Color(.40f,.10f,.12f)), Mat("Numbers navy",new Color(.08f,.16f,.29f)) };
        BuildTerminal();
        ResetLottery();
    }
    private Material Mat(string name, Color color) => resources.Own(new Material(Shader.Find("Universal Render Pipeline/Lit")) {name=name,color=color});
    private GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material material,bool solid=false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=material;
        if(!solid){go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());}
        return go;
    }
    private void BuildTerminal()
    {
        terminal = new GameObject("Purchased Lottery Point").transform;
        terminal.SetParent(shop.Counter.transform,false);
        // A short extension at the stockroom end keeps the register, scanner and handover clear.
        terminal.localPosition=new Vector3(2f,.64f,.01f);
        terminal.localRotation=Quaternion.Euler(0,180,0);
        Box(terminal,"Lottery pedestal",new Vector3(0,-.66f,0),new Vector3(.65f,1.10f,.68f),dark,true);
        Box(terminal,"Lottery worktop",new Vector3(0,-.06f,0),new Vector3(.67f,.08f,.72f),paper,true);
        var body=Box(terminal,"Lottery terminal body",new Vector3(0,.13f,0),new Vector3(.49f,.26f,.28f),dark,true);
        body.AddComponent<ShopStation>().Configure(shop,ShopStationKind.Lottery);
        Box(terminal,"Lottery display",new Vector3(0,.16f,-.147f),new Vector3(.43f,.15f,.012f),paper);
        screen=ProductVisuals.Text(terminal,"Lottery terminal status","LOTTÓPONT",new Vector3(0,.16f,-.155f),Quaternion.identity,.024f,Color.black);
        Box(terminal,"Lottery printer slot",new Vector3(0,.035f,-.15f),new Vector3(.24f,.018f,.015f),gold);
        Box(terminal,"Lottery operator header",new Vector3(0,.59f,-.06f),new Vector3(.52f,.085f,.02f),ticketMaterials[0]);
        ProductVisuals.Text(terminal,"Lottery operator",ShopLotteryCatalog.Operator,new Vector3(0,.59f,-.075f),Quaternion.identity,.022f,ShopComputerTheme.Paper);
        for(int i=0;i<3;i++)
        {
            int type=ShopLotteryCatalog.FirstType+i;
            var card=Box(terminal,"Lottery stock "+type,new Vector3((i-1)*.16f,.40f,-.03f),new Vector3(.145f,.20f,.012f),ticketMaterials[i],true);
            card.AddComponent<ShopStation>().ConfigureLottery(shop,type);
            ProductVisuals.Text(card.transform,"Rack ticket name",i==0?"FALUSI\nFORTUNA":i==1?"NYUGDÍJ\nPLUSZ":"ÖTÖSKE",new Vector3(0,.18f,-.51f),Quaternion.identity,.12f,Color.white);
            stockLabels[i]=ProductVisuals.Text(card.transform,"Rack quantity","",new Vector3(0,-.23f,-.51f),Quaternion.identity,.085f,Color.white);
        }
        returnedPaper=Box(terminal,"Customer lottery receipt",new Vector3(.37f,.08f,0),new Vector3(.15f,.21f,.008f),paper,true);
        returnedPaper.AddComponent<ShopStation>().Configure(shop,ShopStationKind.Lottery);
        returnedLabel=ProductVisuals.Text(returnedPaper.transform,"Returned receipt ID","",new Vector3(0,0,-.51f),Quaternion.identity,.09f,Color.black);
        returnedPaper.SetActive(false);
    }
    public void ResetLottery()
    {
        Book=new ShopLotteryState(UnityEngine.Random.Range(1,int.MaxValue));
        ScannedId="";NumberEntry="";Page=0;Feedback="Válassz szolgáltatást.";
        terminal.gameObject.SetActive(false);returnedPaper.SetActive(false);
        shop.Ledger?.SetLotteryReserve(0);
    }
    public bool TryBuy()
    {
        if(!shop.CanDevelopShop||!Book.TryBuy(()=>shop.Ledger.TryImprove(ShopLotteryCatalog.UpgradePrice)))return false;
        terminal.gameObject.SetActive(true);shop.Navigation?.Rebuild();shop.SyncCashDrawer();Refresh();
        shop.Notify("Lottópont megnyitva. Rendelj bizományos készletet a terminálon vagy a TobaccOS-ban.");return true;
    }
    public void Tick(float dt)
    {
        bool pending = Enumerable.Range(13,3).Any(t=>Book.Pending(t)>0);
        Book.Tick(dt);
        if(pending&&!Enumerable.Range(13,3).Any(t=>Book.Pending(t)>0))
            Feedback="Megérkezett a szelvénykészlet.";
        Refresh();
    }
    public void AdvanceDay(){Book.AdvanceDay(shop.Day);SyncReserve();}
    public void SyncReserve()=>shop.Ledger.SetLotteryReserve(Book.Reserve);
    public int Available(int type) => Owned ? Book.Stock(type)+shop.StockCount(type) : 0;
    public bool Order(int type,int quantity=5)
    {
        if(shop.Ledger.Closed||!Book.Order(type,quantity)){Feedback="Nem rendelhető: zárt elszámolás vagy megtelt készlet.";return false;}
        Feedback="Bizományos készlet úton · 8 mp. Elszámolás eladás után.";return true;
    }
    public int ChooseOrder(int visit)
    {
        if(!Owned)return -1;
        var types=Enumerable.Range(13,3).Where(t=>Available(t)>0).ToArray();
        return types.Length==0?-1:types[(visit/4)%types.Length];
    }
    public bool Take(int type,PlayerInteractor player)
    {
        var customer=shop.ActiveCustomer;
        if(!Owned||shop.Ledger.Closed||player.HasHeldItem||customer==null||shop.Stage!=ServiceStage.Order
            ||customer.OrderTypes.Length!=1||customer.Product!=type)
        {Feedback="A soron lévő vevő által kért szelvényt válaszd, üres kézzel.";shop.Notify(Feedback);return false;}
        int[] numbers=null;
        if(type==15 && (!ShopLotteryCatalog.TryNumbers(NumberEntry,out numbers)
            ||!customer.LotteryQuickPick&&!numbers.SequenceEqual(customer.LotteryNumbers)))
        {Feedback="Adj meg öt különböző számot 1–20 között, a vevő kérése szerint.";return false;}
        var existing=FindObjectsByType<ShopLotteryPaper>().FirstOrDefault(p=>p.gameObject.activeInHierarchy
            &&Book.Find(p.TicketId)?.Type==type&&Book.CanSell(p.TicketId,shop.Day)
            &&(type!=15||Book.Find(p.TicketId).Numbers.SequenceEqual(numbers)));
        if(existing!=null)
        {
            var item=existing.GetComponent<ShopProduct>();
            if(player.TryHold(item)){shop.ClosePanel();return true;}return false;
        }
        var ticket=Book.Print(type,shop.Day,numbers);
        if(ticket==null){Feedback="Elfogyott a szelvény. Rendelj új készletet.";shop.Notify(Feedback);return false;}
        var product=shop.CreateLotteryProduct(type);
        var receipt=product.gameObject.AddComponent<ShopLotteryPaper>();receipt.TicketId=ticket.Id;
        product.transform.position=terminal.TransformPoint(new Vector3(0,.04f,-.24f));
        product.transform.rotation=terminal.rotation;
        ProductVisuals.Text(product.transform,"Ticket ID",ticket.Id,new Vector3(0,-.34f,-.51f),Quaternion.identity,.065f,Color.white);
        if(!player.TryHold(product)){Book.VoidUnpaid(ticket.Id);product.RemoveFromWorld();return false;}
        shop.ClosePanel();shop.Notify("Tedd a szelvényt a vonalkódolvasóra, majd fizettesd ki a kasszánál.");Refresh();return true;
    }
    public void UseRack(int type,PlayerInteractor player)
    {
        if(player.HasHeldProduct)
        {
            var receipt=player.HeldProduct.GetComponent<ShopLotteryPaper>();
            if(receipt!=null&&Book.Find(receipt.TicketId)?.Type==type&&Book.VoidUnpaid(receipt.TicketId))
            {player.RemoveHeld(player.HeldProduct);shop.Notify("A ki nem fizetett szelvény érvénytelenítve, a készlet visszaállt.");}
            return;
        }
        if(type==15){Page=1;Open();}else Take(type,player);
    }
    public void Open(){if(Owned)shop.Devices.Focus(ShopDeviceKind.Lottery);}
    public bool SettleSale(MvpCustomerActor actor,IReadOnlyList<ShopProduct> items,Func<bool> payment)
    {
        if(!ShopLotteryCatalog.IsTicket(actor.Product))return payment();
        var receipt=items.Count==1&&items[0]!=null?items[0].GetComponent<ShopLotteryPaper>():null;
        if(receipt==null||Book.Find(receipt.TicketId)?.Type!=actor.Product)return false;
        bool sold=Book.TrySell(receipt.TicketId,actor.Person,shop.Day,payment);
        if(sold)SyncReserve();return sold;
    }
    public void CancelVisit(MvpCustomerActor actor)
    {
        returnedPaper.SetActive(false);ScannedId="";
        if(actor==null||!ShopLotteryCatalog.IsTicket(actor.Product))return;
        foreach(var receipt in FindObjectsByType<ShopLotteryPaper>())
        {
            var t=Book.Find(receipt.TicketId);
            if(t==null||t.Type!=actor.Product||t.Sold||t.Voided)continue;
            var player=FindAnyObjectByType<PlayerInteractor>();
            if(player.HeldProduct==receipt.GetComponent<ShopProduct>())player.CancelHeld();
            Book.VoidUnpaid(t.Id);receipt.GetComponent<ShopProduct>().RemoveFromWorld();
        }
    }
    public string Arrive(MvpCustomerActor actor)
    {
        ScannedId="";Feedback="Válassz szolgáltatást.";
        if(actor.LotteryClaimId!=null)
        {
            returnedPaper.SetActive(true);returnedLabel.text="SZELVÉNY\n"+actor.LotteryClaimId+"\nELLENŐRZÉS";Page=2;
            return "Ellenőrizné ezt a szelvényt? "+actor.LotteryClaimId+". Már előre elköltöttem, csak még nem tudom, mennyit.";
        }
        if(actor.Product==15)
        {
            NumberEntry="";Page=1;
            return actor.LotteryQuickPick?"Egy Ötöskét kérek gépi számokkal. A gép legalább nem a születésnapomra gondol.":
                "Egy Ötöskét kérek: "+string.Join(", ",actor.LotteryNumbers)+". Ugyanaz az öt szám, csak nehogy felismerjék.";
        }
        if(ShopLotteryCatalog.IsTicket(actor.Product))return "Egy "+ShopCatalog.ShortNames[actor.Product]+" sorsjegyet kérek. Most már statisztikailag muszáj lenne.";
        return ShopPeople.Order(actor.Person,actor.OrderTypes,actor.Visit,shop.Day);
    }
    public bool ScanCurrent()
    {
        var c=shop.ActiveCustomer;
        if(c==null||c.LotteryClaimId==null||shop.Stage!=ServiceStage.Order)return false;
        ScannedId=c.LotteryClaimId;Feedback=StatusText(Book.Scan(ScannedId,shop.Day));Refresh();return true;
    }
    public bool PayPrize()
    {
        var c=shop.ActiveCustomer;
        if(c==null||c.LotteryClaimId!=ScannedId||shop.Stage!=ServiceStage.Order)return false;
        if(!Book.TryRedeem(ScannedId,shop.Day,amount=>shop.Ledger.TryOperatorTransfer(-amount)))
        {Feedback=Book.Scan(ScannedId,shop.Day)==LotteryTicketStatus.Winning?"Nincs elég készpénz a kasszában. A szelvény beváltatlan maradt.":"Ez a szelvény nem fizethető ki.";return false;}
        SyncReserve();shop.SyncCashDrawer();Feedback="A nyereményt kifizettük.";
        shop.FinishLotteryVisit("Kifizetve: "+ShopCatalog.Money(Book.Find(ScannedId).Prize)+". Most már csak értelmesen kellene elkölteni.");return true;
    }
    public void FinishCheck()
    {
        var c=shop.ActiveCustomer;
        if(c==null||c.LotteryClaimId!=ScannedId||shop.Stage!=ServiceStage.Order)return;
        Book.FinishReturn(ScannedId,shop.Day);
        shop.FinishLotteryVisit(StatusText(Book.Scan(ScannedId,shop.Day)));
    }
    public bool SettleOperator()
    {
        if(shop.Ledger.Closed||!Book.TrySettle(delta=>shop.Ledger.TryOperatorTransfer(delta)))
        {Feedback="Nincs rendezendő egyenleg, vagy nincs elegendő kasszafedezet.";return false;}
        SyncReserve();shop.SyncCashDrawer();Feedback="Az üzemeltetői egyenleg rendezve.";return true;
    }
    public string StatusText(LotteryTicketStatus status)
    {
        switch(status)
        {
            case LotteryTicketStatus.Unpaid:return "ÉRVÉNYTELEN · A szelvényt nem fizették ki.";
            case LotteryTicketStatus.AwaitingDraw:return "MÉG NINCS SORSOLÁS · Gyere vissza a sorsolás után.";
            case LotteryTicketStatus.Expired:return "LEJÁRT · A játékbeli beváltási idő 7 nap.";
            case LotteryTicketStatus.Redeemed:return "MÁR BEVÁLTVA · Újabb kifizetés nem lehetséges.";
            case LotteryTicketStatus.Losing:return "NEM NYERT · Ezúttal a remény maradt.";
            case LotteryTicketStatus.Winning:return "NYERT · "+ShopCatalog.Money(Book.Find(ScannedId)?.Prize??0);
            case LotteryTicketStatus.OperatorPayout:return "NAGYNYEREMÉNY · Ügyintézés a Szerecsenjáték Zrt.-nél.";
            default:return "ÉRVÉNYTELEN · Ismeretlen szelvény.";
        }
    }
    private void Refresh()
    {
        if(!Owned)return;
        for(int i=0;i<3;i++)stockLabels[i].text=Book.Stock(13+i)+" DB\n"+ShopCatalog.Money(ShopCatalog.Prices[13+i]);
        screen.text=string.IsNullOrEmpty(ScannedId)?"LOTTÓPONT\nE / BAL KATTINTÁS":"SZELVÉNY\n"+ScannedId;
    }
    public void AddTicketVisual(GameObject root,int type)
    {
        root.GetComponent<Renderer>().sharedMaterial=ticketMaterials[ShopLotteryCatalog.Index(type)];
        string title=type==13?"FALUSI\nFORTUNA":type==14?"NYUGDÍJ\nPLUSZ":"ÖTÖSKE\n5 / 20";
        ProductVisuals.Text(root.transform,"Ticket title",title+"\n"+ShopCatalog.Money(ShopCatalog.Prices[type]),new Vector3(0,.06f,-.51f),Quaternion.identity,.13f,Color.white);
    }
    public void Focus(Camera view)
    {
        var center=terminal.TransformPoint(new Vector3(0,.25f,-.05f));
        view.transform.position=center-terminal.forward*1.1f+terminal.up*.2f;view.transform.LookAt(center,terminal.up);
    }
    private void OnDestroy()=>resources.Dispose();
}
