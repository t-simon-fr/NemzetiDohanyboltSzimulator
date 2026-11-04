using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class ShopRegisterModelBuilder
{
    private const string Folder = "Assets/Game/Resources/Equipment/ModernRegister";
    [Serializable] private sealed class Model { public Part[] parts; }
    [Serializable] private sealed class Part
    {
        public string name;
        public Color color;
        public Vector3[] vertices, normals;
        public int[] triangles;
    }

    public static void ImportTypography()
    {
        var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
        AssetDatabase.ImportPackage(Path.Combine(package.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage"), false);
    }

    public static string Generate()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var model = JsonUtility.FromJson<Model>(File.ReadAllText("ArtSource/Checkout/ModernRegister.mesh.json"));
        var root = new GameObject("Modern Cash Register");
        try
        {
            foreach (var part in model.parts)
            {
                var mesh = new Mesh { name = part.name, vertices = part.vertices, normals = part.normals, triangles = part.triangles };
                mesh.RecalculateBounds();
                string meshPath = Folder + "/" + part.name + ".asset";
                if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null) AssetDatabase.DeleteAsset(meshPath);
                AssetDatabase.CreateAsset(mesh, meshPath);
                bool lcd = part.name == "LCD_Glass";
                var material = new Material(Shader.Find(lcd ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")) { name = part.name, color = part.color };
                if (!lcd) { material.SetFloat("_Smoothness", .22f); material.SetFloat("_Metallic", part.name.Contains("Handle") ? .45f : 0); }
                string materialPath = Folder + "/" + part.name + ".mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null) AssetDatabase.DeleteAsset(materialPath);
                AssetDatabase.CreateAsset(material, materialPath);
                if (part.name == "Keycap_Template") continue;
                var item = new GameObject(part.name);
                item.transform.SetParent(root.transform, false);
                item.AddComponent<MeshFilter>().sharedMesh = mesh;
                item.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/ModernRegister.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }

        const string fontPath = Folder + "/RegisterFont.asset";
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath) == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            if (source == null) throw new Exception("Import TMP Essential Resources before generating register typography.");
            var font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
            font.name = "Register Liberation Sans SDF";
            string characters = "";
            for (int code = 32; code < 256; code++) characters += (char)code;
            characters += "ŐőŰű–—•×";
            if (!font.TryAddCharacters(characters, out string missing)) Debug.Log("Register font unsupported glyphs: " + missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            font.material.shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            AssetDatabase.CreateAsset(font, fontPath);
            font.material.name = "Register SDF material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var texture in font.atlasTextures)
            {
                texture.name = "Register SDF atlas";
                AssetDatabase.AddObjectToAsset(texture, font);
            }
            EditorUtility.SetDirty(font);
        }
        AssetDatabase.SaveAssets();
        return "Original register prefab and SDF font generated; scene unchanged.";
    }
}
