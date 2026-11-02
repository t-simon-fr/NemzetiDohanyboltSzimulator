using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShopWorkflowSetup
{
    [MenuItem("Nemzeti Dohánybolt/Upgrade Carton And Computer Workflow")]
    public static void UpgradeCurrentScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var scene=SceneManager.GetActiveScene();
        var shop=Object.FindAnyObjectByType<MvpShopController>();
        if(shop==null)throw new System.InvalidOperationException("Open ShopPrototype first.");
        WarehouseSetup.UpgradeShelves();
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true));
        var monitor=all.First(t=>t.name=="Order Monitor Assembly");
        var station=monitor.GetComponent<ShopStation>();if(station==null)station=monitor.gameObject.AddComponent<ShopStation>();
        station.Configure(shop,ShopStationKind.Monitor);
        var rack=all.FirstOrDefault(t=>t.name=="Extra Cigarette Shelf");
        if(rack==null)
        {
            rack=new GameObject("Extra Cigarette Shelf").transform;
            rack.position=new Vector3(2.12f,0,.25f);rack.rotation=Quaternion.Euler(0,90,0);
            Material wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/MAT_CompactWood.mat");
            Material trim=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Materials/MAT_CompactTrim.mat");
            Box(rack,"Rack back",new Vector3(0,.9f,.19f),new Vector3(1.2f,1.7f,.05f),wood);
            for(int row=0;row<3;row++)
            {
                float y=.3f+row*.46f;
                Box(rack,"Shelf "+row,new Vector3(0,y,0),new Vector3(1.2f,.06f,.4f),trim);
                for(int col=0;col<3;col++)
                {
                    var cell=new GameObject("Extra cigarette slot "+row+"-"+col).transform;cell.SetParent(rack,false);
                    cell.localPosition=new Vector3((col-1)*.37f,y+.03f,0);
                    var slot=cell.gameObject.AddComponent<ProductSlot>();slot.Configure(new Vector3(.32f,.38f,.3f));slot.RestrictTo(ShopCatalog.Ids[col]);
                }
            }
            ProductVisuals.Text(rack,"Extra shelf sign","CIGARETTÁK",new Vector3(0,1.7f,-.005f),Quaternion.identity,.07f,Color.black);
        }
        shop.GetComponent<ShopLayout>().ConfigureExtraShelf(rack.gameObject);
        EditorUtility.SetDirty(shop.GetComponent<ShopLayout>());
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("Warehouse shelving, whole-monitor interaction and purchasable nine-slot cigarette rack configured; counter preserved.");
    }
    private static void Box(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
    }
}
