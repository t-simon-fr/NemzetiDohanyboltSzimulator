using System.Collections.Generic;
using UnityEngine;

public sealed partial class MvpShopController
{
    private readonly List<MvpCustomerActor> fleeing=new List<MvpCustomerActor>();
    public ShopActivities Activities { get; private set; }
    public int StolenToday { get; private set; }
    public int CaughtToday { get; private set; }
    public int FleeingCount=>fleeing.Count;
    public bool CardPayment=>active!=null&&active.UsesCard;
    public string CardAmount {get;private set;}="";
    public void EnterCardDigit(int digit){if(Stage==ServiceStage.Payment&&CardPayment&&digit>=0&&digit<=9&&CardAmount.Length<8)CardAmount+=digit;}
    public void ClearCardAmount()=>CardAmount="";
    public void UndoCardDigit(){if(CardAmount.Length>0)CardAmount=CardAmount.Substring(0,CardAmount.Length-1);}
    public bool ConfirmCardPayment()
    {
        if(Stage!=ServiceStage.Payment||!CardPayment||!PreparedIsValid())return false;
        if(!int.TryParse(CardAmount,out int entered)||entered!=active.TotalPrice){Notify("Hibás összeg. Írd be a pontos fizetendő összeget.");return false;}
        if(!Ledger.TrySellCard(active.TotalPrice,active.TotalCost))return false;
        Drawer.CancelTender();SyncCashDrawer();ClearCardAmount();CompleteSale();Notify("Sikeres kártyás fizetés. A kasszaegyenleg frissült.");return true;
    }
    public void SyncCashDrawer(){int spent=Drawer.Total-Mathf.Max(0,Ledger.Cash);if(spent>0)Drawer.Spend(spent);else if(spent<0)Drawer.AddFunds(-spent);}
    public bool CoinPayment=>active!=null && active.UsesCoins;
    public ShopProduct CreateCoffee(int type=ShopCatalog.Coffee)=>CreateProduct(type);
    private void AdvanceFleeing(float dt)
    {
        for(int i=fleeing.Count-1;i>=0;i--)
        {
            var thief=fleeing[i];if(!thief.Advance(dt,Vector3.zero))continue;
            if(thief.BasketItem!=null){int loss=thief.StolenCost;Ledger.RecordStockLoss(loss);StolenToday++;thief.SellBasket();Notify("Egy tolvaj kijutott az áruval. Készletveszteség: "+ShopCatalog.Money(loss)+".");}
            thief.gameObject.SetActive(false);Destroy(thief.gameObject);fleeing.RemoveAt(i);
        }
    }
    public bool StopThief(MvpCustomerActor thief)
    {
        if(thief==null||!thief.IsStealing||!fleeing.Remove(thief))return false;
        thief.StopTheft();thief.Leave();departing.Add(thief);CaughtToday++;
        Notify("Megállítottad a tolvajt! Az eldobott árut újra felveheted.");return true;
    }
    public void ToggleCustomerCoin(int index)
    {
        if(Stage!=ServiceStage.Payment||!CoinPayment||Activities.CoinOffer==null)return;
        Activities.CoinOffer.Toggle(index);Activities.RefreshHandCoins();
    }
    public void UndoCustomerCoin()
    {
        if(Stage!=ServiceStage.Payment||!CoinPayment||Activities.CoinOffer==null)return;
        Activities.CoinOffer.Undo();Activities.RefreshHandCoins();
    }
    public bool ConfirmCustomerCoins()
    {
        if(Stage!=ServiceStage.Payment||!CoinPayment||!PreparedIsValid()||Activities.CoinOffer==null)return false;
        var offer=Activities.CoinOffer;
        if(offer.Total!=active.TotalPrice){Notify("Pontosan a fizetendő összeget válaszd ki a vásárló érméiből.");return false;}
        int[] pieces=offer.Pieces();Drawer.OfferTenderPieces(pieces);
        if(!Ledger.TrySell(offer.Price,active.TotalCost,offer.Price,0)){Drawer.CancelTender();return false;}
        Drawer.Settle(offer.Price,new int[ShopCatalog.Denominations.Length]);CompleteSale();return true;
    }
    private void CompleteSale()
    {
        active.ReturnUnusedBasket(receivedOrder);foreach(var sold in receivedOrder.ToArray())if(sold!=null){var slot=sold.CurrentSlot;sold.AcceptDelivery();if(slot!=null&&slot.name.StartsWith("Order receipt"))Destroy(slot.gameObject);}
        receivedOrder.Clear();prepared=null;active.SellBasket();
        Activities.ClearHand();Stage=ServiceStage.Thanks;thanksTimer=2.5f;
        Dialogue=ShopPeople.Thanks[active.Person];Message="Sikeres fizetés. A kassza és a készlet frissült.";
        SetModal(ShopModal.None);changeCoins.Clear();
    }
}
