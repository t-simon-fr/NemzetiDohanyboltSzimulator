using UnityEngine;

public enum ShopToolKind { Bat, Mop, CoffeeRefill }
public static class ShopLoosePhysics
{
    public static void Freeze(GameObject item)
    {var body=item.GetComponent<Rigidbody>();if(body!=null){body.isKinematic=true;body.useGravity=false;}}
    public static void Throw(GameObject item,Vector3 position,Vector3 velocity)
    {
        item.transform.SetParent(null,true);item.transform.position=position;
        var body=item.GetComponent<Rigidbody>();if(body==null)body=item.AddComponent<Rigidbody>();
        body.mass=.7f;body.isKinematic=false;body.useGravity=true;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        body.linearVelocity=velocity;body.angularVelocity=new Vector3(.3f,1,.2f);
    }
}
public sealed class ShopHandTool : MonoBehaviour
{
    public ShopToolKind Kind { get; private set; }
    public bool IsHeld { get; private set; }
    public string DisplayName
    {
        get
        {
            if (Kind!=ShopToolKind.CoffeeRefill) return Kind==ShopToolKind.Bat?"Baseballütő":"Felmosó";
            var package=GetComponent<ShopIngredientPackage>();
            return (package!=null&&package.Milk>0?"Tej":"Kávéalapanyag")+" • E a gépen: feltöltés";
        }
    }
    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Collider[] colliders;
    private bool dropped;
    private float swing;
    public void PlaySwing(){swing=.24f;}
    private void LateUpdate(){if(!IsHeld)return;swing=Mathf.Max(0,swing-Time.deltaTime);float angle=swing>0?Mathf.Sin((.24f-swing)/.24f*Mathf.PI)*85:0;transform.localRotation=Quaternion.Euler(-30,angle,-18);}
    public void Configure(ShopToolKind kind){Kind=kind;colliders=GetComponentsInChildren<Collider>();RememberHome();}
    private void RememberHome(){originalParent=transform.parent;originalPosition=transform.position;originalRotation=transform.rotation;}
    public bool PickUp(Transform hand)
    {
        if(IsHeld||hand==null)return false;RememberHome();ShopLoosePhysics.Freeze(gameObject);
        foreach(var collider in colliders)if(collider!=null)collider.enabled=false;
        transform.SetParent(hand,true);transform.localPosition=new Vector3(.04f,-.16f,.06f);transform.localRotation=Quaternion.Euler(-30,0,-18);IsHeld=true;return true;
    }
    public void Drop(Vector3 position,Vector3 velocity)
    {
        if(!IsHeld)return;IsHeld=false;dropped=true;
        foreach(var collider in colliders)if(collider!=null)collider.enabled=true;
        ShopLoosePhysics.Throw(gameObject,position,velocity);
    }
    public void ReturnToPickupPosition()
    {
        if(!IsHeld)return;IsHeld=false;transform.SetParent(originalParent,true);transform.SetPositionAndRotation(originalPosition,originalRotation);
        foreach(var collider in colliders)if(collider!=null)collider.enabled=true;
        if(dropped){var body=GetComponent<Rigidbody>();if(body!=null){body.isKinematic=false;body.useGravity=true;}}
    }
}
public enum ShopFeatureKind { CoffeeMachine, Spill }
public sealed class ShopFeatureTarget : MonoBehaviour
{
    public ShopFeatureKind Kind;
    public float Cleaning { get; private set; }
    public string Prompt=>Kind==ShopFeatureKind.CoffeeMachine?"1: rövid • 2: hosszú • 3: latte • E: főzés / felvétel / feltöltés":"Felmosóval tartsd nyomva a bal egérgombot • "+Mathf.RoundToInt(Cleaning/1.5f*100)+"%";
    public void Scrub(float dt)
    {
        if(Kind!=ShopFeatureKind.Spill)return;Cleaning+=Mathf.Clamp(dt,0,.1f);
        if(Cleaning>=1.5f){gameObject.SetActive(false);Destroy(gameObject);MvpShopController.Instance?.Notify("A padló ismét tiszta.");}
    }
}
