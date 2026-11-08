using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class ShopDevices
{
    public const float ComputerWidth = 1200, ComputerHeight = 680;
    public static readonly Rect ComputerBody = new Rect(24, 124, 1152, 478);
    public bool ComputerShellActive => FocusKind == ShopDeviceKind.Monitor;
    private static readonly string[] AppNames = { "Asztal", "Árurendelés", "Üzletkezelés", "Kávégép", "Kamerák", "Játékok", "Árak", "Lottópont", "Üzlet hírneve", "Telefonfeltöltés" };

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

    private int furnishingPage;
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
            Add("OpenApp:9", "Telefonfeltöltés", new Rect(704,154,380,72), () => OpenApp(ShopComputerApp.Mobile));
            Add("OpenApp:7","Lottópont",new Rect(704,410,380,72),()=>OpenApp(ShopComputerApp.Lottery));
            Add("OpenApp:8","Üzlet hírneve",new Rect(704,494,380,72),()=>OpenApp(ShopComputerApp.Reputation));
        }
        if (ComputerApp == ShopComputerApp.Furnishing)
        {
            AddFurnishingControls(controls);
        }
        shop.Activities.AddComputerControls(controls, ComputerBody);
        if(ComputerApp==ShopComputerApp.Mobile)controls.AddRange(shop.Mobile.Controls(ComputerBody));
        if(ComputerApp==ShopComputerApp.Lottery)controls.AddRange(shop.Lottery.Controls(ComputerBody));
        if(ComputerApp==ShopComputerApp.Reputation)shop.Reputation.AddControls(controls,ComputerBody);
        return controls;
    }

    public List<ShopKeyboardTarget> ComputerKeyboardTargets()
    {
        var targets = ComputerControls();
        if (ComputerApp == ShopComputerApp.Prices)
            shop.Pricing.AddKeyboardTargets(targets, ComputerBody);
        if(ComputerApp==ShopComputerApp.Orders) {
            targets.Add(new ShopKeyboardTarget("Cart:Submit","Megrendelem",new Rect(ComputerBody.x+802,ComputerBody.y+400,330,46),()=>SubmitCart()));
            var supplyTypes = shop.SupplyTypes;
            for(int row=0;row<supplyTypes.Length;row++) {
                int type=supplyTypes[row];int i=type;float y=ComputerBody.y+48+row*62-cartScroll.y;
                if(y<ComputerBody.y+48||y+50>ComputerBody.y+370)continue;
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
            else if (ComputerApp == ShopComputerApp.Mobile) shop.Mobile.Draw(ComputerBody);
            else if (ComputerApp == ShopComputerApp.Lottery) shop.Lottery.Draw(ComputerBody);
            else if (ComputerApp == ShopComputerApp.Reputation) shop.Reputation.Draw(ComputerBody);
            else shop.Activities.DrawComputerContent(ComputerBody);
            foreach (var target in ComputerControls())
            {
                bool selected = target.Id == keyboardTargetId && keyboardOutline ||
                    ComputerApp==ShopComputerApp.Reputation && target.Id==shop.Reputation.SelectedTabId ||
                    ComputerApp==ShopComputerApp.Mobile && (target.Id=="Mobile:Page:"+shop.Mobile.Page || target.Id=="Mobile:Provider:"+shop.Mobile.Provider) ||
                    target.Id == (furnishingPage == 0 ? "Furnishing:Developments" : furnishingPage == 1 ? "Furnishing:Furniture" : "Furnishing:Partners");
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
        DrawFurnishingRows();
    }

    private static void DrawComputerStripe(float y)
    {
        ShopUi.Fill(new Rect(0, y, ComputerWidth, 3), ShopComputerTheme.Red);
        ShopUi.Fill(new Rect(0, y + 3, ComputerWidth, 3), ShopComputerTheme.Paper);
        ShopUi.Fill(new Rect(0, y + 6, ComputerWidth, 3), ShopComputerTheme.Green);
    }
}
