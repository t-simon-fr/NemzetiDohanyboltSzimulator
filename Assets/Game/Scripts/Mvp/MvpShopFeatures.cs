using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CardTerminalStatus { Ready, Entry, Error, Approved }

public sealed partial class MvpShopController
{
    private readonly List<MvpCustomerActor> fleeing=new List<MvpCustomerActor>();
    public ShopActivities Activities { get; internal set; }
    public int StolenToday { get; private set; }
    public int CaughtToday { get; private set; }
    public int FleeingCount=>fleeing.Count;
    public bool CardPayment=>active!=null&&active.UsesCard;
    public string CardAmount {get;private set;}="";
    public CardTerminalStatus CardStatus { get; private set; }
    public int LastApprovedCardAmount { get; private set; }
    public void EnterCardDigit(int digit)
    {
        if (Stage != ServiceStage.Payment || !CardPayment || digit < 0 || digit > 9 || CardAmount.Length >= 8) return;
        CardAmount += digit;
        CardStatus = CardTerminalStatus.Entry;
    }
    public void ClearCardAmount()
    {
        CardAmount = "";
        CardStatus = Stage == ServiceStage.Payment && CardPayment ? CardTerminalStatus.Entry : CardTerminalStatus.Ready;
    }
    public void ResetCardTerminal()
    {
        CardAmount = "";
        LastApprovedCardAmount = 0;
        CardStatus = CardTerminalStatus.Ready;
    }
    public void UndoCardDigit()
    {
        if (Stage != ServiceStage.Payment || !CardPayment) return;
        if (CardAmount.Length > 0) CardAmount = CardAmount.Substring(0, CardAmount.Length - 1);
        CardStatus = CardTerminalStatus.Entry;
    }
    public bool ConfirmCardPayment()
    {
        if(Stage!=ServiceStage.Payment||!CardPayment||!PreparedIsValid())return false;
        if(!int.TryParse(CardAmount,out int entered)||entered!=active.TotalPrice){CardStatus=CardTerminalStatus.Error;Notify("Hibás összeg. Írd be a pontos fizetendő összeget.");return false;}
        if(!TrySettleSale(() => Ledger.TrySellCard(active.TotalPrice,CurrentOrderCost)))return false;
        int paid = active.TotalPrice;
        ShopInputGate.ConsumeWorldAction();
        Drawer.CancelTender();SyncCashDrawer();ClearCardAmount();CompleteSale();
        LastApprovedCardAmount = paid;CardStatus = CardTerminalStatus.Approved;
        Notify("Sikeres kártyás fizetés. A kasszaegyenleg frissült.");return true;
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
        if(!TrySettleSale(() => Ledger.TrySell(offer.Price,CurrentOrderCost,offer.Price,0))){Drawer.CancelTender();return false;}
        Drawer.Settle(offer.Price,new int[ShopCatalog.Denominations.Length]);CompleteSale();return true;
    }
    private void CompleteSale()
    {
        Reputation?.Complete(active,ShopVisitOutcome.Served);
        active.ReturnUnusedBasket(receivedOrder.ToArray());foreach(var sold in receivedOrder.ToArray())if(sold!=null){var slot=sold.CurrentSlot;sold.AcceptDelivery();if(checkout.OwnsReceipt(slot))checkout.RemoveReceipt(slot);}
        checkout.ClearItems();prepared=null;active.SellBasket();
        Activities.ClearHand();Stage=ServiceStage.Thanks;thanksTimer=2.5f;
        Dialogue=ShopPeople.Thanks[active.Person];Message="Sikeres fizetés. A kassza és a készlet frissült.";
        SetModal(ShopModal.None);
        changeCoins.Clear();
    }
}
