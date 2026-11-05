using UnityEngine;

public sealed class ShopToolRack : MonoBehaviour
{
    public ShopHandTool Tool { get; private set; }
    private Transform homeParent;
    private Pose home;
    public string Prompt => "E / bal kattintás – Visszarakás: " + Tool.DisplayName;

    public void Configure(ShopHandTool tool)
    {
        Tool = tool;
        homeParent = tool.transform.parent;
        home = new Pose(tool.transform.position,tool.transform.rotation);
        transform.SetParent(homeParent,false);
        transform.SetPositionAndRotation(home.position,home.rotation);
        var target = gameObject.AddComponent<BoxCollider>();
        target.isTrigger = true;
        target.center = Vector3.up*.55f;
        target.size = new Vector3(.45f,1.25f,.35f);
        tool.Rack = this;
    }

    public bool TryStore(ShopHandTool candidate)
    {
        if (candidate != Tool || !candidate.IsHeld) return false;
        Restore();
        return true;
    }

    internal void Restore() => Tool.ReturnToRackPose(homeParent,home);
}
