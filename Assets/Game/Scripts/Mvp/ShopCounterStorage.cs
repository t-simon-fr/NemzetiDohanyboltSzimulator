using UnityEngine;

public sealed class ShopCounterStorage : MonoBehaviour
{
    public ProductSlot[] Slots { get; private set; }
    public ProductSlot[] TicketSlots { get; private set; }
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
        Box(root,"Counter storage middle shelf",new Vector3(0,-.02f,.08f),new Vector3(1.3f,.035f,.48f));
        Slots = new ProductSlot[10];
        for (int i=0;i<Slots.Length;i++)
        {
            var slot = new GameObject("Under counter Dnyipro slot "+i).AddComponent<ProductSlot>();
            slot.transform.SetParent(root,false);
            slot.transform.localPosition = new Vector3((i%5-2)*.22f,i<5?-.30f:.005f,.23f);
            slot.Configure(new Vector3(.20f,.26f,.16f));
            slot.RestrictExclusivelyTo(ShopCatalog.Ids[ShopCatalog.Contraband]);
            Slots[i] = slot;
        }
        var label = ProductVisuals.Text(root,"Private storage label","",new Vector3(0,-.42f,.387f),Quaternion.Euler(0,180,0),.025f,new Color(.8f,.75f,.6f));
        label.gameObject.AddComponent<ShopShelfPriceLabel>().Configure(label, Slots, true);
        TicketSlots = new ProductSlot[10];
        for (int i=0;i<TicketSlots.Length;i++)
        {
            bool book = i>=5;
            var slot = new GameObject("Under counter ticket slot "+i).AddComponent<ProductSlot>();
            slot.transform.SetParent(root,false);
            slot.transform.localPosition = new Vector3(((i%5)-2)*.22f,book?.005f:-.30f,-.02f);
            slot.Configure(new Vector3(.18f,.24f,.06f));
            slot.RestrictExclusivelyTo(ShopCatalog.Ids[book?ShopCatalog.TicketBook:ShopCatalog.TicketSingle]);
            TicketSlots[i] = slot;
        }
        var ticketLabel = ProductVisuals.Text(root,"Ticket storage label","",new Vector3(0,.30f,.387f),Quaternion.Euler(0,180,0),.025f,new Color(.8f,.75f,.6f));
        ticketLabel.gameObject.AddComponent<ShopShelfPriceLabel>().Configure(ticketLabel, TicketSlots, true);
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
