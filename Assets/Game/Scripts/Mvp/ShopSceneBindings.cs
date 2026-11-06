using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Resolve authored names once at the scene boundary. Runtime systems reuse these references.
public sealed class ShopSceneBindings
{
    private readonly Dictionary<string, Transform> authored;
    public ShopSceneBindings(Scene scene)
    {
        authored = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .GroupBy(item => item.name).ToDictionary(group => group.Key, group => group.First());
    }
    public Transform Find(string name) => authored.TryGetValue(name, out var item) && item != null ? item : null;
}
