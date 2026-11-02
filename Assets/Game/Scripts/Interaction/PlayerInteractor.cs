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
    private ShopProduct target;
    private ShopProduct heldProduct;
    private ProductSlot targetSlot;
    private bool canPlace;
    private GUIStyle promptStyle;
    private GUIStyle hintStyle;

    public void Configure(Camera cameraReference) => viewCamera = cameraReference;
    private ProductSlot[] placementSlots;
    public bool HasHeldProduct => heldProduct != null;
    public bool HasHeldItem => heldProduct != null || heldCarton != null;
    public WarehouseCarton HeldCarton => heldCarton;
    public bool TryHoldCarton(WarehouseCarton carton)
    {
        if(HasHeldItem || carton==null || !carton.PickUp(holdPoint))return false;
        heldCarton=carton;UpdatePlacementHints(false);return true;
    }
    public void ReturnCarton()
    {
        if(heldCarton==null)return;
        heldCarton.ReturnToStorage();heldCarton=null;
    }
    public bool ThrowAwayCarton()
    {
        if(heldCarton==null || heldCarton.Quantity!=0)return false;
        var carton=heldCarton;heldCarton=null;carton.ThrowAway();return true;
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
    public void CancelHeld()
    {
        if (heldProduct != null) heldProduct.ReturnToPickupPosition();
        heldProduct = null; ReturnCarton(); UpdatePlacementHints();
    }
    public bool RemoveHeld(ShopProduct expected)
    {
        if (heldProduct == null || heldProduct != expected) return false;
        heldProduct.RemoveFromWorld(); heldProduct=null; UpdatePlacementHints(); return true;
    }
    public bool TryPlaceHeld()
    {
        if (heldProduct == null || !canPlace || !heldProduct.TryPlace(targetSlot)) return false;
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
        { station=null;targetBin=null;target=null;targetSlot=null;canPlace=false;UpdatePlacementHints(false);return; }
        RefreshTarget(viewCamera.ViewportPointToRay(new Vector3(.5f,.5f,0)));
        if (Keyboard.current == null) return;
        if (station != null && !HasHeldProduct && Keyboard.current.qKey.wasPressedThisFrame) station.Cycle();
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;
        if (heldCarton != null)
        {
            if(targetBin!=null)targetBin.Use(this);
            else if(LookingAtSupplies)station.Use(this);
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
        station=null;targetBin=null;target=null;targetSlot=null;canPlace=false;
        foreach (var hit in Physics.RaycastAll(ray,reach,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance))
        {
            var slot=hit.collider.GetComponent<ProductSlot>();
            if (hit.collider.isTrigger)
            {
                if (heldProduct != null && slot != null)
                { targetSlot=slot;canPlace=heldProduct.TryGetPlacement(slot,out _);break; }
                continue;
            }
            station=hit.collider.GetComponentInParent<ShopStation>();
            targetBin=hit.collider.GetComponentInParent<WarehouseBin>();
            var product=hit.collider.GetComponentInParent<ShopProduct>();
            if (heldProduct != null)
            {
                if (!LookingAtSupplies && targetBin==null)
                {
                    targetSlot=product != null ? product.CurrentSlot : NearestSlotOnSurface(hit,ray.origin);
                    canPlace=heldProduct.TryGetPlacement(targetSlot,out _);
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
            bool show=visible && heldProduct!=null && slot.isActiveAndEnabled &&
                (selected || slot.CanAccept(heldProduct));
            slot.ShowPlacementHint(show,selected,!selected||canPlace);
        }
    }

    private void OnDisable()
    {
        if (heldProduct != null) heldProduct.ReturnToPickupPosition();
        heldProduct = null; ReturnCarton();
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
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 19, wordWrap = true };
            promptStyle.normal.textColor = Color.white;
            hintStyle = new GUIStyle(promptStyle) { fontSize = 14 };
        }
        bool cursorLocked = Cursor.lockState == CursorLockMode.Locked;
        Color previousColor = GUI.color;
        Color activeColor = new Color(0.4f, 1f, 0.6f);
        GUI.color = (target != null || station != null || targetBin != null || canPlace) ? activeColor : Color.white;
        if (cursorLocked)
        {
            float x = Screen.width * 0.5f;
            float y = Screen.height * 0.5f;
            GUI.DrawTexture(new Rect(x - 5, y - 1, 10, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - 1, y - 5, 2, 10), Texture2D.whiteTexture);
            if (heldProduct != null && targetSlot != null)
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
            targetSlot != null && !targetSlot.AcceptsType(heldProduct) ? "Ez a hely másik termékhez tartozik" :
            targetSlot != null && !targetSlot.CanAccept(heldProduct) ? "Ez a termékhely foglalt" :
            targetSlot != null ? "Az áru nem fér el ezen a helyen" : "Nézz egy kijelölt, üres polc- vagy pulthelyre";
        string prompt = !cursorLocked ? "Kattints a játékba a folytatáshoz" :
            heldCarton != null ? (targetBin != null ? targetBin.Prompt : LookingAtSupplies ? "E – Üres karton visszarakása a raktárba" : "Vidd az üres kartont a hátsó papírgyűjtőhöz") :
            heldProduct != null ? (targetBin != null ? "A kukába csak üres kartondoboz kerülhet" : LookingAtSupplies ? "E – Kézben tartott áru visszarakása a dobozba" : placementPrompt) :
            targetBin != null ? targetBin.Prompt : station != null ? station.Prompt : target != null ? "E – Felvétel: " + target.DisplayName : "Nézz egy termékre vagy egy interakciós pontra";
        float panelWidth = Mathf.Min(620f, Screen.width - 24f);
        Rect panel = new Rect((Screen.width - panelWidth) * 0.5f, Screen.height - 116, panelWidth, 96);
        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = previousColor;
        GUI.Label(new Rect(panel.x + 10, panel.y + 5, panel.width - 20, 48), prompt, promptStyle);
        string hint = heldCarton != null ? "Kézben: üres "+ShopCatalog.Names[heldCarton.ProductType]+" kartondoboz" : heldProduct != null ? "Kézben: " + heldProduct.DisplayName + "  •  A termék automatikusan igazodik a helyéhez" : "WASD – Mozgás  •  Egér – Körülnézés  •  Escape – Kurzor";
        GUI.Label(new Rect(panel.x + 10, panel.y + 54, panel.width - 20, 32), hint, hintStyle);
        if (LookingAtSupplies)
        {
            Rect contents=new Rect(12,158,330,220);
            GUI.color=new Color(0,0,0,.88f);GUI.DrawTexture(contents,Texture2D.whiteTexture);GUI.color=previousColor;
            var stockStyle=new GUIStyle(hintStyle){alignment=TextAnchor.UpperLeft,fontSize=16};
            GUI.Label(new Rect(contents.x+12,contents.y+10,contents.width-24,contents.height-20),station.Contents,stockStyle);
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
