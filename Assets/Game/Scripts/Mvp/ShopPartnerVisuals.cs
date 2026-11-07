using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Imported packaging is borrowed; only the shared runtime inks belong to this module.
public sealed class ShopPartnerVisuals : IDisposable
{
    private readonly ShopRuntimeResources resources = new ShopRuntimeResources();
    private readonly Dictionary<string, Material> inks = new Dictionary<string, Material>();
    private Material Ink(string name, Color color)
    {
        if (inks.TryGetValue(name, out var material)) return material;
        material = resources.Own(new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color });
        material.SetFloat("_Smoothness", .28f);
        inks.Add(name, material);
        return material;
    }
    private GameObject Shape(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 size, Material material, bool solid = false)
    {
        var item = GameObject.CreatePrimitive(primitive);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = size;
        item.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) { item.GetComponent<Collider>().enabled = false; UnityEngine.Object.Destroy(item.GetComponent<Collider>()); }
        return item;
    }
    private GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool solid = false)
        => Shape(parent, name, PrimitiveType.Cube, position, size, material, solid);
    private TextMesh Label(Transform parent, string name, string text, Vector3 position, float size, Color color, float width = 0)
    {
        var result = ProductVisuals.Text(parent,name,text,position,Quaternion.identity,size,color);
        if (width > 0) ProductVisuals.FitText(result,width,result.characterSize);
        return result;
    }

    public void DecorateFridge(ShopFurniture item, ShopPartnerKind kind)
    {
        bool hamster = kind == ShopPartnerKind.Hamster;
        var body = Ink(hamster ? "Hamster cabinet" : "Loca cabinet", hamster ? new Color(.035f,.045f,.035f) : new Color(.64f,.025f,.045f));
        var shelf = Ink(hamster ? "Hamster shelves" : "Loca shelves", hamster ? new Color(.17f,.33f,.07f) : new Color(.9f,.86f,.73f));
        foreach (var part in item.GetComponentsInChildren<Renderer>(true))
        {
            if (part.name is "Fridge back" or "Fridge side" or "Fridge roof" or "Compressor base" or "Vent fin") part.sharedMaterial = body;
            if (part.name == "Fridge shelf") part.sharedMaterial = shelf;
        }
        var oldSign = item.GetComponentsInChildren<TextMesh>(true).First(t => t.name == "Fridge sign");
        oldSign.text = "";
        Box(item.transform, "Partner header", new Vector3(0,1.9f,-.355f), new Vector3(1.46f,.23f,.018f), body);
        Label(item.transform, "Partner brand", hamster ? "HAMSTER ENERGY" : "Loca Cola", new Vector3(.08f,1.92f,-.37f), .13f, hamster ? new Color(.48f,.91f,.12f) : new Color(.98f,.94f,.83f),1.15f);
        Label(item.transform, "Partner range", hamster ? "ENERGY • ZERO • PACIFIC MANGO" : "LOCA COLA • FANTOM • SPRITZ", new Vector3(0,1.81f,-.371f), .04f, Color.white);
        if (hamster) Claw(item.transform, new Vector3(-.57f,1.93f,-.372f), .14f);
        foreach (var row in item.Slots.GroupBy(s => Mathf.RoundToInt(s.transform.localPosition.y * 100)))
        {
            var text = Label(item.transform, "Partner shelf price", "", new Vector3(0,row.First().transform.localPosition.y-.065f,-.34f), .045f, Color.white);
            text.gameObject.AddComponent<ShopShelfPriceLabel>().Configure(text,row.ToArray(),true,1.32f);
        }
    }
    private void Claw(Transform parent, Vector3 position, float height)
    {
        var green = Ink("Hamster claw",new Color(.48f,.91f,.12f));
        for (int i = 0; i < 3; i++)
        {
            var mark = new GameObject("Hamster claw mark").AddComponent<LineRenderer>();
            mark.transform.SetParent(parent,false);
            mark.transform.localPosition = position + Vector3.right * ((i-1)*height*.22f);
            mark.sharedMaterial = green;
            mark.useWorldSpace = false;
            mark.positionCount = 4;
            mark.SetPositions(new[] { new Vector3(-height*.1f,height*.5f,0), new Vector3(height*.08f,height*.2f,0), new Vector3(-height*.05f,-height*.08f,0), new Vector3(height*.06f,-height*.5f,0) });
            mark.widthMultiplier = height*.08f;
            mark.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    public void AddProduct(GameObject item, int type)
    {
        item.GetComponent<Renderer>().enabled = false;
        var visual = new GameObject("Partner packaging").transform;
        visual.SetParent(item.transform,false);
        Vector3 size = ProductVisuals.Size(type);
        visual.localScale = new Vector3(1/size.x,1/size.y,1/size.z);
        if (type == ShopPartnerCatalog.Zero || type == ShopPartnerCatalog.Mango)
        {
            var model = UnityEngine.Object.Instantiate(ProductVisuals.LoadModel(6),item.transform,false);
            model.transform.localScale = Vector3.one;
            bool zero = type == ShopPartnerCatalog.Zero;
            var band = Ink(zero ? "Zero silver" : "Mango teal",zero ? new Color(.88f,.9f,.86f) : new Color(.06f,.48f,.48f));
            Box(visual,"Variant band",new Vector3(0,-.09f,-.076f),new Vector3(.116f,.063f,.003f),band);
            Label(visual,"Variant name",zero ? "ZERO" : "PACIFIC\nMANGO",new Vector3(0,zero ? -.08f : -.09f,-.078f),zero ? .05f : .026f,zero ? Color.black : new Color(1,.72f,.15f),.105f);
            if (zero) Label(visual,"Variant subtitle","CUKORMENTES",new Vector3(0,-.11f,-.078f),.012f,Color.black,.105f);
            return;
        }
        if (ShopPartnerCatalog.IsSpirit(type)) { Bottle(visual,type); return; }
        bool orange = type == ShopPartnerCatalog.Fantom;
        var paint = Ink(orange ? "Fantom orange" : "Spritz green",orange ? new Color(.95f,.3f,.025f) : new Color(.08f,.48f,.17f));
        var metal = Ink("Can aluminium",new Color(.72f,.77f,.78f));
        Shape(visual,"Can body",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.137f,.11f,.137f),paint);
        foreach (float y in new[] { -.114f,.114f }) Shape(visual,"Can rim",PrimitiveType.Cylinder,Vector3.up*y,new Vector3(.14f,.003f,.14f),metal);
        Box(visual,"Can tab",new Vector3(0,.119f,0),new Vector3(.018f,.003f,.032f),metal);
        Label(visual,"Drink brand",orange ? "FANTOM" : "SPRITZ",new Vector3(0,.025f,-.071f),.055f,Color.white,.118f);
        Label(visual,"Drink flavour",orange ? "NARANCS" : "CITRUS",new Vector3(0,-.025f,-.071f),.026f,Color.white,.115f);
        Shape(visual,"Fruit emblem",PrimitiveType.Sphere,new Vector3(0,-.069f,-.069f),new Vector3(.04f,.04f,.006f),Ink("Citrus gold",new Color(1,.72f,.06f)));
    }
    private void Bottle(Transform parent, int type)
    {
        var glass = Ink("Palinka glass",new Color(.72f,.81f,.71f,.32f));
        glass.shader = Shader.Find("Shop/PET");
        glass.renderQueue = 3000;
        glass.color = new Color(.72f,.81f,.71f,.32f);
        var lid = Ink("Palinka black cap",new Color(.055f,.045f,.035f));
        var paper = Ink("Palinka cream label",new Color(.94f,.88f,.71f));
        Color fruit = type == ShopPartnerCatalog.Plum ? new Color(.31f,.12f,.47f) : type == ShopPartnerCatalog.Pear ? new Color(.42f,.61f,.13f) : new Color(.8f,.3f,.11f);
        Shape(parent,"Glass bottle",PrimitiveType.Cylinder,new Vector3(0,-.025f,0),new Vector3(.078f,.075f,.078f),glass);
        Shape(parent,"Glass neck",PrimitiveType.Cylinder,new Vector3(0,.067f,0),new Vector3(.033f,.03f,.033f),glass);
        Shape(parent,"Bottle cap",PrimitiveType.Cylinder,new Vector3(0,.101f,0),new Vector3(.041f,.009f,.041f),lid);
        Box(parent,"Bottle paper",new Vector3(0,-.018f,-.040f),new Vector3(.065f,.10f,.002f),paper);
        Box(parent,"Bottle flavour band",new Vector3(0,-.060f,-.042f),new Vector3(.065f,.018f,.002f),Ink("Fruit "+type,fruit));
        Label(parent,"Palinka brand","MŰSZAK-\nKEZDŐ",new Vector3(0,.003f,-.043f),.024f,new Color(.055f,.025f,.01f),.055f);
        Label(parent,"Palinka flavour",type == ShopPartnerCatalog.Plum ? "SZILVA" : type == ShopPartnerCatalog.Pear ? "KÖRTE" : "VEGYES",new Vector3(0,-.038f,-.043f),.021f,new Color(.055f,.025f,.01f),.055f);
        Label(parent,"Palinka size","5 cl",new Vector3(0,-.06f,-.044f),.012f,Color.white,.05f);
    }
    public GameObject CreateCounterDisplay(Transform parent)
    {
        var root = new GameObject("Műszakkezdő partner display").transform;
        root.SetParent(parent,false);
        root.SetPositionAndRotation(new Vector3(-1.6f,2.05f,-4.15f),Quaternion.Euler(0,180,0));
        var wood = Ink("Display walnut",new Color(.25f,.12f,.065f));
        var cream = Ink("Display cream",new Color(.94f,.88f,.71f));
        Box(root,"Display back",new Vector3(0,0,.13f),new Vector3(1.05f,.66f,.035f),wood,true);
        foreach (int side in new[] { -1,1 }) Box(root,"Display side",new Vector3(side*.51f,0,0),new Vector3(.035f,.66f,.3f),wood,true);
        Box(root,"Display header",new Vector3(0,.36f,0),new Vector3(1.1f,.14f,.3f),cream,true);
        Label(root,"Display brand","MŰSZAKKEZDŐ",new Vector3(0,.36f,-.155f),.10f,new Color(.25f,.12f,.065f));
        for (int row = 0; row < 2; row++)
        {
            float y = -.28f + row*.31f;
            Box(root,"Display shelf",new Vector3(0,y,0),new Vector3(1.0f,.025f,.29f),wood,true);
            var slots = new List<ProductSlot>();
            for (int col = 0; col < 3; col++)
            {
                var slot = new GameObject("Partner spirit stock").AddComponent<ProductSlot>();
                slot.transform.SetParent(root,false);
                slot.transform.localPosition = new Vector3((col-1)*.29f,y+.018f,0);
                slot.Configure(new Vector3(.25f,.26f,.25f));
                slot.RestrictTo("water");
                slot.SetStockFilter(id => ShopPartnerCatalog.PartnerFor(ShopCatalog.Index(id)) == 2);
                slots.Add(slot);
            }
            var text = Label(root,"Partner display price","",new Vector3(0,y-.045f,-.157f),.025f,Color.white);
            text.gameObject.AddComponent<ShopShelfPriceLabel>().Configure(text,slots.ToArray(),true,.96f);
        }
        return root.gameObject;
    }
    public void Dispose() => resources.Dispose();
}
