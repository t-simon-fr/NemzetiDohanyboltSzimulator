using UnityEngine;

public sealed class ShopCounterStorage : MonoBehaviour
{
    public ProductSlot[] Slots { get; private set; }
    private Material wood;

    public void Configure(MvpShopController shop)
    {
        var body = shop.FloorPlan.Find("Service counter");
        wood = body.GetComponent<Renderer>().sharedMaterial;
        body.GetComponent<Renderer>().enabled = false;
        body.GetComponent<Collider>().enabled = false;
        var root = new GameObject("Under Counter Contraband Storage").transform;
        root.SetParent(shop.Counter.transform, false);
        root.SetPositionAndRotation(body.position,body.rotation);
        // Preserve the counter's outer footprint; only its staff-side centre is open.
        Box(root,"Counter left cabinet",new Vector3(-1.175f,0,0),new Vector3(1.05f,1.1f,.75f));
        Box(root,"Counter right cabinet",new Vector3(1.175f,0,0),new Vector3(1.05f,1.1f,.75f));
        Box(root,"Counter storage back",new Vector3(0,0,-.28f),new Vector3(1.3f,1.1f,.19f));
        Box(root,"Counter storage floor",new Vector3(0,-.43f,0),new Vector3(1.3f,.24f,.75f));
        Box(root,"Counter storage roof",new Vector3(0,.44f,0),new Vector3(1.3f,.22f,.75f));
        Slots = new ProductSlot[5];
        for (int i=0;i<Slots.Length;i++)
        {
            var slot = new GameObject("Under counter Dnyipro slot "+i).AddComponent<ProductSlot>();
            slot.transform.SetParent(root,false);
            slot.transform.localPosition = new Vector3((i-2)*.22f,-.30f,.23f);
            slot.Configure(new Vector3(.20f,.35f,.16f));
            slot.RestrictExclusivelyTo(ShopCatalog.Ids[ShopCatalog.Contraband]);
            Slots[i] = slot;
        }
        ProductVisuals.Text(root,"Private storage label","DNYIPRO",new Vector3(0,-.42f,.387f),Quaternion.Euler(0,180,0),.025f,new Color(.8f,.75f,.6f));
    }

    private void Box(Transform root,string title,Vector3 position,Vector3 size)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = title;
        part.transform.SetParent(root,false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        part.GetComponent<Renderer>().sharedMaterial = wood;
    }
}
