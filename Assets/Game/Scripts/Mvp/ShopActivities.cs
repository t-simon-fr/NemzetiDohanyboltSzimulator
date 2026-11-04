using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed partial class ShopActivities : MonoBehaviour
{
    public const int CoffeeMachinePrice=16000;
    private MvpShopController shop;
    private PlayerInteractor player;
    private Transform worldRoot,coffeeRoot,hand;
    private readonly List<Material> materials=new List<Material>();
    private readonly List<Camera> cameras=new List<Camera>();
    private readonly List<RenderTexture> cameraImages=new List<RenderTexture>();
    private readonly List<ShopFeatureTarget> spills=new List<ShopFeatureTarget>();
    private Material metal,ivory,dark,coffee,skin,coin,dirt;
    private ShopHandTool bat,mop;
    private Vector3 batHome,mopHome;
    private float dirtTimer=48,coffeeDelivery,brewRemaining,cameraTimer,snakeTimer,swingCooldown;
    private int pendingCoffee,selectedCamera,gameChoice;
    private TextMesh coffeeLabel;
    private ShopSnake snake;
    private ShopMemoryGame memory;
    private GUIStyle heading,label,buttonStyle;
    public bool CoffeeOwned { get; private set; }
    public int CoffeeSupplies { get; private set; }
    public int PendingCoffee=>pendingCoffee;
    public float BrewRemaining=>brewRemaining;
    public ProductSlot CoffeeOutput { get; private set; }
    public ProductSlot BasketSlot { get; private set; }
    public ShopCoinOffer CoinOffer { get; private set; }
    public Transform CustomerHand=>hand;
    public ShopHandTool Bat=>bat;
    public ShopHandTool Mop=>mop;
    public int DirtCount=>spills.Count(s=>s!=null&&s.gameObject.activeSelf);
    public ShopSnake Snake=>snake;
    public ShopMemoryGame Memory=>memory;
    public int CameraCount=>cameras.Count;
    public RenderTexture CameraImage(int index)=>cameraImages[index];
    public void Configure(MvpShopController controller,PlayerInteractor interactor)
    {
        shop=controller;player=interactor;
        worldRoot=new GameObject("Shop Activities").transform;worldRoot.SetParent(transform,false);
        metal=Mat("Brushed steel",new Color(.45f,.49f,.51f));ivory=Mat("Coffee porcelain",new Color(.90f,.87f,.78f));dark=Mat("Activities charcoal",new Color(.045f,.065f,.07f));
        coffee=Mat("Coffee",new Color(.12f,.055f,.018f));skin=Mat("Customer palm",new Color(.73f,.54f,.39f));coin=Mat("Customer coins",new Color(.77f,.64f,.29f));dirt=Mat("Floor stain",new Color(.18f,.13f,.055f));
        var spare=shop.DeliverySlot.transform.parent.Find("Counter Slot 1");BasketSlot=spare!=null?spare.GetComponent<ProductSlot>():new GameObject("Customer basket handover").AddComponent<ProductSlot>();BasketSlot.transform.SetParent(shop.DeliverySlot.transform.parent,false);
        BasketSlot.transform.localPosition=new Vector3(.47f,.012f,-.01f);BasketSlot.Configure(new Vector3(.35f,.4f,.33f));
        Box(BasketSlot.transform,"Customer basket mat",new Vector3(-.13f,-.006f,-.13f),new Vector3(.58f,.006f,.58f),metal);
        ProductVisuals.Text(BasketSlot.transform,"Basket handover label","VÁSÁRLÓ TERMÉKEI",new Vector3(0,.004f,.18f),Quaternion.Euler(90,180,0),.022f,Color.white);
        BuildCoffeeMachine();BuildTools();
        BuildCamera("Bejárat",new Vector3(-1.9f,2.65f,-4.12f),new Vector3(.3f,1.1f,-2.7f));
        BuildCamera("Önkiszolgáló",new Vector3(6.15f,2.65f,1.55f),new Vector3(3.6f,1,-2.5f));
        ResetFeatures();
    }
    private Material Mat(string name,Color color){var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};materials.Add(material);return material;}
    private static GameObject Shape(Transform parent,string name,Vector3 position,Vector3 scale,Material material,PrimitiveType type=PrimitiveType.Cube,bool collision=false)
    {
        var item=GameObject.CreatePrimitive(type);item.name=name;item.transform.SetParent(parent,false);item.transform.localPosition=position;item.transform.localScale=scale;item.GetComponent<Renderer>().sharedMaterial=material;
        if(!collision){var collider=item.GetComponent<Collider>();collider.enabled=false;Destroy(collider);}return item;
    }
    private static GameObject Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material,bool collision=false)=>Shape(parent,name,position,scale,material,PrimitiveType.Cube,collision);
    private void BuildCoffeeMachine()
    {
        coffeeRoot=new GameObject("Purchased Coffee Machine").transform;coffeeRoot.SetParent(worldRoot,false);coffeeRoot.position=new Vector3(1.66f,0,1.02f);
        Box(coffeeRoot,"Coffee cabinet",new Vector3(0,.41f,0),new Vector3(.65f,.82f,.55f),dark,true);
        Box(coffeeRoot,"Coffee machine body",new Vector3(0,1.08f,.05f),new Vector3(.43f,.48f,.37f),metal,true);
        Box(coffeeRoot,"Coffee drip tray",new Vector3(0,.84f,-.16f),new Vector3(.34f,.025f,.24f),dark,true);
        Box(coffeeRoot,"Coffee nozzle",new Vector3(0,1.17f,-.18f),new Vector3(.06f,.065f,.09f),dark,true);
        Shape(coffeeRoot,"Coffee bean hopper",new Vector3(0,1.37f,.05f),new Vector3(.22f,.07f,.22f),coffee,PrimitiveType.Cylinder);
        coffeeLabel=ProductVisuals.Text(coffeeRoot,"Coffee machine screen","",new Vector3(0,1.28f,-.144f),Quaternion.identity,.026f,Color.white);
        var target=coffeeRoot.gameObject.AddComponent<ShopFeatureTarget>();target.Kind=ShopFeatureKind.CoffeeMachine;
        CoffeeOutput=new GameObject("Coffee output slot").AddComponent<ProductSlot>();CoffeeOutput.transform.SetParent(coffeeRoot,false);CoffeeOutput.transform.localPosition=new Vector3(0,.86f,-.16f);CoffeeOutput.Configure(new Vector3(.2f,.23f,.2f));CoffeeOutput.RestrictTo("coffee");CoffeeOutput.Configure(new Vector3(.2f,.32f,.2f));
        ProductVisuals.Text(coffeeRoot,"Coffee station sign","TRAFIK PRESSZÓ",new Vector3(0,.62f,-.281f),Quaternion.identity,.038f,Color.white);
    }
    private void BuildTools()
    {
        var batRoot=new GameObject("Baseball bat");batRoot.transform.SetParent(worldRoot,false);batHome=new Vector3(1.9f,.12f,.25f);batRoot.transform.position=batHome;
        Shape(batRoot.transform,"Bat grip",new Vector3(0,.15f,0),new Vector3(.035f,.15f,.035f),dark,PrimitiveType.Cylinder);
        Shape(batRoot.transform,"Bat barrel",new Vector3(0,.49f,0),new Vector3(.075f,.22f,.075f),ivory,PrimitiveType.Cylinder);
        var hitbox=batRoot.AddComponent<BoxCollider>();hitbox.center=Vector3.up*.35f;hitbox.size=new Vector3(.10f,.72f,.10f);
        bat=batRoot.AddComponent<ShopHandTool>();bat.Configure(ShopToolKind.Bat);
        var mopRoot=new GameObject("Floor mop");mopRoot.transform.SetParent(worldRoot,false);mopHome=new Vector3(-1.8f,.035f,.68f);mopRoot.transform.position=mopHome;
        Shape(mopRoot.transform,"Mop handle",new Vector3(0,.59f,0),new Vector3(.025f,.58f,.025f),metal,PrimitiveType.Cylinder);
        Box(mopRoot.transform,"Mop head",new Vector3(0,.015f,0),new Vector3(.28f,.045f,.17f),ivory);
        var mopBox=mopRoot.AddComponent<BoxCollider>();mopBox.center=Vector3.up*.60f;mopBox.size=new Vector3(.28f,1.23f,.17f);
        mop=mopRoot.AddComponent<ShopHandTool>();mop.Configure(ShopToolKind.Mop);
    }
    private void BuildCamera(string name,Vector3 position,Vector3 target)
    {
        var mount=new GameObject("Security camera - "+name).transform;mount.SetParent(worldRoot,false);mount.position=position;mount.LookAt(target);
        Box(mount,"Camera housing",Vector3.zero,new Vector3(.21f,.12f,.27f),ivory);
        Shape(mount,"Camera lens",new Vector3(0,0,.15f),new Vector3(.085f,.035f,.085f),dark,PrimitiveType.Cylinder).transform.localRotation=Quaternion.Euler(90,0,0);
        var eye=new GameObject("CCTV view - "+name).AddComponent<Camera>();eye.transform.SetParent(mount,false);eye.transform.localPosition=Vector3.forward*.2f;eye.fieldOfView=78;eye.nearClipPlane=.05f;eye.farClipPlane=25;eye.enabled=false;
        var rt=new RenderTexture(480,270,16){name="CCTV - "+name};rt.Create();eye.targetTexture=rt;cameras.Add(eye);cameraImages.Add(rt);
    }
    public bool BuyCoffeeMachine()
    {
        if(CoffeeOwned||shop.Phase!=ShopPhase.Preparing){shop.Notify("Kávégépet nyitás előtt, egyszer vásárolhatsz.");return false;}
        if(!shop.Ledger.TryImprove(CoffeeMachinePrice)){shop.Notify("Nincs elég pénz a kávégépre.");return false;}
        shop.SyncCashDrawer();CoffeeOwned=true;shop.Navigation?.Rebuild();coffeeRoot.gameObject.SetActive(true);shop.Notify("Kávégép felszerelve. A Kávé alkalmazásban rendelhetsz alapanyagot.");return true;
    }
    public bool OrderCoffeeSupplies()
    {
        int quantity=10;
        if(!CoffeeOwned||shop.Phase==ShopPhase.Summary||CoffeeSupplies+pendingCoffee+ReadyCoffee+quantity>60){shop.Notify("Kávégép szükséges; legfeljebb 60 adag alapanyagot tárolhatsz.");return false;}
        if(!shop.Ledger.TryBuy(ShopCatalog.Costs[ShopCatalog.Coffee],quantity)){shop.Notify("Nincs elég pénz a kávéalapanyagra.");return false;}
        shop.SyncCashDrawer();pendingCoffee+=quantity;coffeeDelivery=8;shop.Notify("Megrendelve 10 adag kávéalapanyag. Szállítás: 8 másodperc.");return true;
    }
    public bool BrewCoffee()
    {
        if(selectedRecipe==ShopCatalog.Latte&&MilkSupplies==0){shop.Notify("A lattéhoz tölts tejet a gépbe.");return false;}
        if(!CoffeeOwned||shop.Phase==ShopPhase.Summary||CoffeeSupplies==0||brewRemaining>0||CoffeeOutput.Occupant!=null){shop.Notify("Kávéfőzéshez szabad tálca és megérkezett alapanyag szükséges.");return false;}
        CoffeeSupplies--;if(selectedRecipe==ShopCatalog.Latte)MilkSupplies--;brewRemaining=3.5f;shop.Notify("Készül a kávé…");return true;
    }
    public void UseCoffeeMachine()
    {
        if(RefillFromHand())return;
        if(CoffeeOutput.Occupant!=null){if(!player.TryHold(CoffeeOutput.Occupant))shop.Notify("Előbb tedd le a kezedben levő tárgyat.");}
        else BrewCoffee();
    }
    public bool PlaceBasket(MvpCustomerActor customer)
    {
        if(customer.PlaceBasket(BasketSlot))return true;shop.Notify("Tedd szabaddá az önkiszolgáló áru pulthelyét.");return false;
    }
    public void Tick(float dt)
    {
        TickMilk(Mathf.Max(0,dt));dt=Mathf.Max(0,dt);swingCooldown=Mathf.Max(0,swingCooldown-dt);
        if(pendingCoffee>0){coffeeDelivery-=dt;if(coffeeDelivery<=0){SpawnIngredientPackage(pendingCoffee);pendingCoffee=0;shop.Notify("Megérkezett a kávéalapanyag.");}}
        if(brewRemaining>0)
        {
            brewRemaining=Mathf.Max(0,brewRemaining-dt);
            if(brewRemaining==0){var cup=shop.CreateCoffee(selectedRecipe);if(!cup.AssignInitialSlot(CoffeeOutput)){cup.RemoveFromWorld();CoffeeSupplies++;if(selectedRecipe==ShopCatalog.Latte)MilkSupplies++;shop.Notify("A főzőtálca foglalt, az alapanyag megmaradt.");}else shop.Notify("Elkészült a kávé. E a gépen: felvétel.");}
        }
        coffeeLabel.text=brewRemaining>0?"FŐZÉS "+brewRemaining.ToString("0.0")+" mp":ShopCatalog.ShortNames[selectedRecipe]+"\n"+CoffeeSupplies+" kávé / "+MilkSupplies+" tej";
        if(shop.Modal==ShopModal.None&&shop.Phase==ShopPhase.Open)
        {dirtTimer-=dt;if(dirtTimer<=0){SpawnSpill();dirtTimer=55+(shop.Spawned%3)*9;}}
        if(shop.Devices.FocusKind==ShopDeviceKind.Monitor)
        {
            if(shop.Devices.ComputerApp==ShopComputerApp.Cameras){cameraTimer-=dt;if(cameraTimer<=0){RenderCamera(selectedCamera);cameraTimer=.2f;}}
            if(shop.Devices.ComputerApp==ShopComputerApp.Games)TickGames(dt);
        }
    }
    public ShopFeatureTarget SpawnSpill()
    {
        spills.RemoveAll(s=>s==null);if(DirtCount>=4)return null;
        Vector3[] positions={new Vector3(-.8f,.008f,-.1f),new Vector3(.4f,.008f,.25f),new Vector3(1.65f,.008f,-2.6f),new Vector3(-1.2f,.008f,-3.1f)};
        foreach(var point in positions)
        {
            if(spills.Any(s=>s!=null&&Vector3.Distance(s.transform.position,point)<.5f))continue;
            if(Physics.CheckBox(point+Vector3.up*.22f,new Vector3(.24f,.15f,.21f),Quaternion.identity,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))continue;
            var root=new GameObject("Floor spill");root.transform.SetParent(worldRoot,false);root.transform.position=point;
            Shape(root.transform,"Mud patch",Vector3.zero,new Vector3(.43f,.0025f,.34f),dirt,PrimitiveType.Cylinder);
            Shape(root.transform,"Mud edge",new Vector3(.14f,0,.04f),new Vector3(.20f,.0025f,.16f),dirt,PrimitiveType.Cylinder);
            var collider=root.AddComponent<BoxCollider>();collider.size=new Vector3(.5f,.065f,.4f);
            var spill=root.AddComponent<ShopFeatureTarget>();spill.Kind=ShopFeatureKind.Spill;spills.Add(spill);return spill;
        }
        return null;
    }
    public bool SwingBat(Camera view)
    {
        if(swingCooldown>0||player.HeldTool==null||player.HeldTool.Kind!=ShopToolKind.Bat)return false;swingCooldown=.65f;player.HeldTool.PlaySwing();
        if(Physics.SphereCast(view.ViewportPointToRay(new Vector3(.5f,.5f,0)),.12f,out var hit,1.8f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
        {
            var customer=hit.collider.GetComponentInParent<MvpCustomerActor>();
            if(customer!=null&&shop.StopThief(customer))return true;
        }
        shop.Notify("Az ütővel a közelben menekülő tolvajt állíthatod meg.");return false;
    }
    public bool RenderCamera(int index)
    {
        if(index<0||index>=cameras.Count||index==1&&!shop.Layout.Expanded)return false;
        cameras[index].Render();return true;
    }
    public void ShowCoins(MvpCustomerActor customer)
    {
        ClearHand();CoinOffer=new ShopCoinOffer(customer.TotalPrice,shop.Day*19+shop.Spawned);
        hand=new GameObject("Customer coin palm").transform;hand.SetParent(customer.transform,false);hand.localPosition=new Vector3(-.22f,shop.DeliverySlot.transform.position.y-customer.transform.position.y+.14f,.32f);
        Shape(hand,"Open palm",new Vector3(0,-.025f,0),new Vector3(.42f,.045f,.23f),skin,PrimitiveType.Capsule);
        for(int i=0;i<4;i++)Shape(hand,"Finger",new Vector3((i-1.5f)*.075f,-.015f,.13f),new Vector3(.055f,.025f,.16f),skin,PrimitiveType.Capsule).transform.localRotation=Quaternion.Euler(90,0,0);
        RefreshHandCoins();
    }
    public void RefreshHandCoins()
    {
        if(hand==null||CoinOffer==null)return;
        var previous=hand.Find("Offered coins");if(previous!=null){previous.gameObject.SetActive(false);Destroy(previous.gameObject);}
        var root=new GameObject("Offered coins").transform;root.SetParent(hand,false);
        for(int i=0;i<CoinOffer.Coins.Count;i++)
        {
            Vector3 position=new Vector3((i%5-2)*.07f,CoinOffer.Selected[i]?.09f:.035f,(i/5)*.07f-.055f);
            var item=Shape(root,"Customer coin "+i,position,new Vector3(.058f,.004f,.058f),coin,PrimitiveType.Cylinder,true);
            var control=item.AddComponent<ShopWorldControl>();control.Kind=WorldControlKind.CustomerCoin;control.Value=i;
            ProductVisuals.Text(root,"Coin value "+i,CoinOffer.Coins[i].ToString(),position+Vector3.up*.006f,Quaternion.Euler(90,0,0),.018f,Color.black);
        }
    }
    public void ClearHand(){CoinOffer=null;if(hand!=null){hand.gameObject.SetActive(false);Destroy(hand.gameObject);hand=null;}}
    public void ResetFeatures()
    {
        ResetIngredientPackages();ClearHand();gameChoice=0;snake=null;memory=null;selectedCamera=0;CoffeeOwned=false;CoffeeSupplies=0;pendingCoffee=0;brewRemaining=0;coffeeDelivery=0;dirtTimer=48;coffeeRoot.gameObject.SetActive(false);
        foreach(var spill in spills)if(spill!=null){spill.gameObject.SetActive(false);Destroy(spill.gameObject);}spills.Clear();
        ResetTool(bat,batHome);ResetTool(mop,mopHome);
    }
    private void ResetTool(ShopHandTool tool,Vector3 home){tool.ReturnToPickupPosition();ShopLoosePhysics.Freeze(tool.gameObject);tool.transform.SetParent(worldRoot,true);tool.transform.SetPositionAndRotation(home,Quaternion.identity);}
    public void StartSnake(){snake=new ShopSnake(shop.Day*37+Time.frameCount);gameChoice=1;snakeTimer=.17f;}
    public void StartMemory(){memory=new ShopMemoryGame(shop.Day*51+Time.frameCount);gameChoice=2;}
    private void TickGames(float dt)
    {
        if(gameChoice==1&&snake!=null)
        {
            var keys=Keyboard.current;
            if(keys!=null)
            {
                if(ShopBindings.Pressed(UnityEngine.InputSystem.Key.UpArrow)||ShopBindings.Pressed(UnityEngine.InputSystem.Key.W))snake.Turn(Vector2Int.down);
                if(ShopBindings.Pressed(UnityEngine.InputSystem.Key.DownArrow)||ShopBindings.Pressed(UnityEngine.InputSystem.Key.S))snake.Turn(Vector2Int.up);
                if(ShopBindings.Pressed(UnityEngine.InputSystem.Key.LeftArrow)||ShopBindings.Pressed(UnityEngine.InputSystem.Key.A))snake.Turn(Vector2Int.left);
                if(ShopBindings.Pressed(UnityEngine.InputSystem.Key.RightArrow)||ShopBindings.Pressed(UnityEngine.InputSystem.Key.D))snake.Turn(Vector2Int.right);
            }
            snakeTimer-=dt;
            if(snakeTimer<=0){snake.Step();snakeTimer=.17f;if(snake.Ended&&snake.Score>PlayerPrefs.GetInt("TobaccOS.SnakeRecord",0)){PlayerPrefs.SetInt("TobaccOS.SnakeRecord",snake.Score);PlayerPrefs.Save();}}
        }
        if(gameChoice==2)memory?.Tick(dt);
    }
    private void Styles(float width)
    {
        int size=Mathf.Clamp(Mathf.RoundToInt(width/46),13,22);
        heading=ShopUi.Text(size+3,true);
        label=ShopUi.Text(size);buttonStyle=ShopUi.ButtonStyle(size);
    }
    private void OnGUI()
    {
        if(shop==null||shop.Ledger==null||ShopMenu.Active)return;
        DrawMachineRecipeHint();
        if(shop.Devices.FocusKind==ShopDeviceKind.Coins&&CoinOffer!=null)
        {
            Styles(Screen.width*.6f);var panel=new Rect(Screen.width*.18f,Screen.height-170,Screen.width*.64f,150);ShopUi.Panel(panel);
            GUI.Label(new Rect(panel.x+12,panel.y+8,panel.width-24,35),"Fizetendő: "+ShopCatalog.Money(CoinOffer.Price)+" • Kiválasztva: "+ShopCatalog.Money(CoinOffer.Total),heading);
            GUI.Label(new Rect(panel.x+12,panel.y+45,panel.width-24,30),"Tab / nyilak: érme • Space: kiválasztás • Enter: átvétel • Backspace: vissza",label);
            if(GUI.Button(new Rect(panel.x+12,panel.y+86,panel.width*.3f,45),"Újrakezdem",buttonStyle)){CoinOffer.Clear();RefreshHandCoins();}
            if(GUI.Button(new Rect(panel.x+panel.width*.35f,panel.y+86,panel.width*.62f,45),"Átveszem az összeget",buttonStyle))shop.ConfirmCustomerCoins();
            return;
        }
        if(shop.Devices.FocusKind!=ShopDeviceKind.Monitor)return;
        var app=shop.Devices.ComputerApp;
        if(app!=ShopComputerApp.Coffee&&app!=ShopComputerApp.Cameras&&app!=ShopComputerApp.Games)return;
        Rect rect=shop.Devices.AppRect;Styles(rect.width);GUI.BeginGroup(rect);ShopUi.Panel(new Rect(0,0,rect.width,rect.height));
        try
        {
            if(app==ShopComputerApp.Coffee)DrawCoffee(rect.width,rect.height);
            else if(app==ShopComputerApp.Cameras)DrawCameras(rect.width,rect.height);
            else DrawGames(rect.width,rect.height);
        }
        finally{GUI.EndGroup();}
    }
    private void DrawCoffee(float width,float height)
    {
        GUI.Label(new Rect(16,5,width-32,32),"TRAFIK PRESSZÓ",heading);
        if(!CoffeeOwned){GUI.Label(new Rect(16,55,width-32,100),"Kávégép vásárolható az Üzletfejlesztésben: "+ShopCatalog.Money(CoffeeMachinePrice),label);return;}
        GUI.Label(new Rect(16,40,width-32,60),"Betöltve: "+CoffeeSupplies+" kávé • "+MilkSupplies+" tej\nÚton: "+pendingCoffee+" kávé • "+pendingMilk+" tej",label);
        if(GUI.Button(new Rect(12,105,(width-30)/2,32),"10 adag kávé • 2000 Ft",buttonStyle))OrderCoffeeSupplies();
        if(GUI.Button(new Rect(width/2+3,105,(width-30)/2,32),"10 adag tej • 1500 Ft",buttonStyle))OrderMilkSupplies();
        DrawRecipeControls(width,height);
        GUI.Label(new Rect(16,height-45,width-32,42),shop.Message,label);
    }
    private void DrawCameras(float width,float height)
    {
        if(GUI.Button(new Rect(10,8,width*.42f,35),"1 • Bejárat",buttonStyle)){selectedCamera=0;RenderCamera(0);}
        GUI.enabled=shop.Layout.Expanded;
        if(GUI.Button(new Rect(width*.45f,8,width*.42f,35),"2 • Önkiszolgáló",buttonStyle)){selectedCamera=1;RenderCamera(1);}
        GUI.enabled=true;
        if(selectedCamera==1&&!shop.Layout.Expanded)selectedCamera=0;
        GUI.DrawTexture(new Rect(10,50,width-20,height-85),cameraImages[selectedCamera],ScaleMode.ScaleToFit);
        GUI.Label(new Rect(12,height-30,width-24,28),"ÉLŐ KÉP • A bolt közben tovább működik.",label);
    }
    private void DrawGames(float width,float height)
    {
        if(gameChoice==0)
        {
            GUI.Label(new Rect(16,10,width-32,40),"MŰSZAKSZÜNET • A bolt közben nyitva marad",heading);
            if(GUI.Button(new Rect(16,70,width*.46f,60),"SNAKE\nRekord: "+PlayerPrefs.GetInt("TobaccOS.SnakeRecord",0),buttonStyle))StartSnake();
            int record=PlayerPrefs.GetInt("TobaccOS.MemoryRecord",0);
            if(GUI.Button(new Rect(width*.51f,70,width*.46f-16,60),"MEMÓRIA\nLegjobb: "+(record==0?"–":record+" lépés"),buttonStyle))StartMemory();
            GUI.Label(new Rect(16,160,width-32,60),"Figyelj a vásárlókra és a bejáratra. Az ASZTAL gombbal bármikor kiléphetsz.",label);return;
        }
        if(GUI.Button(new Rect(width-155,5,145,30),"Menü",buttonStyle))gameChoice=0;
        if(gameChoice==1&&snake!=null)
        {
            GUI.Label(new Rect(12,5,width-180,32),"SNAKE • "+snake.Score+" pont • Nyilak / WASD",label);
            float cell=Mathf.Min((width-30)/ShopSnake.Width,(height-90)/ShopSnake.Height);float left=(width-cell*ShopSnake.Width)*.5f;
            var old=GUI.color;GUI.color=new Color(.06f,.1f,.12f);GUI.DrawTexture(new Rect(left,43,cell*ShopSnake.Width,cell*ShopSnake.Height),Texture2D.whiteTexture);
            GUI.color=new Color(.93f,.52f,.25f);GUI.DrawTexture(new Rect(left+snake.Food.x*cell,43+snake.Food.y*cell,cell-2,cell-2),Texture2D.whiteTexture);
            GUI.color=new Color(.24f,.82f,.65f);foreach(var part in snake.Body)GUI.DrawTexture(new Rect(left+part.x*cell,43+part.y*cell,cell-2,cell-2),Texture2D.whiteTexture);GUI.color=old;
            if(snake.Ended&&GUI.Button(new Rect(width*.25f,height-42,width*.5f,35),"Játék vége • Újra",buttonStyle))StartSnake();
        }
        else if(gameChoice==2&&memory!=null)
        {
            GUI.Label(new Rect(12,5,width-180,32),"MEMÓRIA • "+memory.Pairs+" / 8 pár • "+memory.Moves+" lépés",label);
            float cardWidth=(width-40)/4,cardHeight=(height-88)/4;
            for(int i=0;i<16;i++)
            {
                GUI.enabled=!memory.Matched[i]&&memory.Second<0;
                if(GUI.Button(new Rect(12+(i%4)*(cardWidth+4),43+(i/4)*(cardHeight+4),cardWidth,cardHeight),memory.Visible(i)?(memory.Cards[i]+1).ToString():"?",buttonStyle))
                {
                    ChooseMemoryCard(i);
                }
            }
            GUI.enabled=true;
            if(memory.Won&&GUI.Button(new Rect(width*.25f,height-36,width*.5f,30),"Minden pár megvan • Újra",buttonStyle))StartMemory();
        }
    }
    private void OnDestroy()
    {
        ClearHand();foreach(var rt in cameraImages){rt.Release();Destroy(rt);}foreach(var material in materials)if(material!=null)Destroy(material);
        if(BasketSlot!=null)Destroy(BasketSlot.gameObject);if(worldRoot!=null)Destroy(worldRoot.gameObject);
    }
}
