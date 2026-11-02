using UnityEngine;

public static class ProductVisuals
{
    public static readonly Color[] Colors = {new Color(.87f,.81f,.67f),new Color(.72f,.04f,.07f),new Color(.28f,.035f,.075f),new Color(.035f,.035f,.04f),new Color(.2f,.6f,.8f),new Color(.72f,.025f,.04f),new Color(.025f,.03f,.025f)};
    public static Vector3 Size(int type)
    {
        if (ShopCatalog.IsCigarette(type)) return new Vector3(.16f,.26f,.08f);
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
    public static void Add(GameObject item,int type)
    {
        // Product roots retain their interaction collider; prefabs contain only visual meshes.
        string[] models = { "CigarettePacks/LuckyStroke", "CigarettePacks/Marlbroke", "CigarettePacks/Downhill", "CigarettePacks/Davidon", "Drinks/ReturAqua", "Drinks/LocaCola", "Drinks/HamsterEnergy" };
        if (type < 0 || type >= models.Length) throw new System.ArgumentOutOfRangeException(nameof(type));
        var prefab = Resources.Load<GameObject>(models[type]);
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
        Debug.LogError("Missing product packaging prefab: " + models[type]);
        Transform face=new GameObject("Package print").transform;face.SetParent(item.transform,false);
        face.localPosition=new Vector3(0,0,-.506f);face.localRotation=Quaternion.identity;
        string label=ShopCatalog.IsCigarette(type)?ShopCatalog.Names[type].Replace(" ","\n")+"\n\n18+\nA dohányzás káros":ShopCatalog.ShortNames[type];
        Text(face,"Product label",label,Vector3.zero,Quaternion.identity,.1f,Color.white);
    }
}
