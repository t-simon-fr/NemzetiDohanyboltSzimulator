using System.Collections.Generic;
using System.Collections.Generic;
using UnityEngine;

// Procedural part animation for the scripted Pisti model: walk/run cycles,
// counter presentation, blinking and mood mouths. Visual only: queue, basket,
// payment and theft stay in MvpCustomerActor and the shop, so frozen ManualTick
// fixtures and save/load flows never depend on this.
public sealed class ShopPistiRig : MonoBehaviour
{
    public enum Mood { Neutral, Smile, Grim, Panic }

    private MvpCustomerActor actor;
    private MvpShopController shop;
    private readonly Dictionary<string, Transform> parts = new Dictionary<string, Transform>();
    private readonly Dictionary<string, GameObject> mouths = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, Vector3> eyeBase = new Dictionary<string, Vector3>();
    private Vector3 lastPosition;
    private Vector3 torsoBase;
    private float walkPhase, presentWeight, blinkTimer, nextBlink = 2.5f;
    private Mood shownMood = (Mood)(-1);

    public static Mood MoodFor(bool stealing, ServiceStage stage, bool isActive, bool complained)
    {
        if (stealing) return Mood.Panic;
        if (isActive && stage == ServiceStage.Thanks) return Mood.Smile;
        if (complained) return Mood.Grim;
        return Mood.Neutral;
    }

    public void Configure(MvpCustomerActor customer, MvpShopController game)
    {
        actor = customer;
        shop = game;
        parts.Clear();
        foreach (var t in GetComponentsInChildren<Transform>(true)) parts[t.name] = t;
        lastPosition = actor.transform.position;
        torsoBase = parts["Torso"].localPosition;
        mouths["Neutral"] = parts["MouthNeutral"].gameObject;
        mouths["Smile"] = parts["MouthSmile"].gameObject;
        mouths["Grim"] = parts["MouthGrim"].gameObject;
        mouths["Panic"] = parts["MouthPanic"].gameObject;
        blinkTimer = nextBlink;
        foreach (string eye in new[] { "EyeWhiteL", "EyeWhiteR", "PupilL", "PupilR" })
            if (parts.TryGetValue(eye, out var e)) eyeBase[eye] = e.localScale;
        ApplyMood(Mood.Neutral);
        Tick(0f);
    }

    private Transform Part(string name) => parts.TryGetValue(name, out var t) ? t : null;

    private float rigTime;

    // Driven from the shop tick so animation follows game time deterministically:
    // manual-tick fixtures, paused rendering and low frame rates all stay in sync.
    public void Tick(float dt)
    {
        if (actor == null) return;
        dt = Mathf.Max(0f, dt);
        rigTime += dt;
        Vector3 position = actor.transform.position;
        float speed = dt > 0f ? (position - lastPosition).magnitude / dt : 0f;
        lastPosition = position;
        bool moving = speed > .15f;
        bool running = actor.IsStealing;
        bool active = shop != null && shop.ActiveCustomer == actor;
        bool presenting = active && !moving && !actor.Leaving && shop.Stage == ServiceStage.Order;
        presentWeight = Mathf.MoveTowards(presentWeight, presenting ? 1f : 0f, dt * 6f);
        if (moving) walkPhase += dt * (running ? 11f : 7f);
        float swing = moving ? Mathf.Sin(walkPhase) * (running ? 45f : 28f) : 0f;
        float knee = moving ? Mathf.Max(0f, Mathf.Sin(walkPhase - .6f)) * (running ? 55f : 40f) : 0f;
        SetX("LegUpperL", -swing);
        SetX("LegUpperR", swing);
        SetX("LegLowerL", knee);
        SetX("LegLowerR", Mathf.Max(0f, Mathf.Sin(walkPhase + Mathf.PI - .6f)) * (running ? 55f : 40f));
        SetX("ArmUpperL", swing * .7f);
        float raise = -65f * presentWeight;
        SetX("ArmUpperR", Mathf.Lerp(swing * -.7f, raise, presentWeight));
        SetX("ArmLowerL", moving ? -15f : -8f);
        SetX("ArmLowerR", Mathf.Lerp(moving ? -20f : -10f, -25f, presentWeight));
        var torso = Part("Torso");
        if (torso != null)
        {
            float bob = moving ? Mathf.Abs(Mathf.Cos(walkPhase)) * (running ? .05f : .03f) : Mathf.Sin(rigTime * 1.5f) * .005f;
            torso.localPosition = torsoBase + new Vector3(0f, bob, 0f);
            float lean = running ? 10f : 0f;
            torso.localRotation = Quaternion.Euler(lean, 0f, 0f);
        }
        bool complained = shop != null && ShopPeople.IsComplaintLine(actor.Person, shop.Dialogue);
        ApplyMood(MoodFor(running, shop != null ? shop.Stage : ServiceStage.None, active, complained));
        blinkTimer -= dt;
        float lid = blinkTimer < .12f ? .12f : 1f;
        if (blinkTimer <= 0f) { blinkTimer = nextBlink; nextBlink = 2f + (walkPhase % 1.7f); }
        SetEyeScale(lid);
    }

    private void SetX(string name, float degrees)
    {
        var t = Part(name);
        if (t != null) t.localRotation = Quaternion.Euler(degrees, 0f, 0f);
    }

    private void SetEyeScale(float lid)
    {
        foreach (var pair in eyeBase)
        {
            if (!parts.TryGetValue(pair.Key, out var t) || t == null) continue;
            Vector3 b = pair.Value;
            t.localScale = new Vector3(b.x, b.y * lid, b.z);
        }
    }

    private void ApplyMood(Mood mood)
    {
        if (mood == shownMood) return;
        shownMood = mood;
        mouths["Neutral"].SetActive(mood == Mood.Neutral);
        mouths["Smile"].SetActive(mood == Mood.Smile);
        mouths["Grim"].SetActive(mood == Mood.Grim);
        mouths["Panic"].SetActive(mood == Mood.Panic);
    }
}
