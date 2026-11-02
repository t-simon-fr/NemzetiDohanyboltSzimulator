using UnityEngine;

public sealed class MvpCustomerActor : MonoBehaviour
{
    public int Person { get; private set; }
    public int Product { get; private set; }
    public int Tender { get; private set; }
    public float WaitTime { get; set; }
    public bool Entered { get; private set; }
    public bool Leaving { get; private set; }
    public bool Gone { get; private set; }
    public ShopProduct BasketItem { get; private set; }
    public bool ShoppingComplete { get; private set; }
    private int exitStep, shoppingStep;
    private ShopLayout layout;
    private ShopProduct shelfItem;
    private Transform basket;

    public void Initialize(int person,int product,int visit)
    {
        Person=person;Product=product;
        int price=ShopCatalog.Prices[product];
        Tender=visit%3==1?price:price>2000?5000:price>500?1000:500;
        layout=Object.FindAnyObjectByType<ShopLayout>();
        transform.SetPositionAndRotation(layout.EntryOutside,Quaternion.identity);
        CharacterController body=GetComponent<CharacterController>();if(body!=null)body.enabled=false;
        var old=GetComponent<CustomerOrderController>();if(old!=null)old.enabled=false;
        foreach(Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if(renderer.name!="Body")continue;
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor",ShopPeople.Colors[person]);renderer.SetPropertyBlock(block);
        }
        gameObject.name="Vásárló - "+ShopPeople.Names[person];
        layout=Object.FindAnyObjectByType<ShopLayout>();
        ShoppingComplete=layout==null||!layout.IsPublicProduct(product);
        if(!ShoppingComplete)
        {
            basket=new GameObject("Kosár").transform;basket.SetParent(transform,false);basket.localPosition=new Vector3(.3f,1.05f,.25f);
        }
    }
    public bool Advance(float dt,Vector3 queueTarget)
    {
        if(Gone)return true;
        if(Leaving)
        {
            Vector3 target=exitStep==0?layout.EntryInside:exitStep==1?layout.ExitDoorway:layout.EntryOutside;
            if(Move(target,dt)){exitStep++;if(exitStep>=3)Gone=true;}return Gone;
        }
        if(!Entered){if(Move(layout.EntryInside,dt))Entered=true;return false;}
        if(!ShoppingComplete)
        {
            if(shoppingStep==0)
            {
                if(Move(layout.AisleGate,dt))shoppingStep=1;
            }
            else if(shoppingStep==1)
            {
                shelfItem=layout.FindPublicStock(Product);
                if(shelfItem==null){shoppingStep=2;return false;}
                if(Move(layout.ShelfApproach(shelfItem),dt))
                {
                    // Selection and reservation happen together on the Unity main thread.
                    if(shelfItem.PickUp(basket))BasketItem=shelfItem;
                    shoppingStep=2;
                }
            }
            else if(Move(layout.AisleGate,dt))ShoppingComplete=true;
            return false;
        }
        return Move(queueTarget,dt);
    }
    public void ReturnBasket(){if(BasketItem!=null)BasketItem.ReturnToPickupPosition();BasketItem=null;}
    public void SellBasket(){if(BasketItem!=null)BasketItem.RemoveFromWorld();BasketItem=null;}
    public void Leave(){Leaving=true;exitStep=0;}
    private bool Move(Vector3 target,float dt)
    {
        Vector3 delta=target-transform.position;if(delta.sqrMagnitude<.0025f)return true;
        transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),360f*dt);
        transform.position=Vector3.MoveTowards(transform.position,target,1.3f*dt);
        return(transform.position-target).sqrMagnitude<.0025f;
    }
}
