using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ShopCoffeeMachineModelBuilder
{
    private const string Folder = "Assets/Game/Resources/Equipment/CoffeeMachine";
    [Serializable] private sealed class Model { public Part[] parts; }
    [Serializable] private sealed class Part
    {
        public string name;
        public Color color;
        public Vector3[] vertices, normals;
        public int[] triangles;
    }

    public static string Generate()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var model = JsonUtility.FromJson<Model>(File.ReadAllText("ArtSource/CoffeeMachine/CoffeeMachine.mesh.json"));
        var root = new GameObject("LaVazze Barista");
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
                item.transform.SetParent(root.transform, false);
                item.AddComponent<MeshFilter>().sharedMesh = mesh;
                item.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/CoffeeMachine.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
        return "LaVazze Barista prefab generated; scene unchanged.";
    }
}
