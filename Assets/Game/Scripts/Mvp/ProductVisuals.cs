using UnityEngine;

public static class ProductVisuals
{
    public static readonly Color[] Colors = {new Color(.68f,.12f,.12f),new Color(.12f,.28f,.6f),new Color(.68f,.49f,.15f),new Color(.2f,.6f,.8f),new Color(.2f,.6f,.3f),new Color(.72f,.5f,.15f)};
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
        // Counter-scale packs, with fictional branding and a clear tobacco warning stripe.
        Transform face=new GameObject("Package print").transform;face.SetParent(item.transform,false);
        face.localPosition=new Vector3(0,0,-.506f);face.localRotation=Quaternion.identity;
        string label=type<3?ShopCatalog.Names[type].Replace(" ","\n")+"\n\n18+\nA dohányzás káros":ShopCatalog.ShortNames[type];
        Text(face,"Product label",label,Vector3.zero,Quaternion.identity,.1f,Color.white);
    }
}
