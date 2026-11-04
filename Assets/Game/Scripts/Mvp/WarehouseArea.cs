using System.Collections.Generic;
using System.Linq;
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
    private TextMesh[] shelfLabels;
    private float nextLabelRefresh;

    private void Start() => RefreshLabels();
    private void Update()
    {
        if (Time.unscaledTime < nextLabelRefresh) return;
        nextLabelRefresh = Time.unscaledTime + .25f;
        RefreshLabels();
    }

    public void RefreshLabels()
    {
        if (docks == null) return;
        if (shelfLabels == null)
            shelfLabels = docks.Select(dock => dock != null ? dock.Find("Brand dock label")?.GetComponent<TextMesh>() : null).ToArray();
        for (int i = 0; i < docks.Length; i++)
        {
            if (shelfLabels[i] == null) continue;
            var dock = docks[i];
            var types = cartons.Where(carton => carton != null && carton.gameObject.activeInHierarchy && !carton.IsHeld && carton.Quantity > 0)
                .Where(carton =>
                {
                    Vector3 point = dock.InverseTransformPoint(carton.transform.position);
                    return Mathf.Abs(point.x) < .35f && Mathf.Abs(point.z) < .25f && Mathf.Abs(point.y - .055f) < .12f;
                }).Select(carton => carton.ProductType).Distinct();
            shelfLabels[i].text = string.Join(" / ", types.Select(type => ShopCatalog.Names[type]));
        }
    }
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
            int delta=Shop.WarehouseCount(i)-cartons.Where(c=>c!=null&&c.ProductType==i).Sum(c=>c.Quantity);
            if(delta>0)
            {
                if(current[i]==null)
                {
                    var box=new GameObject("Carton - "+ShopCatalog.Names[i]);
                    box.transform.SetParent(docks[i],false);
                    box.transform.localPosition=Vector3.up*.055f;
                    var carton=box.AddComponent<WarehouseCarton>();
                    carton.Configure(this,i,cardboard,packingTape,productMaterials[i]);
                    cartons.Add(carton);current[i]=carton;
                }
                current[i].AdjustQuantity(delta);
            }
            else if(delta<0)
            {
                // Legacy single-unit callers still share the same finite inventory.
                foreach(var carton in cartons.Where(c=>c!=null&&c.ProductType==i).OrderBy(c=>c.IsHeld))
                {
                    int remove=Mathf.Min(carton.Quantity,-delta);carton.AdjustQuantity(-remove);delta+=remove;
                    if(delta==0)break;
                }
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
            var dock=docks[type];
            foreach(float height in new[]{.605f,-.495f})
            {
                Vector3 point=dock.TransformPoint(Vector3.up*height);
                if(cartons.Any(c=>c!=null&&c!=carton&&!c.IsHeld&&Vector3.Distance(c.transform.position,point)<.2f))continue;
                carton.DropLoose(dock,point);return;
            }
            int loose=cartons.Count(c=>c!=null&&c.transform.parent==looseCardboardPoint);
            carton.DropLoose(looseCardboardPoint,looseCardboardPoint.position+Vector3.up*(.02f+loose*.30f));
        }
    }
    public void Dispose(WarehouseCarton carton)
    {
        if(carton==null||carton.Quantity!=0||!cartons.Contains(carton))return;
        Release(carton);cartons.Remove(carton);DiscardedCartons++;
        carton.gameObject.SetActive(false);Destroy(carton.gameObject);
        Shop.Notify("Üres kartondoboz a papírgyűjtőben. Kidobva: "+DiscardedCartons+" db.");
    }
    public void ResetStorage()
    {
        foreach(var carton in cartons)if(carton!=null){carton.gameObject.SetActive(false);Destroy(carton.gameObject);}
        cartons.Clear();current=new WarehouseCarton[ShopCatalog.Count];DiscardedCartons=0;
        RefreshLabels();
    }
}
