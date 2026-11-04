using UnityEngine;

public sealed class MvpShopHud : MonoBehaviour
{
    [SerializeField] private MvpShopController shop;
    private GUIStyle title,text,small,button;
    private Vector2 summaryScroll;
    public static Rect SummaryDetailsRect=>new Rect(240,120,800,354);
    public static Rect SummaryActionsRect=>new Rect(240,486,800,158);
    public void Configure(MvpShopController controller)=>shop=controller;
    private void OnGUI()
    {
        if(shop==null||shop.Ledger==null||ShopMenu.Active)return;
        if(shop.AfterHours!=null&&shop.AfterHours.TradeOpen)return;
        if(title==null)
        {
            title=new GUIStyle(GUI.skin.label){fontSize=24,fontStyle=FontStyle.Bold,wordWrap=true};
            text=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true};small=new GUIStyle(text){fontSize=15};
            button=new GUIStyle(GUI.skin.button){fontSize=18,wordWrap=true};
            title.normal.textColor=text.normal.textColor=small.normal.textColor=Color.white;
        }
        var old=GUI.matrix;var color=GUI.color;
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)*.5f,(Screen.height-720*scale)*.5f),Quaternion.identity,Vector3.one*scale);
        try
        {
            if(shop.Devices!=null&&shop.Devices.Focused)
            {
                if(shop.CardPayment){CardTerminal();return;}
                if(shop.Devices.FocusKind==ShopDeviceKind.Monitor||shop.Devices.FocusKind==ShopDeviceKind.Coins)return;
                Panel(new Rect(250,612,780,94));GUILayout.BeginArea(new Rect(266,622,748,78));
                GUILayout.Label("Enter: fizetés • Backspace: utolsó darab vissza • Del: törlés • Esc: kilépés",small);
                GUILayout.Label(shop.Message,small);GUILayout.EndArea();return;
            }
            if(shop.Modal==ShopModal.None){Game();return;}
            Panel(new Rect(208,48,864,624));
            if(shop.Modal==ShopModal.Summary){Summary();return;}
            GUILayout.BeginArea(new Rect(240,72,800,572));
            switch(shop.Modal)
            {
                case ShopModal.Help:Help();break;
                case ShopModal.Summary:Summary();break;
            }
            GUILayout.EndArea();
        }
        finally{GUI.matrix=old;GUI.color=color;}
    }
    private void Panel(Rect r){var c=GUI.color;GUI.color=new Color(.035f,.055f,.07f,.91f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=c;}
    private void Label(string s,bool heading=false)=>GUILayout.Label(s,heading?title:text);
    private bool Button(string s)=>GUILayout.Button(s,button,GUILayout.Height(40));
    private void Game()
    {
        Panel(new Rect(12,12,295,156));GUILayout.BeginArea(new Rect(26,20,268,140));
        Label(shop.Day+". nap • "+(shop.Phase==ShopPhase.Preparing?"Felkészülés":shop.Phase==ShopPhase.Open?"Nyitva":shop.Phase==ShopPhase.AfterHours?"Zárás után":"Zárunk"),true);
        Label("Kassza: "+ShopCatalog.Money(shop.Ledger.Cash));

        GUILayout.Label("Eladások: "+shop.Ledger.Sales+" • Sorban: "+shop.QueueCount+" • Érkezett: "+shop.Spawned+"/"+shop.TotalVisitors,small);
        GUILayout.Label(shop.Phase==ShopPhase.Closing||shop.Phase==ShopPhase.AfterHours?"ZÁRVA • E az ajtónál: nap lezárása":shop.Security!=null&&shop.Security.Locked?"BEJÁRAT RETESZELVE • E a vészgombon":"B Számítógép • H Súgó • E a nyitvatartási táblán",small);GUILayout.EndArea();
        if(shop.ActiveCustomer!=null)
        {
            var c=shop.ActiveCustomer;Panel(new Rect(844,12,424,216));GUILayout.BeginArea(new Rect(860,24,392,190));
            Label(ShopPeople.Names[c.Person],true);Label(shop.Stage==ServiceStage.Order?"Jó napot! "+c.OrderDescription+" lenne, kérem.":shop.Dialogue);
            GUILayout.Label("Ár: "+ShopCatalog.Money(c.TotalPrice),small);
            if(shop.Stage==ServiceStage.Order)Label("Még szükséges: "+string.Join(", ",System.Array.ConvertAll(shop.MissingOrderTypes,t=>ShopCatalog.ShortNames[t])));
            GUILayout.Label(shop.Stage==ServiceStage.Order?(shop.HasUnavailableRequest?"T: Másik árut ajánlok • X: Nem tudom kiszolgálni":"A kért árut az átadóhelyre tedd. X: Nem tudom kiszolgálni"):shop.Stage==ServiceStage.Payment?"E a kasszánál vagy F: visszajáró kiadása.":"A vásárló elköszön.",small);GUILayout.EndArea();
        }
        if(!string.IsNullOrEmpty(shop.Message))
        {
            Panel(new Rect(844,240,424,120));GUILayout.BeginArea(new Rect(860,250,392,100));
            GUILayout.Label(shop.Message,small);
            if(shop.NextDelivery>0)GUILayout.Label("Szállítás: "+Mathf.CeilToInt(shop.NextDelivery)+" mp",small);
            GUILayout.EndArea();
        }
    }
    private void Help()
    {
        Label("Műszaksegéd",true);
        Label("WASD + egér: mozgás és körülnézés\nE: termék vagy állomás használata\nE a monitoron / B a pult közelében: számítógép, alkalmazások\nE a kasszán / F: fizetés a pénztárgépen\nRaktári polcnál E: teljes doboz felvétele / visszarakása\nDobozzal a kézben, zöld polchelynél E: egy termék kipakolása\nÜres kartonon E: felvétel • Papírgyűjtőn E: kidobás\nE a táblán: nyitás / zárás • E az ajtónál: nap lezárása\nT: helyettesítő áru ajánlása • X: aktuális vásárló elküldése\nH: súgó • Escape: vissza a boltba / kurzor elengedése");
        GUILayout.Space(14);Label("Kassza: 1–6 bankjegy, Shift+1–6 érme; Enter fizetés, Backspace vissza, Del törlés. Tab / nyilak: választás; Space: címlet vagy érme kiválasztása. Monitor: Tab / nyilak, Enter; Backspace vissza; Asztal 1–5 alkalmazás; rendelésnél 1–9 termék, + / − mennyiség. A Váltás gomb nagy címletet vált fel.");
        GUILayout.Space(14);Label("Az Üzletfejlesztés alkalmazásban nyitás előtt 24 000 Ft-ért bővíthetsz, 9000 Ft-ért cigarettapolcot, 16 000 Ft-ért kávégépet vehetsz. A kamerák és játékok használata közben a bolt tovább működik. G: kézben tartott tárgy eldobása; felmosóval és ütővel bal egérgomb: használat. A készletet neked kell feltöltened az új polcokon is.");
        GUILayout.FlexibleSpace();if(Button("Vissza a boltba"))shop.ClosePanel();
    }
    private void Summary()
    {
        GUILayout.BeginArea(new Rect(240,72,800,42));Label(shop.Day+". nap • Zárás",true);GUILayout.EndArea();
        GUILayout.BeginArea(SummaryDetailsRect);summaryScroll=GUILayout.BeginScrollView(summaryScroll);
        Label("Kiszolgált vásárlók: "+shop.Ledger.Sales+" / "+shop.Spawned+" • Elküldve: "+shop.Missed+" • Ár miatt: "+shop.PriceRefusals);
        GUILayout.Space(10);Label("Árbevétel: "+ShopCatalog.Money(shop.Ledger.Revenue));
        Label("Lopásból eredő készletveszteség: "+ShopCatalog.Money(shop.Ledger.StockLoss)+" • Megállítva: "+shop.CaughtToday);
        Label("Eladott áruk beszerzési értéke: "+ShopCatalog.Money(shop.Ledger.CostOfGoods));
        Label("Működési költség: "+ShopCatalog.Money(shop.Ledger.OperatingCosts));
        Label("Nyereség: "+ShopCatalog.Money(shop.Ledger.Profit),true);
        Label("Utánrendelés: "+ShopCatalog.Money(shop.Ledger.Purchases)+" • Beruházás: "+ShopCatalog.Money(shop.Ledger.Improvements));
        Label("Zárókassza: "+ShopCatalog.Money(shop.Drawer.Total),true);
        Label("Ebből kártyás bevétel: "+ShopCatalog.Money(shop.Ledger.CardRevenue));
        if(shop.Ledger.Cash<0)Label("Ki nem fizetett működési költség: "+ShopCatalog.Money(-shop.Ledger.Cash)+". A folytatáshoz indíts új játékot.");GUILayout.Space(12);
        for(int i=0;i<shop.AvailableProducts;i++)GUILayout.Label(ShopCatalog.Names[i]+": "+shop.StockCount(i)+" a boltban, "+shop.WarehouseCount(i)+" a dobozban",small);
        if(shop.StockCount(ShopCatalog.Contraband)>0)GUILayout.Label("Dnyipro: "+shop.StockCount(ShopCatalog.Contraband)+" csomag",small);
        GUILayout.Label("A készlet, a pénzek és a bővítés a futó játék következő napjára megmaradnak.",small);
        GUILayout.EndScrollView();GUILayout.EndArea();
        GUILayout.BeginArea(SummaryActionsRect);
        if(shop.Ledger.Cash>=0){if(SummaryButton("Következő nap • Enter"))shop.NextDay();}else GUILayout.Label("Elfogyott a pénz. Az Új játék gombbal újrakezdhetsz.",small);
        if(SummaryButton("Új játék • R"))shop.ResetDemo();
        if(SummaryButton("Kilépés"))Quit();GUILayout.EndArea();
    }
    private bool SummaryButton(string label)=>GUILayout.Button(label,button,GUILayout.Height(32));
    private void CardTerminal()
    {
        Panel(new Rect(340,190,600,330));GUILayout.BeginArea(new Rect(365,210,550,290));
        Label("BANKKÁRTYÁS FIZETÉS",true);Label("Fizetendő: "+ShopCatalog.Money(shop.ActiveCustomer.TotalPrice));
        Label("Beírt összeg: "+shop.CardAmount+" Ft");
        Label("Számjegyek: összeg • Backspace: javítás • Del: törlés");Label(shop.Message);
        GUILayout.Space(12);if(Button("Fizetés jóváhagyása • Enter"))shop.ConfirmCardPayment();
        if(Button("Vissza • Esc"))shop.ClosePanel();GUILayout.EndArea();
    }
    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }
}
