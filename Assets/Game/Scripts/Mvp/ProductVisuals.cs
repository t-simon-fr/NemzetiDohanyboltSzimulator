using UnityEngine;

public static class ProductVisuals
{
    public static readonly Color[] Colors = {new Color(.87f,.81f,.67f),new Color(.72f,.04f,.07f),new Color(.28f,.035f,.075f),new Color(.035f,.035f,.04f),new Color(.2f,.6f,.8f),new Color(.72f,.025f,.04f),new Color(.025f,.03f,.025f),new Color(.75f,.24f,.025f),new Color(.12f,.32f,.12f)};
    public static Vector3 Size(int type)
    {
        if(ShopCatalog.IsCoffee(type))return new Vector3(.14f,type==ShopCatalog.Coffee?.16f:type==ShopCatalog.LongCoffee?.22f:.27f,.14f);
        if (ShopCatalog.IsCigarette(type)) return new Vector3(.16f,.26f,.08f);
        if (type == ShopCatalog.Index("kevert")) return new Vector3(.085f,.20f,.085f);
        if (type == ShopCatalog.Index("water")) return new Vector3(.15f,.35f,.15f);
        if (type == ShopCatalog.Index("cola")) return new Vector3(.14f,.24f,.14f);
        return new Vector3(.15f,.32f,.15f);
    }
    public static TextMesh Text(Transform parent,string name,string value,Vector3 position,Quaternion rotation,float size,Color color)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=rotation;
        var t=go.AddComponent<TextMesh>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.GetComponent<MeshRenderer>().sharedMaterial=WorldFontMaterial();
        t.text=value;t.characterSize=size*.18f;t.fontSize=64;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;go.AddComponent<ShopWorldText>();
        return t;
    }
    private static Material fontMaterial;
    public static Material WorldFontMaterial()
    {
        var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(fontMaterial==null)
        {
            fontMaterial=new Material(Shader.Find("Shop/WorldText")){name="World font"};
            fontMaterial.mainTexture=font.material.mainTexture;
            Font.textureRebuilt-=UpdateFontTexture;Font.textureRebuilt+=UpdateFontTexture;
        }
        return fontMaterial;
    }
    private static void UpdateFontTexture(Font font)
    {
        if(fontMaterial!=null && font==Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"))fontMaterial.mainTexture=font.material.mainTexture;
    }
    internal static void ReleaseWorldFont()
    {
        Font.textureRebuilt -= UpdateFontTexture;
        if (fontMaterial != null) Object.Destroy(fontMaterial);
        fontMaterial = null;
    }
    private static readonly string[] ModelPaths = { "CigarettePacks/LuckyStroke", "CigarettePacks/Marlbroke", "CigarettePacks/Downhill", "CigarettePacks/Davidon", "Drinks/ReturAqua", "Drinks/LocaCola", "Drinks/HamsterEnergy", "Drinks/Kevert", "Drinks/Kofejtoi" };
    public static GameObject LoadModel(int type)
    {
        if(ShopCatalog.IsCoffee(type)||type==ShopCatalog.Contraband)return null;
        if (type < 0 || type >= ModelPaths.Length) throw new System.ArgumentOutOfRangeException(nameof(type));
        return Resources.Load<GameObject>(ModelPaths[type]);
    }
    public static void Add(GameObject item,int type)
    {
        if(ShopCatalog.IsCoffee(type))
        {
            item.GetComponent<Renderer>().enabled=false;
            var body=GameObject.CreatePrimitive(PrimitiveType.Cylinder);body.name="Coffee cup";body.transform.SetParent(item.transform,false);body.transform.localScale=new Vector3(.84f,.48f,.84f);
            var cup=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.9f,.87f,.78f)};body.GetComponent<Renderer>().sharedMaterial=cup;
            var liquid=GameObject.CreatePrimitive(PrimitiveType.Cylinder);liquid.name="Fresh coffee";liquid.transform.SetParent(item.transform,false);liquid.transform.localPosition=Vector3.up*.485f;liquid.transform.localScale=new Vector3(.73f,.008f,.73f);
            var brown=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=type==ShopCatalog.Latte?new Color(.80f,.66f,.46f):new Color(.10f,.04f,.01f)};liquid.GetComponent<Renderer>().sharedMaterial=brown;
            foreach(var collider in new[]{body.GetComponent<Collider>(),liquid.GetComponent<Collider>()}){collider.enabled=false;Object.Destroy(collider);}
            var cleanup=item.AddComponent<ShopProductMaterials>();cleanup.Materials=new[]{cup,brown};return;
        }
        if(type==ShopCatalog.Contraband)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.06f,.16f,.30f)};item.GetComponent<Renderer>().sharedMaterial=m;
            item.AddComponent<ShopProductMaterials>().Materials=new[]{m};
            Text(item.transform,"Dnyipro label","DNYIPRO\nUKRAJNA\n20 SZÁL\n18+",new Vector3(0,0,-.506f),Quaternion.identity,.18f,new Color(1,.78f,.2f));return;
        }
        // Product roots retain their interaction collider; prefabs contain only visual meshes.
        var prefab = LoadModel(type);
        if (prefab != null)
        {
            var model = Object.Instantiate(prefab, item.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            var oldRenderer = item.GetComponent<Renderer>();
            if (oldRenderer != null) oldRenderer.enabled = false;
            return;
        }
        Debug.LogError("Missing product packaging prefab for " + ShopCatalog.Names[type]);
        Transform face=new GameObject("Package print").transform;face.SetParent(item.transform,false);
        face.localPosition=new Vector3(0,0,-.506f);face.localRotation=Quaternion.identity;
        string label=ShopCatalog.IsCigarette(type)?ShopCatalog.Names[type].Replace(" ","\n")+"\n\n18+\nA dohányzás káros":ShopCatalog.ShortNames[type];
        Text(face,"Product label",label,Vector3.zero,Quaternion.identity,.1f,Color.white);
    }
}
