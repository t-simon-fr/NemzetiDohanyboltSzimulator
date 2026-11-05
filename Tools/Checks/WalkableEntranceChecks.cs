using System;
using UnityEngine;
using Object=UnityEngine.Object;

public static class WalkableEntranceChecks
{
    public static object Run()
    {
        int passed=0;
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed++;};
        var g=MvpShopController.Instance;
        var player=Object.FindAnyObjectByType<PlayerInteractor>();
        var body=player.GetComponent<CharacterController>();
        var home=player.transform.position;
        Action inside=()=>{body.enabled=false;player.transform.position=g.Layout.EntryInside+Vector3.up*.1f;body.enabled=true;Physics.SyncTransforms();};
        Action<Vector3> walk=direction=>{for(int i=0;i<20;i++)body.Move(direction*.1f);};
        try
        {
            player.CancelHeld();g.ResetDemo();g.ClosePanel();g.ManualTick=true;
            ShopCheckFixtures.Fund(g,30000);check(g.BuyPanicButton(),"Buy existing security upgrade");
            g.AutomaticArrivals=false;g.OpenShop();inside();g.Entrance.Tick(1);Physics.SyncTransforms();
            check(g.Entrance.IsOpen && g.Entrance.PlayerBoundary.isTrigger,"Player proximity opens a nonblocking doorway");
            walk(Vector3.back);
            check(player.transform.position.z<g.Entrance.Position.z-.3f && g.Phase==ShopPhase.Open && !g.Ledger.Closed,"Actual character movement exits without ending the open day");
            walk(Vector3.forward);
            check(player.transform.position.z>g.Entrance.Position.z+.3f,"Character can walk back inside");
            inside();check(g.Security.Toggle(),"Security locks during the shift");g.Entrance.Tick(1);Physics.SyncTransforms();walk(Vector3.back);
            check(player.transform.position.z>g.Entrance.Position.z,"Purchased shutter still blocks actual movement");
            check(g.Security.Toggle(),"Security unlocks again");g.Entrance.Tick(1);Physics.SyncTransforms();walk(Vector3.back);
            check(player.transform.position.z<g.Entrance.Position.z-.3f,"Unlocked entrance is traversable again");
            g.RequestClose();g.Tick(1);
            check(g.Phase==ShopPhase.AfterHours && !g.Ledger.Closed,"Being outside after closing does not automatically finish the day");
            int cash=g.Ledger.Cash;
            check(g.TryEndDay() && g.Phase==ShopPhase.Summary && g.Ledger.Cash==cash-300,"Explicit exit interaction closes the day and ledger once");
            check(!g.TryEndDay() && g.Ledger.Cash==cash-300,"Repeat exit cannot charge twice");
            return new{passed,failed=0};
        }
        finally{player.CancelHeld();g.ResetDemo();g.ManualTick=false;body.enabled=false;player.transform.position=home;body.enabled=true;}
    }
}
