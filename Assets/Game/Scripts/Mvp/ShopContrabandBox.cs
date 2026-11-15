using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ShopContrabandBox : MonoBehaviour
{
    private readonly List<ShopProduct> products = new List<ShopProduct>();
    private TextMesh contentsLabel;
    public int Quantity => products.Count(p=>p!=null);
    public void Configure(MvpShopController shop, int quantity)
    {
        var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shell.name="Sealed Dnyipro shipping carton"; shell.transform.SetParent(transform,false);
        shell.transform.localPosition=Vector3.up*.13f; shell.transform.localScale=new Vector3(.48f,.26f,.32f);
        ShopDnyiproVisuals.Carton(shell.transform);
        contentsLabel=ProductVisuals.Text(transform,"Carton contents","",new Vector3(0,.025f,-.172f),Quaternion.identity,.019f,new Color(.76f,.70f,.57f));
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
        Refresh();
        gameObject.AddComponent<ShopHandTool>().Configure(ShopToolKind.ContrabandBox);
    }
    private void Refresh()
    {
        if (contentsLabel != null) contentsLabel.text = Quantity > 0 ? Quantity + " × DNYIPRO" : "ÜRES DOBOZ";
    }
    // One E places exactly one pack, like warehouse cartons; the box keeps the remainder.
    public bool UnpackOne(ProductSlot slot)
    {
        var product = products.FirstOrDefault(p=>p!=null);
        if (product == null || slot == null || slot.Occupant != null) return false;
        product.AssignInitialSlot(slot);
        product.transform.SetParent(slot.transform.parent,true);
        foreach (var renderer in product.GetComponentsInChildren<Renderer>()) renderer.enabled=true;
        foreach (var collider in product.GetComponentsInChildren<Collider>()) collider.enabled=true;
        products.Remove(product);
        Refresh();
        return true;
    }
}
