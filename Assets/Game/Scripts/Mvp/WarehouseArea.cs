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
    private List<Transform> storageDocks;
    public void RegisterStorage(IEnumerable<Transform> locations)
    {
        EnsureStorageDocks();storageDocks.AddRange(locations);
    }

    public IReadOnlyList<Transform> StorageDocks { get { EnsureStorageDocks(); return storageDocks; } }

    private void EnsureStorageDocks()
    {
        storageDocks?.RemoveAll(d=>d==null||!d.gameObject.activeInHierarchy);
        if (storageDocks != null || docks == null) return;
        storageDocks = docks.Where(dock => dock != null).ToList();
        // Every authored shelf board is usable, including rows without a former brand dock.
        foreach (var rack in docks)
        {
            var boards = rack.GetComponentsInChildren<BoxCollider>()
                .Where(board => board.name.StartsWith("Warehouse shelf ") || board.name == "Warehouse top shelf").ToArray();
            foreach (var board in boards)
            {
                if (storageDocks.Any(dock => Vector3.Distance(dock.position, board.transform.position) < .05f)) continue;
                var dock = new GameObject("Warehouse storage cell").transform;
                dock.SetParent(rack, true);
                dock.SetPositionAndRotation(board.transform.position, rack.rotation);
                dock.gameObject.AddComponent<ShopStation>().Configure(Shop, ShopStationKind.Supplies);
                var labelPlate = rack.Find("Brand shelf label plate");
                if (labelPlate != null)
                {
                    var plate = Instantiate(labelPlate.gameObject, dock, false);
                    foreach (var collider in plate.GetComponents<Collider>()) { collider.enabled = false; Destroy(collider); }
                }
                ProductVisuals.Text(dock, "Brand dock label", "", new Vector3(0,-.055f,-.357f), Quaternion.identity, .042f, Color.black);
                storageDocks.Add(dock);
            }
        }
        foreach (var dock in storageDocks)
        {
            var selection = dock.gameObject.AddComponent<BoxCollider>();
            selection.isTrigger = true;
            selection.center = Vector3.up * .19f;
            selection.size = new Vector3(.72f,.32f,.58f);
        }
    }

    public bool IsStorageDock(Transform dock) => dock != null && StorageDocks.Contains(dock);

    public Transform DockOnShelf(Transform column, Vector3 point) => StorageDocks
        .Where(dock => Vector3.ProjectOnPlane(dock.position-column.position, Vector3.up).sqrMagnitude < .0025f)
        .OrderBy(dock => (dock.position-point).sqrMagnitude).FirstOrDefault();

    public WarehouseCarton CartonAt(Transform dock)
    {
        if (!IsStorageDock(dock)) return null;
        return cartons.FirstOrDefault(carton => carton != null && carton.gameObject.activeInHierarchy && !carton.IsHeld &&
            Vector3.Distance(carton.transform.position, dock.TransformPoint(Vector3.up * .055f)) < .12f);
    }

    public bool TryDock(WarehouseCarton carton, Transform dock)
    {
        if (carton == null || !cartons.Contains(carton) || !carton.IsHeld || !IsStorageDock(dock) || CartonAt(dock) != null) return false;
        carton.AttachToDock(dock);
        if (current[carton.ProductType] == null) current[carton.ProductType] = carton;
        RefreshLabels();
        return true;
    }

    private Transform FreeDock(int preferred)
    {
        if (CartonAt(docks[preferred]) == null) return docks[preferred];
        return StorageDocks.FirstOrDefault(dock => CartonAt(dock) == null);
    }
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
        foreach (var dock in StorageDocks)
        {
            var label = dock.Find("Brand dock label")?.GetComponent<TextMesh>();
            if (label == null) continue;
            var carton = CartonAt(dock);
            label.text = carton != null && carton.Quantity > 0 ? ShopCatalog.Names[carton.ProductType] : "";
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
                    var dock = FreeDock(i);
                    box.transform.SetParent(dock != null ? dock : looseCardboardPoint, false);
                    box.transform.localPosition=Vector3.up*.055f;
                    var carton=box.AddComponent<WarehouseCarton>();
                    carton.Configure(this,i,cardboard,packingTape,productMaterials[i]);
                    cartons.Add(carton);current[i]=carton;
                    if (dock == null) PlaceLoose(carton);
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
        var source = carton.PickupDock;
        if (!IsStorageDock(source)) source = docks[carton.ProductType];
        if (TryDock(carton, source)) return;
        // A delivery may have occupied the source during carrying: never overwrite it.
        PlaceLoose(carton);
    }

    private void PlaceLoose(WarehouseCarton carton)
    {
        int loose = cartons.Count(c => c != null && c != carton && c.transform.parent == looseCardboardPoint && !c.IsHeld);
        carton.DropLoose(looseCardboardPoint, looseCardboardPoint.position + Vector3.up * (.02f + loose * .30f));
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
