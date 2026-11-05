using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public sealed class ShopAfterHours:MonoBehaviour
{
    public const int BundleQuantity=5;
    private MvpShopController shop;
    private GameObject merchant;
    private readonly List<Material> materials=new List<Material>();
    private readonly List<ProductSlot> slots=new List<ProductSlot>();
    private Light yardLight,sun;
    private float sunIntensity,ambientIntensity;
    private Color sunColor,ambientColor,background;
    private AmbientMode ambientMode;
    private Material skybox;
    private CameraClearFlags clearFlags;
    private bool night;
    private Vector2 tradeScroll;
    private int tradeDay;
    private readonly int scheduleSeed = System.Guid.NewGuid().GetHashCode();
    public int OfferedQuantity { get; private set; }
    public ShopContrabandBox DeliveryBox { get; private set; }
    public static int QuantityForEvening(int seed, int day)
    {
        var random = new System.Random(unchecked(seed+day*7919));
        if (day>1 && random.NextDouble()>=.65) return 0;
        return random.Next(1,11);
    }
    public bool BoughtTonight{get;private set;}
    public bool TradeOpen{get;private set;}
    public Transform Merchant=>merchant.transform;
    public ProductSlot[] PickupSlots=>slots.ToArray();
    public bool IsNight=>night;
    public void Configure(MvpShopController owner)
    {
        shop=owner;var root=new GameObject("Evening Rear Yard").transform;root.SetParent(transform,false);
        merchant=new GameObject("Suspicious Night Vendor");merchant.transform.SetParent(root,false);merchant.transform.position=new Vector3(-5.3f,0,3.35f);merchant.transform.rotation=Quaternion.Euler(0,180,0);
        var coat=Mat(new Color(.045f,.05f,.065f));var face=Mat(new Color(.55f,.40f,.30f));
        Shape(merchant.transform,"Vendor coat",new Vector3(0,.85f,0),new Vector3(.5f,.70f,.4f),coat,PrimitiveType.Capsule);
        Shape(merchant.transform,"Vendor head",new Vector3(0,1.55f,0),Vector3.one*.30f,face,PrimitiveType.Sphere);
        Shape(merchant.transform,"Vendor hood",new Vector3(0,1.63f,-.04f),new Vector3(.36f,.34f,.30f),coat,PrimitiveType.Sphere);
        merchant.AddComponent<ShopStation>().Configure(shop,ShopStationKind.Smuggler);
        foreach(float x in new[]{-.13f,.13f})Shape(merchant.transform,"Vendor leg",new Vector3(x,.25f,0),new Vector3(.16f,.5f,.18f),coat);
        ProductVisuals.Text(merchant.transform,"Vendor name","GYANÚS ALAK\nE – Beszélgetés",new Vector3(0,1.98f,0),Quaternion.Euler(0,180,0),.045f,Color.white);
        merchant.SetActive(false);
        var table=Shape(root,"Night vendor pickup table",new Vector3(-5.3f,.55f,4.45f),new Vector3(.8f,.12f,.65f),Mat(new Color(.25f,.17f,.10f)));
        foreach(float x in new[]{-.30f,.30f})Shape(root,"Pickup table leg",new Vector3(-5.3f+x,.25f,4.45f),new Vector3(.06f,.5f,.48f),coat);
        for(int i=0;i<BundleQuantity;i++){var slot=new GameObject("Night purchase pickup "+i).AddComponent<ProductSlot>();slot.transform.SetParent(root,false);slot.transform.position=new Vector3(-5.3f+(i%3-1)*.22f,.62f,4.45f+(i/3-.5f)*.29f);slot.Configure(new Vector3(.2f,.35f,.24f));slots.Add(slot);}
        ProductVisuals.Text(root,"Vendor pickup sign","ÁTVÉTEL • KÉZZEL PAKOLD BE",new Vector3(-5.3f,.50f,4.10f),Quaternion.identity,.031f,Color.white);
        yardLight=new GameObject("Rear yard night light").AddComponent<Light>();yardLight.transform.SetParent(root,false);yardLight.transform.position=new Vector3(-4.8f,2.6f,3.7f);yardLight.type=LightType.Point;yardLight.range=7;yardLight.intensity=2;yardLight.color=new Color(1,.65f,.32f);yardLight.enabled=false;
    }
    Material Mat(Color c){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=c};materials.Add(m);return m;}
    GameObject Shape(Transform parent,string name,Vector3 p,Vector3 scale,Material m,PrimitiveType type=PrimitiveType.Cube){var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=m;return go;}
    public void Enter()
    {
        if(shop.Phase!=ShopPhase.AfterHours||night)return;
        if(tradeDay!=shop.Day){tradeDay=shop.Day;BoughtTonight=false;OfferedQuantity=QuantityForEvening(scheduleSeed,shop.Day);}
        night=true;merchant.SetActive(OfferedQuantity>0);yardLight.enabled=true;
        sun=FindObjectsByType<Light>().FirstOrDefault(l=>l.type==LightType.Directional&&l.enabled);if(sun!=null){sunIntensity=sun.intensity;sunColor=sun.color;sun.intensity*=.18f;sun.color=new Color(.45f,.55f,1);}
        ambientMode=RenderSettings.ambientMode;ambientColor=RenderSettings.ambientLight;ambientIntensity=RenderSettings.ambientIntensity;skybox=RenderSettings.skybox;
        RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.10f,.13f,.23f);RenderSettings.ambientIntensity=.35f;
        var camera=Camera.main;if(camera!=null){clearFlags=camera.clearFlags;background=camera.backgroundColor;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.025f,.055f);}
    }
    public void EndEvening()
    {
        CloseTrade();merchant.SetActive(false);yardLight.enabled=false;if(!night)return;night=false;
        if(sun!=null){sun.intensity=sunIntensity;sun.color=sunColor;}RenderSettings.ambientMode=ambientMode;RenderSettings.ambientLight=ambientColor;RenderSettings.ambientIntensity=ambientIntensity;RenderSettings.skybox=skybox;
        var camera=Camera.main;if(camera!=null){camera.clearFlags=clearFlags;camera.backgroundColor=background;}
    }
    public void ResetEvenings()
    {
        EndEvening();tradeDay=0;BoughtTonight=false;OfferedQuantity=0;
        if (DeliveryBox!=null) { DeliveryBox.gameObject.SetActive(false); Destroy(DeliveryBox.gameObject); }
    }
    public bool OpenTrade()
    {
        if(shop.Phase!=ShopPhase.AfterHours||!night||!merchant.activeSelf||FindAnyObjectByType<PlayerInteractor>().HasHeldItem||Vector3.Distance(FindAnyObjectByType<PlayerInteractor>().transform.position,merchant.transform.position)>2.5f)return false;
        shop.ClosePanel();TradeOpen=true;ShopInputGate.Blocked=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;return true;
    }
    public void CloseTrade(){if(!TradeOpen)return;TradeOpen=false;shop.ClosePanel();}
    public bool BuyBundle()
    {
        if(!TradeOpen||shop.Phase!=ShopPhase.AfterHours||BoughtTonight)return false;
        if(OfferedQuantity<=0 || DeliveryBox!=null && DeliveryBox.Quantity>0){shop.Notify("Előbb pakold be az előző dobozt a pult alá.");return false;}
        if(!shop.Ledger.TryBuyAfterHours(ShopCatalog.Costs[ShopCatalog.Contraband],OfferedQuantity)){shop.Notify("Nincs elég pénzed a szállítmányra.");return false;}
        shop.SyncCashDrawer();
        DeliveryBox = new GameObject("Paid Dnyipro delivery box").AddComponent<ShopContrabandBox>();
        DeliveryBox.transform.position = new Vector3(-5.3f,.62f,4.45f);
        DeliveryBox.Configure(shop,OfferedQuantity);
        BoughtTonight=true;shop.Notify(OfferedQuantity+" csomag Dnyipro dobozban az udvari asztalon. Vidd be a pult alatti polchoz.");return true;
    }
    void Update(){if(TradeOpen&&Application.isFocused&&Keyboard.current!=null){if(ShopBindings.Pressed(UnityEngine.InputSystem.Key.Escape))CloseTrade();else if(ShopBindings.Pressed(UnityEngine.InputSystem.Key.Enter)||ShopBindings.Pressed(UnityEngine.InputSystem.Key.NumpadEnter))BuyBundle();}}
    void OnGUI()
    {
        if(!TradeOpen||ShopMenu.Active)return;var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)*.5f,(Screen.height-720*scale)*.5f),Quaternion.identity,Vector3.one*scale);
        ShopUi.Panel(new Rect(300,150,680,420));GUILayout.BeginArea(new Rect(320,170,640,235));tradeScroll=GUILayout.BeginScrollView(tradeScroll);var style=ShopUi.Text(21);
        GUILayout.Label("GYANÚS ALAK",style);GUILayout.Space(12);GUILayout.Label("Jó estét. Ma "+OfferedQuantity+" csomag Dnyiprót tudtam hozni. Dobozban van, egyben elviheted. Nem minden este jutok erre.",style);
        GUILayout.Label(OfferedQuantity+" × 700 Ft = "+ShopCatalog.Money(OfferedQuantity*700)+" • Egy csomagban 20 szál",style);GUILayout.Label("Pénzed: "+ShopCatalog.Money(shop.Ledger.AvailableFunds),style);GUILayout.Label(shop.Message,style);GUILayout.EndScrollView();GUILayout.EndArea();
        GUILayout.BeginArea(new Rect(320,425,640,130));var button=ShopUi.ButtonStyle(20);
        GUI.enabled=!BoughtTonight;if(GUILayout.Button(BoughtTonight?"Ma elfogyott":"Megveszem • "+ShopCatalog.Money(OfferedQuantity*700)+" • Enter",button,GUILayout.Height(48)))BuyBundle();GUI.enabled=true;
        if(GUILayout.Button("Most ennyi • Esc",button,GUILayout.Height(48)))CloseTrade();GUILayout.EndArea();GUI.matrix=old;
    }
    void OnDestroy(){if(night)EndEvening();foreach(var material in materials)if(material!=null)Destroy(material);}
}
