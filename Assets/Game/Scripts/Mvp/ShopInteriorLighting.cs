using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ShopInteriorLighting : MonoBehaviour
{
    private readonly List<Light> lights = new List<Light>();
    private Material tubeMaterial;
    public IReadOnlyList<Light> Lights => lights;

    public void Configure(MvpShopController shop)
    {
        // These lights belonged to earlier prop positions. Do not leave an
        // invisible emitter at the cigarette rack's former position.
        foreach (string name in new[] { "Cigarette display fill", "Interior light", "Customer area fill" })
        {
            var source = shop.FloorPlan.Find(name);
            if (source != null && source.TryGetComponent<Light>(out var oldLight)) oldLight.enabled = false;
        }
        tubeMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) {
            name = "Illuminated fluorescent tubes", color = new Color(.98f,.97f,.86f)
        };
        var fixtures = gameObject.scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(item => item.name == "Fluorescent fixture");
        foreach (var fixture in fixtures)
        {
            var light = new GameObject("Fluorescent fixture light").AddComponent<Light>();
            light.transform.SetParent(fixture,false);
            light.transform.localPosition = new Vector3(0,-.08f,0);
            light.transform.localRotation = Quaternion.Euler(90,0,0);
            light.type = LightType.Spot;
            light.spotAngle = 115;
            light.innerSpotAngle = 80;
            light.range = 6.5f;
            light.intensity = 3.5f;
            light.color = new Color(.98f,.97f,.88f);
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            lights.Add(light);
            foreach (var tube in fixture.GetComponentsInChildren<Renderer>())
                if (tube.name == "Warm white tube") tube.sharedMaterial = tubeMaterial;
        }
    }

    private void OnDestroy()
    {
        foreach (var light in lights) if (light != null) Destroy(light.gameObject);
        if (tubeMaterial != null) Destroy(tubeMaterial);
    }
}
