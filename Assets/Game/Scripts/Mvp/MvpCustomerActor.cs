using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public sealed class MvpCustomerActor : MonoBehaviour
{
    public int Person { get; private set; }
    public int Product { get; private set; }
    public int[] OrderTypes {get;private set;}
    public int[] UnitPrices {get;private set;}
    public bool AcceptsPrices {get;private set;}
    public int TotalPrice=>UnitPrices.Sum();
    public int TotalCost=>OrderTypes.Sum(t=>ShopCatalog.Costs[t]);
    public string OrderDescription=>string.Join(" + ",OrderTypes.Select(t=>ShopCatalog.ShortNames[t]));
    public int Tender { get; private set; }
    public float WaitTime { get; set; }
    public bool Entered { get; private set; }
    public bool Leaving { get; private set; }
    public bool Gone { get; private set; }
    public bool IsStealing { get; private set; }
    public bool WillSteal { get; private set; }
    public bool UsesCoins { get; private set; }
    public bool UsesCard {get;private set;}
    public readonly List<ShopProduct> ExtraItems=new List<ShopProduct>();
    private readonly List<ProductSlot> extraSources=new List<ProductSlot>();
    public int StolenCost=>(BasketItem!=null?ShopCatalog.Costs[ShopCatalog.Index(BasketItem.ProductId)]:0)+ExtraItems.Where(p=>p!=null).Sum(p=>ShopCatalog.Costs[ShopCatalog.Index(p.ProductId)]);
    public ShopProduct BasketItem { get; private set; }
    public bool ShoppingComplete { get; private set; }
    private int exitStep,shoppingStep,shoppingOrderIndex;
    private ShopLayout layout;
    private ShopSecurity security;
    private ShopProduct shelfItem;
    private ProductSlot basketSource;
    private Transform basket;
    public void Initialize(int person,int product,int visit)
    {
        Person=person;Product=product;var game=MvpShopController.Instance;int count=game.SingleProductVisits?1:1+visit%3;OrderTypes=Enumerable.Range(0,count).Select(i=>i==0?product:(product+i)%game.AvailableProducts).ToArray();UnitPrices=OrderTypes.Select(ShopCatalog.SalePrice).ToArray();AcceptsPrices=OrderTypes.Select((t,i)=>ShopPricing.Accepts(t,UnitPrices[i],visit,game.Day)).All(x=>x);int price=TotalPrice;
        Tender=visit%3==1?price:price>2000?Mathf.CeilToInt(price/5000f)*5000:price>500?Mathf.CeilToInt(price/1000f)*1000:500;
        UsesCard=!game.SingleProductVisits&&visit%4==3;
        UsesCoins=!UsesCard&&price<=1000 && visit%4<=2;
        layout=Object.FindAnyObjectByType<ShopLayout>();security=layout.GetComponent<ShopSecurity>();
        WillSteal=AcceptsPrices&&layout.IsPublicProduct(product) && (visit+MvpShopController.Instance.Day)%6==4;
        transform.SetPositionAndRotation(layout.EntryOutside,Quaternion.identity);
        var body=GetComponent<CharacterController>();if(body!=null)body.enabled=false;
        var old=GetComponent<CustomerOrderController>();if(old!=null)old.enabled=false;
        foreach(var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if(renderer.name!="Body")continue;var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetColor("_BaseColor",ShopPeople.Colors[person]);renderer.SetPropertyBlock(block);
        }
        var hitbox=gameObject.AddComponent<CapsuleCollider>();hitbox.height=1.8f;hitbox.radius=.24f;hitbox.center=Vector3.up*.9f;gameObject.AddComponent<ShopCharacterDetail>().Configure(person);
        gameObject.name="Vásárló - "+ShopPeople.Names[person];ShoppingComplete=!AcceptsPrices||!OrderTypes.Any(layout.IsPublicProduct);
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
                shelfItem=layout.FindPublicStock(OrderTypes[shoppingOrderIndex]);
                if(shelfItem==null){shoppingOrderIndex++;if(shoppingOrderIndex>=OrderTypes.Length)shoppingStep=2;return false;}
                if(Move(layout.ShelfApproach(shelfItem),dt))
                {
                    var source=shelfItem.CurrentSlot;
                    if(shelfItem.PickUp(basket))
                    {
                        if(shoppingOrderIndex==0){basketSource=source;BasketItem=shelfItem;}
                        else{shelfItem.transform.localPosition=new Vector3(.18f*(ExtraItems.Count+1),0,0);ExtraItems.Add(shelfItem);extraSources.Add(source);}
                    }
                    shoppingOrderIndex++;if(shoppingOrderIndex>=OrderTypes.Length)shoppingStep=2;
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
    public void SetTheftIntent(){if(layout.IsPublicProduct(Product))WillSteal=true;}
    public bool PlaceBasket(ProductSlot slot)=>BasketItem!=null && (!BasketItem.IsHeld ? BasketItem.CurrentSlot==slot : BasketItem.TryPlace(slot));
    public void ReturnBasket()
    {
        for(int i=0;i<ExtraItems.Count;i++){var item=ExtraItems[i];if(item==null)continue;if(item.IsHeld)item.ReturnToPickupPosition();else if(extraSources[i]!=null&&extraSources[i].CanAccept(item))item.AssignInitialSlot(extraSources[i]);}ExtraItems.Clear();extraSources.Clear();
        if(BasketItem!=null)
        {
            if(BasketItem.IsHeld)BasketItem.ReturnToPickupPosition();
            else if(basketSource!=null && basketSource.CanAccept(BasketItem))BasketItem.AssignInitialSlot(basketSource);
        }
        BasketItem=null;
    }
    public void ReturnUnusedBasket(ICollection<ShopProduct> sold)
    {
        for(int i=ExtraItems.Count-1;i>=0;i--){var item=ExtraItems[i];if(item==null||sold.Contains(item))continue;if(item.IsHeld)item.ReturnToPickupPosition();else if(extraSources[i]!=null&&extraSources[i].CanAccept(item))item.AssignInitialSlot(extraSources[i]);ExtraItems.RemoveAt(i);extraSources.RemoveAt(i);}
        if(BasketItem!=null&&!sold.Contains(BasketItem)){if(BasketItem.IsHeld)BasketItem.ReturnToPickupPosition();else if(basketSource!=null&&basketSource.CanAccept(BasketItem))BasketItem.AssignInitialSlot(basketSource);BasketItem=null;}
    }
    public void SellBasket(){foreach(var item in ExtraItems)if(item!=null)item.RemoveFromWorld();ExtraItems.Clear();extraSources.Clear();if(BasketItem!=null)BasketItem.RemoveFromWorld();BasketItem=null;}
    public void StopTheft()
    {
        IsStealing=false;WillSteal=false;foreach(var item in ExtraItems)if(item!=null)item.Drop(transform.position+Vector3.up*.7f,transform.forward*.4f);ExtraItems.Clear();extraSources.Clear();
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
