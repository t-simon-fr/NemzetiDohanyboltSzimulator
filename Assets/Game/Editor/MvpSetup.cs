using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MvpSetup
{
    [MenuItem("Nemzeti Dohánybolt/Enable Complete MVP")]
    public static void EnableInCurrentScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (ShopInteractionSetup.ConfigureScene(scene, true) < 0) return;
        ConfigureScene(scene, true);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }
    public static void ConfigureScene(Scene scene, bool useUndo)
    {
        PlayerSettings.productName = "Nemzeti Dohánybolt Szimulátor";
        PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
        var roots = scene.GetRootGameObjects();
        PlayerInteractor player = roots.SelectMany(r=>r.GetComponentsInChildren<PlayerInteractor>(true)).FirstOrDefault();
        CustomerOrderController legacy = roots.SelectMany(r=>r.GetComponentsInChildren<CustomerOrderController>(true)).FirstOrDefault();
        ProductSlot delivery = roots.SelectMany(r=>r.GetComponentsInChildren<ProductSlot>(true)).FirstOrDefault(s=>s.name=="Counter Slot 2");
        Transform till = roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name=="Register blockout");
        if (player==null || legacy==null || delivery==null || till==null) throw new System.InvalidOperationException("Generate the graybox shop before enabling MVP.");
        ProductSlot[] slots = roots.SelectMany(r=>r.GetComponentsInChildren<ProductSlot>(true)).Where(s=>s.name.StartsWith("Product Slot ")).ToArray();
        int[] types = slots.Select(s=>s.transform.parent.name=="Left aisle shelf"?0:s.transform.parent.name=="Right aisle shelf"?1:2).ToArray();
        ShopProduct[] products = roots.SelectMany(r=>r.GetComponentsInChildren<ShopProduct>(true)).ToArray();
        Material[] materials = ShopCatalog.Ids.Select(id=>products.First(p=>p.ProductId==id).GetComponent<Renderer>().sharedMaterial).ToArray();
        MvpShopController game = roots.SelectMany(r=>r.GetComponentsInChildren<MvpShopController>(true)).FirstOrDefault();
        if(game==null)
        {
            GameObject go=new GameObject("MVP Shop"); if(useUndo)Undo.RegisterCreatedObjectUndo(go,"Create MVP shop"); game=go.AddComponent<MvpShopController>();
        }
        Transform crate=game.transform.Find("Supply Crate");
        if(crate==null)
        {
            GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Supply Crate";go.transform.SetParent(game.transform,false);
            go.transform.position=new Vector3(2.65f,.45f,1.6f);go.transform.localScale=new Vector3(.85f,.9f,.85f);
            go.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/MAT_GrayboxWood.mat");crate=go.transform;
        }
        ConfigureStation(crate.gameObject,game,ShopStationKind.Supplies);
        ConfigureStation(till.gameObject,game,ShopStationKind.Till);
        Transform sign=game.transform.Find("Open Close Sign");
        if(sign==null)
        {
            GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Open Close Sign";go.transform.SetParent(game.transform,false);
            go.transform.position=new Vector3(-2.2f,1.55f,-3.85f);go.transform.localScale=new Vector3(.7f,.5f,.08f);
            go.GetComponent<Renderer>().sharedMaterial=materials[0];sign=go.transform;
        }
        ConfigureStation(sign.gameObject,game,ShopStationKind.Door);
        if(useUndo) { Undo.RecordObject(legacy,"Disable first-customer demo");Undo.RecordObject(legacy.gameObject,"Hide customer template");Undo.RecordObject(game,"Configure MVP"); }
        legacy.enabled=false;legacy.gameObject.SetActive(false);
        game.Configure(player,delivery,legacy.gameObject,slots,types,materials,crate,till);
        MvpShopHud hud=game.GetComponent<MvpShopHud>();if(hud==null)hud=game.gameObject.AddComponent<MvpShopHud>();hud.Configure(game);
        EditorUtility.SetDirty(game);EditorUtility.SetDirty(hud);EditorUtility.SetDirty(legacy);EditorUtility.SetDirty(legacy.gameObject);
    }
    private static void ConfigureStation(GameObject target,MvpShopController game,ShopStationKind kind)
    {
        ShopStation station=target.GetComponent<ShopStation>();if(station==null)station=target.AddComponent<ShopStation>();station.Configure(game,kind);EditorUtility.SetDirty(station);
    }
}
