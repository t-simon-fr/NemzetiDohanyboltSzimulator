using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField, Min(0.1f)] private float reach = 2.5f;
    [SerializeField] private Vector3 carryOffset = new Vector3(.26f, -.07f, .65f);
    private Transform holdPoint;
    private ShopStation station;
    private WarehouseBin targetBin;
    private WarehouseCarton heldCarton;
    private ShopHandTool heldTool,targetTool;
    private ShopToolRack targetRack;
    public ShopToolRack TargetToolRack => targetRack;
    private ShopFeatureTarget featureTarget;
    public ShopHandTool HeldTool=>heldTool;
    public void ConsumeHeldTool(){if(heldTool==null)return;var tool=heldTool;heldTool=null;tool.gameObject.SetActive(false);Destroy(tool.gameObject);UpdatePlacementHints();}
    public ShopFeatureTarget TargetFeature=>featureTarget;
    private ShopProduct target;
    private ShopProduct heldProduct;
    private ProductSlot targetSlot;
    private bool canPlace;

    public void Configure(Camera cameraReference) => viewCamera = cameraReference;
    private ProductSlot[] placementSlots;
    public bool HasHeldProduct => heldProduct != null;
    public bool HasHeldItem => heldProduct != null || heldCarton != null || heldTool != null;
    public WarehouseCarton HeldCarton => heldCarton;
    public bool TryHoldCarton(WarehouseCarton carton)
    {
        if(HasHeldItem || carton==null || !carton.PickUp(holdPoint))return false;
        heldCarton=carton;placementSlots=FindObjectsByType<ProductSlot>(FindObjectsInactive.Include);UpdatePlacementHints();return true;
    }
    public bool TryPlaceCarton(Transform dock)
    {
        if (heldCarton == null || !MvpShopController.Instance.Warehouse.TryDock(heldCarton, dock)) return false;
        heldCarton = null;
        UpdatePlacementHints();
        return true;
    }
    public void ReturnCarton()
    {
        if(heldCarton==null)return;
        heldCarton.ReturnToStorage();heldCarton=null;UpdatePlacementHints();
    }
    public bool ThrowAwayCarton()
    {
        if(heldCarton==null || heldCarton.Quantity!=0)return false;
        var carton=heldCarton;heldCarton=null;carton.ThrowAway();UpdatePlacementHints();return true;
    }
    public ShopProduct HeldProduct => heldProduct;
    public ProductSlot TargetSlot => targetSlot;
    public bool CanPlaceHeld => canPlace;
    public bool LookingAtSupplies => station != null && station.IsSupplies;
    public bool TryHold(ShopProduct product)
    {
        if (HasHeldItem || product == null || !product.PickUp(holdPoint)) return false;
        heldProduct = product; target = null;
        placementSlots=FindObjectsByType<ProductSlot>(FindObjectsInactive.Include);
        UpdatePlacementHints(); return true;
    }
    public bool TryHoldTool(ShopHandTool tool)
    {if(HasHeldItem||tool==null||!tool.PickUp(holdPoint))return false;heldTool=tool;UpdatePlacementHints();return true;}
    public bool TryReturnHeldTool(ShopToolRack rack)
    {
        if(heldTool==null || rack==null || !rack.TryStore(heldTool))return false;
        heldTool=null;targetRack=null;
        ShopInputGate.ConsumeWorldAction();UpdatePlacementHints();return true;
    }
    public bool DropHeld()
    {
        if(!HasHeldItem)return false;
        Vector3 position=viewCamera.transform.position+viewCamera.transform.forward*.8f;
        if(Physics.SphereCast(viewCamera.transform.position,.18f,viewCamera.transform.forward,out var hit,.8f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))position=hit.point-viewCamera.transform.forward*.23f;
        Vector3 velocity=viewCamera.transform.forward*1.5f+Vector3.up*.35f;
        if(heldProduct!=null){heldProduct.Drop(position,velocity);heldProduct=null;}
        else if(heldCarton!=null){heldCarton.Drop(position,velocity);heldCarton=null;}
        else {heldTool.Drop(position,velocity);heldTool=null;}
        UpdatePlacementHints();return true;
    }
    public void CancelHeld()
    {
        if (heldProduct != null) heldProduct.ReturnToPickupPosition();
        heldProduct = null; ReturnCarton();if(heldTool!=null)heldTool.ReturnToPickupPosition();heldTool=null; UpdatePlacementHints();
    }
    public bool RemoveHeld(ShopProduct expected)
    {
        if (heldProduct == null || heldProduct != expected) return false;
        heldProduct.RemoveFromWorld(); heldProduct=null; UpdatePlacementHints(); return true;
    }
    public bool TryPlaceHeld()
    {
        if(heldCarton!=null)
        {
            if(!canPlace||!heldCarton.TryPlaceOne(targetSlot))return false;
            canPlace=false;targetSlot=null;UpdatePlacementHints();return true;
        }
        if (heldProduct == null || !canPlace || targetSlot==null || !targetSlot.CanPlaceManually(heldProduct) || !heldProduct.TryPlace(targetSlot)) return false;
        heldProduct=null; UpdatePlacementHints(); return true;
    }

    private void Awake()
    {
        if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        if (viewCamera == null)
        {
            Debug.LogError("PlayerInteractor requires a player camera.", this);
            enabled = false;
            return;
        }
        holdPoint = new GameObject("Product Hold Point").transform;
        holdPoint.SetParent(viewCamera.transform, false);
        // Migrate the old serialized offset so the control panel no longer hides the pack.
        if (Vector3.Distance(carryOffset,new Vector3(.3f,-.25f,.65f))<.001f) carryOffset=new Vector3(.26f,-.07f,.65f);
        holdPoint.localPosition = carryOffset;
    }

    private void Update()
    {
        if (ShopInputGate.Blocked || viewCamera == null || Cursor.lockState != CursorLockMode.Locked || !Application.isFocused)
        { station=null;targetBin=null;target=null;targetSlot=null;canPlace=false;targetTool=null;featureTarget=null;UpdatePlacementHints(false);return; }
        RefreshTarget(viewCamera.ViewportPointToRay(new Vector3(.5f,.5f,0)));
        if (ShopInputGate.WorldActionConsumed) return;
        if (Keyboard.current == null) return;
        if(ShopBindings.Pressed(UnityEngine.InputSystem.Key.G)){DropHeld();return;}
        if(heldTool!=null&&Mouse.current!=null)
        {
            if(heldTool.Kind==ShopToolKind.Bat&&targetRack==null&&Mouse.current.leftButton.wasPressedThisFrame)MvpShopController.Instance.Activities.SwingBat(viewCamera);
            if(heldTool.Kind==ShopToolKind.Mop&&Mouse.current.leftButton.isPressed&&featureTarget!=null&&featureTarget.Kind==ShopFeatureKind.Spill)
            {
                heldTool.PlaySweep();
                featureTarget.Scrub(Time.deltaTime);
            }
        }
        if (station != null && !HasHeldProduct && ShopBindings.Pressed(UnityEngine.InputSystem.Key.Q)) station.Cycle();
        if (!ShopBindings.Pressed(UnityEngine.InputSystem.Key.E) && !((heldTool==null||targetRack!=null)&&Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame)) return;
        if(targetRack!=null && heldTool!=null){TryReturnHeldTool(targetRack);return;}
        if (station != null && station.IsPanicButton) { station.Use(this); return; }
        if (station != null && station.IsCoffeeControl && !HasHeldItem) { station.Use(this); return; }
        if(featureTarget!=null&&featureTarget.Kind==ShopFeatureKind.CoffeeMachine&&(!HasHeldItem||heldTool!=null&&heldTool.Kind==ShopToolKind.CoffeeRefill)){MvpShopController.Instance.Activities.UseCoffeeMachine();return;}
        if(heldTool!=null)
        {
            if (heldTool.Kind==ShopToolKind.ContrabandBox && targetSlot!=null && MvpShopController.Instance.CounterStorage.Slots.Contains(targetSlot))
            {
                if (heldTool.GetComponent<ShopContrabandBox>().Unpack(MvpShopController.Instance.CounterStorage)) ConsumeHeldTool();
                else MvpShopController.Instance.Notify("Nincs elég szabad hely a teljes doboznak.");
            }
            return;
        }
        if(!HasHeldItem&&targetTool!=null){TryHoldTool(targetTool);return;}
        if (heldCarton != null)
        {
            if(targetBin!=null)targetBin.Use(this);
            else if(LookingAtSupplies)station.Use(this);
            else TryPlaceHeld();
        }
        else if (heldProduct != null)
        {
            if(targetBin!=null)targetBin.Use(this);
            else if (LookingAtSupplies) station.Use(this);
            else TryPlaceHeld();
        }
        else if (targetBin != null) targetBin.Use(this);
        else if (station != null) station.Use(this);
        else if (target != null) TryHold(target);
    }
    // Shared by keyboard interaction and the scene-level targeting checks.
    public void RefreshTarget(Ray ray)
    {
        station=null;targetBin=null;target=null;targetSlot=null;canPlace=false;targetTool=null;featureTarget=null;targetRack=null;
        var hits=Physics.RaycastAll(ray,reach,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance).ToArray();
        var firstSolid=hits.FirstOrDefault(hit=>!hit.collider.isTrigger);
        bool visibleTool=!HasHeldItem && firstSolid.collider!=null && firstSolid.collider.GetComponentInParent<ShopHandTool>()!=null;
        foreach (var hit in hits)
        {
            var slot=hit.collider.GetComponent<ProductSlot>();
            if (hit.collider.isTrigger)
            {
                var rack=hit.collider.GetComponent<ShopToolRack>();
                if(rack!=null && heldTool!=null && rack.Tool==heldTool){targetRack=rack;break;}
                var supply = hit.collider.GetComponent<ShopStation>();
                if(supply!=null && supply.IsExit && !HasHeldItem){station=supply;break;}
                if (supply != null && supply.IsSupplies && MvpShopController.Instance.Warehouse.IsStorageDock(supply.transform))
                {
                    // An empty storage trigger must not hide a visible refill package.
                    // Only the nearest solid hit qualifies, so walls and cartons still occlude it.
                    if (visibleTool) continue;
                    station = supply;
                    break;
                }
                if (heldTool!=null && heldTool.Kind==ShopToolKind.ContrabandBox && slot!=null)
                { targetSlot=slot; break; }
                if ((heldProduct!=null||heldCarton!=null) && slot != null)
                {
                    if(heldProduct!=null && !slot.CanPlaceManually(heldProduct))continue;
                    targetSlot=slot;canPlace=heldCarton!=null?heldCarton.CanPlace(slot):heldProduct.TryGetPlacement(slot,out _);break;
                }
                continue;
            }
            station=hit.collider.GetComponentInParent<ShopStation>();
            if (station != null && station.IsSupplies && MvpShopController.Instance.Warehouse.IsStorageDock(station.transform))
            {
                var dock = MvpShopController.Instance.Warehouse.DockOnShelf(station.transform, hit.point);
                if (dock != null) station = dock.GetComponent<ShopStation>();
            }
            targetBin=hit.collider.GetComponentInParent<WarehouseBin>();
            featureTarget=hit.collider.GetComponentInParent<ShopFeatureTarget>();
            targetTool=hit.collider.GetComponentInParent<ShopHandTool>();
            if(heldTool!=null)break;
            var product=hit.collider.GetComponentInParent<ShopProduct>();
            if (HasHeldItem)
            {
                if (!LookingAtSupplies && targetBin==null && (station==null || !station.IsPanicButton))
                {
                    targetSlot=product != null ? product.CurrentSlot : NearestSlotOnSurface(hit,ray.origin);
                    canPlace=heldCarton!=null?heldCarton.CanPlace(targetSlot):targetSlot!=null&&targetSlot.CanPlaceManually(heldProduct)&&heldProduct.TryGetPlacement(targetSlot,out _);
                }
            }
            else if (heldCarton==null && product != null && product.enabled && !product.IsHeld) target=product;
            break;
        }
        UpdatePlacementHints();
    }
    private ProductSlot NearestSlotOnSurface(RaycastHit hit,Vector3 eye)
    {
        Transform scope=hit.collider.transform.parent;
        if (scope==null || placementSlots==null) return null;
        bool counterSurface=hit.collider.name=="Counter top"||hit.collider.name=="Blue handover tray";
        var nearest=placementSlots.Where(s=>s!=null&&s.isActiveAndEnabled&&
            (s.transform.parent==scope || (counterSurface&&s.transform.IsChildOf(scope))))
            .OrderBy(s=>(s.HintCenter-hit.point).sqrMagnitude).FirstOrDefault();
        if (nearest==null || Vector3.Distance(nearest.HintCenter,hit.point)>.34f) return null;
        Vector3 delta=nearest.HintCenter-eye;
        if (Physics.Raycast(eye,delta.normalized,delta.magnitude-.03f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) return null;
        return nearest;
    }
    private void UpdatePlacementHints(bool visible=true)
    {
        if (placementSlots==null) return;
        foreach (var slot in placementSlots)
        {
            if (slot==null) continue;
            bool selected=slot==targetSlot;
            bool show=visible && (heldProduct!=null||heldCarton!=null) && slot.isActiveAndEnabled && (heldProduct==null||slot.CanPlaceManually(heldProduct)) &&
                (selected || (heldCarton!=null ? heldCarton.Quantity>0&&slot.CanAcceptStock(ShopCatalog.Ids[heldCarton.ProductType]) : slot.CanAccept(heldProduct)));
            slot.ShowPlacementHint(show,selected,!selected||canPlace);
        }
    }

    private void OnDisable()
    {
        if (heldProduct != null) heldProduct.ReturnToPickupPosition();
        heldProduct = null; ReturnCarton();if(heldTool!=null)heldTool.ReturnToPickupPosition();heldTool=null;
        target = null;
        targetSlot = null; UpdatePlacementHints(false);
    }

    private void OnDestroy()
    {
        if (holdPoint != null) Destroy(holdPoint.gameObject);
    }

    private void OnGUI()
    {
        if (ShopInputGate.Blocked || viewCamera == null || !Application.isPlaying) return;
        bool cursorLocked = Cursor.lockState == CursorLockMode.Locked;
        Color previousColor = GUI.color;
        Color activeColor = new Color(0.4f, 1f, 0.6f);
        GUI.color = (target != null || station != null || targetBin != null || targetTool!=null || featureTarget!=null || canPlace) ? activeColor : Color.white;
        if (cursorLocked)
        {
            float x = Screen.width * 0.5f;
            float y = Screen.height * 0.5f;
            GUI.DrawTexture(new Rect(x - 5, y - 1, 10, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - 1, y - 5, 2, 10), Texture2D.whiteTexture);
            if (HasHeldItem && targetSlot != null)
            {
                Vector3 screen = viewCamera.WorldToScreenPoint(targetSlot.transform.position + Vector3.up * 0.15f);
                if (screen.z > 0f)
                {
                    GUI.color = canPlace ? activeColor : new Color(1f, 0.4f, 0.3f);
                    DrawSlotMarker(screen.x, Screen.height - screen.y);
                }
            }
        }
        GUI.color = previousColor;
        string placementPrompt = canPlace ? "E – Lerakás a kijelölt helyre" :
            heldProduct!=null && targetSlot!=null && !targetSlot.CanPlaceManually(heldProduct)
                ? (ShopCatalog.IsCigarette(ShopCatalog.Index(heldProduct.ProductId)) ? "A cigarettát a vonalkódolvasóra tedd" : "Az italt a vásárlói zóna egyik üres helyére tedd") :
            targetSlot != null && !targetSlot.AcceptsType(heldProduct) ? "Ez a hely másik termékhez tartozik" :
            targetSlot != null && !targetSlot.CanAccept(heldProduct) ? "Ez a termékhely foglalt" :
            targetSlot != null ? "Az áru nem fér el ezen a helyen" : "Nézz egy kijelölt, üres polc- vagy pulthelyre";
        string prompt = !cursorLocked ? "Kattints a játékba a folytatáshoz" :
            station != null && station.IsPanicButton ? station.Prompt :
            station != null && station.IsCoffeeControl && !HasHeldItem ? station.Prompt :
            heldTool!=null && targetRack!=null ? targetRack.Prompt :
            heldTool!=null ? (featureTarget!=null&&featureTarget.Kind==ShopFeatureKind.Spill?featureTarget.Prompt:heldTool.DisplayName+" • Bal egérgomb: használat • G: eldobás") :
            !HasHeldItem&&targetTool!=null ? "E – Felvétel: "+targetTool.DisplayName :
            !HasHeldItem&&featureTarget!=null ? featureTarget.Prompt :
            heldCarton != null ? (targetBin != null ? (heldCarton.Quantity==0?targetBin.Prompt:"A papírgyűjtőbe csak üres karton kerülhet") : LookingAtSupplies ? station.CartonPlacementPrompt : heldCarton.Quantity==0 ? "Üres doboz: vidd vissza vagy dobd a hátsó papírgyűjtőbe" : canPlace ? "E – Egy termék kipakolása a kijelölt polchelyre" : targetSlot!=null ? "Ez a hely foglalt, vagy más márkához tartozik" : "Nézz egy zölddel jelölt üres polchelyre") :
            heldProduct != null ? (targetBin != null ? "A kukába csak üres kartondoboz kerülhet" : LookingAtSupplies ? "E – Kézben tartott áru visszarakása a dobozba" : placementPrompt) :
            targetBin != null ? targetBin.Prompt : station != null ? station.Prompt : target != null ? "E – Felvétel: " + target.DisplayName + (ShopCatalog.Index(target.ProductId)>=0?" • "+ShopCatalog.Money(ShopCatalog.SalePrice(ShopCatalog.Index(target.ProductId))):"") : "Nézz egy termékre vagy egy interakciós pontra";
        var hud = MvpShopController.Instance.GetComponent<MvpShopHud>();
        string hint = HasHeldItem ? "G: Eldobás" : hud.MovementLearned ? "" : "W / A / S / D: Mozgás  •  Egér: Körülnézés  •  H: Súgó";
        if (heldCarton != null) hint = heldCarton.Quantity + " db a dobozban  /  " + hint;
        using (ShopUi.Frame())
        {
            string label = ShopUi.BoundText(prompt);
            float textHeight = ShopUi.Text(17).CalcHeight(new GUIContent(label), 580);
            float height = textHeight + 28 + (string.IsNullOrEmpty(hint) ? 0 : 28);
            bool meaningful = HasHeldItem || target != null || station != null || targetBin != null || targetTool != null || featureTarget != null || !cursorLocked || !hud.MovementLearned;
            if (meaningful && !string.IsNullOrEmpty(prompt))
            {
                Rect panel = new Rect(334, 696 - height, 612, height);
                ShopUi.Panel(panel, true);
                GUI.Label(new Rect(panel.x + 16, panel.y + 12, 580, textHeight), label, ShopUi.Text(17, false, true, TextAnchor.MiddleCenter));
                if (!string.IsNullOrEmpty(hint)) GUI.Label(new Rect(panel.x + 16, panel.y + textHeight + 19, 580, 24), ShopUi.BoundText(hint), ShopUi.Text(14, false, true, TextAnchor.MiddleCenter));
            }
        }
    }

    private static void DrawSlotMarker(float x, float y)
    {
        GUI.DrawTexture(new Rect(x - 14, y - 14, 28, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 14, y + 12, 28, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 14, y - 14, 2, 28), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + 12, y - 14, 2, 28), Texture2D.whiteTexture);
    }
}
