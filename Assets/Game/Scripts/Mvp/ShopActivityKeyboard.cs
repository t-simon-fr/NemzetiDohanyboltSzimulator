using System.Collections.Generic;
using UnityEngine;

public sealed partial class ShopActivities
{
    public bool SnakeRunning=>gameChoice==1&&snake!=null&&!snake.Ended;
    public bool BackFromGame(){if(gameChoice==0)return false;gameChoice=0;return true;}
    public void ChooseMemoryCard(int index)
    {
        if(memory==null||!memory.Choose(index))return;
        if(memory.Won){int previous=PlayerPrefs.GetInt("TobaccOS.MemoryRecord",0);if(previous==0||memory.Moves<previous){PlayerPrefs.SetInt("TobaccOS.MemoryRecord",memory.Moves);PlayerPrefs.Save();}}
    }
    public void AddKeyboardTargets(List<ShopKeyboardTarget> targets,Rect rect)
    {
        float w=rect.width,h=rect.height;
        System.Action<string,string,Rect,System.Action,bool> add=(id,text,local,action,enabled)=>targets.Add(new ShopKeyboardTarget(id,text,new Rect(rect.x+local.x,rect.y+local.y,local.width,local.height),action,enabled));
        var app=shop.Devices.ComputerApp;
        if(app==ShopComputerApp.Coffee&&CoffeeOwned)add("Coffee:Order","Alapanyag rendelése",new Rect(12,105,(w-30)/2,32),()=>OrderCoffeeSupplies(),true);
        if(app==ShopComputerApp.Coffee&&CoffeeOwned)
        {
            add("Coffee:Milk","Tej rendelése",new Rect(w/2+3,105,(w-30)/2,32),()=>OrderMilkSupplies(),true);
            for(int i=0;i<3;i++){int type=ShopCatalog.Coffee+i;add("Coffee:Recipe:"+type,ShopCatalog.ShortNames[type],new Rect(12+i*(w-24)/3,h-88,(w-30)/3,32),()=>SelectRecipe(type),brewRemaining==0);}
        }
        if(app==ShopComputerApp.Furnishing){var upgrades=shop.GetComponent<ShopNextUpgrades>();if(upgrades!=null)add("Upgrade:Fridge","Hűtő",new Rect(10,h-28,w-20,26),()=>upgrades.BuyFridge(),!upgrades.FridgeOwned&&shop.Layout.Expanded);}
        if(app==ShopComputerApp.Cameras)
        {
            add("Camera:0","Bejárat",new Rect(10,8,w*.42f,35),()=>{selectedCamera=0;RenderCamera(0);},true);
            add("Camera:1","Önkiszolgáló",new Rect(w*.45f,8,w*.42f,35),()=>{selectedCamera=1;RenderCamera(1);},shop.Layout.Expanded);
        }
        if(app!=ShopComputerApp.Games)return;
        if(gameChoice==0)
        {
            add("Game:Snake","Snake",new Rect(16,70,w*.46f,60),StartSnake,true);
            add("Game:Memory","Memória",new Rect(w*.51f,70,w*.46f-16,60),StartMemory,true);return;
        }
        add("Game:Menu","Menü",new Rect(w-155,5,145,30),()=>gameChoice=0,true);
        if(gameChoice==1&&snake!=null&&snake.Ended)add("Game:Restart","Újra",new Rect(w*.25f,h-42,w*.5f,35),StartSnake,true);
        if(gameChoice==2&&memory!=null)
        {
            float cw=(w-40)/4,ch=(h-88)/4;
            for(int i=0;i<16;i++)
            {
                int index=i;add("Memory:"+i,"Kártya "+(i+1),new Rect(12+i%4*(cw+4),43+i/4*(ch+4),cw,ch),()=>ChooseMemoryCard(index),!memory.Matched[i]&&memory.Second<0&&memory.First!=i);
            }
            if(memory.Won)add("Game:Restart","Újra",new Rect(w*.25f,h-36,w*.5f,30),StartMemory,true);
        }
    }
}
