using System.Collections.Generic;
using UnityEngine;

// Computer-only presentation; the in-game HUD keeps its own visual hierarchy.
public static class ShopComputerTheme
{
    public static readonly Color Brown = new Color(.28f, .15f, .10f);
    public static readonly Color Paper = new Color(.95f, .92f, .84f);
    public static readonly Color Green = new Color(.08f, .27f, .18f);
    public static readonly Color Red = new Color(.63f, .16f, .12f);
    public static readonly Color Muted = new Color(.46f, .43f, .36f);
    public static readonly Color Rule = new Color(.80f, .77f, .68f);
    private static readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();

    public static GUIStyle Text(int size = 22, bool bold = false, bool light = false,
        TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        string key = $"text:{size}:{bold}:{light}:{alignment}";
        if (!styles.TryGetValue(key, out var style))
        {
            style = new GUIStyle(GUI.skin.label) {
                fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                alignment = alignment, wordWrap = true, padding = new RectOffset()
            };
            foreach (var state in new[] { style.normal, style.hover, style.active, style.focused,
                style.onNormal, style.onHover, style.onActive, style.onFocused })
                state.textColor = light ? Paper : Brown;
            styles[key] = style;
        }
        return style;
    }

    public static GUIStyle Button(bool primary = false, int size = 21)
    {
        string key = $"button:{primary}:{size}";
        if (!styles.TryGetValue(key, out var style))
        {
            style = new GUIStyle(ShopUi.ButtonStyle(size, primary));
            style.normal.textColor = primary ? Paper : Brown;
            style.hover.textColor = primary ? Paper : Brown;
            style.normal.background = Swatch(primary ? Green : new Color(.88f, .85f, .77f));
            style.hover.background = Swatch(primary ? new Color(.14f, .35f, .24f) : new Color(.82f, .79f, .70f));
            style.focused.background = Swatch(Brown);
            style.focused.textColor = Paper;
            style.active.background = Swatch(Green);
            style.active.textColor = Paper;
            styles[key] = style;
        }
        return style;
    }

    public static GUIStyle Field(int size = 28)
    {
        string key = "field:" + size;
        if (!styles.TryGetValue(key, out var style))
        {
            style = new GUIStyle(GUI.skin.textField) {
                fontSize = size, alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 4, 4), border = new RectOffset()
            };
            foreach (var state in new[] { style.normal, style.focused, style.hover, style.active })
            {
                state.background = Swatch(new Color(.99f, .97f, .91f));
                state.textColor = Brown;
            }
            styles[key] = style;
        }
        return style;
    }

    private static Texture2D Swatch(Color color)
    {
        var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    public static void Label(Rect area, string text, int size = 22, bool bold = false,
        bool light = false, TextAnchor alignment = TextAnchor.MiddleLeft)
        => GUI.Label(area, text, Text(size, bold, light, alignment));
}
