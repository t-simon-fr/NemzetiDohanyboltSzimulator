using System;
using UnityEngine;
using Object=UnityEngine.Object;

public static class ToolStorageChecks
{
    public static object Run()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var g=MvpShopController.Instance;
        var player=Object.FindAnyObjectByType<PlayerInteractor>();
        try
        {
            player.CancelHeld();g.ResetDemo();g.ClosePanel();g.ManualTick=true;
            var broom=g.Activities.Mop;var bat=g.Activities.Bat;
            check(broom.transform.position.z>1.5f && broom.transform.position.x< -2.8f && Quaternion.Angle(broom.transform.rotation,Quaternion.identity)>5,"Broom leans in the warehouse corner");
            foreach(var tool in new[]{broom,bat})
            {
                var home=new Pose(tool.transform.position,tool.transform.rotation);
                var rack=tool.Rack;
                check(rack!=null && rack.GetComponent<Collider>().isTrigger,"Tool has a separate non-blocking return target");
                check(player.TryHoldTool(tool),"Pick up stored "+tool.DisplayName);
                check(!player.TryReturnHeldTool(tool==broom?bat.Rack:broom.Rack) && player.HeldTool==tool,"Wrong tool rack leaves the held item unchanged");
                Physics.SyncTransforms();
                var center=rack.transform.TransformPoint(Vector3.up*.55f);
                var eye=center+(tool==broom?new Vector3(-1,.6f,-.5f):new Vector3(0,.6f,1));
                player.RefreshTarget(new Ray(eye,(center-eye).normalized));
                check(player.TargetToolRack==rack,"Actual targeting finds the empty permanent rack");
                check(player.TryReturnHeldTool(rack) && !player.HasHeldItem && !tool.IsHeld,"Store tool and clear hand exactly once");
                check(Vector3.Distance(tool.transform.position,home.position)<.001f && Quaternion.Angle(tool.transform.rotation,home.rotation)<.01f,"Return restores exact leaning/mounted pose");
                check(!player.TryReturnHeldTool(rack),"Repeated return cannot duplicate a tool");
                check(player.TryHoldTool(tool) && player.DropHeld() && player.TryHoldTool(tool),"Dropped tool can be picked up again");
                check(player.TryReturnHeldTool(rack) && Vector3.Distance(tool.transform.position,home.position)<.001f,"Permanent rack survives G-drop and a different pickup origin");
            }
            g.ResetDemo();
            check(!broom.IsHeld && !bat.IsHeld && Quaternion.Angle(broom.transform.rotation,Quaternion.identity)>5,"New game preserves both homes and broom lean");
            return new{passed,failed=0};
        }
        finally{player.CancelHeld();g.ResetDemo();g.ManualTick=false;}
    }
}
