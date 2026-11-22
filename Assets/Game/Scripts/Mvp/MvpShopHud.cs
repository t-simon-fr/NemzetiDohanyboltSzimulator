using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class MvpShopHud : MonoBehaviour
{
    [SerializeField] private MvpShopController shop;
    private Vector2 summaryScroll, orderScroll, helpScroll;
    private readonly ShopUiState state = new ShopUiState();
    public static Rect SummaryDetailsRect => new Rect(278, 348, 724, 182);
    public static Rect SummaryActionsRect => new Rect(278, 552, 724, 112);
    public string CurrentNotice => state.Notice(Time.unscaledTime);
    public bool MovementLearned => state.MovementLearned;
    public ShopOrderLine[] OrderLines => ShopUiState.Order(shop);
    public void Configure(MvpShopController controller) => shop = controller;

    private void Update()
    {
        if (shop == null || ShopMenu.Active) return;
        state.Observe(shop.Message, shop.MessageRevision, Time.unscaledTime);
        if (Keyboard.current != null && !ShopInputGate.Blocked &&
            new[] { Key.W, Key.A, Key.S, Key.D }.Any(ShopBindings.Held)) state.MovementLearned = true;
    }
    private void OnGUI()
    {
        if (shop == null || shop.Ledger == null || ShopMenu.Active || shop.Arrangement.Active || shop.AfterHours.TradeOpen) return;
        using (ShopUi.Frame())
        {
            if (shop.Modal == ShopModal.Summary) { Summary(); return; }
            if (shop.Modal == ShopModal.Help) { Help(); return; }
            if (shop.Devices.Focused)
            {
                if (shop.Devices.FocusKind == ShopDeviceKind.Terminal) CardTerminal();
                else if (shop.Devices.FocusKind == ShopDeviceKind.Coffee) CoffeeControls();
                else if (shop.Devices.FocusKind == ShopDeviceKind.Till) CashFeedback();
                return;
            }
            Game();
        }
    }
    private void Game()
    {
        ShopUi.Panel(new Rect(24, 24, 330, 91), true);
        string phase = shop.Phase == ShopPhase.Preparing ? "FELKÉSZÜLÉS" : shop.Phase == ShopPhase.Open ? "NYITVA" : shop.Phase == ShopPhase.AfterHours ? "ZÁRÁS UTÁN" : "ZÁRUNK";
        ShopUi.Label(new Rect(40, 38, 298, 22), shop.Day + ". NAP  /  " + phase, 16, true, true);
        ShopUi.Label(new Rect(40, 69, 194, 30), ShopCatalog.Money(shop.Ledger.Cash), 24, true, true);
        ShopUi.Label(new Rect(245, 73, 95, 24), ShopUiState.Clock(shop.ClockElapsed), 18, false, true);
        string task = shop.Security.Locked ? "A bejárat reteszelve. A vészgombbal oldhatod fel." :
            shop.Phase == ShopPhase.Preparing ? "Nyitás az ajtó táblájának megfordításával." :
            shop.Phase == ShopPhase.AfterHours ? "A napot az ajtó alsó részénél zárhatod le." :
            shop.Phase == ShopPhase.Closing ? "Új vendég már nem jön. A bent lévőket még kiszolgálhatod." : "";
        if (!string.IsNullOrEmpty(task))
        {
            float height = ShopUi.Text(16).CalcHeight(new GUIContent(task), 298) + 24;
            ShopUi.Panel(new Rect(24, 127, 330, height));
            ShopUi.Label(new Rect(40, 139, 298, height - 24), task, 16);
        }
        if (shop.ActiveCustomer != null) Customer();
        if (shop.DialoguePanelVisible) DialoguePanel();
        string notice = CurrentNotice;
        if (!string.IsNullOrEmpty(notice) && notice != shop.Dialogue)
        {
            float h = ShopUi.Text(16).CalcHeight(new GUIContent(ShopUi.BoundText(notice)), 298) + 34;
            ShopUi.Panel(new Rect(24, 242, 330, h));
            ShopUi.Label(new Rect(40, 258, 298, h - 28), ShopUi.BoundText(notice), 16);
        }
        if (shop.NextDelivery > 0)
            ShopUi.Label(new Rect(40, 205, 312, 30), "Szállítás úton  /  " + Mathf.CeilToInt(shop.NextDelivery) + " mp", 16, true);
    }
    private void Customer()
    {
        const float x = 866, width = 390, inner = 358;
        var customer = shop.ActiveCustomer;
        int person = Mathf.Clamp(customer.Person, 0, ShopPeople.Names.Length - 1);
        string mood = customer.IsRegular ? "TÖRZSVENDÉG" : ShopPeople.Mood(person);
        float listHeight = Mathf.Min(148, OrderLines.Sum(line => ShopUi.Text(16).CalcHeight(new GUIContent(line.Description), inner - 72) + 12));
        float height = 198 + listHeight;
        ShopUi.Panel(new Rect(x, 24, width, height));
        ShopUi.Fill(new Rect(x + 16, 36, 40, 40), ShopPeople.Colors[person]);
        ShopUi.Label(new Rect(x + 16, 36, 40, 40), ShopPeople.Monogram(person), 22, true, true);
        ShopUi.Label(new Rect(x + 64, 32, 180, 30), ShopPeople.Names[person], 24, true);
        ShopUi.Label(new Rect(x + 64, 60, 180, 20), mood, 13, true);
        GUI.Label(new Rect(x + 230, 44, 144, 24), shop.Stage == ServiceStage.Payment ? "FIZETÉS" : shop.Stage == ServiceStage.Thanks ? "ELKÖSZÖNÉS" : "RENDELÉS",
            ShopUi.Text(13, true, false, TextAnchor.UpperRight));
        float top = 88;
        ShopUi.Fill(new Rect(x + 16, top, inner, 1), ShopUi.Rule);
        top += 10;
        ShopUi.Label(new Rect(x + 16, top, inner, 22), "RENDELÉS", 14, true);
        top += 26;
        GUILayout.BeginArea(new Rect(x + 16, top, inner, listHeight));
        orderScroll = GUILayout.BeginScrollView(orderScroll, GUIStyle.none, GUI.skin.verticalScrollbar);
        foreach (var line in OrderLines)
        {
            string status = line.Delivered == line.Required ? "KÉSZ" : line.Unavailable ? "HIÁNY" : "";
            GUILayout.BeginHorizontal();
            GUILayout.Label(line.Description, ShopUi.Text(16), GUILayout.Width(inner - 72));
            GUILayout.Label(status, ShopUi.Text(12, true), GUILayout.Width(52));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        top += listHeight + 12;
        ShopUi.Label(new Rect(x + 16, top, inner, 28), customer.LotteryClaimId!=null?"Szelvényellenőrzés":"Összesen  " + ShopCatalog.Money(customer.TotalPrice), 20, true);
        string action = shop.Stage == ServiceStage.Order ?
            (customer.MobileRequest != null ? "E a terminálon: telefonfeltöltés\nX: Most nem tudom kiszolgálni" : customer.LotteryClaimId!=null?"Olvasd be a szelvényt a lottóponton.\nX: Most nem tudom kiszolgálni":shop.HasUnavailableRequest ? "T: Másik árut ajánlok\nX: Nem tudom kiszolgálni" : "Tedd a kért árut az átadóhelyre.\nX: Nem tudom kiszolgálni") :
            shop.Stage == ServiceStage.Payment ? (shop.CardPayment ? "Bankkártya  /  F: Terminál" : shop.CoinPayment ? "Aprópénz  /  F: Érmék átvétele" : "Készpénz  /  F: Kassza") : "A vásárló elköszön.";
        ShopUi.Label(new Rect(x + 16, top + 34, inner, 52), ShopUi.BoundText(action), 15);
    }
    private void DialoguePanel()
    {
        var customer = shop.ActiveCustomer;
        if (customer == null) return;
        int person = Mathf.Clamp(customer.Person, 0, ShopPeople.Names.Length - 1);
        string name = ShopPeople.Names[person];
        string mood = customer.IsRegular ? "TÖRZSVENDÉG" : ShopPeople.Mood(person);
        string shown = shop.DialogueShown;
        bool typing = shop.DialogueDirector.Typing;
        const float x = 24, width = 1232, inner = 1200;
        float textHeight = ShopUi.Text(20).CalcHeight(new GUIContent(shown), inner - 40);
        float height = 128 + textHeight;
        float y = 720 - height - 16;
        ShopUi.Panel(new Rect(x, y, width, height), true);
        ShopUi.Fill(new Rect(x + 16, y + 14, 44, 44), ShopPeople.Colors[person]);
        ShopUi.Label(new Rect(x + 16, y + 14, 44, 44), ShopPeople.Monogram(person), 24, true, true);
        ShopUi.Label(new Rect(x + 72, y + 12, 340, 48), name, 30, true, true);
        float nameWidth = ShopUi.Text(30, true, true).CalcSize(new GUIContent(name)).x;
        float moodWidth = ShopUi.Text(14, true, true).CalcSize(new GUIContent(mood)).x + 28;
        ShopUi.Fill(new Rect(x + 84 + nameWidth, y + 22, moodWidth, 30), ShopPeople.Colors[person]);
        ShopUi.Label(new Rect(x + 84 + nameWidth, y + 22, moodWidth, 30), mood, 14, true, true);
        ShopUi.Label(new Rect(x + 32, y + 70, inner - 40, textHeight + 8), shown, 20, false, true);
        if (typing)
            GUI.Label(new Rect(x + width - 236, y + height - 38, 220, 28), "[E] Folytatás", ShopUi.Text(15, false, true, TextAnchor.UpperRight));
    }
    private void CashFeedback()
    {
        ShopUi.Panel(new Rect(24, 24, 350, 208));
        ShopUi.Label(new Rect(40, 39, 318, 26), "KÉSZPÉNZES FIZETÉS", 19, true);
        if (shop.ActiveCustomer != null)
        {
            ShopUi.Label(new Rect(40, 77, 318, 30), "Fizetendő  " + ShopCatalog.Money(shop.ActiveCustomer.TotalPrice), 20, true);
            ShopUi.Label(new Rect(40, 113, 318, 28), "Kiválasztva  " + ShopCatalog.Money(shop.ChangeSelected), 18);
        }
        ShopUi.Label(new Rect(40, 153, 318, 68), shop.Message, 16);
    }
    private void CardTerminal()
    {
        ShopUi.Panel(new Rect(24, 24, 300, 296));
        ShopUi.Label(new Rect(40, 40, 268, 32), "BANKKÁRTYATERMINÁL", 18, true);
        ShopUi.Label(new Rect(40, 86, 268, 138), "Kattints a terminál számgombjaira, vagy gépeld be az összeget.\n\nJAV / Backspace: utolsó számjegy\nTÖR / Del: teljes törlés\nOK / Enter: jóváhagyás\nESC: vissza a boltba", 16);
        ShopUi.Label(new Rect(40, 238, 268, 66), shop.Message, 16);
    }
    private void CoffeeControls()
    {
        ShopUi.Panel(new Rect(24, 24, 300, 252));
        ShopUi.Label(new Rect(40, 40, 268, 28), "LaVazze Barista", 20, true);
        ShopUi.Label(new Rect(40, 84, 268, 120), "Válassz italt a gép kijelzőjén, majd indítsd el a főzést.\n\nKattintás: receptválasztás\nEnter: főzés vagy italátvétel\nAz állapotot a gép jelzi.", 16);
        if (ShopUi.Button(new Rect(40, 216, 268, 40), "Vissza a boltba / Esc")) shop.ClosePanel();
    }
    private void Summary()
    {
        ShopUi.Fill(new Rect(0, 0, 1280, 720), new Color(.06f, .10f, .08f, .86f));
        ShopUi.Panel(new Rect(250, 32, 780, 656));
        ShopUi.Label(new Rect(278, 52, 724, 22), "NEMZETI DOHÁNYBOLT  /  NAPI ELSZÁMOLÁS", 15, true);
        ShopUi.Label(new Rect(278, 86, 724, 38), shop.Day + ". műszak lezárva", 30, true);
        ShopUi.Fill(new Rect(278, 138, 724, 1), ShopUi.Rule);
        ShopUi.Label(new Rect(278, 154, 350, 24), "NAPI NYERESÉG", 14, true);
        ShopUi.Label(new Rect(654, 154, 348, 24), "ZÁRÓKASSZA", 14, true);
        ShopUi.Label(new Rect(278, 182, 350, 40), ShopCatalog.Money(shop.Ledger.Profit), 30, true);
        ShopUi.Label(new Rect(654, 182, 348, 40), ShopCatalog.Money(shop.Drawer.Total), 30, true);
        ShopUi.Label(new Rect(278, 234, 724, 48), "Árbevétel: " + ShopCatalog.Money(shop.Ledger.Revenue) + "  /  Költség: " + ShopCatalog.Money(shop.Ledger.CostOfGoods + shop.Ledger.OperatingCosts), 18);
        ShopUi.Label(new Rect(278, 285, 724, 42), "Kiszolgálva: " + shop.Ledger.Sales + " / " + shop.Spawned + "  •  Elküldve: " + shop.Missed + "  •  Ár miatt távozott: " + shop.PriceRefusals, 16);
        ShopUi.Fill(new Rect(278, 335, 724, 1), ShopUi.Rule);
        GUILayout.BeginArea(SummaryDetailsRect);
        summaryScroll = GUILayout.BeginScrollView(summaryScroll);
        shop.Reputation?.DrawSummary();
        GUILayout.Label("Működési költség: " + ShopCatalog.Money(shop.Ledger.OperatingCosts) + "  •  Kártyás bevétel: " + ShopCatalog.Money(shop.Ledger.CardRevenue), ShopUi.Text(16));
        GUILayout.Label("Lopáskár: " + ShopCatalog.Money(shop.Ledger.StockLoss) + "  •  Megállított tolvaj: " + shop.CaughtToday, ShopUi.Text(16));
        GUILayout.Label("Utánrendelés: " + ShopCatalog.Money(shop.Ledger.Purchases) + "  •  Beruházás: " + ShopCatalog.Money(shop.Ledger.Improvements), ShopUi.Text(16));
        if (shop.Ledger.PurchaseRounding != 0)
            GUILayout.Label("Beszerzési kerekítés: " + ShopCatalog.Money(shop.Ledger.PurchaseRounding), ShopUi.Text(16));
        if (shop.Mobile.Owned)
        {
            var mobile = shop.Mobile.Book;
            GUILayout.Label("TELEFONFELTÖLTÉS", ShopUi.Text(14,true));
            GUILayout.Label("Névérték: " + ShopCatalog.Money(mobile.DailySales) + " · Jutalék: " + ShopCatalog.Money(mobile.DailyCommission), ShopUi.Text(16));
            GUILayout.Label("Szolgáltatói rész: " + ShopCatalog.Money(mobile.DailySales - mobile.DailyCommission) + " · Tartozás / elkülönítve: " + ShopCatalog.Money(mobile.Outstanding), ShopUi.Text(16));
        }
        if(shop.Lottery.Owned)
        {
            var book=shop.Lottery.Book;
            GUILayout.Label("SZERECSENJÁTÉK ZRT. / LOTTÓPONT",ShopUi.Text(14,true));
            GUILayout.Label("Szelvényeladás: "+ShopCatalog.Money(book.DailySales)+" · Kifizetés: "+ShopCatalog.Money(book.DailyPayouts),ShopUi.Text(16));
            GUILayout.Label("Bolti jutalék: "+ShopCatalog.Money(book.DailyCommission)+" · Üzemeltetői egyenleg: "+ShopCatalog.Money(book.Outstanding),ShopUi.Text(16));
            GUILayout.Label("Elkülönített pénz: "+ShopCatalog.Money(book.Reserve)+" · Elkölthető: "+ShopCatalog.Money(shop.Ledger.AvailableFunds),ShopUi.Text(16));
        }
        GUILayout.Space(12);
        GUILayout.Label("KÉSZLET  /  BOLT + RAKTÁR", ShopUi.Text(14, true));
        foreach (int i in shop.SupplyTypes)
            GUILayout.Label(ShopCatalog.Names[i] + "  —  " + shop.StockCount(i) + " + " + shop.WarehouseCount(i) + " db", ShopUi.Text(16));
        if (shop.StockCount(ShopCatalog.Contraband) > 0) GUILayout.Label("Dnyipro  —  " + shop.StockCount(ShopCatalog.Contraband) + " csomag", ShopUi.Text(16));
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        if (shop.Ledger.Cash >= 0)
        {
            if (ShopUi.Button(new Rect(278, 552, 724, 44), "Következő nap  /  Enter", true)) shop.NextDay();
        }
        else ShopUi.Label(new Rect(278, 552, 724, 44), "Elfogyott a kasszapénz. Az Új játék gombbal újrakezdhetsz.", 18, true);
        if (ShopUi.Button(new Rect(278, 606, 350, 42), "Új játék  /  R")) shop.ResetDemo();
        if (ShopUi.Button(new Rect(640, 606, 362, 42), "Kilépés")) Quit();
        ShopUi.Label(new Rect(278, 660, 724, 20), "A pénz, készlet és elrendezés a következő napra megmarad.", 13);
    }
    private void Help()
    {
        ShopUi.Panel(new Rect(250, 48, 780, 624));
        ShopUi.Label(new Rect(278, 72, 724, 40), "Műszaksegéd", 30, true);
        GUILayout.BeginArea(new Rect(278, 132, 724, 440));
        helpScroll = GUILayout.BeginScrollView(helpScroll);
        string[] sections = {
            "A BOLTBAN\nW / A / S / D: mozgás  •  Egér: körülnézés\n" + ShopUi.Key(Key.LeftShift) + ": sprint  •  " + ShopUi.Key(Key.Space) + ": ugrás (csak a földről)\nE: használat, felvétel vagy lerakás  •  G: eldobás\nB: számítógép a pultnál  •  F: kassza  •  Esc: menü",
            "KISZOLGÁLÁS\nA jobb oldali listán követheted az átadott darabszámokat. Tedd a kért árut az átadóhelyre. T: helyettesítés készlethiánynál. X: aktuális vevő elküldése.",
            "FELTÖLTÉS\nE a raktári dobozon: felvétel. Dobozzal E a zöld helyen: egy termék kipakolása. Az üres kartont a hátsó papírgyűjtő fogadja. A pult alatti rekesz csak Dnyipro csomagoknak való.",
            "KASSZA\n1–6: bankjegy  •  Shift + 1–6: érme\nEnter: fizetés  •  Backspace: javítás  •  Del: törlés\nKártyánál írd be a pontos fizetendő összeget. Tab / nyilak: választás, Space: kiválasztás.",
            "MŰSZAK\nE az ajtó tábláján: nyitás vagy zárás. Az óra nyitáskor indul. Zárás után várd meg a vendégek távozását, majd üres kézzel E az ajtó alsó részén: nap lezárása.",
            "TOBACCOS\nA monitoron rendelsz árut, vásárolsz fejlesztést és rendezed át az üzletet. Tab / nyilak és Enter is használható. Az átrendezés szünetelteti a boltot; a kamerák és játékok közben tovább folyik a műszak.",
            "PÉNZKÉPEK\nForrás: Magyar Nemzeti Bank — mnb.hu. Bankjegytervek: Pálinkás György. Az eredeti MNB-illusztrációk felhasználásával."
        };
        foreach (string section in sections)
        {
            GUILayout.Label(ShopUi.BoundText(section), ShopUi.Text(18));
            GUILayout.Space(20);
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        if (ShopUi.Button(new Rect(278, 604, 724, 44), "Vissza a boltba  /  Esc", true)) shop.ClosePanel();
    }
    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
