using System.Text;
using UnityEngine;

public sealed class MvpShopHud : MonoBehaviour
{
    [SerializeField] private MvpShopController shop;
    private GUIStyle title, text, small, button;
    public void Configure(MvpShopController controller) => shop = controller;
    private void OnGUI()
    {
        if (shop == null || shop.Ledger == null) return;
        if (title == null)
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true };
            text = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            small = new GUIStyle(text) { fontSize = 14 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 17, wordWrap = true, padding = new RectOffset(10,10,6,6) };
            title.normal.textColor = text.normal.textColor = small.normal.textColor = Color.white;
        }
        Matrix4x4 matrix = GUI.matrix; Color color = GUI.color; bool enabledBefore = GUI.enabled;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        if (shop.Modal != ShopModal.None)
        {
            GUI.matrix = Matrix4x4.identity;
            GUI.color = new Color(0,0,0,.78f);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-1280f*scale)*.5f,(Screen.height-720f*scale)*.5f),Quaternion.identity,Vector3.one*scale);
        try
        {
            if (shop.Modal == ShopModal.None) DrawGame();
            else
            {
                GUI.color = Color.white;
                Panel(new Rect(220,52,840,616)); GUILayout.BeginArea(new Rect(244,72,792,572));
                switch (shop.Modal)
                {
                    case ShopModal.Welcome: Welcome(); break;
                    case ShopModal.Help: Help(); break;
                    case ShopModal.Supplier: Supplier(); break;
                    case ShopModal.Cash: Cash(); break;
                    case ShopModal.Summary: Summary(); break;
                }
                GUILayout.EndArea();
            }
        }
        finally { GUI.matrix = matrix; GUI.color = color; GUI.enabled = enabledBefore; }
    }
    private void Panel(Rect rect)
    {
        Color old = GUI.color; GUI.color = new Color(.04f,.08f,.12f,.94f); GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color = old;
    }
    private void Label(string value, bool heading = false) => GUILayout.Label(value,heading ? title : text);
    private bool Button(string value, float height = 38f) => GUILayout.Button(value,button,GUILayout.Height(height));
    private void DrawGame()
    {
        Panel(new Rect(12,12,310,280)); GUILayout.BeginArea(new Rect(26,20,282,260));
        Label(shop.Day + ". nap • " + PhaseName(),true);
        Label("Kassza: " + ShopCatalog.Money(shop.Ledger.Cash));
        Label("Kiszolgálva: " + shop.Ledger.Sales + "   Elküldve: " + shop.Missed);
        GUILayout.Label("Eltelt: " + System.TimeSpan.FromSeconds(shop.Elapsed).ToString(@"mm\:ss") + " • Érkezett: " + shop.Spawned + "/" + shop.TotalVisitors,small);
        GUILayout.Space(8);
        for(int i=0;i<3;i++) GUILayout.Label(ShopCatalog.ShortNames[i] + ": " + shop.StockCount(i) + " a boltban • Doboz: " + shop.WarehouseCount(i),small);
        if(shop.NextDelivery>0) GUILayout.Label("Szállítás: " + Mathf.CeilToInt(shop.NextDelivery) + " mp",small);
        GUILayout.Space(8); GUILayout.Label("B Beszerzés  •  H Súgó\nO Nyitás/zárás  •  F Kassza  •  X Elküldés",small);
        GUILayout.EndArea();
        Panel(new Rect(796,12,472,248)); GUILayout.BeginArea(new Rect(812,22,440,224));
        if(shop.ActiveCustomer != null)
        {
            var c=shop.ActiveCustomer;
            Label(ShopPeople.Names[c.Person] + " • " + ShopCatalog.Names[c.Product],true);
            Label("Ár: " + ShopCatalog.Money(ShopCatalog.Prices[c.Product]) + "  •  Sorban: " + shop.QueueCount);
            GUILayout.Space(10); Label(shop.Dialogue);
            GUILayout.Space(10); GUILayout.Label(shop.Stage == ServiceStage.Order ? "Tedd a kért árut a KÉK átadóhelyre. (E)" : shop.Stage == ServiceStage.Payment ? "F – Kassza: számold ki és add ki a visszajárót." : "A vásárló elköszön és távozik.",small);
        }
        else
        {
            Label(shop.Phase == ShopPhase.Preparing ? "Nyitás előtt" : "A következő vásárló érkezik",true);
            Label(shop.Phase == ShopPhase.Preparing ? "Nézd át a polcokat. O-val nyithatsz." : "A polcokat közben is feltöltheted.");
        }
        GUILayout.EndArea();
        if(!string.IsNullOrEmpty(shop.Message))
        {
            Panel(new Rect(796,270,472,104)); GUILayout.BeginArea(new Rect(812,280,440,86));
            GUILayout.Label(shop.Message,small); GUILayout.Label("Dobozból kiválasztva: " + ShopCatalog.Names[shop.SelectedSupply],small); GUILayout.EndArea();
        }
    }
    private string PhaseName() => shop.Phase == ShopPhase.Preparing ? "Felkészülés" : shop.Phase == ShopPhase.Open ? "Nyitva" : "Zárunk";
    private void Welcome()
    {
        Label("Nemzeti Dohánybolt Szimulátor",true); Label(shop.Day + ". műszak • A pult mögött kezdődik a történet.");
        GUILayout.Space(20);
        Label("Ma nyolc vásárló érkezik. Készítsd össze a rendelésüket, ellenőrizd a kapott pénzt, és címletekből állítsd össze a visszajárót.");
        GUILayout.Space(14); Label("A kék termék ásványvíz, a zöld rágó, az arany sör. A kért árut a pult KÉK helyére tedd.");
        GUILayout.Space(14); Label("B-vel rendelhetsz utánpótlást. A hátsó dobozból E-vel veheted ki az árut; Q váltja a típust. Ezután tedd egy üres polchelyre.");
        GUILayout.Space(14); Label("Kassza: " + ShopCatalog.Money(shop.Ledger.Cash) + " • Napi működés: 300 Ft");
        GUILayout.FlexibleSpace();
        if(Button("Körülnézek és felkészülök")) shop.ClosePanel();
        if(Button("Kinyitom a boltot")) shop.OpenShop();
        if(Button("Kilépés")) QuitGame();
    }
    private void Help()
    {
        Label("Műszaksegéd",true);
        Label("WASD + egér: mozgás és körülnézés\nE: termék felvétele / fix helyre lerakása / állomás használata\nF: fizetés a kassza közelében\nB: utánrendelés • Q: termékváltás a hátsó doboznál\nO: bolt nyitása vagy zárás kérése • X: aktuális vásárló elküldése\nEscape: panel bezárása vagy kurzor elengedése");
        GUILayout.Space(18); Label("Rendelés → KÉK átadóhely → kapott pénz megszámolása → visszajáró kiválasztása → fizetés lezárása.");
        GUILayout.Space(16); Label("A téves árut visszaveheted. A megfelelő áru csak sikeres fizetéskor kerül ki a készletből. A fizetési és beszerzési panel alatt a vásárlók várakozási ideje szünetel.");
        GUILayout.Space(16); Label("A műszak nyolc érkezés után, a sor kiszolgálásával ér véget. O-val korábban is zárhatsz; a bent várakozókat még rendezni kell.");
        GUILayout.FlexibleSpace(); if(Button("Vissza a boltba")) shop.ClosePanel(); if(Button("Kilépés"))QuitGame();
    }
    private void Supplier()
    {
        Label("Beszerzés • 3 darabos csomagok",true); Label("Kassza: " + ShopCatalog.Money(shop.Ledger.Cash) + " • Szállítás: 8 másodperc");
        GUILayout.Space(18);
        for(int i=0;i<3;i++)
        {
            Label(ShopCatalog.Names[i],true);
            GUILayout.Label("Boltban: " + shop.StockCount(i) + " • Dobozban: " + shop.WarehouseCount(i) + " • Úton: " + shop.PendingQuantity(i),small);
            GUI.enabled = shop.Ledger.Cash >= ShopCatalog.Costs[i]*3 && shop.WarehouseCount(i)+shop.PendingQuantity(i)<24;
            if(Button("3 db rendelése – " + ShopCatalog.Money(ShopCatalog.Costs[i]*3))) shop.OrderSupply(i);
            GUI.enabled = true; GUILayout.Space(14);
        }
        GUILayout.Label(shop.Message,small); GUILayout.FlexibleSpace(); if(Button("Vissza a boltba")) shop.ClosePanel(); if(Button("Kilépés"))QuitGame();
    }
    private void Cash()
    {
        var c=shop.ActiveCustomer;
        if(c==null) { Label("Nincs folyamatban fizetés."); if(Button("Vissza"))shop.ClosePanel(); return; }
        Label("Fizetés • " + ShopPeople.Names[c.Person],true);
        Label(ShopCatalog.Names[c.Product] + " • Fizetendő: " + ShopCatalog.Money(ShopCatalog.Prices[c.Product]));
        GUILayout.Space(10); Label("A vásárló ezt adta: " + TenderNotes(c.Tender));
        GUILayout.Label("Számold meg a kapott pénzt, és válaszd ki a visszajáró címleteit.",small);
        GUILayout.Space(12); Label("Kiválasztott visszajáró: " + ShopCatalog.Money(shop.ChangeSelected),true);
        for(int row=0;row<3;row++)
        {
            GUILayout.BeginHorizontal();
            for(int col=0;col<3;col++) { int value=ShopCatalog.Denominations[row*3+col]; if(Button("+ " + ShopCatalog.Money(value),40))shop.AddChange(value); }
            GUILayout.EndHorizontal();
        }
        GUILayout.BeginHorizontal(); if(Button("Utolsó címlet vissza"))shop.UndoChange(); if(Button("Törlés"))shop.ClearChange(); GUILayout.EndHorizontal();
        GUILayout.Space(12); GUILayout.Label(shop.Message,small);
        GUILayout.FlexibleSpace();
        if(Button("Visszajáró kiadása és fizetés lezárása",46)) shop.SubmitChange(shop.ChangeSelected);
        if(Button("Vissza a pulthoz"))shop.ClosePanel();
    }
    private static string TenderNotes(int amount)
    {
        StringBuilder notes=new StringBuilder();
        foreach(int value in ShopCatalog.Denominations)
        {
            int count=amount/value; if(count==0)continue; amount%=value;
            if(notes.Length>0)notes.Append(" + "); notes.Append(count).Append(" × ").Append(ShopCatalog.Money(value));
        }
        return notes.ToString();
    }
    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void Summary()
    {
        Label(shop.Day + ". nap • Zárás",true);
        Label("Kiszolgált vásárlók: " + shop.Ledger.Sales + " / " + shop.Spawned + " • Elküldött vagy távozó: " + shop.Missed);
        GUILayout.Space(16); GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(370)); Label("A nap eredménye",true);
        Label("Árbevétel: " + ShopCatalog.Money(shop.Ledger.Revenue));
        Label("Eladott áruk beszerzési értéke: " + ShopCatalog.Money(shop.Ledger.CostOfGoods));
        Label("Működési költség: " + ShopCatalog.Money(shop.Ledger.OperatingCosts));
        Label("Nyereség: " + ShopCatalog.Money(shop.Ledger.Profit),true); GUILayout.EndVertical();
        GUILayout.BeginVertical(); Label("Pénz a kasszában",true);
        Label("Nyitópénz: " + ShopCatalog.Money(shop.Ledger.OpeningCash));
        Label("Bevétel: +" + ShopCatalog.Money(shop.Ledger.Revenue));
        Label("Utánrendelés: −" + ShopCatalog.Money(shop.Ledger.Purchases));
        Label("Működés: −" + ShopCatalog.Money(shop.Ledger.OperatingCosts));
        Label("Zárópénz: " + ShopCatalog.Money(shop.Ledger.Cash),true); GUILayout.EndVertical(); GUILayout.EndHorizontal();
        GUILayout.Space(16);
        for(int i=0;i<3;i++) GUILayout.Label(ShopCatalog.Names[i]+": "+shop.StockCount(i)+" a boltban, "+shop.WarehouseCount(i)+" a dobozban",text);
        GUILayout.Label("A megmaradt áru és a kasszapénz átkerül a következő napra.",small);
        GUILayout.FlexibleSpace(); if(Button("Következő nap"))shop.NextDay(); if(Button("Új játék – kezdőkészlet és 6000 Ft"))shop.ResetDemo(); if(Button("Kilépés"))QuitGame();
    }
}
