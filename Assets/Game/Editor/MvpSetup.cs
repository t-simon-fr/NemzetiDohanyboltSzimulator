using UnityEditor;
using UnityEngine;

public static class MvpSetup
{
    [MenuItem("Nemzeti Dohánybolt/Enable Complete MVP")]
    public static void EnableInCurrentScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(Object.FindAnyObjectByType<ShopLayout>()!=null){Debug.Log("The compact shop is already configured. Use Build Compact Shop to rebuild it.");return;}
        CompactShopSetup.BuildFromMenu();
    }
}
