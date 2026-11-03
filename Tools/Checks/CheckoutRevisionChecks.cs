using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Object=UnityEngine.Object;
public static class CheckoutRevisionChecks
{
public static object Run(){int passed=0;Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);passed++;};var g=MvpShopController.Instance;g.GetComponent<ShopMenu>().Resume();g.ManualTick=true;var hand=new GameObject("Revision hand").transform;hand.position=Vector3.one*1000;
try{
var ledger=new ShopLedger(6000,5000);check(ledger.Cash==11000&&ledger.BankBalance==0,"Legacy opening balances merge once");check(ledger.TrySellCard(2200,1500)&&ledger.Cash==13200,"Card revenue enters cash");check(ledger.TryImprove(7500)&&ledger.Cash==5700,"Purchase uses single balance");
g.ResetDemo();g.ClosePanel();g.Ledger.TrySellCard(5000,0);g.SyncCashDrawer();check(g.Drawer.Total==11000&&g.Drawer.Total==g.Ledger.Cash,"Card money visible in actual drawer");check(g.BuyPanicButton()&&g.Drawer.Total==3500,"Upgrade debits unified drawer once");
g.ResetDemo();g.ClosePanel();g.OpenShop();typeof(MvpShopController).GetProperty("Spawned").SetValue(g,3);typeof(MvpShopController).GetMethod("SpawnCustomer",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(g,new object[]{0,false});for(int n=0;n<1000&&g.ActiveCustomer==null;n++)g.Tick(.1f);var c=g.ActiveCustomer;check(c!=null&&c.UsesCard,"Real card customer");foreach(int t in c.OrderTypes){var item=Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId==ShopCatalog.Ids[t]&&p.CurrentSlot!=null&&p.CurrentSlot.IsStockShelf);check(item.PickUp(hand),"Pick requested product");Physics.SyncTransforms();check(item.TryPlace(g.DeliverySlot),"Deliver product");}check(g.Stage==ServiceStage.Payment,"Card basket ready");int balance=g.Ledger.Cash;check(!g.ConfirmCardPayment(),"Empty terminal rejected");g.EnterCardDigit(1);check(!g.ConfirmCardPayment()&&g.Ledger.Cash==balance,"Wrong amount does not charge");g.ClearCardAmount();foreach(char digit in c.TotalPrice.ToString())g.EnterCardDigit(digit-'0');check(g.ConfirmCardPayment(),"Exact typed total accepted");check(g.Ledger.Cash==balance+c.TotalPrice&&g.Drawer.Total==g.Ledger.Cash,"Card payment reconciles physical cash");check(!g.ConfirmCardPayment(),"No double charge");
int recipe=g.Activities.SelectedRecipe;g.Activities.CycleRecipe();check(g.Activities.SelectedRecipe!=recipe,"Cycle recipe at machine");g.Activities.SelectRecipe(ShopCatalog.Latte);check(g.Activities.SelectedRecipe==ShopCatalog.Latte,"Latte selectable");
var menu=g.GetComponent<ShopMenu>();var modal=g.Modal;menu.Open();check(ShopMenu.Active&&Time.timeScale==0&&ShopInputGate.Blocked,"Pause blocks world and clock");menu.Resume();check(!ShopMenu.Active&&Time.timeScale==1&&g.Modal==modal,"Resume preserves game state");
g.ResetDemo();g.ClosePanel();g.OpenShop();g.Tick(.1f);for(int n=0;n<1000&&g.ActiveCustomer==null;n++)g.Tick(.1f);int before=g.ActiveCustomer.OrderTypes[0];foreach(var item in Object.FindObjectsByType<ShopProduct>().Where(p=>p.ProductId==ShopCatalog.Ids[before]))item.RemoveFromWorld();check(g.OfferAlternative()&&g.ActiveCustomer.OrderTypes[0]!=before,"Customer accepts stocked alternative");check(g.Ledger.Sales==0,"Dialogue does not charge");
return new {passed,failed=0};}finally{g.ResetDemo();g.ManualTick=false;Object.Destroy(hand.gameObject);}}
}
