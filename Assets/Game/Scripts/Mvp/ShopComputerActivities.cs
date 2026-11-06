using System.Collections.Generic;
using UnityEngine;

public sealed partial class ShopActivities
{
    public int SelectedCamera => selectedCamera;
    public void AddComputerControls(List<ShopKeyboardTarget> targets, Rect r)
    {
        var app = shop.Devices.ComputerApp;
        void Add(string id, string text, Rect local, System.Action action, bool enabled = true)
            => targets.Add(new ShopKeyboardTarget(id, text, new Rect(r.position + local.position, local.size), action, enabled));
        if (app == ShopComputerApp.Coffee && CoffeeOwned)
        {
            Add("Coffee:Order", "+10 adag · 2 000 Ft", new Rect(748, 28, 372, 54), () => OrderCoffeeSupplies());
            Add("Coffee:Milk", "+10 adag · 1 500 Ft", new Rect(748, 122, 372, 54), () => OrderMilkSupplies());
            for (int i = 0; i < 3; i++)
            {
                int type = ShopCatalog.Coffee + i;
                Add("Coffee:Recipe:" + type, ShopCatalog.ShortNames[type] + "\n" + ShopCatalog.Money(ShopCatalog.SalePrice(type)),
                    new Rect(24 + i * 376, 282, 352, 88), () => SelectRecipe(type), brewRemaining == 0);
            }
        }
        if (app == ShopComputerApp.Cameras)
        {
            Add("Camera:0", "Bejárat", new Rect(24, 12, 290, 46), () => { selectedCamera = 0; RenderCamera(0); });
            Add("Camera:1", "Önkiszolgáló", new Rect(330, 12, 290, 46), () => { selectedCamera = 1; RenderCamera(1); }, shop.Layout.Expanded);
        }
        if (app != ShopComputerApp.Games) return;
        if (gameChoice == 0)
        {
            Add("Game:Snake", "Snake", new Rect(24, 124, 536, 180), StartSnake);
            Add("Game:Memory", "Memória", new Rect(592, 124, 536, 180), StartMemory);
            return;
        }
        Add("Game:Menu", "Játékok", new Rect(940, 10, 188, 44), () => gameChoice = 0);
        if (gameChoice == 1 && snake != null && snake.Ended)
            Add("Game:Restart", "Újra", new Rect(440, 412, 280, 48), StartSnake);
        if (gameChoice == 2 && memory != null)
        {
            for (int i = 0; i < 16; i++)
            {
                int card = i;
                string caption = memory.Visible(i) ? (memory.Cards[i] + 1).ToString() : "?";
                Add("Memory:" + i, caption, new Rect(270 + i % 4 * 152, 66 + i / 4 * 86, 140, 76),
                    () => ChooseMemoryCard(card), !memory.Matched[i] && memory.Second < 0 && memory.First != i);
            }
            if (memory.Won) Add("Game:Restart", "Újra", new Rect(440, 418, 280, 44), StartMemory);
        }
    }

    public void DrawComputerContent(Rect r)
    {
        GUI.BeginGroup(r);
        try
        {
            var app = shop.Devices.ComputerApp;
            if (app == ShopComputerApp.Coffee)
            {
                if (!CoffeeOwned)
                {
                    ShopComputerTheme.Label(new Rect(28, 40, 1000, 60), "Kávégép az Üzletfejlesztésben · 16 000 Ft", 26);
                    return;
                }
                ShopComputerTheme.Label(new Rect(24, 24, 700, 62), $"La Vazze kávé   {CoffeeSupplies} adag   ·   úton {pendingCoffee}", 26, true);
                ShopComputerTheme.Label(new Rect(24, 118, 700, 62), $"Albro tej   {MilkSupplies} adag   ·   úton {pendingMilk}", 26, true);
                ShopUi.Fill(new Rect(24, 214, r.width - 48, 1), ShopComputerTheme.Rule);
                ShopComputerTheme.Label(new Rect(24, 232, 700, 36), "Kiválasztott recept", 22);
                ShopComputerTheme.Label(new Rect(24, 412, r.width - 48, 40), "A gépet a raktárból, kézzel töltheted fel.", 19);
            }
            if (app == ShopComputerApp.Cameras)
            {
                if (selectedCamera == 1 && !shop.Layout.Expanded) selectedCamera = 0;
                GUI.DrawTexture(new Rect(24, 72, r.width - 48, r.height - 118), cameraImages[selectedCamera], ScaleMode.ScaleToFit);
                ShopComputerTheme.Label(new Rect(24, r.height - 38, r.width - 48, 30), "Élő kép", 19);
            }
            if (app != ShopComputerApp.Games) return;
            if (gameChoice == 0)
            {
                ShopComputerTheme.Label(new Rect(24, 24, r.width - 48, 42), "A bolt játék közben is nyitva marad.", 22);
                ShopComputerTheme.Label(new Rect(24, 326, 536, 46), "Rekord: " + PlayerPrefs.GetInt("TobaccOS.SnakeRecord", 0) + " pont", 22);
                int record = PlayerPrefs.GetInt("TobaccOS.MemoryRecord", 0);
                ShopComputerTheme.Label(new Rect(592, 326, 536, 46), "Legjobb: " + (record == 0 ? "–" : record + " lépés"), 22);
            }
            else if (gameChoice == 1 && snake != null)
            {
                ShopComputerTheme.Label(new Rect(24, 10, 860, 44), "Snake   " + snake.Score + " pont", 24, true);
                float cell = Mathf.Min(25, 330f / ShopSnake.Height);
                float left = (r.width - cell * ShopSnake.Width) * .5f;
                var board = new Rect(left, 66, cell * ShopSnake.Width, cell * ShopSnake.Height);
                ShopUi.Fill(board, ShopComputerTheme.Green);
                ShopUi.Fill(new Rect(left + snake.Food.x * cell, 66 + snake.Food.y * cell, cell - 2, cell - 2), ShopComputerTheme.Red);
                foreach (var part in snake.Body)
                    ShopUi.Fill(new Rect(left + part.x * cell, 66 + part.y * cell, cell - 2, cell - 2), ShopComputerTheme.Paper);
            }
            else if (gameChoice == 2 && memory != null)
                ShopComputerTheme.Label(new Rect(24, 10, 860, 44), $"Memória   {memory.Pairs}/8 pár   ·   {memory.Moves} lépés", 24, true);
        }
        finally { GUI.EndGroup(); }
    }
}
