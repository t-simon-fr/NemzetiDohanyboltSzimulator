#if UNITY_EDITOR || SHOP_ACCEPTANCE
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;

// Built only into the dedicated local acceptance player, never the shipping player.
[Preserve]
public sealed class ShopAcceptanceProbe : MonoBehaviour
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private MvpShopController Shop => MvpShopController.Instance;
    private PlayerInteractor Player => FindAnyObjectByType<PlayerInteractor>();
    private Camera View => Player.GetComponentInChildren<Camera>();

#if SHOP_ACCEPTANCE
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() => new GameObject("AcceptanceProbe").AddComponent<ShopAcceptanceProbe>();
#endif

    private static void Set(object target, string property, object value)
    {
        target.GetType().GetProperty(property).SetValue(target, value);
    }

    private void Visit(int visit, int type)
    {
        Shop.OpenShop();
        Set(Shop, "Spawned", visit);
        typeof(MvpShopController).GetMethod("SpawnCustomer", Private).Invoke(Shop, new object[] { type, false });
        for (int i = 0; i < 2000 && Shop.ActiveCustomer == null; i++) Shop.Tick(.1f);
        if (Shop.ActiveCustomer == null) throw new Exception("Fixture customer did not arrive.");
    }

    private void DeliverAll()
    {
        foreach (int type in Shop.ActiveCustomer.OrderTypes)
        {
            var item = FindObjectsByType<ShopProduct>().First(p => p.ProductId == ShopCatalog.Ids[type] && p.CurrentSlot != null && p.CurrentSlot.IsStockShelf);
            item.AssignInitialSlot(Shop.DeliverySlot);
            Shop.DeliverySlot.NotifyProductPlaced(item);
        }
    }

    [Preserve]
    public void Prepare(string scenario)
    {
        Player.CancelHeld();
        Shop.GetComponent<ShopMenu>().Resume();
        Shop.ResetDemo();
        Shop.ClosePanel();
        Shop.ManualTick = true;
        if (scenario == "card")
        {
            Visit(3, 0);
            DeliverAll();
            Shop.Devices.Focus(ShopDeviceKind.Till);
        }
        else if (scenario == "triple")
        {
            Visit(2, 0);
            Set(Shop.ActiveCustomer, "OrderTypes", new[] { 0, 0, 0 });
            Set(Shop.ActiveCustomer, "UnitPrices", new[] { 2200, 2200, 2200 });
            DeliverAll();
        }
        else if (scenario == "pickup")
        {
            var item = Shop.CreateCoffee(0);
            item.AssignInitialSlot(Shop.DeliverySlot);
            Aim(item.transform.position);
        }
        else if (scenario == "bat")
        {
            Player.TryHoldTool(Shop.Activities.Bat);
            Aim(Shop.DeliverySlot.transform.position);
        }
        else if (scenario == "mop")
        {
            var spill = Shop.Activities.SpawnSpill();
            if (spill == null) throw new Exception("No spill fixture location available.");
            Player.TryHoldTool(Shop.Activities.Mop);
            Aim(spill.transform.position + Vector3.up * .02f);
        }
        else if (scenario == "coffee")
        {
            Shop.Ledger.TrySellCard(60000, 0);
            Shop.SyncCashDrawer();
            Shop.Activities.BuyCoffeeMachine();
            Set(Shop.Activities, "CoffeeSupplies", 5);
            Aim(Shop.Activities.CoffeeOutput.transform.position + Vector3.up * .25f);
        }
        else if (scenario == "dialogue" || scenario == "refuse")
        {
            Visit(scenario == "refuse" ? 2 : 0, 0);
            foreach (var item in FindObjectsByType<ShopProduct>().Where(p => p.ProductId == ShopCatalog.Ids[0])) item.RemoveFromWorld();
            Shop.SetModal(ShopModal.Dialogue);
        }
        else if (scenario == "summary")
        {
            Shop.OpenShop();
            Shop.RequestClose();
            Shop.Tick(.1f);
            Shop.ManualTick = false;
        }
        else if (scenario == "menu") Shop.GetComponent<ShopMenu>().Open();
        Physics.SyncTransforms();
        Snapshot("");
    }

    [Preserve]
    public void Action(string action)
    {
        if (action == "aim-place") Aim(Shop.Activities.BasketSlot.transform.position + Vector3.up * .05f);
        if (action == "milk") Set(Shop.Activities, "MilkSupplies", 2);
        if (action == "finish-brew") Shop.Activities.Tick(4);
        if (action == "remove-cup")
        {
            Shop.Activities.CoffeeOutput.Occupant?.RemoveFromWorld();
        }
        if (action == "monitor-coffee")
        {
            Shop.Devices.Focus(ShopDeviceKind.Monitor);
            Shop.Devices.OpenApp(ShopComputerApp.Coffee);
        }
        if (action == "live-clock") { Shop.OpenShop(); Shop.ManualTick = false; }
        if (action == "resume") Shop.GetComponent<ShopMenu>().Resume();
        Snapshot("");
    }

    private void Aim(Vector3 target)
    {
        var root = Player.transform;
        var body = Player.GetComponent<CharacterController>();
        body.enabled = false;
        root.rotation = Quaternion.Euler(0, 180, 0);
        Vector3 eye = new Vector3(target.x, Mathf.Max(target.y + .3f, 1.75f), target.z + .85f);
        root.position = eye - root.rotation * View.transform.localPosition;
        float pitch = Mathf.Atan2(eye.y - target.y, .85f) * Mathf.Rad2Deg;
        View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        typeof(FirstPersonController).GetField("pitch", Private).SetValue(Player.GetComponent<FirstPersonController>(), pitch);
        body.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Player.RefreshTarget(View.ViewportPointToRay(new Vector3(.5f, .5f, 0)));
    }

    [Serializable]
    private sealed class State
    {
        public string stage, phase, modal, message, entered, held, cup, order, interactionKey, tool;
        public bool menu, blocked, unavailable;
        public int balance, drawer, sales, missing, received, recipe, milk, coffee, day, missed, dirt;
        public float elapsed, timeScale;
        public Vector3 position;
        public Vector3[] receipts;
    }

    [Preserve]
    public void Snapshot(string unused)
    {
        var g = Shop;
        if (g == null) return;
        var receipts = FindObjectsByType<ShopProduct>().Where(p => p.CurrentSlot != null && (p.CurrentSlot == g.Activities.BasketSlot || p.CurrentSlot.name.StartsWith("Order receipt"))).ToArray();
        var state = new State
        {
            tool = Player.HeldTool == null ? "" : Player.HeldTool.Kind.ToString(), dirt = g.Activities.DirtCount,
            stage = g.Stage.ToString(), phase = g.Phase.ToString(), modal = g.Modal.ToString(),
            message = g.Message, entered = g.CardAmount, held = Player.HeldProduct?.ProductId ?? "",
            cup = g.Activities.CoffeeOutput.Occupant?.ProductId ?? "", order = g.ActiveCustomer?.OrderDescription ?? "",
            interactionKey = ShopBindings.Resolve(Key.E).ToString(), menu = ShopMenu.Active, blocked = ShopInputGate.Blocked,
            unavailable = g.HasUnavailableRequest, balance = g.Ledger.Cash, drawer = g.Drawer.Total, sales = g.Ledger.Sales,
            missing = g.MissingOrderTypes.Length, received = receipts.Length, recipe = g.Activities.SelectedRecipe,
            milk = g.Activities.MilkSupplies, coffee = g.Activities.CoffeeSupplies, day = g.Day, missed = g.Missed,
            elapsed = g.Elapsed, timeScale = Time.timeScale, position = Player.transform.position,
            receipts = receipts.Select(p => p.transform.position).ToArray()
        };
        Debug.Log("ACCEPTANCE_STATE:" + JsonUtility.ToJson(state));
    }
}
#endif
