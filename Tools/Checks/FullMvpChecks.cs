using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class FullMvpChecks
{
    public static object RunDay()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<MvpShopController>();
        if(!Application.isPlaying || game==null || game.Ledger==null)throw new InvalidOperationException("Start the MVP in Play Mode first.");
        int n=0;Transform hand=new GameObject("Temporary MVP Check Hand").transform;hand.position=Vector3.one*1000;
        ShopProduct held=null;game.ManualTick=true;
        try
        {
            game.ResetDemo();
            Require(game.Phase==ShopPhase.Preparing&&game.Ledger.Cash==6000,"Opening preparation",ref n);
            Require(Enumerable.Range(0,3).All(i=>game.StockCount(i)==2),"Small starting stock",ref n);
            Require(!game.OrderSupply(-1)&&!game.OrderSupply(3),"Invalid product orders rejected",ref n);
            Require(game.OrderSupply(0)&&game.Ledger.Cash==5550&&game.PendingQuantity(0)==3,"Supply order paid once",ref n);
            game.Tick(8.1f);
            Require(game.WarehouseCount(0)==3&&game.StockCount(0)==2,"Shipment stays in crate until manually stocked",ref n);
            Require(game.TryCreateSupply(0,out var supply),"Take delivered unit from crate",ref n);
            held=supply;Require(supply.PickUp(hand),"Supply item can be carried",ref n);Physics.SyncTransforms();
            var shelf=UnityEngine.Object.FindObjectsByType<ProductSlot>().First(s=>s.Occupant==null&&s.transform.parent.name=="Left aisle shelf");
            Require(supply.TryPlace(shelf)&&game.StockCount(0)==3&&game.WarehouseCount(0)==2,"Supply placed on fixed shelf",ref n);held=null;
            game.OpenShop();
            for(int i=0;i<520;i++)game.Tick(.1f);
            Require(game.QueueCount==3&&game.Spawned==3,"Automatic arrival and queue cap",ref n);
            Require(game.ActiveCustomer!=null&&game.Stage==ServiceStage.Order,"First customer at counter",ref n);
            var wrong=UnityEngine.Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId=="gum");
            held=wrong;Require(wrong.PickUp(hand),"Pick wrong product",ref n);Physics.SyncTransforms();Require(wrong.TryPlace(game.DeliverySlot),"Offer wrong product",ref n);held=null;
            Require(game.Stage==ServiceStage.Order&&game.Ledger.Sales==0&&wrong.gameObject.activeSelf,"Wrong product does not charge or consume stock",ref n);
            var spare=UnityEngine.Object.FindObjectsByType<ProductSlot>().First(s=>s.name=="Counter Slot 1");
            held=wrong;Require(wrong.PickUp(hand),"Recover wrong product",ref n);Physics.SyncTransforms();Require(wrong.TryPlace(spare),"Put wrong product aside",ref n);held=null;
            var people=new HashSet<int>();int guard=0;bool checkedChangeButtons=false;
            while(game.Ledger.Sales<game.TotalVisitors&&guard++<10000)
            {
                if(game.ActiveCustomer==null||game.Stage!=ServiceStage.Order){game.Tick(.1f);continue;}
                int type=game.ActiveCustomer.Product;people.Add(game.ActiveCustomer.Person);
                ShopProduct item=UnityEngine.Object.FindObjectsByType<ShopProduct>().FirstOrDefault(p=>p.ProductId==ShopCatalog.Ids[type]);
                if(item==null)
                {
                    Require(game.TryCreateSupply(type,out item),"Replenishment resolves stock shortage",ref n);
                    held=item;Require(item.PickUp(hand),"Carry replenished stock",ref n);Physics.SyncTransforms();
                    var empty=UnityEngine.Object.FindObjectsByType<ProductSlot>().First(s=>s.Occupant==null&&s.transform.parent.name=="Left aisle shelf");
                    Require(item.TryPlace(empty),"Restock during service",ref n);held=null;
                }
                int cashBefore=game.Ledger.Cash;int stockBefore=game.StockCount(type);int tender=game.ActiveCustomer.Tender;
                held=item;Require(item.PickUp(hand),"Pick requested item",ref n);Physics.SyncTransforms();Require(item.TryPlace(game.DeliverySlot),"Prepare requested item",ref n);held=null;
                Require(game.Stage==ServiceStage.Payment&&game.Modal==ShopModal.Cash,"Cash screen opens after preparation",ref n);
                Require(game.Ledger.Cash==cashBefore&&item.gameObject.activeSelf&&game.StockCount(type)==stockBefore,"Unpaid product remains in stock",ref n);
                int due=tender-ShopCatalog.Prices[type];
                Require(!game.SubmitChange(due+50)&&game.Ledger.Cash==cashBefore&&item.gameObject.activeSelf,"Wrong change leaves transaction recoverable",ref n);
                if(!checkedChangeButtons)
                {
                    game.AddChange(100);game.AddChange(50);Require(game.ChangeSelected==150,"Choose denominations",ref n);
                    game.UndoChange();Require(game.ChangeSelected==100,"Undo denomination",ref n);game.ClearChange();Require(game.ChangeSelected==0,"Clear denominations",ref n);checkedChangeButtons=true;
                }
                Require(game.SubmitChange(due),"Correct payment completes sale",ref n);
                Require(game.Ledger.Cash==cashBefore+ShopCatalog.Prices[type]&&game.StockCount(type)==stockBefore-1,"Money and inventory settle once",ref n);
                Require(!game.SubmitChange(due),"Cannot charge the same order twice",ref n);
                game.Tick(.1f);
            }
            Require(guard<10000&&game.Ledger.Sales==8,"Eight customers served",ref n);
            for(int i=0;i<1000&&game.Phase!=ShopPhase.Summary;i++)game.Tick(.1f);
            Require(game.Phase==ShopPhase.Summary&&game.Modal==ShopModal.Summary,"Day ends after queue and exits are resolved",ref n);
            Require(game.Missed==0&&people.Count==4,"Four recurring personalities and no unintended lost customers",ref n);
            Require(game.Ledger.Revenue==3100&&game.Ledger.CostOfGoods==1360&&game.Ledger.Profit==1440,"Daily profit is correct",ref n);
            Require(game.Ledger.Cash==8350&&game.Ledger.Purchases==450,"Cash balance counts purchase outflow separately",ref n);
            Require(Enumerable.Range(0,3).Sum(i=>game.StockCount(i)+game.WarehouseCount(i))==1,"Stock conservation across whole shift",ref n);
            Require(!game.OrderSupply(0),"Closed day rejects purchases",ref n);
            return new {passed=n,failed=0,customers=game.Ledger.Sales,revenue=game.Ledger.Revenue,profit=game.Ledger.Profit,cash=game.Ledger.Cash};
        }
        finally
        {
            if(held!=null&&held.IsHeld)held.ReturnToPickupPosition();
            UnityEngine.Object.DestroyImmediate(hand.gameObject);game.ManualTick=false;
        }
    }
    public static object CheckNextDayAndReset()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<MvpShopController>();int n=0;
        if(game.Phase!=ShopPhase.Summary)throw new InvalidOperationException("Run the complete-day check first.");
        game.ManualTick=true;
        try
        {
            int cash=game.Ledger.Cash;int remaining=Enumerable.Range(0,3).Sum(i=>game.StockCount(i)+game.WarehouseCount(i));
            game.NextDay();Require(game.Day==2&&game.Phase==ShopPhase.Preparing&&game.Ledger.Cash==cash,"Next day carries cash",ref n);
            Require(Enumerable.Range(0,3).Sum(i=>game.StockCount(i)+game.WarehouseCount(i))==remaining,"Next day carries stock",ref n);
            game.ResetDemo();Require(game.Day==1&&game.Ledger.Cash==6000&&Enumerable.Range(0,3).All(i=>game.StockCount(i)==2),"Restart rebuilds starting state",ref n);
            game.OrderSupply(0);game.OpenShop();game.RequestClose();game.Tick(.1f);
            Require(game.Phase==ShopPhase.Summary&&game.Ledger.Sales==0,"Early closing with no guests finishes safely",ref n);
            Require(game.Ledger.Cash==5250&&game.WarehouseCount(0)==3,"Paid pending goods are retained at closing",ref n);
            game.Tick(100);Require(game.Ledger.Cash==5250,"Summary cannot repeatedly charge daily costs",ref n);
            return new {passed=n,failed=0};
        }
        finally{game.ResetDemo();game.ManualTick=false;}
    }
    private static void Require(bool test,string message,ref int n){if(!test)throw new InvalidOperationException(message);n++;}
}
