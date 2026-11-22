using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Builds the scripted Pisti customer model from ArtSource/Characters/Pisti.mesh.json
// into meshes, materials and a part hierarchy prefab. Mirrors the coffee machine
// and register builders: Blender owns the shape, Unity owns pivots and assembly.
// Each part GameObject sits at its animation pivot; the rig rotates them.
public static class ShopPistiModelBuilder
{
    private const string Folder = "Assets/Game/Resources/Customers/Pisti";
    [Serializable] private sealed class Model { public Part[] parts; }
    [Serializable] private sealed class Part
    {
        public string name, parent;
        public Vector3 pivot;
        public Color color;
        public Vector3[] vertices, normals;
        public int[] triangles;
    }

    public static string Generate()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var model = JsonUtility.FromJson<Model>(File.ReadAllText("ArtSource/Characters/Pisti.mesh.json"));
        var pivots = new Dictionary<string, Vector3>();
        foreach (var part in model.parts) pivots[part.name] = part.pivot;
        var built = new Dictionary<string, GameObject>();
        var root = new GameObject("Pisti");
        try
        {
            foreach (var part in model.parts)
            {
                var mesh = new Mesh { name = part.name, vertices = part.vertices, normals = part.normals, triangles = part.triangles };
                mesh.RecalculateBounds();
                string meshPath = Folder + "/" + part.name + ".asset";
                if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null) AssetDatabase.DeleteAsset(meshPath);
                AssetDatabase.CreateAsset(mesh, meshPath);
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = part.name, color = part.color };
                material.SetFloat("_Smoothness", .3f);
                string materialPath = Folder + "/" + part.name + ".mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null) AssetDatabase.DeleteAsset(materialPath);
                AssetDatabase.CreateAsset(material, materialPath);
                var item = new GameObject(part.name);
                if (part.parent == "ROOT")
                {
                    item.transform.SetParent(root.transform, false);
                    item.transform.localPosition = part.pivot;
                }
                else
                {
                    if (!built.TryGetValue(part.parent, out var parent)) throw new Exception("Unknown Pisti parent: " + part.parent);
                    item.transform.SetParent(parent.transform, false);
                    item.transform.localPosition = part.pivot - pivots[part.parent];
                }
                item.AddComponent<MeshFilter>().sharedMesh = mesh;
                item.AddComponent<MeshRenderer>().sharedMaterial = material;
                if (part.name == "MouthSmile" || part.name == "MouthPanic" || part.name == "MouthGrim") item.SetActive(false);
                built[part.name] = item;
            }
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/Pisti.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
        return "Pisti prefab generated with " + model.parts.Length + " parts; scene unchanged.";
    }
}
