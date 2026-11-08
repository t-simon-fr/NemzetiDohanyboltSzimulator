using System;
using UnityEngine;

public sealed partial class ShopMobileTopUp : MonoBehaviour
{
    private MvpShopController shop;
    public ShopMobileTopUpState Book { get; private set; } = new ShopMobileTopUpState();
    public bool Owned => Book.Owned;
    public int Provider { get; private set; } = -1;
    public string NumberEntry { get; set; } = "";
    public string AmountEntry { get; set; } = "";
    public string Feedback { get; private set; } = "Válaszd ki a szolgáltatót és add meg a vevő adatait.";
    public bool Editing { get; private set; }
    public ShopMobileRequest Request => shop.ActiveCustomer?.MobileRequest;
    public bool IsCurrent => Request != null;
    public bool CanEdit => Owned && IsCurrent && shop.Stage == ServiceStage.Order && !shop.Ledger.Closed;
    public void Configure(MvpShopController owner) { shop = owner; }
    public void ActivateForDay()
    {
        if (Book.ActivateForDay(shop.Day)) shop.Notify("Második hét, hétfő: a telefonfeltöltés automatikusan aktiválódott a terminálon.");
    }
    public void ResetAll() { Book = new ShopMobileTopUpState(Book.Config); ResetDraft(); SyncReserve(); }
    public void BeginDay() { Book.BeginDay(); ResetDraft(); SyncReserve(); ActivateForDay(); }
    public void Arrive() { Book.Cancel(); ResetDraft(); }
    private void ResetDraft()
    {
        Page = 0; Provider = -1; NumberEntry = AmountEntry = ""; Editing = false;
        Feedback = "Válaszd ki a szolgáltatót és add meg a vevő adatait.";
    }
    public void SelectProvider(int value) { if (CanEdit) Provider = value; }
    public void SelectAmount(int value) { if (CanEdit) AmountEntry = value.ToString(); }
    public void Open() { if (Owned) shop.Devices.Focus(ShopDeviceKind.Mobile); }
    public bool ConfirmDetails()
    {
        if (!CanEdit) return false;
        Feedback = Book.Validate(Request, Provider, NumberEntry, AmountEntry);
        if (Feedback != "") { shop.Notify(Feedback); return false; }
        if (!Book.Prepare(Request, Provider, NumberEntry, AmountEntry)) return false;
        Feedback = "Adatok ellenőrizve. A feltöltés csak sikeres kasszázás után történik meg.";
        shop.BeginMobilePayment(); return true;
    }
    public void CancelDraft()
    {
        if (!IsCurrent || shop.Stage == ServiceStage.Thanks) return;
        Book.Cancel(); shop.CancelMobilePayment(); ResetDraft();
        Feedback = "Megszakítva. Nem történt feltöltés vagy pénzmozgás.";
    }
    public void EndVisit() { Book.Cancel(); ResetDraft(); }
    public bool SettleSale(Func<bool> payment)
    {
        if (!Book.TryComplete(Request, shop.Day, payment)) return false;
        SyncReserve(); Feedback = "Sikeres feltöltés · Bizonylat " + Book.LastReceipt.Id;
        shop.Notify(Feedback); return true;
    }
    public void SyncReserve() => shop.Ledger?.SetMobileReserve(Book.Outstanding);
    public bool SettleOperator()
    {
        if (shop.Ledger.Closed || !Book.TrySettle(amount => shop.Ledger.TrySettleMobile(amount)))
        { Feedback = "Nincs rendezendő egyenleg vagy nincs megfelelő fedezet."; return false; }
        SyncReserve(); shop.SyncCashDrawer(); Feedback = "A szolgáltatói egyenleg rendezve."; return true;
    }
    public string RequestText => IsCurrent ? "Telefonfeltöltés: " + Request.Description : "Nincs feltöltést kérő vásárló.";
}
