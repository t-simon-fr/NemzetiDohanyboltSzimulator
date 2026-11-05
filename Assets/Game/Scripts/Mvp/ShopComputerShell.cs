using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class ShopDevices
{
    public const float ComputerWidth = 1200, ComputerHeight = 680;
    public static readonly Rect ComputerBody = new Rect(24, 124, 1152, 478);
    public bool ComputerShellActive => FocusKind == ShopDeviceKind.Monitor;
    private static readonly string[] AppNames = { "Asztal", "Árurendelés", "Üzletfejlesztés", "Kávégép", "Kamerák", "Játékok", "Árak" };

    public Rect ComputerScreenRect
    {
        get
        {
            var a = view.WorldToScreenPoint(monitor.TransformPoint(new Vector3(-.90f, .51f, .04f)));
            var b = view.WorldToScreenPoint(monitor.TransformPoint(new Vector3(.90f, -.51f, .04f)));
            return new Rect(Mathf.Min(a.x, b.x), Screen.height - Mathf.Max(a.y, b.y), Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }
    }

    public Rect ComputerToScreen(Rect logical)
    {
        var screen = ComputerScreenRect;
        return new Rect(screen.x + logical.x * screen.width / ComputerWidth,
            screen.y + logical.y * screen.height / ComputerHeight,
            logical.width * screen.width / ComputerWidth, logical.height * screen.height / ComputerHeight);
    }

    private List<ShopKeyboardTarget> ComputerControls()
    {
        var controls = new List<ShopKeyboardTarget>();
        void Add(string id, string text, Rect bounds, Action action, bool enabled = true)
            => controls.Add(new ShopKeyboardTarget(id, text, bounds, action, enabled));
        Add("Exit:0", "×", new Rect(1120, 64, 56, 44), shop.ClosePanel);
        if (ComputerApp != ShopComputerApp.Desktop)
            Add("Desktop:0", "Asztal", new Rect(984, 64, 124, 44), () => OpenApp(ShopComputerApp.Desktop));
        if (ComputerApp == ShopComputerApp.Desktop)
        {
            for (int i = 1; i <= 6; i++)
            {
                int app = i;
                Add("OpenApp:" + app, AppNames[app], new Rect(36 + (i - 1) % 2 * 284, 154 + (i - 1) / 2 * 144, 260, 116),
                    () => OpenApp((ShopComputerApp)app));
            }
        }
        if (ComputerApp == ShopComputerApp.Furnishing)
        {
            string[] names = { "Önkiszolgáló bővítés", "Cigarettapolc · +9 hely", "Vészgomb", "Kávégép", "Hűtő" };
            int[] costs = { ShopLayout.ExpansionPrice, ShopLayout.ExtraShelfPrice, ShopSecurity.PurchasePrice, ShopActivities.CoffeeMachinePrice, ShopNextUpgrades.FridgePrice };
            bool[] owned = { shop.Layout.Expanded, shop.Layout.ExtraShelfOwned, shop.Security.Owned, shop.Activities.CoffeeOwned, shop.GetComponent<ShopNextUpgrades>().FridgeOwned };
            string[] ids = { "Expand:0", "ExtraShelf:0", "PanicButtonUpgrade:0", "CoffeeMachineUpgrade:0", "FridgeUpgrade:0" };
            Action[] buy = { () => shop.TryExpand(), () => shop.BuyExtraShelf(), () => PurchasePanicButton(),
                () => shop.Activities.BuyCoffeeMachine(), () => PurchaseFridge() };
            for (int i = 0; i < names.Length; i++)
            {
                float y = ComputerBody.y + 12 + i * 70;
                if (y < ComputerBody.y || y + 50 > ComputerBody.yMax - 68) continue;
                Add(ids[i], owned[i] ? "Megvásárolva" : "Megveszem", new Rect(ComputerBody.xMax - 226, y, 216, 48), buy[i],
                    !owned[i] && shop.CanDevelopShop && shop.Ledger.AvailableFunds >= costs[i] && (i != 4 || shop.Layout.Expanded));
            }
            Add("Arrange:0", "Üzlet átrendezése", new Rect(ComputerBody.x, ComputerBody.yMax - 54, 310, 48), () => BeginArrangement(), shop.CanDevelopShop);
        }
        shop.Activities.AddComputerControls(controls, ComputerBody);
        return controls;
    }

    public List<ShopKeyboardTarget> ComputerKeyboardTargets()
    {
        var targets = ComputerControls();
        if (ComputerApp == ShopComputerApp.Prices)
            shop.Pricing.AddKeyboardTargets(targets, ComputerBody);
        if(ComputerApp==ShopComputerApp.Orders) {
            targets.Add(new ShopKeyboardTarget("Cart:Submit","Megrendelem",new Rect(ComputerBody.x+802,ComputerBody.y+400,330,46),()=>SubmitCart()));
            for(int i=0;i<shop.AvailableProducts;i++) {
                int type=i;float y=ComputerBody.y+48+i*62-cartScroll.y;
                if(y<ComputerBody.y+48||y+50>ComputerBody.y+388)continue;
                targets.Add(new ShopKeyboardTarget("Cart:Minus:"+i,"-",new Rect(ComputerBody.x+796,y+6,48,44),()=>SetCartQuantity(type,CartQuantity(type)-1)));
                targets.Add(new ShopKeyboardTarget("Cart:Plus:"+i,"+",new Rect(ComputerBody.x+962,y+6,48,44),()=>SetCartQuantity(type,CartQuantity(type)+1)));
            }
        }
        foreach (var target in targets) target.Bounds = ComputerToScreen(target.Bounds);
        return targets;
    }

    private void DrawComputer()
    {
        var old = GUI.matrix;
        var oldCursor=GUI.skin.settings.cursorColor;
        GUI.skin.settings.cursorColor=ShopComputerTheme.Brown;
        var rect = ComputerScreenRect;
        GUI.matrix = Matrix4x4.TRS(rect.position, Quaternion.identity,
            new Vector3(rect.width / ComputerWidth, rect.height / ComputerHeight, 1));
        try
        {
            ShopUi.Fill(new Rect(0, 0, ComputerWidth, ComputerHeight), ShopComputerTheme.Brown);
            ShopUi.Fill(new Rect(0, 0, ComputerWidth, 40), ShopComputerTheme.Green);
            ShopComputerTheme.Label(new Rect(24, 0, 300, 40), shop.Day + ". műszak", 20, true, true);
            ShopComputerTheme.Label(new Rect(420, 0, 450, 40), "Kassza " + ShopCatalog.Money(shop.Ledger.Cash), 20, true, true);
            ShopComputerTheme.Label(new Rect(1050, 0, 120, 40), ShopUiState.Clock(shop.ClockElapsed), 20, true, true, TextAnchor.MiddleRight);
            DrawComputerStripe(40);
            ShopComputerTheme.Label(new Rect(24, 64, 900, 44), ComputerApp == ShopComputerApp.Desktop ? "TobaccOS" : AppNames[(int)ComputerApp], 29, true, true);
            if (ComputerApp != ShopComputerApp.Desktop) ShopUi.Fill(ComputerBody, ShopComputerTheme.Paper);
            if (ComputerApp == ShopComputerApp.Desktop)
            {
                ShopComputerTheme.Label(new Rect(700, 260, 450, 90), "TobaccOS", 60, true, true);
                ShopUi.Fill(new Rect(704, 360, 380, 3), ShopComputerTheme.Paper);
            }
            else if (ComputerApp == ShopComputerApp.Orders) DrawOrderCart();
            else if (ComputerApp == ShopComputerApp.Furnishing) DrawComputerUpgrades();
            else if (ComputerApp == ShopComputerApp.Prices) shop.Pricing.DrawEditor(ComputerBody);
            else shop.Activities.DrawComputerContent(ComputerBody);
            foreach (var target in ComputerControls())
            {
                bool selected = target.Id == keyboardTargetId && keyboardOutline;
                GUI.enabled = target.Enabled;
                if (GUI.Button(target.Bounds, target.Label, ShopComputerTheme.Button(selected || target.Id.StartsWith("Coffee:Recipe:") &&
                    target.Id == "Coffee:Recipe:" + shop.Activities.SelectedRecipe || target.Id == "Camera:" + shop.Activities.SelectedCamera, ComputerApp == ShopComputerApp.Desktop ? 25 : 21)))
                {
                    keyboardOutline = false;
                    target.Activate();
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = true;
            }
            ShopUi.Fill(new Rect(0, 636, ComputerWidth, 44), ShopComputerTheme.Green);
            ShopComputerTheme.Label(new Rect(24, 636, 800, 44), ComputerApp == ShopComputerApp.Desktop ? "TobaccOS" : "TobaccOS   /   " + AppNames[(int)ComputerApp], 19, true, true);
        }
        finally { GUI.matrix = old; GUI.enabled = true; GUI.skin.settings.cursorColor=oldCursor; }
    }

    private void DrawComputerUpgrades()
    {
        string[] names = { "Önkiszolgáló bővítés", "Cigarettapolc · +9 hely", "Vészgomb", "Kávégép", "Hűtő" };
        int[] prices = { ShopLayout.ExpansionPrice, ShopLayout.ExtraShelfPrice, ShopSecurity.PurchasePrice, ShopActivities.CoffeeMachinePrice, ShopNextUpgrades.FridgePrice };
        for (int i = 0; i < names.Length; i++)
        {
            float y = ComputerBody.y + 12 + i * 70;
            ShopComputerTheme.Label(new Rect(ComputerBody.x + 18, y, 560, 48), names[i], 24);
            ShopComputerTheme.Label(new Rect(ComputerBody.x + 620, y, 230, 48), ShopCatalog.Money(prices[i]), 22, true);
            ShopUi.Fill(new Rect(ComputerBody.x + 18, y + 58, ComputerBody.width - 36, 1), ShopComputerTheme.Rule);
        }
    }

    private static void DrawComputerStripe(float y)
    {
        ShopUi.Fill(new Rect(0, y, ComputerWidth, 3), ShopComputerTheme.Red);
        ShopUi.Fill(new Rect(0, y + 3, ComputerWidth, 3), ShopComputerTheme.Paper);
        ShopUi.Fill(new Rect(0, y + 6, ComputerWidth, 3), ShopComputerTheme.Green);
    }
}
