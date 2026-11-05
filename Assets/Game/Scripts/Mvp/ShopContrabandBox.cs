using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ShopContrabandBox : MonoBehaviour
{
    private readonly List<ShopProduct> products = new List<ShopProduct>();
    private Material cardboard;
    public int Quantity => products.Count(p=>p!=null);
    public void Configure(MvpShopController shop, int quantity)
    {
        cardboard = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color=new Color(.48f,.31f,.16f) };
        var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shell.name="Sealed Dnyipro shipping carton"; shell.transform.SetParent(transform,false);
        shell.transform.localPosition=Vector3.up*.13f; shell.transform.localScale=new Vector3(.48f,.26f,.32f);
        shell.GetComponent<Renderer>().sharedMaterial=cardboard;
        ProductVisuals.Text(transform,"Carton contents",quantity+" × DNYIPRO",new Vector3(0,.15f,-.162f),Quaternion.identity,.027f,Color.black);
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
    private void OnDestroy() { Destroy(cardboard); }
}
