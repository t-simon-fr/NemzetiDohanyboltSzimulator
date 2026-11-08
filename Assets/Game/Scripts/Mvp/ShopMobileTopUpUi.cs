using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class ShopMobileTopUp
{
    public static readonly Rect TerminalBody = new Rect(104, 140, 1072, 478);
    public int Page { get; private set; }
    public void SelectPage(int page) { Page = Mathf.Clamp(page, 0, 2); Editing = false; if (Event.current != null) GUI.FocusControl(null); }
    public List<ShopKeyboardTarget> Controls(Rect r)
    {
        var result = new List<ShopKeyboardTarget>();
        void Add(string id, string text, Rect rect, Action action, bool enabled = true)
            => result.Add(new ShopKeyboardTarget("Mobile:" + id, text, rect, action, enabled));
        string[] pages = { "Feltöltés", "Elszámolás", "Bizonylat" };
        for (int i = 0; i < pages.Length; i++)
        {
            int page = i;
            Add("Page:" + i, pages[i], new Rect(r.x + i * r.width / 3, r.y, r.width / 3 - 8, 44), () => SelectPage(page));
        }
        if (!Owned) return result;
        if (Page == 0)
        {
            for (int i = 0; i < ShopMobileConfig.Providers.Length; i++)
            {
                int provider = i;
                Add("Provider:" + i, ShopMobileConfig.Providers[i], new Rect(r.x + 24 + i * 336, r.y + 120, 320, 44), () => SelectProvider(provider), CanEdit);
            }
            for (int i = 0; i < ShopMobileConfig.Amounts.Length; i++)
            {
                int amount = ShopMobileConfig.Amounts[i];
                Add("Amount:" + amount, ShopCatalog.Money(amount), new Rect(r.x + 566 + i * 158, r.y + 266, 148, 42), () => SelectAmount(amount), CanEdit);
            }
            Add("Confirm", "Adatok ellenőrzése és fizetés", new Rect(r.x + 24, r.y + 330, 496, 52), () => ConfirmDetails(), CanEdit);
            Add("Cancel", "Feltöltés megszakítása", new Rect(r.x + 566, r.y + 330, 474, 52), CancelDraft, IsCurrent && shop.Stage != ServiceStage.Thanks);
        }
        if (Page == 1)
            Add("Settle", "Szolgáltatói egyenleg rendezése", new Rect(r.x + 24, r.y + 330, 496, 52), () => SettleOperator(), !shop.Ledger.Closed && Book.Outstanding > 0);
        return result;
    }
    public void Draw(Rect r)
    {
        if (!Owned)
        {
            ShopComputerTheme.Label(new Rect(r.x + 24, r.y + 74, r.width - 48, 90), "Telefonfeltöltés: automatikus aktiválás a 2. hét hétfőjén (6. munkanap).", 26, true);
            ShopComputerTheme.Label(new Rect(r.x + 24, r.y + 184, r.width - 48, 60), "Nem kell megvásárolni. A szolgáltatás játékbeli tesztszámokat használ.", 21);
            return;
        }
        if (Page == 0)
        {
            ShopComputerTheme.Label(new Rect(r.x + 24, r.y + 60, r.width - 48, 56), RequestText, 23, true);
            ShopComputerTheme.Label(new Rect(r.x + 24, r.y + 172, 496, 28), "Telefonszám · 11 számjegy", 20);
            ShopComputerTheme.Label(new Rect(r.x + 566, r.y + 172, 474, 28), "Feltöltési összeg (Ft)", 20);
            GUI.enabled = CanEdit;
            GUI.SetNextControlName("MobileNumber");
            NumberEntry = GUI.TextField(new Rect(r.x + 24, r.y + 208, 496, 48), NumberEntry, 20, ShopComputerTheme.Field(26));
            GUI.SetNextControlName("MobileAmount");
            AmountEntry = GUI.TextField(new Rect(r.x + 566, r.y + 208, 474, 48), AmountEntry, 7, ShopComputerTheme.Field(26));
            GUI.enabled = true;
            Editing = GUI.GetNameOfFocusedControl() == "MobileNumber" || GUI.GetNameOfFocusedControl() == "MobileAmount";
            ShopComputerTheme.Label(new Rect(r.x + 24, r.y + 266, 496, 44), "Csak a kért szolgáltató / szám / összeg fogadható el.", 18);
        }
        if (Page == 1)
        {
            string[] names = { "Mai feltöltések névértéke", "Mai bolti jutalék (" + Book.Config.CommissionPercent + "%)", "Szolgáltatói tartozás / elkülönítve", "Elkölthető bolti pénz" };
            int[] values = { Book.DailySales, Book.DailyCommission, Book.Outstanding, shop.Ledger.AvailableFunds };
            for (int i = 0; i < names.Length; i++)
            {
                ShopComputerTheme.Label(new Rect(r.x + 24, r.y + 70 + i * 58, 650, 40), names[i], 23, true);
                ShopComputerTheme.Label(new Rect(r.x + 720, r.y + 70 + i * 58, 320, 40), ShopCatalog.Money(values[i]), 25, true);
            }
        }
        if (Page == 2)
        {
            var receipt = Book.LastReceipt;
            string text = receipt == null ? "Még nincs sikeres feltöltés. Bizonylat csak fizetés után készül." :
                receipt.Id + " · " + receipt.Day + ". munkanap\n" + receipt.Request.Description +
                "\nSIKERES FELTÖLTÉS\nBolti jutalék: " + ShopCatalog.Money(receipt.Commission) +
                "\nSzolgáltatói rész: " + ShopCatalog.Money(receipt.OperatorShare);
            ShopComputerTheme.Label(new Rect(r.x + 24, r.y + 74, r.width - 48, 300), text, 26, true);
        }
        ShopComputerTheme.Label(new Rect(r.x + 24, r.y + 402, r.width - 48, 68), Feedback, 19);
    }
    public void DrawTerminal()
    {
        using (ShopUi.Frame())
        {
            ShopUi.Panel(new Rect(80, 24, 1120, 674));
            ShopUi.Fill(new Rect(80, 24, 1120, 92), ShopComputerTheme.Brown);
            ShopComputerTheme.Label(new Rect(104, 34, 920, 42), "TELEFONFELTÖLTÉS", 29, true, true);
            ShopComputerTheme.Label(new Rect(104, 76, 920, 30), "Pulti terminál · Mobilegyenleg", 20, false, true);
            if (GUI.Button(new Rect(1100, 44, 76, 48), "×", ShopComputerTheme.Button())) shop.ClosePanel();
            Draw(TerminalBody);
            foreach (var control in Controls(TerminalBody))
            {
                GUI.enabled = control.Enabled;
                bool selected = control.Id == "Mobile:Page:" + Page || control.Id == "Mobile:Provider:" + Provider;
                if (GUI.Button(control.Bounds, control.Label, ShopComputerTheme.Button(selected, 21)))
                { control.Activate(); ShopInputGate.ConsumeWorldAction(); GUIUtility.ExitGUI(); }
                GUI.enabled = true;
            }
            ShopComputerTheme.Label(new Rect(104, 656, 1072, 26), "Esc · vissza a boltba     |     Feltöltés kizárólag sikeres fizetéskor történik.", 17);
        }
    }
}
