using System;
using System.Linq;

// Selected pieces are reservations. Customer money is kept separate until settlement.
public sealed class ShopCashDrawer
{
    private readonly int[] counts=new int[ShopCatalog.Denominations.Length];
    private readonly int[] offered=new int[ShopCatalog.Denominations.Length];
    public int Total=>Value(counts);
    public int PendingTender=>Value(offered);
    private static int Value(int[] pieces)=>pieces.Select((n,i)=>n*ShopCatalog.Denominations[i]).Sum();
    public int Count(int value){int i=Array.IndexOf(ShopCatalog.Denominations,value);return i<0?0:counts[i]+offered[i];}
    public ShopCashDrawer(){int[] initial={0,0,0,1,1,2,4,6,6,10,8,4};Array.Copy(initial,counts,counts.Length);}
    public bool CanSelect(int value,int selected)=>Count(value)>selected;
    public void OfferTender(int tender){CancelTender();Deposit(offered,tender);}
    public void CancelTender()=>Array.Clear(offered,0,offered.Length);
    public bool CanSettle(int tender,int[] selected)
    {
        if(tender<0||tender%5!=0||selected.Length!=counts.Length||(PendingTender!=0&&PendingTender!=tender))return false;
        for(int i=0;i<counts.Length;i++)if(selected[i]<0||selected[i]>counts[i]+offered[i])return false;
        return true;
    }
    public void Settle(int tender,int[] selected)
    {
        if(!CanSettle(tender,selected))throw new InvalidOperationException("Cash reservation is no longer available.");
        if(PendingTender>0){for(int i=0;i<counts.Length;i++)counts[i]+=offered[i];CancelTender();}
        else Deposit(counts,tender);
        for(int i=0;i<counts.Length;i++)counts[i]-=selected[i];
    }
    // Supplier/account payments preserve small pieces, exchanging a larger note if needed.
    public void Spend(int amount)
    {
        if(amount<0||amount>Total||amount%5!=0)throw new ArgumentOutOfRangeException(nameof(amount));
        int balance=Total-amount;
        for(int i=counts.Length-1;i>=0;i--)
        {
            int keep=Math.Min(counts[i],balance/ShopCatalog.Denominations[i]);counts[i]=keep;balance-=keep*ShopCatalog.Denominations[i];
        }
        Deposit(counts,balance);
    }
    public bool Exchange(int value)
    {
        int index=Array.IndexOf(ShopCatalog.Denominations,value);
        if(index<0||index==counts.Length-1||counts[index]+offered[index]==0)return false;
        int[] source=counts[index]>0?counts:offered;source[index]--;int amount=value;
        for(int i=index+1;i<counts.Length;i++){source[i]+=amount/ShopCatalog.Denominations[i];amount%=ShopCatalog.Denominations[i];}
        return true;
    }
    private static void Deposit(int[] target,int amount)
    {for(int i=0;i<target.Length;i++){target[i]+=amount/ShopCatalog.Denominations[i];amount%=ShopCatalog.Denominations[i];}}
}
