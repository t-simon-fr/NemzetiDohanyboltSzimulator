using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShopCustomerSetup
{
    [MenuItem("Nemzeti Dohánybolt/Enable First Customer")]
    public static void EnableInCurrentScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (ShopInteractionSetup.ConfigureScene(scene, true) < 0) return;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void ConfigureScene(Scene scene, bool useUndo)
    {
        if (Object.FindAnyObjectByType<ShopLayout>() != null) return;
        ProductSlot delivery = null;
        GameObject visit = null;
        Material water = MaterialAsset("MAT_Product_Water", new Color(0.1f, 0.5f, 0.95f));
        Material gum = MaterialAsset("MAT_Product_Gum", new Color(0.15f, 0.65f, 0.28f));
        Material beer = MaterialAsset("MAT_Product_Beer", new Color(0.8f, 0.53f, 0.12f));
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Customer Visit") visit = root;
            foreach (ProductSlot slot in root.GetComponentsInChildren<ProductSlot>(true))
                if (slot.name == "Counter Slot 2") delivery = slot;
            foreach (ShopProduct product in root.GetComponentsInChildren<ShopProduct>(true))
            {
                if (product.ProductId != "sample") continue;
                if (useUndo) Undo.RecordObject(product, "Assign product identity");
                string shelfName = product.transform.parent.name;
                Material material;
                if (shelfName == "Left aisle shelf") { product.ConfigureProduct("water", "Buborék ásványvíz"); material = water; }
                else if (shelfName == "Right aisle shelf") { product.ConfigureProduct("gum", "Menta rágó"); material = gum; }
                else { product.ConfigureProduct("beer", "Pirkadat sör"); material = beer; }
                Renderer renderer = product.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (useUndo) Undo.RecordObject(renderer, "Color product");
                    renderer.sharedMaterial = material;
                    EditorUtility.SetDirty(renderer);
                }
                EditorUtility.SetDirty(product);
            }
        }
        if (delivery == null || visit != null) return;
        visit = new GameObject("Customer Visit");
        if (useUndo) Undo.RegisterCreatedObjectUndo(visit, "Create first customer");
        Transform entry = Child(visit.transform, "Entrance", new Vector3(0.13f, 0f, -4.8f));
        Transform stand = Child(visit.transform, "Service Point", new Vector3(0.13f, 0f, -2.95f));
        GameObject customer = new GameObject("First Customer - Józsi");
        customer.SetActive(false);
        customer.transform.SetParent(visit.transform, false);
        customer.transform.position = entry.position;
        CharacterController character = customer.AddComponent<CharacterController>();
        character.height = 1.9f; character.center = Vector3.up * 0.95f; character.radius = 0.25f;
        Transform model = Child(customer.transform, "Visual", Vector3.zero);
        Material coat = MaterialAsset("MAT_Customer_Coat", new Color(0.22f, 0.35f, 0.29f));
        Material skin = MaterialAsset("MAT_Customer_Skin", new Color(0.76f, 0.58f, 0.43f));
        Material dark = MaterialAsset("MAT_Customer_Dark", new Color(0.08f, 0.09f, 0.1f));
        Mesh(model, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.85f, 0f), new Vector3(0.55f, 0.85f, 0.55f), coat);
        Mesh(model, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.7f, 0f), Vector3.one * 0.4f, skin);
        Mesh(model, "Left eye", PrimitiveType.Sphere, new Vector3(-0.065f, 1.74f, 0.185f), Vector3.one * 0.035f, dark);
        Mesh(model, "Right eye", PrimitiveType.Sphere, new Vector3(0.065f, 1.74f, 0.185f), Vector3.one * 0.035f, dark);
        customer.AddComponent<CustomerOrderController>().Configure(character, model.gameObject, entry, stand, delivery);
        customer.SetActive(true);
        Mesh(visit.transform, "Blue delivery tray", PrimitiveType.Cube, delivery.transform.position + Vector3.up * 0.003f, new Vector3(0.64f, 0.006f, 0.64f), water);
        EditorUtility.SetDirty(visit);
    }

    private static Transform Child(Transform parent, string name, Vector3 position)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        return child.transform;
    }
    private static void Mesh(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        GameObject mesh = GameObject.CreatePrimitive(type);
        mesh.name = name;
        mesh.transform.SetParent(parent, false);
        mesh.transform.localPosition = position;
        mesh.transform.localScale = scale;
        Object.DestroyImmediate(mesh.GetComponent<Collider>());
        mesh.GetComponent<Renderer>().sharedMaterial = material;
    }
    private static Material MaterialAsset(string name, Color color)
    {
        string path = "Assets/Game/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
