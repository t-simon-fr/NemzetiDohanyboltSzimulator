using System.Collections.Generic;
using UnityEngine;

// Normalized packaging preserves the existing product collider and carry poses.
public static class ShopDnyiproVisuals
{
    private sealed class Print
    {
        public readonly Material Navy, Paper, Blue, Yellow, Cord;
        private readonly List<Material> materials = new List<Material>();

        public Print(GameObject owner)
        {
            Navy = Ink("Dnyipro navy", new Color(.075f, .105f, .17f));
            Paper = Ink("Dnyipro paper", new Color(.76f, .70f, .57f));
            Blue = Ink("Dnyipro blue", new Color(.13f, .28f, .46f));
            Yellow = Ink("Dnyipro yellow", new Color(.84f, .59f, .12f));
            Cord = Ink("Dnyipro cord", new Color(.48f, .36f, .20f));
            owner.AddComponent<ShopProductMaterials>().Materials = materials.ToArray();
        }

        private Material Ink(string name, Color color)
        {
            var value = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            value.SetFloat("_Smoothness", .12f);
            materials.Add(value);
            return value;
        }
    }

    public static void Pack(GameObject owner)
    {
        var print = new Print(owner);
        owner.GetComponent<Renderer>().sharedMaterial = print.Navy;
        var front = Face(owner.transform, "Dnyipro front", new Vector3(0, 0, -.503f), 0);
        var back = Face(owner.transform, "Dnyipro back", new Vector3(0, 0, .503f), 180);
        Label(front, print, false);
        Label(back, print, false);
        // Thin exposed paper seams and a lid crease keep the silhouette compact.
        Box(owner.transform, "Lid crease", new Vector3(0, .28f, -.506f), new Vector3(.98f, .008f, .004f), print.Cord);
        for (int edge = -1; edge <= 1; edge += 2)
            Box(owner.transform, "Worn paper edge", new Vector3(edge * .495f, 0, -.507f), new Vector3(.008f, .93f, .004f), print.Paper);
        var mark = ProductVisuals.Text(back, "Batch mark", "D-48 / 20 ШТ.", new Vector3(0, -.36f, -.01f), Quaternion.identity, .075f, print.Paper.color);
        mark.fontStyle = FontStyle.Italic;
    }

    public static void Carton(Transform shell)
    {
        var print = new Print(shell.gameObject);
        shell.GetComponent<Renderer>().sharedMaterial = print.Navy;
        Label(Face(shell, "Dnyipro carton print", new Vector3(0, 0, -.503f), 0), print, true);
        Label(Face(shell, "Dnyipro carton reverse", new Vector3(0, 0, .503f), 180), print, true);
        foreach (float x in new[] { -.36f, .36f })
        {
            Box(shell, "Twine front", new Vector3(x, 0, -.515f), new Vector3(.014f, 1.025f, .018f), print.Cord);
            Box(shell, "Twine back", new Vector3(x, 0, .515f), new Vector3(.014f, 1.025f, .018f), print.Cord);
            Box(shell, "Twine top", new Vector3(x, .511f, 0), new Vector3(.014f, .022f, 1.04f), print.Cord);
            Box(shell, "Twine bottom", new Vector3(x, -.511f, 0), new Vector3(.014f, .022f, 1.04f), print.Cord);
        }
        var sticker = Face(shell, "Handwritten inventory sticker", new Vector3(-.28f, .32f, -.532f), 0);
        sticker.localRotation = Quaternion.Euler(0, 0, 7);
        Box(sticker, "Paper", Vector3.zero, new Vector3(.26f, .18f, .004f), print.Paper);
        var text = ProductVisuals.Text(sticker, "Inventory mark", "D-48", new Vector3(0, 0, -.005f), Quaternion.identity, .11f, print.Blue.color);
        text.fontStyle = FontStyle.Italic;
    }

    private static void Label(Transform face, Print print, bool carton)
    {
        Box(face, "Cream label", Vector3.zero, new Vector3(.98f, .61f, .004f), print.Paper);
        var brand = ProductVisuals.Text(face, "Dnyipro wordmark", "DNYIPRO", new Vector3(0, .055f, -.007f), Quaternion.identity, .23f, print.Navy.color);
        brand.fontStyle = FontStyle.BoldAndItalic;
        ProductVisuals.Text(face, "Filter print", "— FILTER CIGARETTES —", new Vector3(0, -.105f, -.007f), Quaternion.identity, .065f, print.Navy.color);
        for (int i = 0; i < 10; i++)
        {
            float x = -.45f + i * .1f;
            float y = -.29f + i * .015f;
            Box(face, "Blue wrap stripe", new Vector3(x, y, -.009f), new Vector3(.103f, .06f, .004f), print.Blue);
            Box(face, "Yellow wrap stripe", new Vector3(x, y - .06f, -.009f), new Vector3(.103f, .045f, .004f), print.Yellow);
        }
        if (!carton)
            ProductVisuals.Text(face, "Pack size", "20 SZÁL · 18+", new Vector3(0, -.43f, -.01f), Quaternion.identity, .07f, print.Paper.color);
    }

    private static Transform Face(Transform owner, string name, Vector3 position, float yaw)
    {
        var face = new GameObject(name).transform;
        face.SetParent(owner, false);
        face.localPosition = position;
        face.localRotation = Quaternion.Euler(0, yaw, 0);
        return face;
    }

    private static void Box(Transform owner, string name, Vector3 position, Vector3 size, Material material)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(owner, false);
        box.transform.localPosition = position;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = material;
        var collider = box.GetComponent<Collider>();
        collider.enabled = false;
        Object.Destroy(collider);
    }
}
