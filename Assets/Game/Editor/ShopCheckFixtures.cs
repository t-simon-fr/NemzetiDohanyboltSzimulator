using UnityEngine;
public static class ShopCheckFixtures
{
    public static void Fund(MvpShopController game,int amount)
    {
        if(amount<=0)return;
        typeof(ShopLedger).GetProperty("OpeningCash").SetValue(game.Ledger,game.Ledger.OpeningCash+amount);
        typeof(ShopLedger).GetProperty("Cash").SetValue(game.Ledger,game.Ledger.Cash+amount);
        game.Drawer.Settle(amount,new int[ShopCatalog.Denominations.Length]);
    }
    public static bool Expand(MvpShopController game)
    {
        if(!game.Layout.Expanded&&game.Phase==ShopPhase.Preparing)Fund(game,ShopLayout.ExpansionPrice-2000);
        return game.TryExpand();
    }
}
