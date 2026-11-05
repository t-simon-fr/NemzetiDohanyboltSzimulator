using System;
using System.Linq;
using UnityEngine;

public static class FixtureLightingChecks
{
    public static object Run()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var g=MvpShopController.Instance;
        var lights=g.InteriorLighting.Lights;
        check(lights.Count==2,"Both authored fluorescent fixtures have their own emitters");
        foreach (var light in lights)
        {
            check(light.enabled && light.intensity>0 && light.type==LightType.Spot && light.range>=5,"Fixture emits usable interior light");
            check(light.transform.parent.name=="Fluorescent fixture" && light.transform.localPosition.y<0 && Vector3.Dot(light.transform.forward,Vector3.down)>.99f,"Emitter sits beneath and points down from its fixture");
            var tubes=light.transform.parent.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Warm white tube").ToArray();
            check(tubes.Length==2 && tubes.All(tube=>tube.sharedMaterial.shader.name=="Universal Render Pipeline/Unlit"),"Both physical tubes remain visibly illuminated without an emission variant");
        }
        foreach(string name in new[]{"Cigarette display fill","Interior light","Customer area fill"})
            check(!g.FloorPlan.Find(name).GetComponent<Light>().enabled,"Old detached light removed: "+name);
        g.ResetDemo();g.ClosePanel();g.ManualTick=true;
        try
        {
            typeof(MvpShopController).GetProperty("Phase").SetValue(g,ShopPhase.AfterHours);g.AfterHours.Enter();
            check(lights.All(light=>light.enabled && light.intensity>0),"Interior fixtures remain lit after closing");
            g.ResetDemo();
            check(lights.All(light=>light.enabled),"New game does not create or disable duplicate fixtures");
            return new{passed,failed=0};
        }
        finally{g.ResetDemo();g.ManualTick=false;}
    }
}
