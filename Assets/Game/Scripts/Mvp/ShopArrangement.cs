using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class ShopArrangement : MonoBehaviour
{
    private MvpShopController shop;
    private PlayerInteractor player;
    private readonly List<ShopFurniture> furniture = new List<ShopFurniture>();
    private Dictionary<ShopFurniture, ShopFurniturePose> draft;
    private readonly ShopPauseScope pause = new ShopPauseScope();
    public bool Active { get; private set; }
    public IReadOnlyList<ShopFurniture> Furniture => furniture;
    public void Adopt(ShopFurniture item) { if(!furniture.Contains(item))furniture.Add(item); }
    public void Forget(ShopFurniture item) => furniture.Remove(item);
    public string Feedback { get; private set; } = "";
    public static readonly Rect[] Protected = {
        Rect.MinMaxRect(-2.4f, -4.3f, .42f, 1.8f),
        Rect.MinMaxRect(-2.4f, -.1f, 2.4f, .8f),
        Rect.MinMaxRect(.35f, -4.3f, 2.1f, -3.0f),
        Rect.MinMaxRect(.55f, 1.18f, 1.95f, 1.78f)
    };

    public void Configure(MvpShopController owner, PlayerInteractor interactor)
    {
        shop = owner;
        player = interactor;
        var fridge = shop.GetComponentsInChildren<Transform>(true).First(t => t.name == "Purchased Drinks Fridge");
        Register(fridge, "drinks-fridge", "Italhűtő", new Vector2(1.65f, .8f));
    }

    public ShopFurniture Register(Transform root, string id, string title, Vector2 size)
    {
        var item = root.gameObject.AddComponent<ShopFurniture>();
        item.Configure(id, title, size);
        furniture.Add(item);
        return item;
    }

    public Dictionary<ShopFurniture, ShopFurniturePose> Current() => furniture.Where(f => f.gameObject.activeInHierarchy).ToDictionary(f => f, f => f.Pose);
    public Dictionary<ShopFurniture, ShopFurniturePose> Draft => draft == null ? Current() : new Dictionary<ShopFurniture, ShopFurniturePose>(draft);
    public bool CanBegin => !Active && !ShopMenu.Active && shop.Modal == ShopModal.None && !shop.HasHeldProduct &&
        (shop.Phase == ShopPhase.Preparing || shop.Phase == ShopPhase.AfterHours && shop.CanEndDay) &&
        shop.Activities.BrewRemaining <= 0 && !shop.AfterHours.TradeOpen;

    public bool Begin()
    {
        if (!CanBegin)
        {
            Feedback = "Átrendezni üres kézzel, nyitás előtt vagy a vendégek távozása után lehet. Várd meg a gép befejezését.";
            shop.Notify(Feedback);
            return false;
        }
        // Keep the cursor unlocked when moving from the monitor into the editor.
        // ClosePanel would briefly request pointer lock in WebGL before releasing it.
        shop.Devices.Release(false);
        draft = Current();
        pause.Enter();
        Active = true;
        Feedback = "Válassz egy polcot vagy hűtőt. A pult és az ajtók fixek.";
        BeginView();
        return true;
    }

    public bool TryPlace(ShopFurniture item, ShopFurniturePose pose)
    {
        if (!Active || !draft.ContainsKey(item)) return false;
        pose.Position.x = Mathf.Round(pose.Position.x / .05f) * .05f;
        pose.Position.z = Mathf.Round(pose.Position.z / .05f) * .05f;
        pose.Position.y = item.DefaultPose.Position.y;
        var candidate = new Dictionary<ShopFurniture, ShopFurniturePose>(draft) { [item] = pose };
        if (!Validate(candidate, out string reason)) { Feedback = reason; return false; }
        draft = candidate;
        Feedback = "Előnézet rögzítve. A Mentés alkalmazza az egész elrendezést.";
        return true;
    }

    public bool CanActivate(ShopFurniture item)
    {
        var poses = Current();
        poses[item] = item.Pose;
        if (Validate(poses, out string reason)) return true;
        shop.Notify(reason);
        return false;
    }

    public bool Validate(Dictionary<ShopFurniture, ShopFurniturePose> poses, out string reason)
    {
        Rect area = shop.Layout.ShopArea;
        var staticObstacles = shop.Navigation.StaticObstacles(true);
        var placed = new List<Rect>();
        foreach (var pair in poses)
        {
            area=pair.Key.InWarehouse?shop.Layout.WarehouseArea:shop.Layout.ShopArea;
            Rect bounds = pair.Key.BoundsAt(pair.Value);
            if (bounds.xMin < area.xMin || bounds.xMax > area.xMax || bounds.yMin < area.yMin || bounds.yMax > area.yMax)
            { reason = "A berendezés kilóg a megvásárolt üzlettérből."; return false; }
            if (!pair.Key.InWarehouse && Protected.Any(r => r.Overlaps(bounds)))
            { reason = "Fenntartott közlekedősávot vagy fix berendezés helyét blokkolja."; return false; }
            if (staticObstacles.Any(r => ShopNavigation.Expanded(r, .03f).Overlaps(bounds)) ||
                placed.Any(r => r.Overlaps(bounds)))
            { reason = "Nem fér el: falnak, tárgynak vagy másik berendezésnek ütközik."; return false; }
            if (ShopNavigation.Expanded(bounds, .3f).Contains(new Vector2(player.transform.position.x, player.transform.position.z)))
            { reason = "A játékos helyére nem tehető berendezés."; return false; }
            placed.Add(bounds);
        }
        var grid = shop.Navigation.Build(poses);
        var required = new List<Vector3> { shop.Layout.EntryInside, shop.Layout.AisleGate, shop.Layout.EntryOutside, shop.Layout.ExitDoorway };
        for (int i = 0; i < 3; i++) required.Add(shop.Counter.QueuePoint(i));
        foreach (var pair in poses.Where(p=>!p.Key.InWarehouse)) required.AddRange(pair.Key.AccessAt(pair.Value));
        foreach (var point in required)
        {
            if (grid.TryPath(shop.Layout.EntryInside, point, out var unused)) continue;
            reason = "Nem járható az út a bejárat, a sor vagy valamelyik polc között.";
            return false;
        }
        var staffArea=Rect.MinMaxRect(shop.Layout.WarehouseArea.xMin,-4.3f,shop.Layout.ShopArea.xMax,1.9f);
        var staffObstacles=shop.Navigation.StaticObstacles();
        staffObstacles.AddRange(poses.Select(p=>p.Key.BoundsAt(p.Value)));
        var staffGrid=new ShopRouteGrid(staffArea,p=>
        {
            var point=new Vector2(p.x,p.z);
            return (shop.Layout.ShopArea.Contains(point)||shop.Layout.WarehouseArea.Contains(point)||new Rect(-2.7f,-.55f,.5f,1.1f).Contains(point))
                && !staffObstacles.Any(r=>ShopNavigation.Expanded(r,.29f).Contains(point));
        });
        var staffRequired=new List<Vector3>{new Vector3(-4.25f,0,1.45f),ShopFloorPlan.StaffExit};
        foreach(var pair in poses.Where(p=>p.Key.InWarehouse))staffRequired.AddRange(pair.Key.AccessAt(pair.Value));
        foreach(var point in staffRequired)
            if(!staffGrid.TryPath(ShopFloorPlan.StaffSpawn,point,out var route))
            {reason="Nem járható a pult–raktár–hátsó kijárat útvonala.";return false;}
        reason = "Elhelyezhető: az útvonalak szabadok.";
        return true;
    }

    public bool Save()
    {
        if (!Active) return false;
        if (!Validate(draft, out string reason))
        {
            Feedback = reason;
            return false;
        }
        var original = Current();
        try
        {
            foreach (var pair in draft) pair.Key.Apply(pair.Value);
            Physics.SyncTransforms();
            shop.Navigation.Rebuild();
        }
        catch
        {
            foreach (var pair in original) pair.Key.Apply(pair.Value);
            shop.Navigation.Rebuild();
            throw;
        }
        End();
        shop.Notify("Az új üzletelrendezés elmentve. A következő napra is megmarad.");
        return true;
    }

    public void Cancel() { if (Active) End(); }
    private void End()
    {
        EndView();
        Active = false;
        draft = null;
        pause.Exit();
        shop.ClosePanel();
    }
    public void ResetLayout()
    {
        Cancel();
        shop.GetComponent<ShopModularFurniture>()?.ResetPurchases();
        foreach (var item in furniture) item.Apply(item.DefaultPose);
        shop.Navigation.Rebuild();
    }
    partial void BeginView();
    partial void EndView();
    private void OnDestroy()
    {
        if (!Active) return;
        EndView();
        pause.Exit();
        Active = false;
        ShopInputGate.SetBlocked(false);
    }
}
