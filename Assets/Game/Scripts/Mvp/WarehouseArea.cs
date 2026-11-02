using System.Collections.Generic;
using UnityEngine;

public sealed class WarehouseArea : MonoBehaviour
{
    [SerializeField] private Transform[] docks;
    [SerializeField] private Material cardboard, packingTape;
    [SerializeField] private Material[] productMaterials;
    [SerializeField] private Transform looseCardboardPoint;
    private readonly List<WarehouseCarton> cartons = new List<WarehouseCarton>();
    private WarehouseCarton[] current = new WarehouseCarton[ShopCatalog.Count];
    private MvpShopController shop;
    public int DiscardedCartons { get; private set; }
    public MvpShopController Shop => shop != null ? shop : (shop=GetComponent<MvpShopController>());
    public Transform Dock(int type) => docks[type];
    public WarehouseCarton Carton(int type) => current[type];
    public void Configure(Transform[] locations,Material paper,Material tape,Material[] goods,Transform loose)
    {docks=locations;cardboard=paper;packingTape=tape;productMaterials=goods;looseCardboardPoint=loose;}
    public void SyncBoxes()
    {
        if(docks==null||Shop.Ledger==null)return;
        for(int i=0;i<docks.Length;i++)
        {
            if(Shop.WarehouseCount(i)>0&&current[i]==null)
            {
                var box=new GameObject("Carton - "+ShopCatalog.Names[i]);
                box.transform.SetParent(docks[i],false);
                box.transform.localPosition=Vector3.up*.055f;
                var carton=box.AddComponent<WarehouseCarton>();
                carton.Configure(this,i,cardboard,packingTape,productMaterials[i]);
                cartons.Add(carton);current[i]=carton;
            }
            if(current[i]!=null)current[i].Refresh();
        }
    }
    public void Release(WarehouseCarton carton)
    {if(current[carton.ProductType]==carton)current[carton.ProductType]=null;}
    public void PutBack(WarehouseCarton carton)
    {
        int type=carton.ProductType;
        if(current[type]==null)
        {
            current[type]=carton;carton.AttachToDock(docks[type]);
        }
        else
        {
            carton.DropLoose(transform,looseCardboardPoint.position+Vector3.up*.02f);
        }
    }
    public void Dispose(WarehouseCarton carton)
    {
        Release(carton);cartons.Remove(carton);DiscardedCartons++;
        carton.gameObject.SetActive(false);Destroy(carton.gameObject);
        Shop.Notify("Üres kartondoboz a papírgyűjtőben. Kidobva: "+DiscardedCartons+" db.");
    }
    public void ResetStorage()
    {
        foreach(var carton in cartons)if(carton!=null){carton.gameObject.SetActive(false);Destroy(carton.gameObject);}
        cartons.Clear();current=new WarehouseCarton[ShopCatalog.Count];DiscardedCartons=0;
    }
}
