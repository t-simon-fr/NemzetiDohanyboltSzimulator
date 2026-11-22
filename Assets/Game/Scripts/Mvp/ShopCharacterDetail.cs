using System.Collections.Generic;
using UnityEngine;
public sealed class ShopCharacterDetail:MonoBehaviour
{
    readonly List<Material> materials=new List<Material>();
    Material Mat(Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};materials.Add(m);return m;}
    GameObject Shape(string name,Vector3 position,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube)
    {var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(transform,false);o.transform.localPosition=position;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=material;var c=o.GetComponent<Collider>();c.enabled=false;Destroy(c);return o;}
    public void Configure(int person)
    {
        if (person == 2) { ConfigurePisti(); return; }
        if (person == 1) { ConfigureMarika(); return; }
        ConfigureLegacy(person);
    }
    private void ConfigurePisti()
    {
        var body = transform.Find("Body");
        var prefab = Resources.Load<GameObject>("Customers/Pisti/Pisti");
        var actor = GetComponent<MvpCustomerActor>();
        if (prefab == null || body == null || actor == null) { ConfigureLegacy(2); return; }
        body.GetComponent<Renderer>().enabled = false;
        var model = Object.Instantiate(prefab, transform, false);
        model.name = "PistiModel";
        var rig = gameObject.AddComponent<ShopPistiRig>();
        rig.Configure(actor, MvpShopController.Instance);
    }
    private void ConfigureMarika()
    {
        var body = transform.Find("Body");
        var prefab = Resources.Load<GameObject>("Customers/Marika/Marika");
        var actor = GetComponent<MvpCustomerActor>();
        if (prefab == null || body == null || actor == null) { ConfigureLegacy(1); return; }
        body.GetComponent<Renderer>().enabled = false;
        var model = Object.Instantiate(prefab, transform, false);
        model.name = "MarikaModel";
        var rig = gameObject.AddComponent<ShopMarikaRig>();
        rig.Configure(actor, MvpShopController.Instance);
    }
    private void ConfigureLegacy(int person)
    {
        var hair=Mat(new Color(.10f+person*.06f,.055f+person*.05f,.025f+person*.05f));var dark=Mat(new Color(.035f,.04f,.045f));var white=Mat(new Color(.9f,.87f,.79f));
        Shape("Hair",new Vector3(0,1.73f,0),new Vector3(.33f,.14f,.31f),hair,PrimitiveType.Sphere);
        foreach(int side in new[]{-1,1}){Shape("Eye",new Vector3(side*.065f,1.64f,.152f),new Vector3(.035f,.04f,.02f),dark,PrimitiveType.Sphere);Shape("Collar",new Vector3(side*.065f,1.36f,.17f),new Vector3(.08f,.045f,.015f),white).transform.localRotation=Quaternion.Euler(0,0,side*35);}
        var sleeve=Mat(ShopPeople.Colors[person]);var skin=Mat(new Color(.58f,.40f,.28f));
        foreach(int side in new[]{-1,1}){Shape("Sleeve",new Vector3(side*.27f,1.02f,0),new Vector3(.10f,.38f,.10f),sleeve,PrimitiveType.Capsule);Shape("Hand",new Vector3(side*.28f,.76f,.025f),new Vector3(.10f,.13f,.10f),skin,PrimitiveType.Sphere);}
        Shape("Nose",new Vector3(0,1.58f,.17f),new Vector3(.045f,.055f,.045f),skin,PrimitiveType.Sphere);
        Shape("Mouth",new Vector3(0,1.52f,.16f),new Vector3(.065f,.008f,.008f),dark);
        for(int i=0;i<3;i++)Shape("Jacket button",new Vector3(0,1.25f-i*.12f,.205f),new Vector3(.025f,.025f,.02f),dark,PrimitiveType.Sphere);
        if(person%2==0)Shape("Cap brim",new Vector3(0,1.76f,.16f),new Vector3(.36f,.025f,.20f),hair);
    }
    void OnDestroy(){foreach(var m in materials)if(m!=null)Destroy(m);}
}
