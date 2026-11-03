using System.Linq;
using UnityEngine;
public static class CartonWorkflowPreview
{
    public static object Carry()
    {
        var game=MvpShopController.Instance;ShopCheckFixtures.Reset(game);game.ClosePanel();game.ManualTick=true;
        game.OrderSupply(0);ShopCheckFixtures.Tick(game,8.1f);
        var player=Object.FindAnyObjectByType<PlayerInteractor>();player.GetComponent<FirstPersonController>().enabled=false;
        var body=player.GetComponent<CharacterController>();body.enabled=false;player.transform.SetPositionAndRotation(new Vector3(-1.05f,.05f,0),Quaternion.identity);body.enabled=true;
        var camera=Camera.main;camera.fieldOfView=60;camera.transform.localPosition=new Vector3(0,1.62f,0);camera.transform.localRotation=Quaternion.Euler(8,0,0);
        game.Warehouse.Dock(0).GetComponent<ShopStation>().Use(player);
        Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId=="cig_red"&&p.CurrentSlot.name.Contains("3-")).RemoveFromWorld();
        Physics.SyncTransforms();player.RefreshTarget(camera.ViewportPointToRay(new Vector3(.5f,.5f,0)));
        return new{brand=ShopCatalog.Names[player.HeldCarton.ProductType],quantity=player.HeldCarton.Quantity};
    }
    public static object Furniture()
    {
        var game=MvpShopController.Instance;ShopCheckFixtures.Reset(game);game.ClosePanel();game.ManualTick=true;game.BuyExtraShelf();
        var player=Object.FindAnyObjectByType<PlayerInteractor>();player.GetComponent<FirstPersonController>().enabled=false;
        Camera.main.fieldOfView=60;Camera.main.transform.position=new Vector3(.45f,1.65f,-.30f);Camera.main.transform.LookAt(new Vector3(2.12f,.95f,.25f));return "Purchased extra cigarette shelf";
    }
}
