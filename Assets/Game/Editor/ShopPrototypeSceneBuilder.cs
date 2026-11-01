using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShopPrototypeSceneBuilder
{
    private const string ScenePath = "Assets/Game/Scenes/ShopPrototype.unity";
    private const string MaterialsPath = "Assets/Game/Materials";

    [MenuItem("Nemzeti Dohánybolt/Build Graybox Shop Scene")]
    public static void BuildScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog(
                "Replace prototype scene?",
                "ShopPrototype.unity already exists. Replace it with a fresh graybox scene?",
                "Replace",
                "Cancel"))
        {
            return;
        }

        EnsureFolder("Assets/Game", "Materials");
        EnsureFolder("Assets/Game", "Scenes");

        Material floor = GetOrCreateMaterial("MAT_GrayboxFloor", new Color(0.30f, 0.32f, 0.33f));
        Material wall = GetOrCreateMaterial("MAT_GrayboxWall", new Color(0.67f, 0.65f, 0.59f));
        Material wood = GetOrCreateMaterial("MAT_GrayboxWood", new Color(0.35f, 0.22f, 0.13f));
        Material counter = GetOrCreateMaterial("MAT_GrayboxCounter", new Color(0.43f, 0.29f, 0.18f));
        Material product = GetOrCreateMaterial("MAT_GrayboxProduct", new Color(0.68f, 0.31f, 0.17f));
        Material trim = GetOrCreateMaterial("MAT_GrayboxTrim", new Color(0.82f, 0.77f, 0.65f));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject shop = new GameObject("Shop_Graybox");

        CreateBox(shop.transform, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(10f, 0.2f, 8f), floor);
        CreateBox(shop.transform, "Left wall", new Vector3(-5f, 1.5f, 0f), new Vector3(0.2f, 3f, 8f), wall);
        CreateBox(shop.transform, "Right wall", new Vector3(5f, 1.5f, 0f), new Vector3(0.2f, 3f, 8f), wall);
        CreateBox(shop.transform, "Back wall", new Vector3(0f, 1.5f, 4f), new Vector3(10f, 3f, 0.2f), wall);
        CreateBox(shop.transform, "Front wall left", new Vector3(-3.35f, 1.5f, -4f), new Vector3(3.3f, 3f, 0.2f), wall);
        CreateBox(shop.transform, "Front wall right", new Vector3(3.35f, 1.5f, -4f), new Vector3(3.3f, 3f, 0.2f), wall);
        CreateBox(shop.transform, "Door lintel", new Vector3(0f, 2.8f, -4f), new Vector3(3.4f, 0.4f, 0.2f), trim);

        CreateBox(shop.transform, "Service counter", new Vector3(0f, 0.55f, -2.05f), new Vector3(4.2f, 1.1f, 0.75f), counter);
        CreateBox(shop.transform, "Counter top", new Vector3(0f, 1.13f, -2.05f), new Vector3(4.4f, 0.08f, 0.85f), trim);
        CreateBox(shop.transform, "Register blockout", new Vector3(-1.35f, 1.3f, -1.92f), new Vector3(0.55f, 0.28f, 0.42f), product);

        CreateShelf(shop.transform, "Left aisle shelf", new Vector3(-4.45f, 0f, 0.6f), new Vector3(0.7f, 1.9f, 2.6f), wood, trim, product);
        CreateShelf(shop.transform, "Right aisle shelf", new Vector3(4.45f, 0f, 0.6f), new Vector3(0.7f, 1.9f, 2.6f), wood, trim, product);
        CreateShelf(shop.transform, "Back wall shelf", new Vector3(0f, 0f, 3.55f), new Vector3(5.8f, 1.9f, 0.7f), wood, trim, product);

        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0.05f, -0.75f);
        player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        CharacterController character = player.AddComponent<CharacterController>();
        character.height = 1.8f;
        character.radius = 0.3f;
        character.center = new Vector3(0f, 0.9f, 0f);

        GameObject cameraObject = new GameObject("Player Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        cameraObject.transform.localRotation = Quaternion.identity;
        cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        player.AddComponent<FirstPersonController>();

        GameObject sun = new GameObject("Shop Light");
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light directional = sun.AddComponent<Light>();
        directional.type = LightType.Directional;
        directional.intensity = 1.2f;
        RenderSettings.ambientLight = new Color(0.53f, 0.53f, 0.53f);

        ShopInteractionSetup.ConfigureScene(scene, false);
        MvpSetup.ConfigureScene(scene, false);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log("Built the Nemzeti Dohánybolt graybox prototype scene.");
    }

    private static void CreateShelf(Transform parent, string name, Vector3 position, Vector3 size, Material frame, Material shelf, Material products)
    {
        GameObject unit = new GameObject(name);
        unit.transform.SetParent(parent, false);
        unit.transform.localPosition = position;
        bool alongZ = size.z > size.x;

        float shelfDepth = alongZ ? size.x : size.z;
        float shelfLength = alongZ ? size.z : size.x;
        if (alongZ)
        {
            CreateBox(unit.transform, "Left end panel", new Vector3(0f, size.y * 0.5f, -shelfLength * 0.5f + 0.03f), new Vector3(shelfDepth, size.y, 0.06f), frame);
            CreateBox(unit.transform, "Right end panel", new Vector3(0f, size.y * 0.5f, shelfLength * 0.5f - 0.03f), new Vector3(shelfDepth, size.y, 0.06f), frame);
            float backX = position.x < 0f ? -shelfDepth * 0.5f + 0.03f : shelfDepth * 0.5f - 0.03f;
            CreateBox(unit.transform, "Back panel", new Vector3(backX, size.y * 0.5f, 0f), new Vector3(0.06f, size.y, shelfLength), frame);
        }
        else
        {
            CreateBox(unit.transform, "Left end panel", new Vector3(-shelfLength * 0.5f + 0.03f, size.y * 0.5f, 0f), new Vector3(0.06f, size.y, shelfDepth), frame);
            CreateBox(unit.transform, "Right end panel", new Vector3(shelfLength * 0.5f - 0.03f, size.y * 0.5f, 0f), new Vector3(0.06f, size.y, shelfDepth), frame);
            CreateBox(unit.transform, "Back panel", new Vector3(0f, size.y * 0.5f, shelfDepth * 0.5f - 0.03f), new Vector3(shelfLength, size.y, 0.06f), frame);
        }
        for (int level = 0; level < 4; level++)
        {
            float y = 0.25f + level * 0.42f;
            Vector3 boardSize = alongZ
                ? new Vector3(size.x + 0.04f, 0.06f, shelfLength)
                : new Vector3(shelfLength, 0.06f, size.z + 0.04f);
            CreateBox(unit.transform, "Shelf " + (level + 1), new Vector3(0f, y, 0f), boardSize, shelf);

            for (int item = 0; item < 5; item++)
            {
                float offset = (item - 2) * (shelfLength / 6f);
                Vector3 itemPosition = alongZ
                    ? new Vector3(0f, y + 0.18f, offset)
                    : new Vector3(offset, y + 0.18f, 0f);
                Vector3 itemSize = alongZ
                    ? new Vector3(shelfDepth * 0.42f, 0.26f, shelfLength * 0.09f)
                    : new Vector3(shelfLength * 0.09f, 0.26f, shelfDepth * 0.42f);
                CreateBox(unit.transform, "Product blockout", itemPosition, itemSize, products);
            }
        }
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private static Material GetOrCreateMaterial(string name, Color color)
    {
        string path = MaterialsPath + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        material = new Material(shader) { name = name, color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
