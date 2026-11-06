using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class ShopLotteryRetailer
{
    public int Page { get; private set; }
    public bool EditingNumbers { get; private set; }
    private static readonly string[] Pages = { "Sorsjegyek", "Számsorsjáték", "Beváltás", "Elszámolás" };
    public static readonly Rect TerminalBody = new Rect(104,140,1072,478);
    public void SelectPage(int page){Page=Mathf.Clamp(page,0,3);EditingNumbers=false;}
    public List<ShopKeyboardTarget> Controls(Rect r)
    {
        var controls=new List<ShopKeyboardTarget>();
        void Add(string id,string caption,Rect area,Action action,bool enabled=true)
            =>controls.Add(new ShopKeyboardTarget("Lottery:"+id,caption,area,action,enabled));
        for(int i=0;i<Pages.Length;i++)
        {
            int page=i;
            Add("Page:"+i,Pages[i],new Rect(r.x+i*r.width/4,r.y,r.width/4-8,44),()=>SelectPage(page));
        }
        if(!Owned)return controls;
        if(Page==0)
        {
            for(int i=0;i<3;i++)
            {
                int type=13+i;float y=r.y+76+i*90;
                Add("Order:"+type,"+5 készlet",new Rect(r.x+r.width-376,y,170,48),()=>Order(type),!shop.Ledger.Closed&&Book.Stock(type)+Book.Pending(type)<30);
                Add("Take:"+type,type==15?"Számok":"Kiveszem",new Rect(r.x+r.width-194,y,182,48),()=>{
                    if(type==15)SelectPage(1);else Take(type,FindAnyObjectByType<PlayerInteractor>());
                },shop.ActiveCustomer!=null&&shop.ActiveCustomer.Product==type&&shop.Stage==ServiceStage.Order&&Available(type)>0);
            }
        }
        if(Page==1)
        {
            Add("QuickPick","Gépi számok",new Rect(r.x+550,r.y+178,290,54),()=>NumberEntry=string.Join(" ",Book.QuickPick()),shop.ActiveCustomer?.Product==15&&shop.ActiveCustomer.LotteryQuickPick);
            Add("Print","Nyomtatás és felvétel",new Rect(r.x+24,r.y+270,420,52),()=>Take(15,FindAnyObjectByType<PlayerInteractor>()),shop.ActiveCustomer?.Product==15&&shop.Stage==ServiceStage.Order&&Available(15)>0);
        }
        if(Page==2)
        {
            var c=shop.ActiveCustomer;
            bool claim=c!=null&&c.LotteryClaimId!=null&&shop.Stage==ServiceStage.Order;
            Add("Scan","Szelvény beolvasása",new Rect(r.x+24,r.y+132,410,52),()=>ScanCurrent(),claim);
            bool scanned=claim&&ScannedId==c.LotteryClaimId;
            bool winning=scanned&&Book.Scan(ScannedId,shop.Day)==LotteryTicketStatus.Winning;
            Add("Payout","Nyeremény kifizetése",new Rect(r.x+24,r.y+320,410,52),()=>PayPrize(),winning&&shop.Ledger.Cash>=Book.Find(ScannedId).Prize&&!shop.Ledger.Closed);
            Add("Finish","Szelvény visszaadása",new Rect(r.x+454,r.y+320,410,52),FinishCheck,scanned);
        }
        if(Page==3)
            Add("Settle",Book.Outstanding<0?"Jóváírás átvétele":"Egyenleg rendezése",new Rect(r.x+24,r.y+338,420,52),()=>SettleOperator(),Book.Outstanding!=0&&!shop.Ledger.Closed&&(Book.Outstanding<0||shop.Ledger.Cash>=Book.Outstanding));
        return controls;
    }

    public void Draw(Rect r)
    {
        if(!Owned)
        {
            ShopComputerTheme.Label(new Rect(r.x+28,r.y+52,r.width-56,70),"Lottópont az Üzletfejlesztésben · "+ShopCatalog.Money(ShopLotteryCatalog.UpgradePrice),27,true);
            ShopComputerTheme.Label(new Rect(r.x+28,r.y+140,r.width-56,70),ShopLotteryCatalog.Operator+"\nKaparós sorsjegyek, Ötöske és nyereményellenőrzés.",23);
            return;
        }
        if(Page==0)
        {
            for(int i=0;i<3;i++)
            {
                int type=13+i;float y=r.y+72+i*90;
                ShopComputerTheme.Label(new Rect(r.x+24,y,r.width-440,36),ShopCatalog.ShortNames[type]+" · "+ShopCatalog.Money(ShopCatalog.Prices[type]),24,true);
                ShopComputerTheme.Label(new Rect(r.x+24,y+36,r.width-440,32),Book.Stock(type)+" db · úton "+Book.Pending(type)+" · fix ár",19);
            }
            ShopComputerTheme.Label(new Rect(r.x+24,r.y+350,r.width-48,42),"Bizományos készlet · 8 mp szállítás · 10% jutalék · elszámolás eladás után",19);
        }
        if(Page==1)
        {
            var c=shop.ActiveCustomer;
            string request=c?.Product==15?(c.LotteryQuickPick?"A vevő gépi számokat kér.":"Kért számok: "+string.Join(", ",c.LotteryNumbers)):"Várj egy Ötöskét kérő vásárlóra.";
            ShopComputerTheme.Label(new Rect(r.x+24,r.y+68,r.width-48,46),request,25,true);
            ShopComputerTheme.Label(new Rect(r.x+24,r.y+120,r.width-48,38),"Öt különböző szám, 1–20 · elválasztás szóközzel vagy vesszővel",20);
            GUI.SetNextControlName("LotteryNumbers");
            NumberEntry=GUI.TextField(new Rect(r.x+24,r.y+178,500,54),NumberEntry,40,ShopComputerTheme.Field(27));
            EditingNumbers=GUI.GetNameOfFocusedControl()=="LotteryNumbers";
            var draw=Book.DrawNumbers(shop.Day);
            ShopComputerTheme.Label(new Rect(r.x+24,r.y+340,r.width-48,44),draw.Length>0?"Mai sorsolás: "+string.Join(" · ",draw):"Sorsolás a következő játéknapon · nyomtatás után a kasszánál kell fizetni",20);
        }
        if(Page==2)
        {
            var c=shop.ActiveCustomer;
            ShopComputerTheme.Label(new Rect(r.x+24,r.y+68,r.width-48,44),c?.LotteryClaimId!=null?"A vásárló szelvénye: "+c.LotteryClaimId:"Nincs ellenőrzést kérő vásárló.",24,true);
            string result=string.IsNullOrEmpty(ScannedId)?"Helyezd ellenőrzésre a vásárló szelvényét.":StatusText(Book.Scan(ScannedId,shop.Day));
            ShopComputerTheme.Label(new Rect(r.x+24,r.y+204,r.width-48,72),result,25,true);
            ShopComputerTheme.Label(new Rect(r.x+24,r.y+278,r.width-48,32),"Kasszafedezet: "+ShopCatalog.Money(shop.Ledger.Cash)+" · helyi kifizetési limit: "+ShopCatalog.Money(ShopLotteryCatalog.PayoutLimit),19);
        }
        if(Page==3)
        {
            string[] captions={"Mai szelvényeladás","Mai nyereménykifizetés","Mai jutalék","Üzemeltetői egyenleg"};
            int[] values={Book.DailySales,Book.DailyPayouts,Book.DailyCommission,Book.Outstanding};
            for(int i=0;i<4;i++)
            {
                float y=r.y+62+i*54;
                ShopComputerTheme.Label(new Rect(r.x+24,y,530,40),captions[i],23);
                ShopComputerTheme.Label(new Rect(r.x+570,y,r.width-594,40),ShopCatalog.Money(values[i]),25,true);
            }
            ShopComputerTheme.Label(new Rect(r.x+24,r.y+286,r.width-48,42),"Félretéve: "+ShopCatalog.Money(Book.Reserve)+" · elkölthető: "+ShopCatalog.Money(shop.Ledger.AvailableFunds),21,true);
        }
        ShopComputerTheme.Label(new Rect(r.x+24,r.y+402,r.width-48,66),Feedback,19);
    }

    public void DrawTerminal()
    {
        using(ShopUi.Frame())
        {
            ShopUi.Panel(new Rect(80,24,1120,674));
            ShopUi.Fill(new Rect(80,24,1120,92),ShopComputerTheme.Brown);
            ShopComputerTheme.Label(new Rect(104,34,920,42),"LOTTÓPONT",29,true,true);
            ShopComputerTheme.Label(new Rect(104,76,920,30),ShopLotteryCatalog.Operator,20,false,true);
            if(GUI.Button(new Rect(1100,44,76,48),"×",ShopComputerTheme.Button()))shop.ClosePanel();
            Draw(TerminalBody);
            foreach(var control in Controls(TerminalBody))
            {
                GUI.enabled=control.Enabled;
                if(GUI.Button(control.Bounds,control.Label,ShopComputerTheme.Button(control.Id=="Lottery:Page:"+Page,21)))
                {control.Activate();ShopInputGate.ConsumeWorldAction();GUIUtility.ExitGUI();}
                GUI.enabled=true;
            }
            ShopComputerTheme.Label(new Rect(104,656,1072,26),"Esc · vissza a boltba     |     A szelvény csak sikeres fizetés után érvényes.",17);
        }
    }
}
