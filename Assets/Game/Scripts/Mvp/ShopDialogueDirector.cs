using UnityEngine;

// Dialogue zoom + typewriter state for #175.
// Owns no scene objects: the camera is cached lazily so the per-frame
// lerp stays allocation-free, and all decisions derive from shop state
// every tick so resets and fast retriggers cannot desynchronize it.
public sealed class ShopDialogueDirector
{
    public const float BaseFov = 60f;
    public const float ZoomFov = 48f;
    public const float LerpRate = 5f;
    public const float CharsPerSecond = 45f;

    private Camera view;
    private float baseFov = BaseFov;
    private bool baseCaptured;
    private string lastDialogue = "";
    private MvpCustomerActor lastCustomer;
    private float shown;
    private bool panelVisible;

    public bool PanelVisible => panelVisible;
    public bool Typing => panelVisible && shown < lastDialogue.Length;
    public bool HasView => view != null;

    public static bool WantZoom(MvpShopController shop)
    {
        if (shop == null || shop.ActiveCustomer == null) return false;
        if (shop.Stage != ServiceStage.Order) return false;
        if (shop.Modal != ShopModal.None) return false;
        if (ShopMenu.Active) return false;
        if (shop.Devices != null && shop.Devices.Focused) return false;
        if (shop.Arrangement != null && shop.Arrangement.Active) return false;
        if (shop.AfterHours != null && shop.AfterHours.TradeOpen) return false;
        return true;
    }

    public void Tick(MvpShopController shop, float dt)
    {
        string full = shop != null ? shop.Dialogue ?? "" : "";
        if (shop == null || !ReferenceEquals(shop.ActiveCustomer, lastCustomer) || full != lastDialogue)
        {
            lastCustomer = shop != null ? shop.ActiveCustomer : null;
            lastDialogue = full;
            shown = 0f;
        }
        bool want = WantZoom(shop);
        panelVisible = want;
        if (want && dt > 0f && full.Length > 0)
            shown = Mathf.Min(full.Length, shown + CharsPerSecond * dt);
        if (view == null)
            view = Camera.main;
        if (view == null) return;
        // A device focus owns the camera (till, terminal, coins, monitor...);
        // pausing freezes the frame. Never fight either of them.
        if (shop != null && shop.Devices != null && shop.Devices.Focused) return;
        if (ShopMenu.Active) return;
        if (!baseCaptured)
        {
            baseFov = view.fieldOfView;
            baseCaptured = true;
        }
        float target = want ? ZoomFov : baseFov;
        float current = view.fieldOfView;
        if (Mathf.Abs(current - target) < 0.05f)
            view.fieldOfView = target;
        else
            view.fieldOfView = current + (target - current) * Mathf.Min(1f, LerpRate * dt);
    }

    public string ShownText(string full)
    {
        full = full ?? "";
        if (full != lastDialogue)
        {
            lastDialogue = full;
            shown = 0f;
        }
        int count = Mathf.Clamp(Mathf.FloorToInt(shown), 0, full.Length);
        return full.Substring(0, count);
    }

    // E completes the typed sentence as a side effect but never consumes the
    // world action: the press that finishes the line still delivers, picks up
    // or opens exactly as before, so the E flow is unchanged while typing.
    public bool Complete()
    {
        if (!panelVisible || shown >= lastDialogue.Length) return false;
        shown = lastDialogue.Length;
        return true;
    }
}
