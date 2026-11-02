using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShopInteractionSetup
{
    [MenuItem("Nemzeti Dohánybolt/Enable Product Interactions")]
    public static void EnableInCurrentScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Termékinterakció", "Előbb állítsd le a Play módot.", "Rendben");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        int count = ConfigureScene(scene, true);
        if (count < 0)
        {
            EditorUtility.DisplayDialog("Termékinterakció", "Nyisd meg a ShopPrototype jelenetet, amelyben van Player objektum.", "Rendben");
            return;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Product interactions and fixed slots enabled. Pickable products: " + count);
    }

    public static int ConfigureScene(Scene scene, bool useUndo)
    {
        if (Object.FindAnyObjectByType<ShopLayout>() != null) return 0;
        FirstPersonController player = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            player = root.GetComponentInChildren<FirstPersonController>(true);
            if (player != null) break;
        }
        if (player == null) return -1;
        Camera camera = player.GetComponentInChildren<Camera>(true);
        if (camera == null) return -1;
        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
        if (interactor == null) interactor = useUndo ? Undo.AddComponent<PlayerInteractor>(player.gameObject) : player.gameObject.AddComponent<PlayerInteractor>();
        if (useUndo) Undo.RecordObject(interactor, "Configure player interaction");
        interactor.Configure(camera);
        EditorUtility.SetDirty(interactor);
        int count = 0;
        Transform countertop = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (item.name == "Counter top") countertop = item;
                if (item.name != "Product blockout" || item.GetComponent<BoxCollider>() == null) continue;
                ShopProduct product = item.GetComponent<ShopProduct>();
                if (product == null)
                {
                    product = useUndo ? Undo.AddComponent<ShopProduct>(item.gameObject) : item.gameObject.AddComponent<ShopProduct>();
                    product.Configure("Próbaáru");
                }
                if (product.CurrentSlot == null)
                {
                    Transform board = item.parent.Find("Shelf 1");
                    Vector3 capacity = new Vector3(0.65f, 0.38f, 0.65f);
                    if (board != null)
                    {
                        Vector3 size = board.lossyScale;
                        capacity = size.z > size.x
                            ? new Vector3(size.x - 0.04f, 0.38f, size.z / 6f - 0.03f)
                            : new Vector3(size.x / 6f - 0.03f, 0.38f, size.z - 0.04f);
                    }
                    BoxCollider box = item.GetComponent<BoxCollider>();
                    float height = Mathf.Abs(box.size.y * item.lossyScale.y);
                    Vector3 anchor = item.position - Vector3.up * (height * 0.5f + 0.015f);
                    ProductSlot slot = CreateSlot(item.parent, "Product Slot " + count.ToString("00"), anchor, item.parent.rotation, capacity, useUndo);
                    if (useUndo) Undo.RecordObjects(new UnityEngine.Object[] { product, item }, "Assign product slot");
                    product.AssignInitialSlot(slot);
                    EditorUtility.SetDirty(slot);
                }
                EditorUtility.SetDirty(product);
                count++;
            }
        }
        if (countertop != null && countertop.parent.Find("Counter Product Slots") == null)
        {
            GameObject slots = new GameObject("Counter Product Slots");
            if (useUndo) Undo.RegisterCreatedObjectUndo(slots, "Create counter slots");
            slots.transform.SetParent(countertop.parent, false);
            for (int i = 0; i < 4; i++)
            {
                Vector3 anchor = countertop.TransformPoint(new Vector3(-0.15f + i * 0.18f, 0.5f, 0f));
                CreateSlot(slots.transform, "Counter Slot " + (i + 1), anchor, countertop.rotation, new Vector3(0.65f, 0.38f, 0.65f), useUndo);
            }
        }
        ShopCustomerSetup.ConfigureScene(scene, useUndo);
        return count;
    }

    private static ProductSlot CreateSlot(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 capacity, bool useUndo)
    {
        GameObject slotObject = new GameObject(name);
        if (useUndo) Undo.RegisterCreatedObjectUndo(slotObject, "Create product slot");
        slotObject.transform.SetParent(parent, false);
        slotObject.transform.SetPositionAndRotation(position, rotation);
        ProductSlot slot = slotObject.AddComponent<ProductSlot>();
        slot.Configure(capacity);
        return slot;
    }
}
