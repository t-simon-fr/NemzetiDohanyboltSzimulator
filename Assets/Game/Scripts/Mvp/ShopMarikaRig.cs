using System.Collections.Generic;
using UnityEngine;

// Animator-driven rig for the MPFB Marika model (person 1): walk/idle/present/
// complain takes authored in Blender, mood mouths swapped like Pisti. Visual
// only: queue, basket, payment and theft stay in MvpCustomerActor and the shop,
// so frozen ManualTick fixtures and save/load flows never depend on this.
public sealed class ShopMarikaRig : MonoBehaviour
{
    public enum Mood { Neutral, Smile, Grim, Panic }

    private MvpCustomerActor actor;
    private MvpShopController shop;
    private Animator animator;
    private readonly Dictionary<string, GameObject> mouths = new Dictionary<string, GameObject>();
    private Vector3 lastPosition;
    private float presentWeight;
    private Mood shownMood = (Mood)(-1);
    private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

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
        animator = GetComponentInChildren<Animator>(true);
        if (animator == null) animator = gameObject.AddComponent<Animator>();
        var controller = Resources.Load<RuntimeAnimatorController>("Customers/Marika/Marika");
        if (controller != null) animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        // Gameplay and ManualTick fixtures advance the Animator explicitly, so it
        // must sample even while the customer walks outside the camera frustum.
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        // Blender Principled exports as Standard; the URP player needs URP/Lit.
        // The body carries several painted zones (skin/blouse/trousers/shoes).
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            var srcs = renderer.sharedMaterials;
            bool dirty = false;
            for (int i = 0; i < srcs.Length; i++)
            {
                var src = srcs[i];
                if (src == null || src.shader.name == "Universal Render Pipeline/Lit") continue;
                string key = src.name + "|" + renderer.name + i;
                if (!materialCache.TryGetValue(key, out var fixedMat))
                {
                    fixedMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    if (src.HasProperty("_Color")) fixedMat.color = src.color;
                    else if (src.HasProperty("_BaseColor")) fixedMat.SetColor("_BaseColor", src.GetColor("_BaseColor"));
                    if (src.HasProperty("_MainTex")) fixedMat.SetTexture("_BaseMap", src.GetTexture("_MainTex"));
                    if (src.HasProperty("_BumpMap")) fixedMat.SetTexture("_BumpMap", src.GetTexture("_BumpMap"));
                    // The MPFB base ships without skin textures, so its imported grey is
                    // replaced with the game's warm skin tone; clothes keep their colors.
                    if (src.name.Contains("Skin") || (renderer.name == "Human" && i == 0)) fixedMat.color = new Color(.72f, .52f, .38f);
                    fixedMat.SetFloat("_Smoothness", .4f);
                    materialCache[key] = fixedMat;
                }
                srcs[i] = fixedMat;
                dirty = true;
            }
            if (dirty) renderer.sharedMaterials = srcs;
        }
        mouths.Clear();
        mouths["Neutral"] = DeepFind("MarikaMouthNeutral");
        mouths["Smile"] = DeepFind("MarikaMouthSmile");
        mouths["Grim"] = DeepFind("MarikaMouthGrim");
        mouths["Panic"] = DeepFind("MarikaMouthPanic");
        lastPosition = actor.transform.position;
        ApplyMood(Mood.Neutral);
        Tick(0f);
    }

    private GameObject DeepFind(string name)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t.gameObject;
        return null;
    }

    // Driven from the shop tick so animation follows game time deterministically:
    // manual-tick fixtures advance the Animator explicitly, the live game uses
    // the normal Update loop.
    public void Tick(float dt)
    {
        if (actor == null || animator == null) return;
        dt = Mathf.Max(0f, dt);
        Vector3 position = actor.transform.position;
        float speed = dt > 0f ? (position - lastPosition).magnitude / dt : 0f;
        lastPosition = position;
        bool moving = speed > .15f;
        bool running = actor.IsStealing;
        bool active = shop != null && shop.ActiveCustomer == actor;
        bool presenting = active && !moving && !actor.Leaving && shop.Stage == ServiceStage.Order;
        presentWeight = Mathf.MoveTowards(presentWeight, presenting ? 1f : 0f, dt * 6f);
        bool complained = shop != null && ShopPeople.IsComplaintLine(actor.Person, shop.Dialogue);
        bool complainPose = complained && active && !moving;
        animator.SetFloat("Speed", speed);
        animator.SetBool("Present", presentWeight > .5f);
        animator.SetBool("Complain", complainPose);
        animator.speed = running ? 1.6f : 1f;
        if (shop != null && shop.ManualTick) animator.Update(dt);
        ApplyMood(MoodFor(running, shop != null ? shop.Stage : ServiceStage.None, active, complained));
    }

    private void ApplyMood(Mood mood)
    {
        if (mood == shownMood) return;
        shownMood = mood;
        SetMouth("Neutral", mood == Mood.Neutral);
        SetMouth("Smile", mood == Mood.Smile);
        SetMouth("Grim", mood == Mood.Grim);
        SetMouth("Panic", mood == Mood.Panic);
    }

    private void SetMouth(string name, bool visible)
    {
        if (mouths.TryGetValue(name, out var go) && go != null) go.SetActive(visible);
    }
}
