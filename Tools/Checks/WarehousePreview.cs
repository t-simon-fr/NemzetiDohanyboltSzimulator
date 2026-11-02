using System.Linq;
using UnityEngine;

public static class WarehousePreview
{
    public static object Room()
    {
        var game=MvpShopController.Instance;game.ResetDemo();game.ClosePanel();game.ManualTick=true;
        var player=Object.FindAnyObjectByType<PlayerInteractor>();
        for(int i=0;i<3;i++)
        {
            int type=i;var product=Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId==ShopCatalog.Ids[type]);
            player.TryHold(product);game.ReturnSupply(player,type);
        }
        game.TryExpand();for(int i=3;i<6;i++)game.OrderSupply(i);game.Tick(8.1f);
        player.GetComponent<FirstPersonController>().enabled=false;ShopInputGate.Blocked=true;
        var camera=Camera.main;camera.fieldOfView=72;camera.transform.position=new Vector3(-2.85f,1.9f,1.65f);camera.transform.LookAt(new Vector3(-4.65f,1f,-.2f));
        return "Six branded cartons in warehouse";
    }
    public static object Yard()
    {
        Camera.main.fieldOfView=60;Camera.main.transform.position=new Vector3(-4.25f,1.65f,2.65f);Camera.main.transform.LookAt(new Vector3(-4.25f,.9f,4.9f));return "Rear yard paper dumpster";
    }
    public static object Entrance()
    {
        Camera.main.fieldOfView=65;Camera.main.transform.position=new Vector3(-.5f,1.7f,.2f);Camera.main.transform.LookAt(new Vector3(-2.7f,1.4f,.1f));return "Cashier-side warehouse entrance";
    }
}
