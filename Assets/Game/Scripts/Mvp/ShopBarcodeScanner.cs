using UnityEngine;

public sealed class ShopBarcodeScanner : MonoBehaviour
{
    private ProductSlot slot;
    private Material housing, glass, indicator;
    private float confirmationUntil;
    private AudioSource audioSource;
    private AudioClip beep;
    public int ScanCount { get; private set; }
    public string LastProductId { get; private set; }

    public void Configure(MvpShopController shop)
    {
        slot = shop.DeliverySlot;
        transform.SetParent(slot.transform, false);
        var oldTray = shop.FloorPlan.Find("Blue handover tray");
        if (oldTray != null) oldTray.gameObject.SetActive(false);
        var oldLabel = shop.FloorPlan.Find("Átadó felirat");
        if (oldLabel != null) oldLabel.gameObject.SetActive(false);
        housing = MakeMaterial("Scanner housing", new Color(.08f,.10f,.10f));
        glass = MakeMaterial("Scanner glass", new Color(.025f,.035f,.035f));
        indicator = MakeMaterial("Scanner scan line", new Color(.9f,.08f,.04f));
        Box("Scanner bezel", new Vector3(0,-.014f,0), new Vector3(.44f,.026f,.44f), housing);
        Box("Scanner glass", new Vector3(0,.001f,0), new Vector3(.38f,.006f,.36f), glass);
        Box("Scanner status strip", new Vector3(0,.006f,.18f), new Vector3(.22f,.006f,.012f), indicator);
        Box("Scanner laser line", new Vector3(0,.006f,0), new Vector3(.34f,.004f,.004f), indicator);
        ProductVisuals.Text(transform,"Scanner label","VONALKÓDOLVASÓ",new Vector3(0,.005f,-.205f),Quaternion.Euler(90,180,0),.017f,Color.white);
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false; audioSource.spatialBlend = 1; audioSource.maxDistance = 5;
        var samples = new float[2400];
        for (int i=0;i<samples.Length;i++) samples[i] = Mathf.Sin(i*2*Mathf.PI*1400/24000)*.12f*(1-i/(float)samples.Length);
        beep = AudioClip.Create("Scanner confirmation",samples.Length,1,24000,false);
        beep.SetData(samples,0);
    }

    public bool TryScan(ShopProduct product)
    {
        if (product == null || product.IsHeld || product.CurrentSlot != slot || slot.Occupant != product || ShopCatalog.Index(product.ProductId) < 0) return false;
        LastProductId = product.ProductId;
        ScanCount++;
        confirmationUntil = Time.unscaledTime + .55f;
        indicator.color = new Color(.15f,1,.3f);
        audioSource.PlayOneShot(beep);
        return true;
    }

    private void Update()
    {
        if (indicator != null && Time.unscaledTime >= confirmationUntil) indicator.color = new Color(.9f,.08f,.04f);
    }
    private static Material MakeMaterial(string name, Color color) => new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name=name, color=color };
    private void Box(string name, Vector3 position, Vector3 size, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name; part.transform.SetParent(transform,false);
        part.transform.localPosition = position; part.transform.localScale = size;
        part.GetComponent<Collider>().enabled = false;
        part.GetComponent<Renderer>().sharedMaterial = material;
    }
    private void OnDestroy()
    {
        Destroy(housing); Destroy(glass); Destroy(indicator); Destroy(beep);
    }
}
