using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public static class DoorStorageChecks
{
    public static object Run()
    {
        int passed=0;
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);passed++;};
        var g=MvpShopController.Instance;
        var player=Object.FindAnyObjectByType<PlayerInteractor>();
        g.ManualTick=true;
        try
        {
            g.ResetDemo();
            var sign=GameObject.Find("Open Close Sign");
            check(g.Entrance.Sign.parent==g.Entrance.DoorLeaf && sign.transform.parent==g.Entrance.Sign,"Sign is attached to moving door");
            g.Entrance.Tick(1);
            var closed=g.Entrance.Sign.Find("Closed sign face");
            var open=g.Entrance.Sign.Find("Open sign face");
            check(Vector3.Dot(-closed.forward,-g.Entrance.DoorLeaf.forward)>.99f,"Closed label faces street");
            sign.GetComponent<ShopStation>().Use(player);
            g.Entrance.Tick(1);
            check(g.Phase==ShopPhase.Open && Vector3.Dot(-open.forward,-g.Entrance.DoorLeaf.forward)>.99f,"Open label faces street after flipping");
            Vector3 position=g.Entrance.Sign.position;
            g.Entrance.RequestPassage(g.Layout.EntryInside);
            g.Entrance.Tick(.25f);
            check(g.Entrance.IsOpen && Vector3.Distance(position,g.Entrance.Sign.position)>.3f,"Sign follows opening NPC door");
            sign.GetComponent<ShopStation>().Use(player);
            g.Entrance.Tick(1);
            check(g.Phase==ShopPhase.Closing && Vector3.Dot(-closed.forward,-g.Entrance.DoorLeaf.forward)>.99f,"Closing restores outward closed face");
            g.ResetDemo();
            var slots=g.CounterStorage.Slots;
            check(slots.Length==10 && slots.All(s=>s.Occupant==null),"Ten empty private slots across two shelves");
            var ordinary=g.CreateCoffee(0);
            check(!slots[0].CanAccept(ordinary)&&!slots[0].CanAcceptStock(ordinary.ProductId),"Private slots reject normal goods and cartons");
            ordinary.RemoveFromWorld();
            int money=g.Ledger.Cash;
            foreach(var slot in slots)check(g.CreateNightProduct().AssignInitialSlot(slot),"Store physical contraband pack");
            check(g.StockCount(ShopCatalog.Contraband)==10 && g.Ledger.Cash==money,"Storage preserves stock and money");
            var item=slots[0].Occupant;
            check(player.TryHold(item),"Take pack from under counter");
            Physics.SyncTransforms();
            check(item.TryPlace(slots[0]),"Return held pack to open compartment");
            player.CancelHeld();
            g.SingleProductVisits=true;
            g.OpenShop();
            typeof(MvpShopController).GetMethod("SpawnCustomer",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(g,new object[]{ShopCatalog.Contraband,false});
            typeof(MvpShopController).GetProperty("Spawned").SetValue(g,g.TotalVisitors);
            for(int i=0;i<2000&&g.ActiveCustomer==null;i++)g.Tick(.1f);
            check(g.ActiveCustomer!=null&&g.ActiveCustomer.Product==ShopCatalog.Contraband,"Real customer requests private stock");
            check(player.TryHold(item),"Retrieve requested private pack");
            Physics.SyncTransforms();
            check(item.TryPlace(g.DeliverySlot)&&g.Stage==ServiceStage.Payment,"Private pack enters normal checkout");
            player.CancelHeld();
            int due=g.ActiveCustomer.Tender-g.ActiveCustomer.TotalPrice;
            foreach(int value in ShopCatalog.Denominations)while(due>=value){g.AddChange(value);due-=value;}
            check(g.SubmitChange(g.ChangeSelected)&&g.StockCount(ShopCatalog.Contraband)==9,"Sell once without duplicate private stock");
            g.RequestClose();
            for(int i=0;i<3000&&!g.CanEndDay;i++)g.Tick(.1f);
            var body=player.GetComponent<CharacterController>();body.enabled=false;player.transform.position=g.Layout.EntryInside;body.enabled=true;
            check(g.TryEndDay(),"Door still closes the day separately from sign");
            g.NextDay();
            check(slots.Count(s=>s.Occupant!=null)==9,"Private stock survives next day");
            g.ResetDemo();
            check(slots.All(s=>s.Occupant==null),"New game clears private stock");
            return new {passed,failed=0};
        }
        finally{player.CancelHeld();g.ResetDemo();g.ManualTick=false;}
    }
}
