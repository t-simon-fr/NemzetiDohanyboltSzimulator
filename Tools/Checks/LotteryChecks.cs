using System;
using System.Linq;
using UnityEngine;

public static class LotteryChecks
{
    public static object State()
    {
        int passed = 0;
        Action<bool,string> check = (ok,name) => { if (!ok) throw new Exception(name); passed++; };
        var state = new ShopLotteryState(106);
        var ledger = new ShopLedger(100000);
        check(!state.Order(13,1) && state.Print(13,1) == null, "Locked lottery has no stock or printed tickets");
        check(!state.TryBuy(()=>false) && !state.Owned, "Failed development debit does not unlock lottery");
        check(state.TryBuy(()=>ledger.TryImprove(12000)) && !state.TryBuy(()=>true), "Development is charged and unlocked exactly once");
        check(!state.Order(12,1) && !state.Order(13,0) && !state.Order(13,11), "Invalid consignment orders rejected");
        check(state.Order(13,10) && state.Stock(13)==0 && state.Pending(13)==10, "Pending tickets are not on hand");
        state.Tick(7.9f); check(state.Stock(13)==0, "Consignment arrival keeps delivery delay");
        state.Tick(.2f); check(state.Stock(13)==10 && state.Pending(13)==0, "Tickets arrive once");
        var first = state.Print(13,1);
        int prize = first.Prize;
        check(first != null && state.Stock(13)==9 && state.Scan(first.Id,1)==LotteryTicketStatus.Unpaid, "Printing withdraws one blank, unpaid paper cannot win");
        check(!state.TrySell(first.Id,0,1,()=>false) && !first.Sold && state.Sales==0, "Aborted sale leaves no operator liability");
        check(state.VoidUnpaid(first.Id) && !state.VoidUnpaid(first.Id) && state.Stock(13)==10, "Cancelled paper returns consignment exactly once");
        check(state.Scan(first.Id,1)==LotteryTicketStatus.Invalid && !state.TrySell(first.Id,0,1,()=>true), "Voided paper cannot be sold or redeemed");
        var sold = state.Print(13,1);
        check(state.TrySell(sold.Id,0,1,()=>ledger.TrySellCard(500,450)) && !state.TrySell(sold.Id,0,1,()=>true), "Cash posting and ticket activation occur once");
        ledger.SetLotteryReserve(state.Reserve);
        check(ledger.Profit==50 && state.Outstanding==450 && ledger.AvailableFunds==ledger.Cash-450, "Only commission is profit and stakes are reserved");
        int before=ledger.Cash;
        check(state.TrySettle(delta=>ledger.TryOperatorTransfer(delta)) && state.Outstanding==0 && ledger.Cash==before-450 && ledger.Profit==50, "Positive settlement releases liability without second expense");
        check(!state.TrySettle(delta=>true), "Repeated settlement cannot charge twice");
        check(state.Scan("unknown",1)==LotteryTicketStatus.Invalid && !state.TryRedeem("unknown",1,_=>true), "Unknown ticket cannot be paid");
        LotteryTicket winner=null, loser=null, large=null;
        for (int i=0; i<120 && (winner==null || loser==null || large==null); i++)
        {
            if(state.Stock(14)==0){state.Order(14,10);state.Tick(8.1f);}
            var t=state.Print(14,1);state.TrySell(t.Id,i%4,1,()=>true);
            var status=state.Scan(t.Id,1);
            if(status==LotteryTicketStatus.Winning)winner=t;
            if(status==LotteryTicketStatus.Losing)loser=t;
            if(status==LotteryTicketStatus.OperatorPayout)large=t;
        }
        check(winner!=null&&loser!=null&&large!=null, "Real generated series includes winning, losing and above-limit outcomes");
        int originalPrize=winner.Prize;
        check(!state.TryRedeem(winner.Id,1,_=>false) && !winner.Redeemed && winner.Prize==originalPrize, "Failed payout preserves ticket and frozen result");
        check(state.TryRedeem(winner.Id,1,_=>true) && !state.TryRedeem(winner.Id,1,_=>true) && state.Scan(winner.Id,1)==LotteryTicketStatus.Redeemed, "Winning ticket is paid once");
        check(!state.TryRedeem(loser.Id,1,_=>true) && !state.TryRedeem(large.Id,1,_=>true), "Losing and operator-only prizes never debit local cash");
        check(state.Scan(loser.Id,9)==LotteryTicketStatus.Expired && sold.Prize>=0 && first.Prize==prize, "Expiry and scans do not reroll scratch results");
        check(!ShopLotteryCatalog.TryNumbers("1 1 2 3 4",out _) && !ShopLotteryCatalog.TryNumbers("0 2 3 4 5",out _)
            && !ShopLotteryCatalog.TryNumbers("1 2 3 4",out _), "Number entry rejects duplicates, range and wrong count");
        check(ShopLotteryCatalog.TryNumbers("20, 4, 3, 2, 1",out var numbers) && numbers.SequenceEqual(new[]{1,2,3,4,20}), "Number entry accepts and normalizes five valid numbers");
        state.Order(15,2);state.Tick(8.1f);
        check(state.Print(15,1,new[]{1,1,2,3,4})==null && state.Stock(15)==2, "Invalid numbers do not consume a blank");
        var numbered=state.Print(15,1,numbers);state.TrySell(numbered.Id,1,1,()=>true);
        numbers[0]=19;
        check(numbered.Numbers[0]==1 && state.Scan(numbered.Id,1)==LotteryTicketStatus.AwaitingDraw, "Printed numbers are immutable and next-day draw is pending");
        state.AdvanceDay(2);var draw=state.DrawNumbers(2);int result=numbered.Prize;state.AdvanceDay(2);
        check(state.DrawCount==1 && draw.Length==5 && draw.Distinct().Count()==5 && state.DrawNumbers(2).SequenceEqual(draw) && numbered.Prize==result, "Next-day drawing happens once with a stable result");
        check(state.DailySales==0 && state.Sales>0 && state.Find(numbered.Id)==numbered, "Day transition resets daily totals but preserves tickets and operator book");
        var claim=state.ScheduleReturn(2);
        check(claim!=null && claim.Owner>=0 && claim.LastVisitDay==2, "Sold paper can schedule a returning original customer on a later day");
        var credit=new ShopLotteryState(7);credit.TryBuy(()=>true);LotteryTicket big=null;
        for(int i=0;i<100&&big==null;i++)
        {
            if(credit.Stock(13)==0){credit.Order(13,10);credit.Tick(8.1f);}
            var t=credit.Print(13,1);credit.TrySell(t.Id,0,1,()=>true);
            if(t.Prize>=2500&&t.Prize<=10000)big=t;
        }
        credit.TrySettle(_=>true);
        check(big!=null&&credit.TryRedeem(big.Id,1,_=>true)&&credit.Outstanding<0, "A payout can create an operator receivable");
        int deltaReceived=0;check(credit.TrySettle(delta=>{deltaReceived=delta;return true;})&&deltaReceived>0&&credit.Outstanding==0, "Negative settlement pays back cash without an extra sale");
        var reserveLedger=new ShopLedger(1000);reserveLedger.SetLotteryReserve(900);
        check(!reserveLedger.TryBuy(200,1)&&!reserveLedger.TryImprove(200)&&reserveLedger.TryBuy(100,1), "Procurement and upgrades cannot spend operator stakes");
        check(!reserveLedger.TryOperatorTransfer(-1000)&&reserveLedger.Cash==900, "Operator payout cannot overdraft physical cash");
        reserveLedger.Close(0);check(!reserveLedger.TryOperatorTransfer(100), "Finalized day rejects operator movements");
        return new{passed,failed=0};
    }
}
