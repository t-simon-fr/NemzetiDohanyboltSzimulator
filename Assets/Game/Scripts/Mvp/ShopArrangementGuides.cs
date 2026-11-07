using System.Linq;
using UnityEngine;

public sealed partial class ShopArrangement
{
    public string SnapFeedback { get; private set; } = "Rácshoz illesztve · 10 cm";
    public bool ModelPreviewVisible => modelPreview != null && modelPreview.GetComponentsInChildren<Renderer>().Any(r => r.enabled);
    private GameObject modelPreview;
    private ShopFurniture modelSource;

    public ShopFurniturePose SnapToGuides(ShopFurniture item, ShopFurniturePose pose)
    {
        pose = SnapPose(item, pose);
        Rect room = shop.Layout.FurnitureArea(item.InWarehouse);
        var half = item.BoundsAt(pose).size * .5f;
        float x = pose.Position.x, z = pose.Position.z;
        float bestX = .22f, bestZ = .22f;
        bool wall = false, neighbour = false;
        void Align(float value, bool horizontal, bool isWall)
        {
            float delta = Mathf.Abs(value - (horizontal ? pose.Position.x : pose.Position.z));
            if (delta >= (horizontal ? bestX : bestZ)) return;
            if (horizontal) { x = value; bestX = delta; } else { z = value; bestZ = delta; }
            if (isWall) wall = true; else neighbour = true;
        }
        Align(room.xMin + half.x, true, true); Align(room.xMax - half.x, true, true);
        Align(room.yMin + half.y, false, true); Align(room.yMax - half.y, false, true);
        var poses = Active ? draft : Current();
        foreach (var pair in poses.Where(p => p.Key != item && p.Key.InWarehouse == item.InWarehouse))
        {
            Rect other = pair.Key.BoundsAt(pair.Value);
            Rect bounds = item.BoundsAt(pose);
            if (bounds.yMin < other.yMax + .1f && bounds.yMax > other.yMin - .1f)
            {
                Align(other.xMin - half.x, true, false); Align(other.xMax + half.x, true, false);
            }
            if (bounds.xMin < other.xMax + .1f && bounds.xMax > other.xMin - .1f)
            {
                Align(other.yMin - half.y, false, false); Align(other.yMax + half.y, false, false);
            }
        }
        pose.Position = new Vector3(x, pose.Position.y, z);
        SnapFeedback = wall ? "Falhoz illesztve" : neighbour ? "Bútorhoz illesztve" : "Rácshoz illesztve · 10 cm";
        return pose;
    }

    private void UpdateModelPreview()
    {
        if (!object.ReferenceEquals(modelSource, selected))
        {
            ClearModelPreview();
            modelSource = selected;
            if (selected == null) return;
            modelPreview = new GameObject("Arrangement visual preview");
            foreach (var mesh in selected.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = mesh.GetComponent<MeshRenderer>();
                if (renderer == null || mesh.sharedMesh == null) continue;
                var part = new GameObject("Preview mesh");
                part.transform.SetParent(modelPreview.transform, false);
                part.transform.localPosition = selected.transform.InverseTransformPoint(mesh.transform.position);
                part.transform.localRotation = Quaternion.Inverse(selected.transform.rotation) * mesh.transform.rotation;
                part.transform.localScale = mesh.transform.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                part.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            }
        }
        if (modelPreview != null) modelPreview.transform.SetPositionAndRotation(preview.Position, preview.Rotation);
    }

    private void ClearModelPreview()
    {
        if (modelPreview != null) { modelPreview.SetActive(false); Destroy(modelPreview); }
        modelPreview = null;
        modelSource = null;
    }
}
