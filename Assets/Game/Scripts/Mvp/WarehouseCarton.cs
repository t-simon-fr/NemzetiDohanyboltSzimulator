using UnityEngine;

public sealed class WarehouseCarton : MonoBehaviour
{
    public int ProductType { get; private set; }
    public bool IsHeld { get; private set; }
    public bool IsDocked { get; private set; }
    public int Quantity { get; private set; }
    public void AdjustQuantity(int delta) { Quantity=Mathf.Max(0,Quantity+delta);Refresh(); }
    public bool CanPlace(ProductSlot slot) => Quantity>0 && slot!=null && slot.CanStock(ShopCatalog.Ids[ProductType], ProductSize, out _);
    private Vector3 ProductSize => ProductVisuals.Size(ProductType);
    public bool TryPlaceOne(ProductSlot slot) => IsHeld && area.Shop.StockFromCarton(this,slot);
    private WarehouseArea area;
    private Collider[] colliders;
    private TextMesh frontLabel;
    private GameObject[] samples;
    public void Configure(WarehouseArea owner,int type,Material cardboard,Material tape,Material product)
    {
        area=owner;ProductType=type;IsDocked=true;
        Box("Box bottom",new Vector3(0,.012f,0),new Vector3(.52f,.024f,.38f),cardboard);
        Box("Box left",new Vector3(-.25f,.14f,0),new Vector3(.02f,.26f,.38f),cardboard);
        Box("Box right",new Vector3(.25f,.14f,0),new Vector3(.02f,.26f,.38f),cardboard);
        Box("Box front",new Vector3(0,.14f,-.18f),new Vector3(.48f,.26f,.02f),cardboard);
        Box("Box back",new Vector3(0,.14f,.18f),new Vector3(.48f,.26f,.02f),cardboard);
        var left=Box("Left flap",new Vector3(-.315f,.255f,0),new Vector3(.14f,.012f,.35f),cardboard);left.transform.localRotation=Quaternion.Euler(0,0,20);
        var right=Box("Right flap",new Vector3(.315f,.255f,0),new Vector3(.14f,.012f,.35f),cardboard);right.transform.localRotation=Quaternion.Euler(0,0,-20);
        Box("Packing tape",new Vector3(0,.14f,-.192f),new Vector3(.065f,.255f,.003f),tape);
        frontLabel=ProductVisuals.Text(transform,"Brand and quantity","",new Vector3(0,.15f,-.198f),Quaternion.identity,.030f,Color.black);
        samples=new GameObject[6];
        for(int i=0;i<samples.Length;i++)
        {
            samples[i]=Box("Packed product",new Vector3((i%3-1)*.13f,.15f,(i/3-.5f)*.13f),new Vector3(.105f,.16f,.065f),product);
            Destroy(samples[i].GetComponent<Collider>());
            if (!ShopCatalog.IsCigarette(type)) samples[i].transform.localScale = ProductVisuals.Size(type) * .60f;
            ProductVisuals.Add(samples[i], type);
        }
        gameObject.AddComponent<ShopStation>().ConfigureBrand(area.Shop,type);
        colliders=GetComponentsInChildren<Collider>();Refresh();
    }
    private GameObject Box(string name,Vector3 p,Vector3 size,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(transform,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;return go;
    }
    public void Refresh()
    {
        if(frontLabel==null)return;
        frontLabel.text=ShopCatalog.Names[ProductType]+"\n"+(Quantity>0?Quantity+" db":"ÜRES KARTON");
        for(int i=0;i<samples.Length;i++)samples[i].SetActive(i<Quantity);
        area.RefreshLabels();
    }
    public bool PickUp(Transform hand)
    {
        if(IsHeld||hand==null)return false;
        area.Release(this);IsDocked=false;IsHeld=true;ShopLoosePhysics.Freeze(gameObject);
        foreach(var c in colliders)if(c!=null)c.enabled=false;
        transform.SetParent(hand,true);transform.localPosition=new Vector3(-.20f,-.24f,.32f);transform.localRotation=Quaternion.identity;
        Refresh();return true;
    }
    public void ReturnToStorage(){if(IsHeld){IsHeld=false;area.PutBack(this);}}
    public void AttachToDock(Transform dock)
    {
        ShopLoosePhysics.Freeze(gameObject);IsHeld=false;IsDocked=true;transform.SetParent(dock,false);transform.localPosition=Vector3.up*.055f;transform.localRotation=Quaternion.identity;
        foreach(var c in colliders)if(c!=null)c.enabled=true;Refresh();
    }
    public void DropLoose(Transform parent,Vector3 position)
    {
        ShopLoosePhysics.Freeze(gameObject);IsHeld=false;IsDocked=false;transform.SetParent(parent,true);transform.SetPositionAndRotation(position,parent.rotation);
        foreach(var c in colliders)if(c!=null)c.enabled=true;Refresh();
    }
    public void Drop(Vector3 position,Vector3 velocity)
    {
        if(!IsHeld)return;area.Release(this);IsHeld=false;IsDocked=false;
        foreach(var collider in colliders)if(collider!=null)collider.enabled=true;
        ShopLoosePhysics.Throw(gameObject,position,velocity);Refresh();
    }
    public void ThrowAway(){if(IsHeld&&Quantity==0)area.Dispose(this);}
}
