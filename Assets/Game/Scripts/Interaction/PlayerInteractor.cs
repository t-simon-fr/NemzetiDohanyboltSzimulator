using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField, Min(0.1f)] private float reach = 2.5f;
    [SerializeField] private Vector3 carryOffset = new Vector3(0.3f, -0.25f, 0.65f);
    private Transform holdPoint;
    private ShopStation station;
    private ShopProduct target;
    private ShopProduct heldProduct;
    private ProductSlot targetSlot;
    private bool canPlace;
    private GUIStyle promptStyle;
    private GUIStyle hintStyle;

    public void Configure(Camera cameraReference) => viewCamera = cameraReference;
    public bool HasHeldProduct => heldProduct != null;
    public bool TryHold(ShopProduct product)
    {
        if (heldProduct != null || product == null || !product.PickUp(holdPoint)) return false;
        heldProduct = product; target = null; return true;
    }
    public void CancelHeld()
    {
        if (heldProduct != null) heldProduct.ReturnToPickupPosition();
        heldProduct = null;
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
        holdPoint.localPosition = carryOffset;
    }

    private void Update()
    {
        station = null;
        target = null;
        targetSlot = null;
        canPlace = false;
        if (ShopInputGate.Blocked || viewCamera == null || Cursor.lockState != CursorLockMode.Locked || !Application.isFocused) return;
        Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        QueryTriggerInteraction triggers = heldProduct != null ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        if (Physics.Raycast(ray, out RaycastHit hit, reach, Physics.DefaultRaycastLayers, triggers))
        {
            if (heldProduct != null)
            {
                targetSlot = hit.collider.GetComponent<ProductSlot>();
                if (targetSlot == null)
                {
                    ShopProduct hitProduct = hit.collider.GetComponentInParent<ShopProduct>();
                    if (hitProduct != null) targetSlot = hitProduct.CurrentSlot;
                }
                canPlace = heldProduct.TryGetPlacement(targetSlot, out _);
            }
            else
            {
                station = hit.collider.GetComponentInParent<ShopStation>();
                target = hit.collider.GetComponentInParent<ShopProduct>();
                if (target != null && (!target.enabled || target.IsHeld)) target = null;
            }
        }
        if (Keyboard.current == null) return;
        if (station != null && Keyboard.current.qKey.wasPressedThisFrame) station.Cycle();
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;
        if (heldProduct != null)
        {
            if (!canPlace || !heldProduct.TryPlace(targetSlot)) return;
            heldProduct = null;
        }
        else if (station != null) station.Use(this);
        else if (target != null && target.PickUp(holdPoint))
        {
            heldProduct = target;
            target = null;
        }
    }

    private void OnDisable()
    {
        if (heldProduct != null) heldProduct.ReturnToPickupPosition();
        heldProduct = null;
        target = null;
        targetSlot = null;
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
        GUI.color = (target != null || station != null || canPlace) ? activeColor : Color.white;
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
            targetSlot != null && !targetSlot.CanAccept(heldProduct) ? "Ez a termékhely foglalt" :
            targetSlot != null ? "Az áru nem fér el ezen a helyen" : "Nézz egy kijelölt, üres polc- vagy pulthelyre";
        string prompt = !cursorLocked ? "Kattints a játékba a folytatáshoz" :
            heldProduct != null ? placementPrompt :
            station != null ? station.Prompt : target != null ? "E – Felvétel: " + target.DisplayName : "Nézz egy termékre vagy egy interakciós pontra";
        float panelWidth = Mathf.Min(620f, Screen.width - 24f);
        Rect panel = new Rect((Screen.width - panelWidth) * 0.5f, Screen.height - 116, panelWidth, 96);
        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = previousColor;
        GUI.Label(new Rect(panel.x + 10, panel.y + 5, panel.width - 20, 48), prompt, promptStyle);
        string hint = heldProduct != null ? "Kézben: " + heldProduct.DisplayName + "  •  A termék automatikusan igazodik a helyéhez" : "WASD – Mozgás  •  Egér – Körülnézés  •  Escape – Kurzor";
        GUI.Label(new Rect(panel.x + 10, panel.y + 54, panel.width - 20, 32), hint, hintStyle);
    }

    private static void DrawSlotMarker(float x, float y)
    {
        GUI.DrawTexture(new Rect(x - 14, y - 14, 28, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 14, y + 12, 28, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 14, y - 14, 2, 28), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + 12, y - 14, 2, 28), Texture2D.whiteTexture);
    }
}
