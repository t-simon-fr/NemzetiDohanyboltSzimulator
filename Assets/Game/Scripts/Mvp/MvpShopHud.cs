using UnityEngine;

public sealed class MvpShopHud : MonoBehaviour
{
    [SerializeField] private MvpShopController shop;
    private GUIStyle title,text,small,button;
    public void Configure(MvpShopController controller)=>shop=controller;
    private void OnGUI()
    {
        if(shop==null||shop.Ledger==null)return;
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
                if(shop.Devices.FocusKind==ShopDeviceKind.Monitor||shop.Devices.FocusKind==ShopDeviceKind.Coins)return;
                Panel(new Rect(250,612,780,94));GUILayout.BeginArea(new Rect(266,622,748,78));
                GUILayout.Label("Kattints a kassza címleteire, majd az ÁTADÁS gombra.  Esc: vissza",small);
                GUILayout.Label(shop.Message,small);GUILayout.EndArea();return;
            }
            if(shop.Modal==ShopModal.None){Game();return;}
            Panel(new Rect(208,48,864,624));GUILayout.BeginArea(new Rect(240,72,800,572));
            switch(shop.Modal)
            {
                case ShopModal.Welcome:Welcome();break;
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
        Panel(new Rect(12,12,295,134));GUILayout.BeginArea(new Rect(26,20,268,118));
        Label(shop.Day+". nap • "+(shop.Phase==ShopPhase.Preparing?"Felkészülés":shop.Phase==ShopPhase.Open?"Nyitva":"Zárunk"),true);
        Label("Kassza: "+ShopCatalog.Money(shop.Ledger.Cash));
        GUILayout.Label("Eladások: "+shop.Ledger.Sales+" • Sorban: "+shop.QueueCount+" • Érkezett: "+shop.Spawned+"/"+shop.TotalVisitors,small);
        GUILayout.Label(shop.Security!=null&&shop.Security.Locked?"BEJÁRAT RETESZELVE • E a vészgombon":"B Számítógép • H Súgó • O Nyitás/zárás",small);GUILayout.EndArea();
        if(shop.ActiveCustomer!=null)
        {
            var c=shop.ActiveCustomer;Panel(new Rect(844,12,424,216));GUILayout.BeginArea(new Rect(860,24,392,190));
            Label(ShopPeople.Names[c.Person]+" • "+ShopCatalog.Names[c.Product],true);
            GUILayout.Label("Ár: "+ShopCatalog.Money(ShopCatalog.Prices[c.Product]),small);
            Label(shop.Dialogue);
            GUILayout.Label(shop.Stage==ServiceStage.Order?"A kért árut a kék átadóhelyre tedd.":shop.Stage==ServiceStage.Payment?"E a kasszánál vagy F: visszajáró kiadása.":"A vásárló elköszön.",small);GUILayout.EndArea();
        }
        if(!string.IsNullOrEmpty(shop.Message))
        {
            Panel(new Rect(844,240,424,120));GUILayout.BeginArea(new Rect(860,250,392,100));
            GUILayout.Label(shop.Message,small);
            if(shop.NextDelivery>0)GUILayout.Label("Szállítás: "+Mathf.CeilToInt(shop.NextDelivery)+" mp",small);
            GUILayout.EndArea();
        }
    }
    private void Welcome()
    {
        Label("Nemzeti Dohánybolt Szimulátor",true);Label(shop.Day+". műszak • Kis bolt, nagy történetek.");
        GUILayout.Space(22);
        Label("A pult mögötti polcon négy cigarettamárka várja a vásárlókat: Lucky Stroke, Marlbroke, Downhill és Davidon.");
        GUILayout.Space(14);Label("E-vel vedd fel a kért dobozt, és tedd a kék átadóhelyre. A pénztárgépből valódi magyar címletértékű bankjegyeket és érméket kattints ki a visszajáróhoz, majd nyomd meg az ÁTADÁS gombot.");
        GUILayout.Space(14);Label("B-vel nyisd meg a TobaccOS számítógépét. Az Árurendelésben válassz mennyiséget, majd kattints a termékre. Az áru a raktárba érkezik. E-vel vedd fel a márkadobozt, és a polcnál egyesével pakolj ki. Az üres karton a hátsó papírgyűjtőbe való.");
        GUILayout.Space(14);Label("A boltbővítés után a ReturAqua, a Loca Cola és a Hamster Energy is rendelhető. Ezeket a vásárlók az önkiszolgáló polcról veszik le, majd a pultnál fizetnek.");
        GUILayout.FlexibleSpace();if(Button("Körülnézek és felkészülök"))shop.ClosePanel();if(Button("Kinyitom a boltot"))shop.OpenShop();if(Button("Kilépés"))Quit();
    }
    private void Help()
    {
        Label("Műszaksegéd",true);
        Label("WASD + egér: mozgás és körülnézés\nE: termék vagy állomás használata\nE a monitoron / B a pult közelében: számítógép, alkalmazások\nE a kasszán / F: fizetés a pénztárgépen\nRaktári polcnál E: teljes doboz felvétele / visszarakása\nDobozzal a kézben, zöld polchelynél E: egy termék kipakolása\nÜres kartonon E: felvétel • Papírgyűjtőn E: kidobás\nO: nyitás vagy korai zárás • X: aktuális vásárló elküldése\nH: súgó • Escape: vissza a boltba / kurzor elengedése");
        GUILayout.Space(14);Label("A kasszában a rekeszekre kattintva egy bankjegyet vagy érmét veszel ki. A kivett darabok a tálcára kerülnek. Vissza: egy darab visszatétele. Törlés: az egész visszajáró visszarakása. Váltás: nagy címlet felváltása kisebbekre. Átadás: fizetés lezárása.");
        GUILayout.Space(14);Label("Az Üzletfejlesztés alkalmazásban nyitás előtt 24 000 Ft-ért bővíthetsz, 9000 Ft-ért cigarettapolcot, 16 000 Ft-ért kávégépet vehetsz. A kamerák és játékok használata közben a bolt tovább működik. G: kézben tartott tárgy eldobása; felmosóval és ütővel bal egérgomb: használat. A készletet neked kell feltöltened az új polcokon is.");
        GUILayout.FlexibleSpace();if(Button("Vissza a boltba"))shop.ClosePanel();
    }
    private void Summary()
    {
        Label(shop.Day+". nap • Zárás",true);
        Label("Kiszolgált vásárlók: "+shop.Ledger.Sales+" / "+shop.Spawned+" • Elküldve: "+shop.Missed);
        GUILayout.Space(10);Label("Árbevétel: "+ShopCatalog.Money(shop.Ledger.Revenue));
        Label("Lopásból eredő készletveszteség: "+ShopCatalog.Money(shop.Ledger.StockLoss)+" • Megállítva: "+shop.CaughtToday);
        Label("Eladott áruk beszerzési értéke: "+ShopCatalog.Money(shop.Ledger.CostOfGoods));
        Label("Működési költség: "+ShopCatalog.Money(shop.Ledger.OperatingCosts));
        Label("Nyereség: "+ShopCatalog.Money(shop.Ledger.Profit),true);
        Label("Utánrendelés: "+ShopCatalog.Money(shop.Ledger.Purchases)+" • Beruházás: "+ShopCatalog.Money(shop.Ledger.Improvements));
        Label("Zárókassza: "+ShopCatalog.Money(shop.Drawer.Total),true);
        if(shop.Ledger.Cash<0)Label("Ki nem fizetett működési költség: "+ShopCatalog.Money(-shop.Ledger.Cash)+". A folytatáshoz indíts új játékot.");GUILayout.Space(12);
        for(int i=0;i<shop.AvailableProducts;i++)GUILayout.Label(ShopCatalog.Names[i]+": "+shop.StockCount(i)+" a boltban, "+shop.WarehouseCount(i)+" a dobozban",small);
        GUILayout.Label("A készlet, a pénzek és a bővítés a futó játék következő napjára megmaradnak.",small);
        GUILayout.FlexibleSpace();if(shop.Ledger.Cash>=0&&Button("Következő nap"))shop.NextDay();if(Button("Új játék"))shop.ResetDemo();if(Button("Kilépés"))Quit();
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
