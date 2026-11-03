using System;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;
public static class CheckoutBugChecks
{
 public static object Run(){int passed=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};var g=MvpShopController.Instance;g.ManualTick=true;var hand=new GameObject("Checkout check hand").transform;hand.position=Vector3.one*1000;
 try{
 g.ResetDemo();g.ClosePanel();g.Ledger.TrySellCard(5000,0);check(g.Drawer.Total<ShopSecurity.PurchasePrice&&g.Ledger.AvailableFunds>=ShopSecurity.PurchasePrice,"Bank needed for purchase");check(g.BuyPanicButton(),"Panic upgrade accepts bank funds");check(g.Ledger.Cash==3500&&g.Drawer.Total==3500&&g.Ledger.BankBalance==0,"Cash and bank debit once");
 g.ResetDemo();g.ClosePanel();g.OpenShop();typeof(MvpShopController).GetProperty("Spawned").SetValue(g,2);typeof(MvpShopController).GetMethod("SpawnCustomer",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(g,new object[]{0,false});for(int n=0;n<1000&&g.ActiveCustomer==null;n++)g.Tick(.1f);
 var c=g.ActiveCustomer;check(c!=null&&c.OrderTypes.Length==3,"Three-cigarette visit");var items=c.OrderTypes.Select(t=>Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId==ShopCatalog.Ids[t]&&p.CurrentSlot!=null&&p.CurrentSlot.IsStockShelf)).ToArray();
 check(items[0].PickUp(hand),"Pick first item");Physics.SyncTransforms();check(items[0].TryPlace(g.DeliverySlot),"First blue tray delivery");check(items[0].CurrentSlot==g.Activities.BasketSlot&&g.Activities.BasketSlot.Occupant==items[0],"First item reserves permanent basket slot");
 check(items[1].PickUp(hand),"Pick second item");Physics.SyncTransforms();check(!items[1].TryPlace(g.Activities.BasketSlot),"Cannot stack second item over first");check(items[1].TryPlace(g.DeliverySlot),"Deliver second item");check(items[2].PickUp(hand),"Pick third item");Physics.SyncTransforms();check(items[2].TryPlace(g.DeliverySlot),"Deliver third item");
 check(items.Select(p=>p.CurrentSlot).Distinct().Count()==3&&items.All(p=>!p.IsHeld),"Three distinct occupied slots");check(g.MissingOrderTypes.Length==0&&g.Stage==ServiceStage.Payment,"All three recognized for checkout");
 int stock=Enumerable.Range(0,4).Sum(g.StockCount);g.SkipCurrent();check(Enumerable.Range(0,4).Sum(g.StockCount)==stock&&g.Activities.BasketSlot.Occupant==null,"Cancellation returns every item and frees basket");
 g.ResetDemo();g.ClosePanel();g.OpenShop();g.RequestClose();g.Tick(.1f);g.StayAfterHours();check(!g.OrderSupply(0,1)&&g.Message.Contains("elszámolás"),"Closed ledger explains closed day rather than insufficient money");
 return new {passed,failed=0};
 }finally{g.ResetDemo();g.ManualTick=false;Object.Destroy(hand.gameObject);}}
}
