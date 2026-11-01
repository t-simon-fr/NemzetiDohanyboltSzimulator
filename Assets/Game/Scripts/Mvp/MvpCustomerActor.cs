using UnityEngine;

public sealed class MvpCustomerActor : MonoBehaviour
{
    public int Person { get; private set; }
    public int Product { get; private set; }
    public int Tender { get; private set; }
    public float WaitTime { get; set; }
    public bool Entered { get; private set; }
    public bool Leaving { get; private set; }
    public bool Gone { get; private set; }
    private int exitStep;
    private static readonly Vector3 Inside = new Vector3(0.13f, 0f, -3.8f);
    private static readonly Vector3 Entry = new Vector3(0.13f, 0f, -4.9f);
    public void Initialize(int person, int product, int visit)
    {
        Person = person; Product = product;
        Tender = visit % 3 == 1 ? ShopCatalog.Prices[product] : product == 2 ? 1000 : 500;
        transform.position = Entry;
        transform.rotation = Quaternion.identity;
        CharacterController body = GetComponent<CharacterController>();
        if (body != null) body.enabled = false;
        var old = GetComponent<CustomerOrderController>();
        if (old != null) old.enabled = false;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.name != "Body") continue;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block); block.SetColor("_BaseColor", ShopPeople.Colors[person]); renderer.SetPropertyBlock(block);
        }
        gameObject.name = "Vásárló - " + ShopPeople.Names[person];
    }
    public bool Advance(float dt, Vector3 queueTarget)
    {
        if (Gone) return true;
        if (Leaving)
        {
            Vector3 target = exitStep == 0 ? new Vector3(1.35f,0,-3.2f) : exitStep == 1 ? new Vector3(1.35f,0,-3.85f) : Entry;
            if (Move(target, dt)) { exitStep++; if (exitStep >= 3) Gone = true; }
            return Gone;
        }
        if (!Entered) { if (Move(Inside, dt)) Entered = true; return false; }
        return Move(queueTarget, dt);
    }
    public void Leave() { Leaving = true; exitStep = 0; }
    private bool Move(Vector3 target, float dt)
    {
        Vector3 delta = target - transform.position;
        if (delta.sqrMagnitude < .0025f) return true;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(delta), 360f * dt);
        transform.position = Vector3.MoveTowards(transform.position, target, 1.3f * dt);
        return (transform.position - target).sqrMagnitude < .0025f;
    }
}
