using System;
using System.Linq;
using UnityEngine;

public static class LotteryChecks
{
    public static object Numbers()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var g=MvpShopController.Instance;var player=UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();
        try
        {
            player.CancelHeld();g.ClosePanel();g.ResetDemo();g.ManualTick=true;g.AutomaticArrivals=false;
            ShopCheckFixtures.Fund(g,60000);g.Lottery.TryBuy();g.Lottery.Order(15);g.Tick(8.1f);g.OpenShop();
            typeof(MvpShopController).GetProperty("Spawned").SetValue(g,3);
            typeof(MvpShopController).GetMethod("SpawnCustomer",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(g,new object[]{15,false});
            for(int i=0;i<1500&&g.ActiveCustomer==null;i++)g.Tick(.1f);
            check(g.ActiveCustomer!=null&&g.ActiveCustomer.Product==15&&!g.ActiveCustomer.LotteryQuickPick&&g.CardPayment,"NPC requests explicit lottery numbers and normal card payment");
            int stock=g.Lottery.Book.Stock(15);g.Lottery.NumberEntry="1 1 2 3 4";
            check(!g.Lottery.Take(15,player)&&!player.HasHeldItem&&g.Lottery.Book.Stock(15)==stock,"Duplicate number input does not print or consume stock");
            var requested=g.ActiveCustomer.LotteryNumbers;
            var wrong=Enumerable.Range(1,20).Where(n=>!requested.Contains(n)).Take(5).ToArray();
            g.Lottery.NumberEntry=string.Join(" ",wrong);
            check(!g.Lottery.Take(15,player)&&g.Lottery.Book.Stock(15)==stock,"Valid but wrong customer numbers are rejected before printing");
            g.Lottery.NumberEntry=string.Join(", ",requested.Reverse());
            check(g.Lottery.Take(15,player),"Correct normalized numbers print one real paper");
            var item=player.HeldProduct;var id=item.GetComponent<ShopLotteryPaper>().TicketId;
            check(g.Lottery.Book.Find(id).Numbers.SequenceEqual(requested)&&g.Lottery.Book.Stock(15)==stock-1,"Printed paper freezes the requested numbers and consumes one blank");
            player.CancelHeld();check(item.AssignInitialSlot(g.DeliverySlot),"Number ticket fits scanner handover");g.DeliverySlot.NotifyProductPlaced(item);
            check(g.Stage==ServiceStage.Payment&&g.CardPayment,"Number paper hands over into existing physical card flow");
            g.EnterCardDigit(1);check(!g.ConfirmCardPayment()&&!g.Lottery.Book.Find(id).Sold,"Incorrect card amount cannot activate number ticket");
            g.ClearCardAmount();foreach(char digit in "500")g.EnterCardDigit(digit-'0');int cash=g.Ledger.Cash;
            check(g.ConfirmCardPayment()&&g.Ledger.Cash==cash+500&&g.Drawer.Total==g.Ledger.Cash&&g.Lottery.Book.Find(id).Sold,"Exact card payment activates the printed number paper once");
            check(!g.ConfirmCardPayment()&&g.Lottery.Book.Scan(id,1)==LotteryTicketStatus.AwaitingDraw,"Repeated card approval cannot change a pending draw");
            for(int i=0;i<300;i++)g.Tick(.1f);g.RequestClose();for(int i=0;i<300;i++)g.Tick(.1f);
            typeof(MvpShopController).GetMethod("FinishDay",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(g,null);g.NextDay();
            var draw=g.Lottery.Book.DrawNumbers(2);int prize=g.Lottery.Book.Find(id).Prize;g.Lottery.AdvanceDay();
            check(draw.Length==5&&g.Lottery.Book.DrawCount==1&&g.Lottery.Book.Find(id).Prize==prize&&g.Lottery.Book.Find(id).Numbers.SequenceEqual(requested),"Real next-day transition resolves one stable draw without changing ticket numbers");
            check(g.Lottery.Book.Sales==500&&g.Lottery.Book.Outstanding==450&&g.Ledger.LotteryReserve==450,"Unsettled operator money survives replacement of the daily ledger");
            check(!g.Pricing.TrySetPrice(15,1000),"Operator-set ticket price cannot be edited as ordinary shop stock");
            return new{passed,failed=0};
        }
        finally{player.CancelHeld();g.ClosePanel();g.ResetDemo();g.ManualTick=false;}
    }

    public static object Integration()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var g=MvpShopController.Instance;var player=UnityEngine.Object.FindAnyObjectByType<PlayerInteractor>();
        var spawn=typeof(MvpShopController).GetMethod("SpawnCustomer",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        void Spawn(int type)
        {
            spawn.Invoke(g,new object[]{type,false});
            for(int i=0;i<1500&&g.ActiveCustomer==null;i++)g.Tick(.1f);
            if(g.ActiveCustomer==null)throw new Exception("Lottery customer did not reach counter");
        }
        void Drain()
        {
            for(int i=0;i<1000&&(g.ActiveCustomer!=null||g.QueueCount>0||!g.CanEndDay);i++)
            {g.Tick(.1f);if(g.ActiveCustomer==null&&g.QueueCount==0&&i>100)break;}
        }
        try
        {
            player.CancelHeld();g.ClosePanel();g.ResetDemo();g.ManualTick=true;g.AutomaticArrivals=false;g.SingleProductVisits=true;
            check(!g.Lottery.Owned&&!g.Lottery.Terminal.gameObject.activeSelf&&!g.Lottery.TryBuy(),"Lottery hidden and unaffordable in starting shop");
            check(!g.CreateCustomerOrder(13,0).Any(ShopLotteryCatalog.IsTicket),"No lottery requests before development and procurement");
            ShopCheckFixtures.Fund(g,60000);int before=g.Ledger.Cash;
            check(g.Lottery.TryBuy()&&g.Ledger.Cash==before-12000&&!g.Lottery.TryBuy(),"Real lottery development debits once and displays its physical terminal");
            check(g.Lottery.Terminal.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).All(c=>
                g.Devices.TillAssembly.GetComponentsInChildren<Renderer>().All(r=>!c.bounds.Intersects(r.bounds))),"Lottery furniture does not overlap cash register or card terminal");
            var body=player.GetComponent<CharacterController>();var originalPosition=player.transform.position;
            body.enabled=false;player.transform.position=ShopFloorPlan.StaffSpawn;body.enabled=true;Physics.SyncTransforms();
            for(int i=0;i<300;i++)
            {
                var delta=ShopFloorPlan.StaffExit-player.transform.position;delta.y=0;
                if(delta.magnitude<.08f)break;body.Move(Vector3.ClampMagnitude(delta,.04f));
            }
            check(Vector3.ProjectOnPlane(player.transform.position-ShopFloorPlan.StaffExit,Vector3.up).magnitude<.1f,"Lottery extension preserves actual player route to stockroom");
            body.enabled=false;player.transform.position=originalPosition;body.enabled=true;Physics.SyncTransforms();
            check(g.Lottery.Order(13)&&g.AvailableInventory(13)==0,"Pending consignment cannot enter NPC orders");g.Tick(8.1f);
            check(g.Lottery.Book.Stock(13)==5&&g.AvailableInventory(13)==5&&g.Lottery.Feedback.Contains("Megérkezett"),"Delivered consignment enters available assortment with current status");
            var request1=g.CreateCustomerOrder(13,0);var request2=g.CreateCustomerOrder(13,0);
            check(request1.SequenceEqual(new[]{13})&&request2.SequenceEqual(request1)&&g.Lottery.Book.Stock(13)==5,"Queued orders do not reserve tickets or mix with ordinary goods");
            var view=Camera.main;var pose=new Pose(view.transform.localPosition,view.transform.localRotation);
            g.Devices.Focus(ShopDeviceKind.Lottery);
            check(g.Devices.FocusKind==ShopDeviceKind.Lottery&&ShopInputGate.Blocked,"Lottery focus owns cursor/input");g.ClosePanel();
            check(Vector3.Distance(view.transform.localPosition,pose.position)<.001f&&Quaternion.Angle(view.transform.localRotation,pose.rotation)<.01f&&!ShopInputGate.Blocked,"Closing terminal restores exact camera and input");
            foreach(int page in new[]{0,1,2,3})
            {
                g.Lottery.SelectPage(page);
                check(g.Lottery.Controls(ShopLotteryRetailer.TerminalBody).All(c=>ShopLotteryRetailer.TerminalBody.Contains(c.Bounds.min)&&ShopLotteryRetailer.TerminalBody.Contains(c.Bounds.max-Vector2.one*.1f)),"All lottery controls fit their panel: "+page);
            }
            g.OpenShop();Spawn(13);
            check(g.ActiveCustomer.Product==13&&g.Dialogue.Contains("Falusi Fortuna"),"Real NPC asks for an unlocked stocked scratch ticket");
            check(g.Lottery.Take(13,player)&&player.HeldProduct.GetComponent<ShopLotteryPaper>()!=null,"Rack selection creates one physical identifiable ticket in hand");
            var item=player.HeldProduct;var id=item.GetComponent<ShopLotteryPaper>().TicketId;int frozenPrize=g.Lottery.Book.Find(id).Prize;
            Physics.SyncTransforms();check(item.TryGetPlacement(g.DeliverySlot,out _),"Real physical ticket placement is unobstructed on barcode scanner");
            check(g.Lottery.Book.Scan(id,g.Day)==LotteryTicketStatus.Unpaid,"Physical unpaid paper is not eligible for redemption");
            player.CancelHeld();check(item.AssignInitialSlot(g.DeliverySlot),"Physical scratch ticket fits scanner slot");g.DeliverySlot.NotifyProductPlaced(item);
            check(g.Stage==ServiceStage.Payment&&g.Scanner.LastProductId==ShopCatalog.Ids[13],"Barcode handover uses existing multi-item checkout");
            int cash=g.Ledger.Cash;
            check(g.SubmitChange(0)&&g.Lottery.Book.Find(id).Sold&&g.Ledger.Cash==cash+500&&g.Drawer.Total==g.Ledger.Cash,"Cash sale activates ticket and updates physical drawer exactly once");
            check(!g.SubmitChange(0)&&g.Lottery.Book.Sales==500&&g.Ledger.LotteryReserve==450&&g.Ledger.Profit==50,"Repeated payment cannot duplicate operator liability or commission");
            Drain();Spawn(13);check(g.Lottery.Take(13,player),"Next customer can pick a separate ticket");int stock=g.Lottery.Book.Stock(13);
            g.SkipCurrent();check(!player.HasHeldItem&&g.Lottery.Book.Stock(13)==stock+1,"Skipping cancels held unpaid ticket and restores consignment");Drain();
            // Produce real seeded winning outcomes, using the same ledger callback as retail checkout.
            var winner=g.Lottery.Book.Find(id);
            if(winner.Prize==0)g.Lottery.Book.FinishReturn(id,g.Day);
            for(int i=0;i<100&&(winner.Prize==0||winner.Prize>10000);i++)
            {
                if(g.Lottery.Book.Stock(13)==0){g.Lottery.Order(13);g.Tick(8.1f);}
                var t=g.Lottery.Book.Print(13,g.Day);
                g.Lottery.Book.TrySell(t.Id,1,g.Day,()=>g.Ledger.TrySellCard(500,450));
                if(t.Prize>0&&t.Prize<=10000)winner=t;else g.Lottery.Book.FinishReturn(t.Id,g.Day);
            }
            g.Lottery.SyncReserve();g.SyncCashDrawer();
            check(winner.Prize>0&&winner.Prize<=10000,"Seeded retail ledger has a locally payable winning paper");
            g.RequestClose();Drain();
            typeof(MvpShopController).GetMethod("FinishDay",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(g,null);
            int sales=g.Lottery.Book.Sales;g.NextDay();
            check(g.Day==2&&g.Lottery.Owned&&g.Lottery.Book.Find(winner.Id).Prize==winner.Prize&&g.Lottery.Book.Sales==sales,"Development, ticket result and operator liability survive real next-day transition");
            g.SingleProductVisits=false;g.OpenShop();
            typeof(MvpShopController).GetProperty("Spawned").SetValue(g,1);Spawn(-1);
            check(g.ActiveCustomer.LotteryClaimId!=null&&g.ActiveCustomer.Person==g.Lottery.Book.Find(g.ActiveCustomer.LotteryClaimId).Owner,"Original NPC returns with a real owned ticket on a later day");
            check(g.Lottery.ScanCurrent(),"Terminal reads the returning NPC paper");
            var claim=g.Lottery.Book.Find(g.Lottery.ScannedId);cash=g.Ledger.Cash;
            check(g.Lottery.PayPrize()&&g.Ledger.Cash==cash-claim.Prize&&g.Drawer.Total==g.Ledger.Cash&&claim.Redeemed,"Confirmed prize debits the shared physical till");
            check(!g.Lottery.PayPrize()&&g.Stage==ServiceStage.Thanks,"Repeated prize approval cannot pay twice");
            check(g.Lottery.SettleOperator()&&g.Lottery.Book.Outstanding==0&&!g.Lottery.SettleOperator(),"TobaccOS settlement reconciles once");
            check(g.Ledger.LotteryReserve==0&&g.Ledger.AvailableFunds==g.Ledger.Cash,"Operator settlement releases only the actual remaining liability");
            return new{passed,failed=0};
        }
        finally{player.CancelHeld();g.ClosePanel();g.ResetDemo();g.ManualTick=false;}
    }

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
        check(!ShopLotteryCatalog.TryNumbers("-1 2 3 4 5",out _), "Negative sign cannot be mistaken for a number separator");
        check(!ShopLotteryCatalog.TryNumbers("1-2-3-4-5",out _), "Only the advertised number separators are accepted");
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
