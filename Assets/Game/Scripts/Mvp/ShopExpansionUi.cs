using System.Collections.Generic;
using UnityEngine;

public sealed partial class ShopDevices
{
    private void AddExpansionControls(List<ShopKeyboardTarget> controls)
    {
        var geometry=shop.GetComponent<ShopExpansionGeometry>();
        controls.Add(new ShopKeyboardTarget("Warehouse:Expand","Bővítem",new Rect(ComputerBody.xMax-226,ComputerBody.y+12,216,48),
            ()=>geometry.BuyWarehouse(),shop.CanDevelopShop&&shop.Layout.WarehouseLevel<3&&shop.Ledger.AvailableFunds>=shop.Layout.NextWarehousePrice));
        for(int i=0;i<4;i++)
        {
            int index=i;
            controls.Add(new ShopKeyboardTarget("Module:Buy:"+i,"Megveszem",new Rect(ComputerBody.xMax-226,ComputerBody.y+72+i*60,216,48),
                ()=>shop.GetComponent<ShopModularFurniture>().Buy((ShopModuleKind)index),
                shop.CanDevelopShop&&shop.Ledger.AvailableFunds>=ShopModularFurniture.Prices[i]));
        }
        controls.Add(new ShopKeyboardTarget("Arrange:0","Üzlet átrendezése",new Rect(ComputerBody.x,ComputerBody.yMax-54,310,48),()=>BeginArrangement(),shop.CanDevelopShop));
    }
    private void DrawExpansionRows()
    {
        string[] labels={"Raktár · "+(shop.Layout.WarehouseLevel+1)+"/4 szint",ShopModularFurniture.Names[0],ShopModularFurniture.Names[1],ShopModularFurniture.Names[2],ShopModularFurniture.Names[3]};
        for(int i=0;i<labels.Length;i++)
        {
            float y=ComputerBody.y+12+i*60;
            int cost=i==0?shop.Layout.NextWarehousePrice:ShopModularFurniture.Prices[i-1];
            ShopComputerTheme.Label(new Rect(ComputerBody.x+18,y,560,48),labels[i],24);
            ShopComputerTheme.Label(new Rect(ComputerBody.x+620,y,230,48),cost==0?"Kész":ShopCatalog.Money(cost),22,true);
        }
        ShopComputerTheme.Label(new Rect(ComputerBody.x+18,ComputerBody.y+326,1080,56),
            "Üzlet: "+(5+shop.Layout.ShopLevel*2)+" × 6,3 m  ·  Raktár: "+
            (shop.Layout.WarehouseArea.width+.2f).ToString("0.0")+" × "+(shop.Layout.WarehouseArea.height+.2f).ToString("0.0")+" m",21);
    }
}
