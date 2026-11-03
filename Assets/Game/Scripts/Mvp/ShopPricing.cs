using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ShopPricing:MonoBehaviour
{
    private MvpShopController shop;
    private int[] prices;
    private int selected;
    public void Configure(MvpShopController owner){shop=owner;ResetPrices();}
    public void ResetPrices(){prices=(int[])ShopCatalog.Prices.Clone();selected=0;}
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
    public void SelectNext(int direction){var types=AvailableTypes;int i=Array.IndexOf(types,SelectedType);selected=types[(i+direction+types.Length)%types.Length];}
    public void AdjustSelected(int delta){int t=SelectedType;TrySetPrice(t,Mathf.Clamp(prices[t]+delta,Minimum(t),Maximum(t)));}
    public void AddKeyboardTargets(List<ShopKeyboardTarget> targets,Rect r)
    {
        if(shop.Devices.ComputerApp!=ShopComputerApp.Prices)return;
        Action<string,string,Rect,Action> add=(id,label,bounds,action)=>targets.Add(new ShopKeyboardTarget("Price:"+id,label,new Rect(r.x+bounds.x,r.y+bounds.y,bounds.width,bounds.height),action));
        add("Previous","Előző",new Rect(10,8,70,30),()=>SelectNext(-1));add("Next","Következő",new Rect(r.width-100,8,90,30),()=>SelectNext(1));
        var deltas=new[]{-100,-5,5,100};for(int i=0;i<4;i++){int d=deltas[i];add(d.ToString(),(d>0?"+":"")+d+" Ft",new Rect(10+i*(r.width-20)/4,143,(r.width-28)/4,32),()=>AdjustSelected(d));}
        add("Reset","Alapár",new Rect(10,182,r.width-20,30),()=>TrySetPrice(SelectedType,ShopCatalog.Prices[SelectedType]));
    }
    void OnGUI()
    {
        if(shop==null||shop.Devices.FocusKind!=ShopDeviceKind.Monitor||shop.Devices.ComputerApp!=ShopComputerApp.Prices)return;
        var r=shop.Devices.AppRect;var style=new GUIStyle(GUI.skin.label){fontSize=Mathf.Clamp((int)(r.width/40),13,20),wordWrap=true,alignment=TextAnchor.MiddleCenter};
        GUI.Box(r,"");int type=SelectedType;
        GUI.Label(new Rect(r.x+82,r.y+6,r.width-184,32),ShopCatalog.Names[type],style);
        GUI.Label(new Rect(r.x+10,r.y+45,r.width-20,65),"Eladási ár: "+ShopCatalog.Money(prices[type])+"\nBeszerzés: "+ShopCatalog.Money(ShopCatalog.Costs[type])+" • Alapár: "+ShopCatalog.Money(ShopCatalog.Prices[type]),style);
        GUI.Label(new Rect(r.x+10,r.y+108,r.width-20,32),"Becsült ár-elfogadás: "+DemandPercent(type)+"%",style);
        var targets=new List<ShopKeyboardTarget>();AddKeyboardTargets(targets,r);foreach(var t in targets)if(GUI.Button(t.Bounds,t.Label,new GUIStyle(GUI.skin.button){fontSize=Mathf.Clamp((int)(r.width/60),12,18)}))t.Activate();
        GUI.Label(new Rect(r.x+10,r.yMax-47,r.width-20,44),"+ / −: 100 Ft • Shift: 5 Ft. A már leadott rendelés ára rögzített.",style);
    }
}
