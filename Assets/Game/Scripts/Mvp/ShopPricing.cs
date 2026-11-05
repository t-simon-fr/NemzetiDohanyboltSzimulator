using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed partial class ShopPricing:MonoBehaviour
{
    private MvpShopController shop;
    private int[] prices;
    private int selected;
    public void Configure(MvpShopController owner){shop=owner;ResetPrices();}
    public void ResetPrices(){prices=(int[])ShopCatalog.Prices.Clone();selected=0;ResetEditor();}
    public int Price(int type)=>prices[type];
    public int[] AvailableTypes=>Enumerable.Range(0,shop.AvailableProducts).Concat(shop.Activities.CoffeeOwned?new[]{ShopCatalog.Coffee,ShopCatalog.LongCoffee,ShopCatalog.Latte}:Array.Empty<int>()).Concat(shop.StockCount(ShopCatalog.Contraband)>0?new[]{ShopCatalog.Contraband}:Array.Empty<int>()).ToArray();
    public int SelectedType=>AvailableTypes.Contains(selected)?selected:0;
    public static int Minimum(int type)=>Mathf.CeilToInt(ShopCatalog.Prices[type]*.5f/5)*5;
    public static int Maximum(int type)=>ShopCatalog.Prices[type]*3;
    public bool TrySetPrice(int type,int value)
    {
        if(shop.Phase==ShopPhase.Summary||!AvailableTypes.Contains(type)||value%5!=0||value<Minimum(type)||value>Maximum(type))return false;
        prices[type]=value;shop.Notify(ShopCatalog.ShortNames[type]+": "+ShopCatalog.Money(value)+". Az újonnan érkező vásárlókra érvényes.");
        foreach(var label in FindObjectsByType<ShopShelfPriceLabel>())label.Refresh();return true;
    }
    public static bool Accepts(int type,int price,int visit,int day)
    {
        int budgetPercent=100+((visit+day-1)%5)*10;
        return (long)price*100<=(long)ShopCatalog.Prices[type]*budgetPercent;
    }
    public int DemandPercent(int type)=>Enumerable.Range(0,5).Count(v=>Accepts(type,prices[type],v,1))*20;
    public void SelectNext(int direction){var types=AvailableTypes;int i=Array.IndexOf(types,SelectedType);ChooseProduct(types[(i+direction+types.Length)%types.Length]);}
    public void AdjustSelected(int delta){int t=SelectedType;TrySetPrice(t,Mathf.Clamp(prices[t]+delta,Minimum(t),Maximum(t)));}
    public void AddKeyboardTargets(List<ShopKeyboardTarget> targets,Rect r)
    {
        if(shop.Devices.ComputerApp!=ShopComputerApp.Prices)return;
        for (int i=0;i<4;i++) {
            int category=i;
            targets.Add(new ShopKeyboardTarget("Price:Category:"+i,CategoryNames[i],new Rect(r.x+i*r.width/4,r.y,r.width/4-8,44),()=>ChooseCategory((ShopPriceCategory)category)));
        }
        if(VisibleTypes.Length==0)return;
        float x=r.x+r.width*.32f+32;
        float fw=Mathf.Min(300,r.width-r.width*.32f-32-200);
        targets.Add(new ShopKeyboardTarget("Price:Edit","Ár szerkesztése",new Rect(x+74,r.y+168,fw,62),BeginEdit));
        targets.Add(new ShopKeyboardTarget("Price:Minus","-",new Rect(x,r.y+168,60,62),()=>StepEditor(-1,Keyboard.current!=null&&Keyboard.current.shiftKey.isPressed)));
        targets.Add(new ShopKeyboardTarget("Price:Plus","+",new Rect(x+74+fw+58,r.y+168,60,62),()=>StepEditor(1,Keyboard.current!=null&&Keyboard.current.shiftKey.isPressed)));
        targets.Add(new ShopKeyboardTarget("Price:Reset","Alapár",new Rect(x,r.y+330,260,44),()=>{CancelEdit();TrySetPrice(SelectedType,ShopCatalog.Prices[SelectedType]);}));
        var visible=VisibleTypes;
        for(int i=0;i<visible.Length;i++) {
            int type=visible[i];
            float y=r.y+60+i*68-productScroll.y;
            if(y<r.y+60||y+60>r.yMax)continue;
            targets.Add(new ShopKeyboardTarget("Price:Product:"+type,ShopCatalog.ShortNames[type],new Rect(r.x,y,r.width*.32f-28,60),()=>ChooseProduct(type)));
        }
    }
}
