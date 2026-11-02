using UnityEditor;

// Retain the original menu name for existing projects and bookmarks.
public static class ShopPrototypeSceneBuilder
{
    [MenuItem("Nemzeti Dohánybolt/Build Graybox Shop Scene")]
    public static void BuildScene()=>CompactShopSetup.BuildFromMenu();
}
