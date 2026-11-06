using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ShopContrabandBox : MonoBehaviour
{
    private readonly List<ShopProduct> products = new List<ShopProduct>();
    public int Quantity => products.Count(p=>p!=null);
    public void Configure(MvpShopController shop, int quantity)
    {
        var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shell.name="Sealed Dnyipro shipping carton"; shell.transform.SetParent(transform,false);
        shell.transform.localPosition=Vector3.up*.13f; shell.transform.localScale=new Vector3(.48f,.26f,.32f);
        ShopDnyiproVisuals.Carton(shell.transform);
        ProductVisuals.Text(transform,"Carton contents",quantity+" × DNYIPRO",new Vector3(0,.025f,-.172f),Quaternion.identity,.019f,new Color(.76f,.70f,.57f));
        for (int i=0;i<quantity;i++)
        {
            var slot = new GameObject("Boxed Dnyipro "+i).AddComponent<ProductSlot>();
            slot.transform.SetParent(transform,false); slot.Configure(new Vector3(.2f,.3f,.2f));
            var product = shop.CreateNightProduct(); product.AssignInitialSlot(slot);
            product.transform.SetParent(transform,true);
            foreach (var renderer in product.GetComponentsInChildren<Renderer>()) renderer.enabled=false;
            foreach (var collider in product.GetComponentsInChildren<Collider>()) collider.enabled=false;
            products.Add(product);
        }
        gameObject.AddComponent<ShopHandTool>().Configure(ShopToolKind.ContrabandBox);
    }
    public bool Unpack(ShopCounterStorage storage)
    {
        var free = storage.Slots.Where(slot=>slot.Occupant==null).ToArray();
        if (free.Length < Quantity) return false;
        int index=0;
        foreach (var product in products.Where(p=>p!=null))
        {
            var target=free[index++]; product.AssignInitialSlot(target);
            product.transform.SetParent(target.transform.parent,true);
            foreach (var renderer in product.GetComponentsInChildren<Renderer>()) renderer.enabled=true;
            foreach (var collider in product.GetComponentsInChildren<Collider>()) collider.enabled=true;
        }
        products.Clear();
        return true;
    }
}
