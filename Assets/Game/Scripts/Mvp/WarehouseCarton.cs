using UnityEngine;

public sealed class WarehouseCarton : MonoBehaviour
{
    public int ProductType { get; private set; }
    public bool IsHeld { get; private set; }
    public bool IsDocked { get; private set; }
    public int Quantity => IsDocked ? area.Shop.WarehouseCount(ProductType) : 0;
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
    }
    public bool PickUp(Transform hand)
    {
        if(IsHeld||Quantity!=0||hand==null)return false;
        area.Release(this);IsDocked=false;IsHeld=true;
        foreach(var c in colliders)if(c!=null)c.enabled=false;
        transform.SetParent(hand,true);transform.localPosition=new Vector3(0,-.18f,.35f);transform.localRotation=Quaternion.identity;
        Refresh();return true;
    }
    public void ReturnToStorage(){if(IsHeld){IsHeld=false;area.PutBack(this);}}
    public void AttachToDock(Transform dock)
    {
        IsHeld=false;IsDocked=true;transform.SetParent(dock,false);transform.localPosition=Vector3.up*.055f;transform.localRotation=Quaternion.identity;
        foreach(var c in colliders)if(c!=null)c.enabled=true;Refresh();
    }
    public void DropLoose(Transform parent,Vector3 position)
    {
        IsHeld=false;IsDocked=false;transform.SetParent(parent,true);transform.SetPositionAndRotation(position,Quaternion.identity);
        foreach(var c in colliders)if(c!=null)c.enabled=true;Refresh();
    }
    public void ThrowAway(){if(IsHeld&&Quantity==0)area.Dispose(this);}
}
