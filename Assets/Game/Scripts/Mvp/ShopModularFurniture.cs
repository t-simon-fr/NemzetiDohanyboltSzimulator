using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ShopModuleKind { Shelf, Narrow, Island, Warehouse, Corner = 5 }

public sealed class ShopModularFurniture : MonoBehaviour
{
    public static readonly string[] Names = { "Bolti polc · 1,2 m", "Keskeny polc · 0,6 m", "Kétoldalas polc", "Raktári állvány", "Hűtő", "Sarokpolc" };
    public static readonly int[] Prices = { 6000, 3500, 10000, 5000, 18000, 6500 };
    private MvpShopController shop;
    private readonly List<ShopFurniture> purchased = new List<ShopFurniture>();
    private readonly ShopRuntimeResources resources = new ShopRuntimeResources();
    private Material wood, metal;
    private Mesh cornerBoardMesh;
    private int serial;

    public void Configure(MvpShopController owner)
    {
        shop=owner;
        wood=resources.Own(new Material(Shader.Find("Universal Render Pipeline/Lit")) { color=new Color(.28f,.14f,.08f) });
        metal=resources.Own(new Material(Shader.Find("Universal Render Pipeline/Lit")) { color=new Color(.15f,.17f,.15f) });
        // Keep every authored slot object: controller seed arrays and live stock retain identity.
        var legacy=shop.FloorPlan.Find("Public shelf");
        var slots=legacy.GetComponentsInChildren<ProductSlot>(true).OrderBy(s=>s.transform.localPosition.z).ToArray();
        var groups=slots.ToDictionary(s=>s,s=>Mathf.Clamp(Mathf.FloorToInt((s.transform.localPosition.z+1.5f)/1f),0,2));
        foreach(var r in legacy.GetComponentsInChildren<Renderer>(true))r.enabled=false;
        foreach(var c in legacy.GetComponentsInChildren<Collider>(true))if(c.GetComponent<ProductSlot>()==null)c.enabled=false;
        for(int part=0;part<3;part++)
        {
            var root=CreateRoot("Starter modular shelf "+part,new Vector3(3.95f,0,-2.9f+part*1.25f),legacy);
            root.rotation=Quaternion.Euler(0,90,0);
            BuildFrame(root,1.2f,.5f,false,5);
            var group=slots.Where(s=>groups[s]==part).ToArray();
            foreach(var slot in group)
            {
                float height=slot.transform.localPosition.y;
                float oldZ=slot.transform.localPosition.z;
                slot.transform.SetParent(root,false);
                slot.transform.localPosition=new Vector3((Mathf.RoundToInt((oldZ+1.25f)/.5f)%2)*.5f-.25f,height,0);
                slot.transform.localRotation=Quaternion.identity;
            }
            AddLabels(root);
            var starter = shop.Arrangement.Register(root,part==0?"public-shelf":"starter-shelf-"+part,"Bolti polc "+(part+1),new Vector2(1.2f,.5f));
            starter.CatalogKind = 0;
            starter.InitiallyOwned = true;
        }
    }

    private Transform CreateRoot(string name, Vector3 position, Transform parent=null)
    {
        var root=new GameObject(name).transform;root.SetParent(parent??transform,false);root.position=position;return root;
    }
    private void Box(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.name=name;
        item.transform.SetParent(parent,false);item.transform.localPosition=position;item.transform.localScale=size;
        item.GetComponent<Renderer>().sharedMaterial=material;
    }
    private void BuildFrame(Transform root,float width,float depth,bool island,int rows=4)
    {
        float height=rows==5?2.5f:2;
        foreach(int side in new[]{-1,1})
            Box(root,"Rack upright",new Vector3(side*(width*.5f-.025f),height*.5f,0),new Vector3(.05f,height,depth),metal);
        if(!island)Box(root,"Rack back",new Vector3(0,height*.5f,depth*.5f-.02f),new Vector3(width,height,.04f),wood);
        for(int row=0;row<rows;row++)
            Box(root,"Shelf "+row,new Vector3(0,rows==5?.3f+row*.46f:.25f+row*.45f,0),new Vector3(width,.045f,depth),wood);
    }
    private void AddLabels(Transform root)
    {
        foreach(var row in root.GetComponentsInChildren<ProductSlot>(true).GroupBy(s=>new {
            Height=Mathf.RoundToInt(s.transform.localPosition.y*100),
            Side=Mathf.RoundToInt(s.transform.localEulerAngles.y/90) }))
        {
            var first=row.First();
            var text=ProductVisuals.Text(first.transform,"Module price", "",new Vector3(0,-.08f,-.26f),Quaternion.identity,.025f,Color.white);
            text.gameObject.AddComponent<ShopShelfPriceLabel>().Configure(text,row.ToArray(),true);
        }
    }
    private ShopFurniture Create(ShopModuleKind kind)
    {
        if (kind == ShopModuleKind.Corner) return CreateCorner();
        bool warehouse=kind==ShopModuleKind.Warehouse, island=kind==ShopModuleKind.Island;
        float width=kind==ShopModuleKind.Narrow?.6f:1.2f, depth=warehouse?.6f:island?.9f:.5f;
        var root=CreateRoot(Names[(int)kind],Vector3.zero);
        int rows = kind == ShopModuleKind.Shelf || kind == ShopModuleKind.Narrow ? 5 : 4;
        BuildFrame(root,width,depth,island,rows);
        if(warehouse)
        {
            var docks=new List<Transform>();
            for(int row=0;row<4;row++)
            {
                var dock=new GameObject("Module carton dock").transform;dock.SetParent(root,false);dock.localPosition=new Vector3(0,.275f+row*.45f,0);
                dock.gameObject.AddComponent<ShopStation>().Configure(shop,ShopStationKind.Supplies);
                ProductVisuals.Text(dock,"Brand dock label","",new Vector3(0,-.055f,-.31f),Quaternion.identity,.025f,Color.white);
                var trigger=dock.gameObject.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=Vector3.up*.19f;trigger.size=new Vector3(1.05f,.32f,.58f);
                docks.Add(dock);
            }
            // Registration occurs only after the paid placement has been validated.
        }
        else
        {
            for(int row=0;row<rows;row++)for(int side=0;side<(island?2:1);side++)for(int col=0;col<(width>.7f?3:1);col++)
            {
                var slot=new GameObject("Module stock slot").AddComponent<ProductSlot>();slot.transform.SetParent(root,false);
                slot.transform.localPosition=new Vector3(width>.7f?(col-1)*.35f:0,rows==5?.325f+row*.46f:.275f+row*.45f,island?(side==0?-.22f:.22f):0);
                slot.transform.localRotation=Quaternion.Euler(0,side*180,0);slot.Configure(new Vector3(.34f,.4f,.4f));slot.RestrictTo("water");
            }
            AddLabels(root);
        }
        var item=root.gameObject.AddComponent<ShopFurniture>();item.Configure("module-"+(++serial),Names[(int)kind],new Vector2(width,depth));
        item.InWarehouse=warehouse;
        item.CatalogKind=(int)kind;
        return item;
    }
    private ShopFurniture CreateCorner()
    {
        var root = CreateRoot("Sarokpolc", Vector3.zero);
        Box(root, "Corner back X", new Vector3(.38f, 1.25f, 0), new Vector3(.04f, 2.5f, .8f), wood);
        Box(root, "Corner back Z", new Vector3(0, 1.25f, .38f), new Vector3(.8f, 2.5f, .04f), wood);
        foreach (var point in new[] {
            new Vector3(-.375f, 1.25f, .375f),
            new Vector3(.375f, 1.25f, .375f),
            new Vector3(.375f, 1.25f, -.375f)
        })
            Box(root, "Corner upright", point, new Vector3(.05f, 2.5f, .05f), metal);

        if (cornerBoardMesh == null)
        {
            var profile = new[] {
                new Vector3(-.4f, 0, .4f), new Vector3(.4f, 0, .4f),
                new Vector3(.4f, 0, -.4f), new Vector3(.1f, 0, -.4f), new Vector3(-.4f, 0, .1f)
            };
            var vertices = profile.Select(v => v + Vector3.down * .0225f)
                .Concat(profile.Select(v => v + Vector3.up * .0225f)).ToArray();
            var triangles = new List<int>();
            for (int i = 1; i < 4; i++)
            {
                triangles.AddRange(new[] { 5, 5 + i, 6 + i });
                triangles.AddRange(new[] { 0, i + 1, i });
            }
            for (int i = 0; i < 5; i++)
            {
                int n = (i + 1) % 5;
                triangles.AddRange(new[] { i, n, 5 + n, i, 5 + n, 5 + i });
            }
            var mesh = resources.Own(new Mesh { name = "Corner shelf pentagon" });
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            cornerBoardMesh = mesh;
        }

        var boardMesh = cornerBoardMesh;
        for (int row = 0; row < 5; row++)
        {
            var board = new GameObject("Corner shelf board");
            board.transform.SetParent(root, false);
            board.transform.localPosition = Vector3.up * (.3f + row * .46f);
            board.AddComponent<MeshFilter>().sharedMesh = boardMesh;
            board.AddComponent<MeshRenderer>().sharedMaterial = wood;
            board.AddComponent<MeshCollider>().sharedMesh = boardMesh;
            for (int col = 0; col < 2; col++)
            {
                var slot = new GameObject("Corner product slot").AddComponent<ProductSlot>();
                slot.transform.SetParent(root, false);
                slot.transform.localPosition = new Vector3(col == 0 ? -.05f : .19f, .325f + row * .46f, col == 0 ? .19f : -.05f);
                slot.transform.localRotation = Quaternion.Euler(0, 45, 0);
                slot.Configure(new Vector3(.31f, .4f, .31f));
                slot.RestrictTo("water");
            }
        }
        AddLabels(root);
        var item = root.gameObject.AddComponent<ShopFurniture>();
        item.CatalogKind = 5;
        item.Configure("module-" + (++serial), "Sarokpolc", new Vector2(.8f, .8f));
        return item;
    }

    public ShopFurniture CreatePreview(ShopModuleKind kind)
    {
        var item = Create(kind);
        item.gameObject.SetActive(false);
        return item;
    }

    public void CommitPlannedPurchase(ShopFurniture item)
    {
        shop.Arrangement.Adopt(item);
        purchased.Add(item);
        if (item.InWarehouse)
            shop.Warehouse.RegisterStorage(item.GetComponentsInChildren<ShopStation>().Select(s => s.transform));
        else
            shop.Layout.RegisterPublicSlots(item.Slots);
    }
    public bool Buy(ShopModuleKind kind)
    {
        if (!System.Enum.IsDefined(typeof(ShopModuleKind), kind)) return false;
        if(!shop.CanDevelopShop||shop.Ledger.AvailableFunds<Prices[(int)kind])return false;
        var item=Create(kind);
        Rect area=item.InWarehouse?shop.Layout.WarehouseArea:shop.Layout.ShopArea;
        var poses=shop.Arrangement.Current(); bool fits=false;
        for(float z=area.yMin+.65f;z<area.yMax-.4f&&!fits;z+=.25f)
            for(float x=area.xMin+.65f;x<area.xMax-.4f&&!fits;x+=.25f)
                for(int turn=0;turn<4&&!fits;turn++)
                {
                    var pose=new ShopFurniturePose(new Vector3(x,0,z),turn);poses[item]=pose;
                    if(!shop.Arrangement.Validate(poses,out var reason))continue;
                    item.Apply(pose);fits=true;
                }
        if(!fits||!shop.Ledger.TryImprove(Prices[(int)kind]))
        {
            item.gameObject.SetActive(false);Destroy(item.gameObject);shop.Notify("Nincs elég szabad hely. Rendezd át vagy bővítsd a helyiséget.");return false;
        }
        shop.Arrangement.Adopt(item);purchased.Add(item);
        if(item.InWarehouse)shop.Warehouse.RegisterStorage(item.GetComponentsInChildren<ShopStation>().Select(s=>s.transform));
        else shop.Layout.RegisterPublicSlots(item.Slots);
        shop.SyncCashDrawer();shop.Navigation.Rebuild();shop.Notify("Berendezés megvásárolva. Az átrendezésben szabadon elhelyezheted.");return true;
    }
    public void ResetPurchases()
    {
        foreach(var item in purchased)if(item!=null){shop.Arrangement.Forget(item);item.gameObject.SetActive(false);Destroy(item.gameObject);}
        purchased.Clear();
    }
    private void OnDestroy()=>resources.Dispose();
}
