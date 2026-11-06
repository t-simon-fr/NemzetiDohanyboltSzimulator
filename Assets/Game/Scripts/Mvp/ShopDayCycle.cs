using System;

// The clock and phase transitions are independent of modal/device focus.
public sealed class ShopDayCycle
{
    public ShopPhase Phase { get; internal set; }
    public int Day { get; internal set; } = 1;
    public float Elapsed { get; internal set; }
    public float ClockElapsed { get; internal set; }
    public bool CanEnd(bool empty) => empty && (Phase == ShopPhase.Closing || Phase == ShopPhase.AfterHours);
    public bool CanDevelop(bool ledgerOpen, bool empty) => ledgerOpen && (Phase == ShopPhase.Preparing || CanEnd(empty));
    public bool Open()
    {
        if (Phase != ShopPhase.Preparing) return false;
        Phase = ShopPhase.Open;
        return true;
    }
    public bool Close()
    {
        if (Phase != ShopPhase.Open) return false;
        Phase = ShopPhase.Closing;
        return true;
    }
    public bool Stay(bool empty)
    {
        if (Phase != ShopPhase.Closing || !empty) return false;
        Phase = ShopPhase.AfterHours;
        return true;
    }
    public void TickClock(float dt)
    {
        if (Phase == ShopPhase.Open || Phase == ShopPhase.Closing || Phase == ShopPhase.AfterHours) ClockElapsed += Math.Max(0, dt);
    }
    public void TickService(float dt) { if (Phase == ShopPhase.Open) Elapsed += Math.Max(0, dt); }
    public void Finish() => Phase = ShopPhase.Summary;
    public void Prepare() { Phase = ShopPhase.Preparing; Elapsed = ClockElapsed = 0; }
}
