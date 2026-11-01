using System;

public static class LedgerChecks
{
    public static object Run()
    {
        int n=0;var ledger=new ShopLedger(6000);
        Require(!ledger.TryBuy(150,0)&&ledger.Cash==6000,"Invalid quantity changes no money",ref n);
        Require(!ledger.TryBuy(10000,3)&&ledger.Cash==6000,"Unaffordable order rejected",ref n);
        Require(ledger.TryBuy(150,3)&&ledger.Cash==5550&&ledger.Purchases==450,"Purchase cost recorded",ref n);
        Require(!ledger.TrySell(350,150,500,100)&&ledger.Cash==5550&&ledger.Sales==0,"Wrong change cannot complete sale",ref n);
        Require(!ledger.TrySell(350,150,300,0),"Underpayment rejected",ref n);
        Require(ledger.TrySell(350,150,500,150),"Correct change accepted",ref n);
        Require(ledger.Cash==5900&&ledger.Revenue==350&&ledger.CostOfGoods==150&&ledger.Sales==1,"Sale ledger balances",ref n);
        Require(ledger.TrySell(300,120,300,0),"Exact payment needs zero change",ref n);
        ledger.Close(300);
        Require(ledger.Cash==5900&&ledger.OperatingCosts==300&&ledger.Profit==80,"Profit differs correctly from cash",ref n);
        ledger.Close(300);Require(ledger.Cash==5900,"Closing cost only charged once",ref n);
        Require(!ledger.TryBuy(100,1)&&!ledger.TrySell(350,150,500,150),"Closed ledger rejects new operations",ref n);
        Require(ledger.Cash==ledger.OpeningCash+ledger.Revenue-ledger.Purchases-ledger.OperatingCosts,"Cash-flow invariant",ref n);
        return new {passed=n,failed=0};
    }
    private static void Require(bool test,string message,ref int n){if(!test)throw new InvalidOperationException(message);n++;}
}
