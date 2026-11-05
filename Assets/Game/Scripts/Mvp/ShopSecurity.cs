using System.Collections.Generic;
using UnityEngine;

// Customer movement and the physical entrance use the same purchased lock.
[DisallowMultipleComponent]
public sealed class ShopSecurity : MonoBehaviour
{
    public const int PurchasePrice = 7500;
    private const float ActorClearance = .28f;
    private MvpShopController shop;
    private Transform entrance;
    private GameObject installation, button, shutter;
    private TextMesh buttonLabel;
    private Material indicator;
    private readonly List<Material> materials = new List<Material>();
    private float entranceWidth;
    public bool Owned { get; private set; }
    public bool Locked { get; private set; }
    public Transform Button => button != null ? button.transform : null;
    public Transform Entrance => entrance;
    public BoxCollider EntranceCollider { get; private set; }
    public string Prompt => Locked ? "E – Bejárat feloldása" : "E – Vészgomb: bejárat reteszelése";

    public void Configure(MvpShopController owner)
    {
        if (installation != null) return;
        shop = owner;
        var top = FindSceneTransform("Counter top");
        var lintel = FindSceneTransform("Door lintel");
        if (top == null || lintel == null)
            throw new System.InvalidOperationException("The panic button requires a countertop and entrance lintel.");
        var metal = MakeMaterial("Security shutter", new Color(.28f,.31f,.32f));
        var casing = MakeMaterial("Panic button housing", new Color(.12f,.13f,.14f));
        indicator = MakeMaterial("Panic button indicator", new Color(.15f,.65f,.35f));
        var red = MakeMaterial("Panic button red", new Color(.85f,.025f,.025f));
        installation = new GameObject("Purchased Entrance Lock");
        entrance = installation.transform;
        entrance.SetParent(lintel.parent, false);
        var direction = shop.Layout.EntryInside - shop.Layout.EntryOutside;
        direction.y = 0;
        entrance.SetPositionAndRotation(new Vector3(lintel.position.x,shop.Layout.EntryInside.y,lintel.position.z),
            Quaternion.LookRotation(direction.normalized,Vector3.up));
        entranceWidth = Mathf.Abs(lintel.lossyScale.x);
        float height = lintel.position.y - Mathf.Abs(lintel.lossyScale.y)*.5f - entrance.position.y;
        Box(entrance,"Shutter housing",new Vector3(0,height+.06f,0),new Vector3(entranceWidth,.12f,.16f),casing);
        shutter = new GameObject("Locked Entrance Shutter");
        shutter.transform.SetParent(entrance,false);
        EntranceCollider = shutter.AddComponent<BoxCollider>();
        EntranceCollider.center = Vector3.up*height*.5f;
        EntranceCollider.size = new Vector3(entranceWidth,height,.10f);
        int slats = Mathf.CeilToInt(height/.16f);
        for (int i=0;i<slats;i++)
            Box(shutter.transform,"Shutter slat",new Vector3(0,(i+.5f)*height/slats,0),
                new Vector3(entranceWidth,height/slats-.008f,.08f),metal);
        ProductVisuals.Text(shutter.transform,"Entrance lock notice","BEJÁRAT RETESZELVE",
            new Vector3(0,1.45f,.065f),Quaternion.Euler(0,180,0),.07f,Color.white);

        button = new GameObject("Panic Button");
        button.transform.SetParent(top.parent,false);
        button.transform.SetPositionAndRotation(top.position+top.rotation*new Vector3(.45f,-.24f,.44f),top.rotation);
        var plate = Box(button.transform,"Panic button mounting plate",Vector3.zero,new Vector3(.24f,.30f,.035f),casing);
        plate.AddComponent<BoxCollider>();
        var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.name = "Red panic button";
        cap.transform.SetParent(button.transform,false);
        cap.transform.localPosition = new Vector3(0,.015f,.035f);
        cap.transform.localRotation = Quaternion.Euler(90,0,0);
        cap.transform.localScale = new Vector3(.10f,.018f,.10f);
        cap.GetComponent<Renderer>().sharedMaterial = red;
        Box(button.transform,"Lock status light",new Vector3(.082f,.015f,.025f),new Vector3(.023f,.023f,.012f),indicator);
        ProductVisuals.Text(button.transform,"Panic button title","VÉSZGOMB",new Vector3(0,.11f,.027f),
            Quaternion.Euler(0,180,0),.032f,Color.white);
        buttonLabel = ProductVisuals.Text(button.transform,"Panic button state","",new Vector3(0,-.095f,.027f),
            Quaternion.Euler(0,180,0),.023f,Color.white);
        button.AddComponent<ShopStation>().Configure(shop,ShopStationKind.PanicButton);
        Refresh();
    }
    public bool TryPurchase()
    {
        if (Owned) { shop.Notify("A vészgombot már megvásároltad."); return false; }
        if (!shop.CanDevelopShop) { shop.Notify("Vészgombot nyitás előtt vagy zárás után, üres boltban vásárolhatsz."); return false; }
        if (!shop.Ledger.TryImprove(PurchasePrice))
        { shop.Notify("Nincs elég elkölthető pénz a vészgombra: "+ShopCatalog.Money(PurchasePrice)+"."); return false; }
        shop.SyncCashDrawer();
        Owned = true; Refresh();
        shop.Notify("Vészgomb felszerelve a pult mögött. E: bejárat reteszelése vagy feloldása.");
        shop.Devices?.RefreshMonitor();
        return true;
    }
    public bool Toggle()
    {
        if (!Owned) { shop.Notify("Előbb vásárold meg a vészgombot a TobaccOS Üzletfejlesztés alkalmazásában."); return false; }
        if (shop.Phase!=ShopPhase.Open) { shop.Notify("A vészgomb nyitvatartás alatt használható."); return false; }
        Locked = !Locked; Refresh();
        shop.Notify(Locked ? "A bejárat reteszelve. Senki nem jut át. E a vészgombon: feloldás." : "A bejárat újra szabad.");
        return true;
    }
    public void ReleaseEntrance() { Locked=false; Refresh(); }
    public void ResetUpgrade() { Owned=false; ReleaseEntrance(); }
    private void Refresh()
    {
        if (installation!=null) installation.SetActive(Owned);
        if (button!=null) button.SetActive(Owned);
        if (shutter!=null) shutter.SetActive(Owned && Locked);
        if (buttonLabel!=null) buttonLabel.text=Locked ? "E – FELOLDÁS" : "E – RETESZELÉS";
        if (indicator!=null) indicator.color=Locked ? new Color(1,.06f,.02f) : new Color(.15f,.65f,.35f);
    }
    public Vector3 ConstrainMovement(Vector3 from,Vector3 next)
    {
        if (!Locked || entrance==null) return next;
        Vector3 a=entrance.InverseTransformPoint(from), b=entrance.InverseTransformPoint(next);
        float side=a.z>=0 ? 1 : -1;
        // Allow movement away from the door if it closed around an actor.
        if (side*b.z>=ActorClearance || side*b.z>=side*a.z) return next;
        float dz=b.z-a.z;
        if (Mathf.Abs(dz)<.00001f) return next;
        float t=Mathf.Clamp01((side*ActorClearance-a.z)/dz);
        Vector3 stop=Vector3.Lerp(a,b,t);
        if (Mathf.Abs(stop.x)>entranceWidth*.5f+.22f) return next;
        stop.z=side*ActorClearance;
        return entrance.TransformPoint(stop);
    }
    private Transform FindSceneTransform(string name)
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                if (item.name==name) return item;
        return null;
    }
    private Material MakeMaterial(string name,Color color)
    {
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit")) {name=name,color=color};
        materials.Add(material); return material;
    }
    private static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        var item=GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name=name;item.transform.SetParent(parent,false);item.transform.localPosition=position;item.transform.localScale=size;
        item.GetComponent<Renderer>().sharedMaterial=material;
        var collider=item.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
        return item;
    }
    private void OnDisable()=>ReleaseEntrance();
    private void OnDestroy()
    {
        if (button!=null) Destroy(button);
        if (installation!=null) Destroy(installation);
        foreach (var material in materials) if(material!=null) Destroy(material);
    }
}
