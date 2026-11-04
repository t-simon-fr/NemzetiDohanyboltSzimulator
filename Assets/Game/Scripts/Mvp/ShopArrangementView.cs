using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed partial class ShopArrangement
{
    private Camera arrangementCamera;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private Rect savedCameraRect;
    private bool savedOrthographic;
    private float savedSize;
    private readonly Dictionary<Renderer, bool> hidden = new Dictionary<Renderer, bool>();
    private ShopFurniture selected;
    private ShopFurniturePose preview;
    private bool dragging, previewValid;
    private string previewReason = "";
    private GUIStyle editorTitle, editorText, editorButton, planLabel;
    private float Sidebar => Mathf.Clamp(Screen.width * .25f, 260, 360);
    public string SelectedFurnitureId => selected == null ? "" : selected.Id;
    public bool Dragging => dragging;
    public bool PreviewValid => previewValid;
    public string PreviewFeedback => previewReason;

    partial void BeginView()
    {
        arrangementCamera = player.GetComponentInChildren<Camera>();
        savedCameraPosition = arrangementCamera.transform.localPosition;
        savedCameraRotation = arrangementCamera.transform.localRotation;
        savedCameraRect = arrangementCamera.rect;
        savedOrthographic = arrangementCamera.orthographic;
        savedSize = arrangementCamera.orthographicSize;
        arrangementCamera.transform.SetPositionAndRotation(new Vector3(2, 12, -1.25f), Quaternion.Euler(90, 0, 0));
        arrangementCamera.orthographic = true;
        arrangementCamera.orthographicSize = 4.4f;
        selected = null;
        dragging = false;
        hidden.Clear();
        foreach (var renderer in FindObjectsByType<Renderer>())
        {
            bool ceiling = renderer.name.IndexOf("ceiling", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool movable = renderer.GetComponentInParent<ShopFurniture>() != null;
            var product = renderer.GetComponentInParent<ShopProduct>();
            bool stock = product != null && product.CurrentSlot != null && product.CurrentSlot.GetComponentInParent<ShopFurniture>() != null;
            if (!ceiling && !movable && !stock && renderer.bounds.min.y < 2.15f) continue;
            hidden[renderer] = renderer.enabled;
            renderer.enabled = false;
        }
    }

    partial void EndView()
    {
        foreach (var pair in hidden) if (pair.Key != null) pair.Key.enabled = pair.Value;
        hidden.Clear();
        if (arrangementCamera != null)
        {
            arrangementCamera.transform.localPosition = savedCameraPosition;
            arrangementCamera.transform.localRotation = savedCameraRotation;
            arrangementCamera.orthographic = savedOrthographic;
            arrangementCamera.orthographicSize = savedSize;
            arrangementCamera.rect = savedCameraRect;
        }
        selected = null;
        dragging = false;
    }

    public void Select(ShopFurniture item)
    {
        if (!Active || !draft.ContainsKey(item)) return;
        selected = item;
        preview = draft[item];
        dragging = true;
        ValidatePreview();
    }

    private void ValidatePreview()
    {
        var poses = new Dictionary<ShopFurniture, ShopFurniturePose>(draft) { [selected] = preview };
        previewValid = Validate(poses, out previewReason);
    }

    private void Update()
    {
        if (!Active) return;
        arrangementCamera.rect = new Rect(Sidebar / Screen.width, 0, 1 - Sidebar / Screen.width, 1);
        arrangementCamera.orthographicSize = Mathf.Max(4.4f, (ShopFloorPlan.PurchasedArea(shop.Layout.Expanded).width + 1) / arrangementCamera.aspect * .5f);
        if (!Application.isFocused || Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (dragging) { dragging = false; preview = draft[selected]; }
            else Cancel();
            return;
        }
        if (selected != null && (Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
        {
            int turn = Keyboard.current.qKey.wasPressedThisFrame ? -1 : 1;
            preview = new ShopFurniturePose(preview.Position, preview.QuarterTurns + turn);
            dragging = true;
            ValidatePreview();
        }
        if (!dragging || Mouse.current == null) return;
        Vector2 pointer = Mouse.current.position.ReadValue();
        if (pointer.x <= Sidebar || pointer.y >= Screen.height - 58) return;
        var plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = arrangementCamera.ScreenPointToRay(pointer);
        if (!plane.Raycast(ray, out float distance)) return;
        Vector3 position = ray.GetPoint(distance);
        position = new Vector3(Mathf.Round(position.x * 4) / 4, selected.DefaultPose.Position.y, Mathf.Round(position.z * 4) / 4);
        if ((position - preview.Position).sqrMagnitude < .001f) return;
        preview = new ShopFurniturePose(position, preview.QuarterTurns);
        ValidatePreview();
    }

    private Rect OnPlan(Rect world)
    {
        Vector3 a = arrangementCamera.WorldToScreenPoint(new Vector3(world.xMin, .02f, world.yMin));
        Vector3 b = arrangementCamera.WorldToScreenPoint(new Vector3(world.xMax, .02f, world.yMax));
        return Rect.MinMaxRect(Mathf.Min(a.x,b.x), Screen.height - Mathf.Max(a.y,b.y), Mathf.Max(a.x,b.x), Screen.height - Mathf.Min(a.y,b.y));
    }
    private static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
    private static void Outline(Rect rect, Color color)
    {
        Fill(new Rect(rect.x,rect.y,rect.width,2),color);
        Fill(new Rect(rect.x,rect.yMax-2,rect.width,2),color);
        Fill(new Rect(rect.x,rect.y,2,rect.height),color);
        Fill(new Rect(rect.xMax-2,rect.y,2,rect.height),color);
    }

    private void OnGUI()
    {
        if (!Active || arrangementCamera == null) return;
        GUI.depth = -500;
        if (editorTitle == null)
        {
            editorTitle = new GUIStyle(GUI.skin.label) { fontSize=22, fontStyle=FontStyle.Bold, wordWrap=true };
            editorText = new GUIStyle(GUI.skin.label) { fontSize=16, wordWrap=true };
            editorButton = new GUIStyle(GUI.skin.button) { fontSize=16, wordWrap=true };
            planLabel = new GUIStyle(editorText) { alignment=TextAnchor.MiddleCenter, fontStyle=FontStyle.Bold };
        }
        Rect area = ShopFloorPlan.PurchasedArea(shop.Layout.Expanded);
        Rect screenArea = OnPlan(area);
        Outline(screenArea, new Color(.5f,.8f,.75f));
        // Half-metre guide lines; placement still snaps to a quarter metre.
        for (float x = -2; x < area.xMax; x += .5f)
        {
            Rect line = OnPlan(new Rect(x,area.yMin,.008f,area.height));
            Fill(line,new Color(.8f,.9f,.85f,.16f));
        }
        for (float z = -4; z < area.yMax; z += .5f)
            Fill(OnPlan(new Rect(area.xMin,z,area.width,.008f)),new Color(.8f,.9f,.85f,.16f));
        foreach (Rect reserved in Protected) Fill(OnPlan(reserved),new Color(.5f,.25f,.16f,.2f));
        if (shop.Layout.Expanded)
            for (float z = -4.3f; z < 1.8f; z += .4f)
                Fill(OnPlan(new Rect(2.5f,z,.025f,.2f)),new Color(.9f,.8f,.45f,.8f));
        foreach (var pair in draft)
        {
            bool ghost = pair.Key == selected && dragging;
            Rect rect = OnPlan(pair.Key.BoundsAt(ghost ? preview : pair.Value));
            Color color = ghost ? previewValid ? new Color(.15f,.75f,.35f,.85f) : new Color(.85f,.2f,.15f,.9f) : new Color(.1f,.42f,.45f,.9f);
            Fill(rect,color);
            Outline(rect,Color.white);
            GUI.Label(rect,pair.Key.Id == "public-shelf" ? "Polc" : pair.Key.Title,planLabel);
        }
        Fill(new Rect(Sidebar+12,12,Screen.width-Sidebar-24,42),new Color(.035f,.06f,.07f,.96f));
        GUI.Label(new Rect(Sidebar+24,18,Screen.width-Sidebar-48,32),"ÜZLET ÁTRENDEZÉSE • Előnézet",editorTitle);
        Fill(new Rect(0,0,Sidebar,Screen.height),new Color(.035f,.06f,.07f,1));
        GUILayout.BeginArea(new Rect(16,16,Sidebar-32,Screen.height-32));
        GUILayout.Label("BERENDEZÉS",editorTitle);
        GUILayout.Label("A bolt szünetel. A pult, ajtók és a feltöltési útvonal fix.",editorText);
        GUILayout.Space(12);
        if (draft.Count == 0) GUILayout.Label("Még nincs átrendezhető berendezés. Az önkiszolgáló bővítéssel nyílik meg az első polc.",editorText);
        foreach (var item in draft.Keys)
            if (GUILayout.Button(item.Title + (selected==item ? " • KIJELÖLVE" : ""),editorButton,GUILayout.Height(44))) Select(item);
        GUILayout.Space(12);
        GUILayout.Label("Egér: pozíció\nBal kattintás: lerakás\nQ / E: forgatás\nJobb kattintás / Esc: előnézet elvetése\nRács: 0,25 m",editorText);
        if (selected != null)
        {
            GUILayout.Space(8);
            GUILayout.Label(selected.Title + " • " + preview.QuarterTurns*90 + "°",editorText);
        }
        GUILayout.FlexibleSpace();
        GUILayout.Label(dragging ? previewReason : Feedback,editorText);
        GUILayout.Space(12);
        GUI.enabled = !dragging;
        if (GUILayout.Button("ELRENDEZÉS MENTÉSE",editorButton,GUILayout.Height(44))) Save();
        GUI.enabled = true;
        if (GUILayout.Button("MÉGSE • eredeti elrendezés",editorButton,GUILayout.Height(44))) Cancel();
        GUILayout.EndArea();
        if (!Active) return;
        var input = Event.current;
        if (input.type != EventType.MouseDown || input.mousePosition.x <= Sidebar || input.mousePosition.y < 58) return;
        if (input.button == 1 && dragging) { dragging=false; preview=draft[selected]; input.Use(); return; }
        if (input.button != 0) return;
        if (dragging)
        {
            if (previewValid && TryPlace(selected,preview)) dragging=false;
        }
        else
        {
            foreach (var pair in draft)
                if (OnPlan(pair.Key.BoundsAt(pair.Value)).Contains(input.mousePosition)) { Select(pair.Key); break; }
        }
        input.Use();
    }
}
