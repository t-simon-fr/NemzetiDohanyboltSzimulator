using UnityEngine;

// Resize authored boundaries without replacing the shop, stock or door roots.
public sealed class ShopExpansionGeometry : MonoBehaviour
{
    private MvpShopController shop;
    private readonly System.Collections.Generic.List<GameObject> lamps=new System.Collections.Generic.List<GameObject>();
    public void Configure(MvpShopController owner)
    {
        shop=owner;
        var template=shop.InteriorLighting.Lights[0].transform.parent;
        for(int i=0;i<3;i++)
        {
            var lamp=Instantiate(template.gameObject,transform,false);
            lamp.name="Expansion fluorescent fixture";
            lamp.transform.position=new Vector3(3.5f+i*2,2.94f,-1.25f);
            lamps.Add(lamp);
        }
        Refresh();
    }
    private void Size(string name, Vector3 position, Vector3 size)
    {
        var item=shop.FloorPlan.Find(name);
        if(item==null)return;
        item.position=position; item.localScale=size;
    }
    public void Refresh()
    {
        if(shop==null)return;
        float width=shop.Layout.ShopLevel*2;
        for(int i=0;i<lamps.Count;i++)lamps[i].SetActive(i<shop.Layout.ShopLevel);
        float right=2.5f+width, middle=2.5f+width*.5f;
        if(width>0)
        {
            Size("Extension floor",new Vector3(middle,-.1f,-1.25f),new Vector3(width,.2f,6.3f));
            Size("Extension ceiling",new Vector3(middle,3.05f,-1.25f),new Vector3(width,.1f,6.3f));
            Size("Extension outer wall",new Vector3(right,1.5f,-1.25f),new Vector3(.15f,3,6.3f));
            Size("Extension back wall",new Vector3(middle,1.5f,1.9f),new Vector3(width,3,.15f));
            Size("Extension front wall",new Vector3(middle,1.5f,-4.4f),new Vector3(width,3,.15f));
        }
        var area=shop.Layout.WarehouseArea;
        float left=area.xMin-.1f, front=area.yMin-.1f;
        float w=-2.5f-left, depth=2-front;
        Size("Warehouse floor",new Vector3(left+w*.5f,-.1f,front+depth*.5f),new Vector3(w,.2f,depth));
        Size("Warehouse ceiling",new Vector3(left+w*.5f,3.05f,front+depth*.5f),new Vector3(w,.1f,depth));
        Size("Warehouse outer wall",new Vector3(left,1.5f,front+depth*.5f),new Vector3(.15f,3,depth));
        Size("Warehouse front wall",new Vector3(left+w*.5f,1.5f,front),new Vector3(w,3,.15f));
        Size("Rear wall left",new Vector3((left-4.9f)*.5f,1.5f,2),new Vector3(-4.9f-left,3,.15f));
        var lean=GetComponentsInChildren<Transform>(true);
        foreach(var item in lean)
            if(item.name=="Trafik stockroom lean roof")
            {
                item.position=new Vector3(left+w*.5f,3.13f,front+depth*.5f);
                item.localScale=new Vector3(w+.4f,.12f,depth+.4f);
            }
        // The part alongside the staff aisle closes the newly extended store edge.
        var wall=shop.FloorPlan.Find("Shop side wall front");
        if(wall!=null) { wall.position=new Vector3(-2.5f,1.5f,(-4.4f-.65f)*.5f); }
        Physics.SyncTransforms();
    }
    public bool BuyWarehouse()
    {
        if(!shop.CanDevelopShop||shop.Layout.WarehouseLevel>=3)return false;
        if(!shop.Ledger.TryImprove(shop.Layout.NextWarehousePrice))return false;
        shop.Layout.SetLevels(shop.Layout.ShopLevel,shop.Layout.WarehouseLevel+1);
        shop.SyncCashDrawer();shop.Navigation.Rebuild();
        shop.Notify("A raktár bővítve. Az új helyre külön vásárolhatsz állványt.");
        return true;
    }
}
