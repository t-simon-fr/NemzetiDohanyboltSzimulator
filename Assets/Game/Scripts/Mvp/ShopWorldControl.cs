using UnityEngine;

public enum WorldControlKind { Order, Expand, Denomination, Undo, Clear, Pay, Exit, Exchange, OpenApp, Desktop, ExtraShelf }
public sealed class ShopWorldControl : MonoBehaviour
{
    public WorldControlKind Kind;
    public int Value;
    public void Activate(MvpShopController shop)
    {
        switch(Kind)
        {
            case WorldControlKind.OpenApp:shop.Devices.OpenApp((ShopComputerApp)Value);break;
            case WorldControlKind.Desktop:shop.Devices.OpenApp(ShopComputerApp.Desktop);break;
            case WorldControlKind.ExtraShelf:shop.BuyExtraShelf();break;
            case WorldControlKind.Order:shop.OrderSupply(Value);break;
            case WorldControlKind.Expand:shop.TryExpand();break;
            case WorldControlKind.Denomination:shop.AddChange(Value);break;
            case WorldControlKind.Undo:shop.UndoChange();break;
            case WorldControlKind.Clear:shop.ClearChange();break;
            case WorldControlKind.Pay:shop.SubmitChange(shop.ChangeSelected);break;
            case WorldControlKind.Exchange:shop.ExchangeCash();break;
            case WorldControlKind.Exit:shop.ClosePanel();break;
        }
    }
}
