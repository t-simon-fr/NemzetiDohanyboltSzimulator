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
    public bool IsStealing { get; private set; }
    public bool WillSteal { get; private set; }
    public bool UsesCoins { get; private set; }
    public ShopProduct BasketItem { get; private set; }
    public bool ShoppingComplete { get; private set; }
    private int exitStep,shoppingStep;
    private ShopLayout layout;
    private ShopSecurity security;
    private ShopProduct shelfItem;
    private ProductSlot basketSource;
    private Transform basket;
    public void Initialize(int person,int product,int visit)
    {
        Person=person;Product=product;int price=ShopCatalog.Prices[product];
        Tender=visit%3==1?price:price>2000?5000:price>500?1000:500;
        UsesCoins=price<=1000 && visit%4<=2;
        layout=Object.FindAnyObjectByType<ShopLayout>();security=layout.GetComponent<ShopSecurity>();
        WillSteal=layout.IsPublicProduct(product) && (visit+MvpShopController.Instance.Day)%6==4;
        transform.SetPositionAndRotation(layout.EntryOutside,Quaternion.identity);
        var body=GetComponent<CharacterController>();if(body!=null)body.enabled=false;
        var old=GetComponent<CustomerOrderController>();if(old!=null)old.enabled=false;
        foreach(var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if(renderer.name!="Body")continue;var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetColor("_BaseColor",ShopPeople.Colors[person]);renderer.SetPropertyBlock(block);
        }
        var hitbox=gameObject.AddComponent<CapsuleCollider>();hitbox.height=1.8f;hitbox.radius=.24f;hitbox.center=Vector3.up*.9f;
        gameObject.name="Vásárló - "+ShopPeople.Names[person];ShoppingComplete=!layout.IsPublicProduct(product);
        basket=new GameObject("Kosár").transform;basket.SetParent(transform,false);basket.localPosition=new Vector3(.3f,1.05f,.25f);
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
            if(shoppingStep==0){if(Move(layout.AisleGate,dt))shoppingStep=1;}
            else if(shoppingStep==1)
            {
                shelfItem=layout.FindPublicStock(Product);
                if(shelfItem==null){shoppingStep=2;return false;}
                if(Move(layout.ShelfApproach(shelfItem),dt))
                {
                    basketSource=shelfItem.CurrentSlot;if(shelfItem.PickUp(basket))BasketItem=shelfItem;shoppingStep=2;
                }
            }
            else if(Move(layout.AisleGate,dt))
            {
                ShoppingComplete=true;
                if(WillSteal && BasketItem!=null){IsStealing=true;Leave();MvpShopController.Instance.Notify("Valaki fizetés nélkül indul a kijárat felé!");}
            }
            return false;
        }
        return Move(queueTarget,dt);
    }
    public bool PlaceBasket(ProductSlot slot)=>BasketItem!=null && (!BasketItem.IsHeld ? BasketItem.CurrentSlot==slot : BasketItem.TryPlace(slot));
    public void ReturnBasket()
    {
        if(BasketItem!=null)
        {
            if(BasketItem.IsHeld)BasketItem.ReturnToPickupPosition();
            else if(basketSource!=null && basketSource.CanAccept(BasketItem))BasketItem.AssignInitialSlot(basketSource);
        }
        BasketItem=null;
    }
    public void SellBasket(){if(BasketItem!=null)BasketItem.RemoveFromWorld();BasketItem=null;}
    public void StopTheft()
    {
        IsStealing=false;WillSteal=false;
        if(BasketItem!=null){BasketItem.Drop(transform.position+transform.forward*.45f+Vector3.up*.7f,transform.forward*.4f);BasketItem=null;}
    }
    public void Leave(){Leaving=true;exitStep=0;}
    private bool Move(Vector3 target,float dt)
    {
        Vector3 delta=target-transform.position;if(delta.sqrMagnitude<.0025f)return true;
        transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),360f*dt);
        Vector3 next=Vector3.MoveTowards(transform.position,target,(IsStealing?1.65f:1.3f)*dt);
        transform.position=security!=null?security.ConstrainMovement(transform.position,next):next;
        return (transform.position-target).sqrMagnitude<.0025f;
    }
}
