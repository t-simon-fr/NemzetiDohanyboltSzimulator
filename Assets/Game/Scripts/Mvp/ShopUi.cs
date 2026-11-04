using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Shared presentation only. All money, inventory and phase transitions stay in the shop.
public static class ShopUi
{
    public static readonly Color Paper = new Color(.94f, .91f, .83f);
    public static readonly Color Ink = new Color(.14f, .20f, .17f);
    public static readonly Color Green = new Color(.12f, .24f, .20f);
    public static readonly Color Muted = new Color(.37f, .42f, .36f);
    public static readonly Color Red = new Color(.52f, .19f, .19f);
    public static readonly Color Rule = new Color(.74f, .75f, .66f);
    private static readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();
    private static readonly Dictionary<Color, Texture2D> textures = new Dictionary<Color, Texture2D>();

    public static float Scale => Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
    public static Vector2 Offset => new Vector2((Screen.width - 1280 * Scale) * .5f, (Screen.height - 720 * Scale) * .5f);
    public static Rect ScreenRect(Rect rect) => new Rect(Offset + rect.position * Scale, rect.size * Scale);
    public static IDisposable Frame() => new GuiFrame();

    private sealed class GuiFrame : IDisposable
    {
        private readonly Matrix4x4 matrix = GUI.matrix;
        private readonly Color color = GUI.color;
        public GuiFrame()
        {
            GUI.matrix = Matrix4x4.TRS(Offset, Quaternion.identity, Vector3.one * Scale);
            GUI.color = Color.white;
        }
        public void Dispose() { GUI.matrix = matrix; GUI.color = color; }
    }

    public static GUIStyle Text(int size = 18, bool bold = false, bool light = false, TextAnchor align = TextAnchor.UpperLeft)
    {
        string key = size + ":" + bold + ":" + light + ":" + align;
        if (!styles.TryGetValue(key, out var style))
        {
            style = new GUIStyle(GUI.skin.label) {
                fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                wordWrap = true, alignment = align, padding = new RectOffset(0, 0, 0, 0)
            };
            style.normal.textColor = light ? Paper : Ink;
            style.hover.textColor = style.normal.textColor;
            style.active.textColor = style.normal.textColor;
            style.focused.textColor = style.normal.textColor;
            styles[key] = style;
        }
        return style;
    }

    private static Texture2D Solid(Color color)
    {
        if (!textures.TryGetValue(color, out var texture))
        {
            texture = new Texture2D(1, 1) { name = "Trafik UI", hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            textures[color] = texture;
        }
        return texture;
    }

    public static GUIStyle ButtonStyle(int size = 18, bool primary = false)
    {
        string key = "button:" + size + ":" + primary;
        if (!styles.TryGetValue(key, out var style))
        {
            style = new GUIStyle(GUI.skin.button) {
                fontSize = size, wordWrap = true, fontStyle = FontStyle.Bold,
                padding = new RectOffset(12, 12, 4, 4), border = new RectOffset()
            };
            style.normal.background = Solid(primary ? Green : new Color(.84f, .85f, .77f));
            style.normal.textColor = primary ? Paper : Ink;
            style.hover.background = Solid(primary ? new Color(.20f, .35f, .28f) : new Color(.77f, .80f, .70f));
            style.hover.textColor = style.normal.textColor;
            style.focused.background = Solid(Red);
            style.focused.textColor = Paper;
            style.active.background = Solid(Red);
            style.active.textColor = Paper;
            styles[key] = style;
        }
        return style;
    }

    public static bool Button(Rect rect, string label, bool primary = false) => GUI.Button(rect, label, ButtonStyle(18, primary));
    public static void Fill(Rect rect, Color color)
    {
        var previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    public static void Panel(Rect rect, bool dark = false)
    {
        Fill(new Rect(rect.x + 3, rect.y + 4, rect.width, rect.height), new Color(0, 0, 0, .20f));
        Fill(rect, dark ? Green : Paper);
        Fill(new Rect(rect.x, rect.y, rect.width, 3), dark ? Rule : Red);
    }

    public static void Label(Rect rect, string text, int size = 18, bool bold = false, bool light = false)
        => GUI.Label(rect, text, Text(size, bold, light));

    public static string Key(Key key)
    {
        var resolved = ShopBindings.Resolve(key);
        return resolved == UnityEngine.InputSystem.Key.Escape ? "Esc" : resolved.ToString();
    }

    public static string BoundText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        // Replace whole key labels, never letters inside Hungarian words.
        return System.Text.RegularExpressions.Regex.Replace(text, @"\b([WASDEGQBFXTH])(?=\s*[–:•/=-]|\s+a\s|\s+az\s|\s+vagy\s)",
            match => Enum.TryParse(match.Value, out Key key) ? Key(key) : match.Value);
    }
}
