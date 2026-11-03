using System.Collections.Generic;
using UnityEngine;

public sealed partial class MvpShopController
{
    private readonly List<MvpCustomerActor> fleeing=new List<MvpCustomerActor>();
    public ShopActivities Activities { get; private set; }
    public int StolenToday { get; private set; }
    public int CaughtToday { get; private set; }
    public int FleeingCount=>fleeing.Count;
    public bool CoinPayment=>active!=null && active.UsesCoins;
    public ShopProduct CreateCoffee()=>CreateProduct(ShopCatalog.Coffee);
    private void AdvanceFleeing(float dt)
    {
        for(int i=fleeing.Count-1;i>=0;i--)
        {
            var thief=fleeing[i];if(!thief.Advance(dt,Vector3.zero))continue;
            if(thief.BasketItem!=null){Ledger.RecordStockLoss(ShopCatalog.Costs[thief.Product]);StolenToday++;thief.SellBasket();Notify("Egy tolvaj kijutott az áruval. Készletveszteség: "+ShopCatalog.Money(ShopCatalog.Costs[thief.Product])+".");}
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
        if(offer.Total!=ShopCatalog.Prices[active.Product]){Notify("Pontosan a fizetendő összeget válaszd ki a vásárló érméiből.");return false;}
        int[] pieces=offer.Pieces();Drawer.OfferTenderPieces(pieces);
        if(!Ledger.TrySell(offer.Price,ShopCatalog.Costs[active.Product],offer.Price,0)){Drawer.CancelTender();return false;}
        Drawer.Settle(offer.Price,new int[ShopCatalog.Denominations.Length]);CompleteSale();return true;
    }
    private void CompleteSale()
    {
        ShopProduct sold=prepared;prepared=null;
        if(active.BasketItem==sold)active.SellBasket();else sold.AcceptDelivery();
        Activities.ClearHand();Stage=ServiceStage.Thanks;thanksTimer=2.5f;
        Dialogue=ShopPeople.Thanks[active.Person];Message="Sikeres fizetés. A kassza és a készlet frissült.";
        SetModal(ShopModal.None);changeCoins.Clear();
    }
}
