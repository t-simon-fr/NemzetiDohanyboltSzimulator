using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed partial class ShopArrangement
{
    private Camera arrangementCamera;
    private ShopCameraSnapshot cameraSnapshot;
    private readonly Dictionary<Renderer, bool> hidden = new Dictionary<Renderer, bool>();
    private ShopFurniture selected;
    private ShopFurniturePose preview;
    private bool dragging, previewValid;
    private string previewReason = "";
    private GUIStyle planLabel;
    private Vector2 furnitureScroll;
    private float Sidebar => Mathf.Clamp(Screen.width * .25f, 260, 360);
    public string SelectedFurnitureId => selected == null ? "" : selected.Id;
    public bool Dragging => dragging;
    public bool PreviewValid => previewValid;
    public Vector3 PreviewPosition => preview.Position;
    public int PreviewQuarterTurns => preview.QuarterTurns;
    public string PreviewFeedback => previewReason;

    partial void BeginView()
    {
        arrangementCamera = player.GetComponentInChildren<Camera>();
        cameraSnapshot = new ShopCameraSnapshot(arrangementCamera);
        arrangementCamera.transform.SetPositionAndRotation(new Vector3((shop.Layout.WarehouseArea.xMin+shop.Layout.ShopArea.xMax)*.5f, 12, -1.25f), Quaternion.Euler(90, 0, 0));
        arrangementCamera.orthographic = true;
        arrangementCamera.orthographicSize = 4.4f;
        selected = null;
        shopping = false;
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
        ClearModelPreview();
        foreach (var pair in hidden) if (pair.Key != null) pair.Key.enabled = pair.Value;
        hidden.Clear();
        cameraSnapshot?.Restore();
        cameraSnapshot = null;
        selected = null;
        dragging = false;
    }

    public void Select(ShopFurniture item)
    {
        if (!Active || !draft.ContainsKey(item)) return;
        DiscardPurchaseGhost();
        selected = item;
        preview = draft[item];
        dragging = true;
        ValidatePreview();
    }

    private void ValidatePreview()
    {
        var poses = new Dictionary<ShopFurniture, ShopFurniturePose>(draft) { [selected] = preview };
        previewValid = Validate(poses, out previewReason);
        if (purchaseGhost != null && PurchaseUnavailable(purchaseKind) != "")
        {
            previewValid = false;
            previewReason = PurchaseUnavailable(purchaseKind);
        }
        UpdateModelPreview();
    }

    private void Update()
    {
        if (!Active) return;
        UpdateModelPreview();
        arrangementCamera.rect = new Rect(0, 90f / Screen.height, 1 - Sidebar / Screen.width, 1 - 148f / Screen.height);
        arrangementCamera.orthographicSize = Mathf.Max(4.4f, (Rect.MinMaxRect(shop.Layout.WarehouseArea.xMin,-4.3f,shop.Layout.ShopArea.xMax,1.9f).width + 1) / arrangementCamera.aspect * .5f);
        if (!Application.isFocused || Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (purchaseGhost != null) DiscardPurchaseGhost();
            else if (dragging) { dragging = false; preview = draft[selected]; }
            else Cancel();
            return;
        }
        if (selected != null && (Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.rKey.wasPressedThisFrame))
        {
            int turn = Keyboard.current.qKey.wasPressedThisFrame ? -1 : 1;
            preview = SnapPose(selected, new ShopFurniturePose(preview.Position, preview.QuarterTurns + turn));
            dragging = true;
            ValidatePreview();
        }
        if (!dragging || Mouse.current == null) return;
        Vector2 pointer = Mouse.current.position.ReadValue();
        if (pointer.x >= Screen.width - Sidebar || pointer.y >= Screen.height - 58 || pointer.y <= 90) return;
        var plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = arrangementCamera.ScreenPointToRay(pointer);
        if (!plane.Raycast(ray, out float distance)) return;
        Vector3 position = ray.GetPoint(distance);
        position = SnapToGuides(selected, new ShopFurniturePose(position, preview.QuarterTurns)).Position;
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

    public List<ShopKeyboardTarget> EditorControls()
    {
        var targets = new List<ShopKeyboardTarget>();
        float x = Screen.width - Sidebar + 12, width = Sidebar - 24;
        targets.Add(new ShopKeyboardTarget("Editor:Owned", "Saját bútorok", new Rect(x, 72, width * .5f - 3, 40), () => { shopping = false; furnitureScroll = Vector2.zero; }));
        targets.Add(new ShopKeyboardTarget("Editor:Catalog", "Vásárlás", new Rect(x + width * .5f + 3, 72, width * .5f - 3, 40), () => { shopping = true; furnitureScroll = Vector2.zero; }));
        if (shopping)
        {
            for (int i = 0; i < CatalogNames.Length; i++)
            {
                int kind = i;
                float y = 122 + i * 90 - furnitureScroll.y;
                if (y < 122 || y + 50 > Screen.height - 310) continue;
                targets.Add(new ShopKeyboardTarget("Editor:Buy:" + kind, CatalogNames[kind] + "\n" + ShopCatalog.Money(CatalogPrice(kind)),
                    new Rect(x, y, width - 20, 50), () => StartPurchase(kind), PurchaseUnavailable(kind) == ""));
            }
        }
        // The fixed actions stay reachable even with a long, scrolling furniture list.
        targets.Add(new ShopKeyboardTarget("Editor:Save", PlannedCount > 0 || SaleCount > 0 ? "Elszámolás és mentés" : "Elrendezés mentése",
            new Rect(Screen.width - 400, Screen.height - 62, 240, 46), () => Save()));
        targets.Add(new ShopKeyboardTarget("Editor:Cancel", "Mégse", new Rect(Screen.width - 148, Screen.height - 62, 136, 46), Cancel));
        if (selected != null)
        {
            targets.Add(new ShopKeyboardTarget("Editor:Rotate", "Forgatás • R", new Rect(x, Screen.height - 220, width, 38), () => {
                preview = SnapPose(selected, new ShopFurniturePose(preview.Position, preview.QuarterTurns + 1));
                dragging = true;
                ValidatePreview();
            }));
            if (purchaseGhost == null && !IsPlanned(selected) && selected.CatalogKind >= 0) targets.Add(new ShopKeyboardTarget("Editor:Sell", "Eladás • " + ShopCatalog.Money(SalePrice(selected)), new Rect(x, Screen.height - 176, width, 38), () => PlanSale(selected)));
            if (IsPlanned(selected)) targets.Add(new ShopKeyboardTarget("Editor:Remove", "Törlés a tervből", new Rect(x, Screen.height - 176, width, 38), () => RemovePlanned(selected)));
        }
        return targets;
    }

    private void OnGUI()
    {
        if (!Active || arrangementCamera == null) return;
        GUI.depth = -500;
        var text = ShopComputerTheme.Text(16);
        var title = ShopComputerTheme.Text(21, true);
        if (planLabel == null) planLabel = new GUIStyle(ShopComputerTheme.Text(12, true, true)) {
            alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip
        };
        Rect shopArea=shop.Layout.FurnitureArea(false), warehouseArea=shop.Layout.FurnitureArea(true);
        Rect area = Rect.MinMaxRect(warehouseArea.xMin, Mathf.Min(shopArea.yMin,warehouseArea.yMin), shopArea.xMax, Mathf.Max(shopArea.yMax,warehouseArea.yMax));
        Outline(OnPlan(area), new Color(.5f, .8f, .75f));
        DrawGrid(shopArea);
        DrawGrid(warehouseArea);
        foreach (Rect reserved in Protected) Fill(OnPlan(reserved), new Color(.5f, .25f, .16f, .2f));
        foreach (var pair in draft) DrawPlanFurniture(pair.Key, pair.Key == selected && dragging ? preview : pair.Value, pair.Key == selected && dragging);
        if (purchaseGhost != null) DrawPlanFurniture(purchaseGhost, preview, true);
        if (selected != null && dragging)
        {
            var bounds = selected.BoundsAt(preview);
            Fill(OnPlan(new Rect(bounds.xMin, area.yMin, .012f, area.height)), new Color(1,.75f,.1f,.6f));
            Fill(OnPlan(new Rect(area.xMin, bounds.yMin, area.width, .012f)), new Color(1,.75f,.1f,.6f));
        }

        Fill(new Rect(0, 0, Screen.width, 58), ShopComputerTheme.Green);
        GUI.Label(new Rect(20, 10, Screen.width - 240, 38), "TobaccOS / Üzlet átrendezése", ShopComputerTheme.Text(23, true, true));
        GUI.Label(new Rect(Screen.width - 220, 10, 200, 38), ShopCatalog.Money(shop.Ledger.AvailableFunds), ShopComputerTheme.Text(21, true, true));
        Fill(new Rect(Screen.width - Sidebar, 58, Sidebar, Screen.height - 148), ShopComputerTheme.Paper);
        float left = Screen.width - Sidebar + 12;
        if (shopping)
        {
            var viewport = new Rect(left, 122, Sidebar - 24, Screen.height - 432);
            furnitureScroll = GUI.BeginScrollView(viewport, furnitureScroll, new Rect(0, 0, Sidebar - 44, CatalogNames.Length * 90));
            for (int i = 0; i < CatalogNames.Length; i++)
            {
                string unavailable = PurchaseUnavailable(i);
                GUI.Label(new Rect(0, i * 90 + 54, Sidebar - 52, 34), unavailable == "" ? CatalogDetails[i] : unavailable, ShopComputerTheme.Text(14));
            }
            GUI.EndScrollView();
        }
        else
        {
            GUILayout.BeginArea(new Rect(left, 122, Sidebar - 24, Screen.height - 432));
            furnitureScroll = GUILayout.BeginScrollView(furnitureScroll);
            foreach (var item in draft.Keys.ToArray())
                if (GUILayout.Button(item.Title + " · " + item.Height.ToString("0.#") + (IsPlanned(item) ? " • ÚJ" : ""), ShopComputerTheme.Button(selected == item, 17), GUILayout.Height(44))) Select(item);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        GUI.Label(new Rect(left, Screen.height - 310, Sidebar - 24, 82), dragging ? SnapFeedback + "\n" + previewReason : Feedback, text);
        if (selected != null) GUI.Label(new Rect(left, Screen.height - 132, Sidebar - 24, 42), selected.Title + " · " + selected.Dimensions, text);
        Fill(new Rect(0, Screen.height - 90, Screen.width, 90), ShopComputerTheme.Paper);
        GUI.Label(new Rect(18, Screen.height - 85, Screen.width - 36, 24),
            "Vétel: " + ShopCatalog.Money(PlannedCost) + " · Eladás: " + ShopCatalog.Money(SaleCredit) + "   ·   Marad: " + ShopCatalog.Money(RemainingFunds), title);
        GUI.Label(new Rect(18, Screen.height - 51, Screen.width - 430, 42), "Rács: 10 cm · Kattintás: lerakás · R: forgatás · Esc: vissza", text);
        foreach (var target in EditorControls())
        {
            GUI.enabled = target.Enabled;
            if (GUI.Button(target.Bounds, target.Label, ShopComputerTheme.Button(target.Id == "Editor:Save" || target.Id == "Editor:Buy:" + purchaseKind || target.Id == (shopping ? "Editor:Catalog" : "Editor:Owned"), 17)))
            {
                target.Activate();
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;
        }
        if (!Active) return;
        var input = Event.current;
        if (input.type != EventType.MouseDown || input.mousePosition.x >= Screen.width - Sidebar || input.mousePosition.y < 58 || input.mousePosition.y >= Screen.height - 90) return;
        if (input.button == 1 && dragging)
        {
            if (purchaseGhost != null) DiscardPurchaseGhost();
            else { dragging = false; preview = draft[selected]; }
            input.Use();
            return;
        }
        if (input.button != 0) return;
        if (dragging)
        {
            if (previewValid)
            {
                if (purchaseGhost != null) PlacePurchase(preview);
                else if (TryPlace(selected, preview)) dragging = false;
            }
        }
        else
        {
            foreach (var pair in draft)
                if (OnPlan(pair.Key.BoundsAt(pair.Value)).Contains(input.mousePosition)) { Select(pair.Key); break; }
        }
        input.Use();
    }

    private void DrawPlanFurniture(ShopFurniture item, ShopFurniturePose pose, bool ghost)
    {
        Rect rect = OnPlan(item.BoundsAt(pose));
        Color color = ghost ? previewValid ? new Color(.15f, .65f, .3f, .65f) : new Color(.85f, .2f, .15f, .8f) :
            IsPlanned(item) ? new Color(.36f, .48f, .2f, .8f) : new Color(.1f, .42f, .45f, .9f);
        Color edge = ghost ? (previewValid ? Color.green : Color.red) : Color.white;
        if (item.CatalogKind == 5) DrawCornerFootprint(rect, pose.QuarterTurns, item != selected, color, edge);
        else { if (item != selected) Fill(rect, color); Outline(rect,edge); }
        DrawFrontArrow(item,pose,edge);
        GUI.Label(rect, ghost && purchaseGhost != null ? ShopCatalog.Money(CatalogPrice(purchaseKind)) :
            (item.InWarehouse ? "R" : item.CatalogKind == 4 ? "H" : item.CatalogKind == 5 ? "S" : "P") + "\n" + item.Height.ToString("0.#") + (IsPlanned(item) ? " +" : ""), planLabel);
    }

    private Texture2D cornerFootprint;

    private static void Segment(Vector2 a, Vector2 b, Color color)
    {
        var old = GUI.matrix;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg, a);
        Fill(new Rect(a.x, a.y, Vector2.Distance(a, b), 2), color);
        GUI.matrix = old;
    }

    private void DrawCornerFootprint(Rect rect, int turns, bool fill, Color color, Color edge)
    {
        if (cornerFootprint == null)
        {
            cornerFootprint = new Texture2D(64, 64) {
                filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave
            };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    cornerFootprint.SetPixel(x, y, new Color(1, 1, 1, (x + .5f) / 64f + (y + .5f) / 64f >= ShopModularFurniture.CornerCutFraction ? 1 : 0));
            cornerFootprint.Apply();
        }
        var old = GUI.matrix;
        GUIUtility.RotateAroundPivot(turns * 90, rect.center);
        if (fill)
        {
            var tint = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, cornerFootprint);
            GUI.color = tint;
        }
        GUI.matrix = old;
        var points = new[] {
            new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y), new Vector2(rect.xMax, rect.yMax),
            new Vector2(rect.x + rect.width * ShopModularFurniture.CornerCutFraction, rect.yMax), new Vector2(rect.x, rect.y + rect.height * (1 - ShopModularFurniture.CornerCutFraction))
        };
        float angle = turns * 90 * Mathf.Deg2Rad;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 delta = points[i] - rect.center;
            points[i] = rect.center + new Vector2(
                Mathf.Cos(angle) * delta.x - Mathf.Sin(angle) * delta.y,
                Mathf.Sin(angle) * delta.x + Mathf.Cos(angle) * delta.y);
        }
        for (int i = 0; i < points.Length; i++)
            Segment(points[i], points[(i + 1) % points.Length], edge);
    }

    private void DrawFrontArrow(ShopFurniture item, ShopFurniturePose pose, Color color)
    {
        Vector3 local = item.CatalogKind == 5 ? new Vector3(-1, 0, -1).normalized : Vector3.back;
        Vector2 centre = OnPlan(new Rect(pose.Position.x, pose.Position.z, 0, 0)).position;
        void Arrow(Vector3 direction)
        {
            var world = pose.Position + pose.Rotation * direction * .45f;
            Vector2 end = OnPlan(new Rect(world.x, world.z, 0, 0)).position;
            Vector2 start = Vector2.Lerp(centre, end, .55f);
            Segment(start, end, color);
            Vector2 delta = (end - start).normalized, side = new Vector2(-delta.y, delta.x);
            Segment(end, end - delta * 5 + side * 3, color);
            Segment(end, end - delta * 5 - side * 3, color);
        }
        Arrow(local);
        if (item.CatalogKind == 2) Arrow(-local);
    }

    private void DrawGrid(Rect room)
    {
        for (int axis = 0; axis < 2; axis++)
        {
            float min = axis == 0 ? room.xMin : room.yMin;
            float max = axis == 0 ? room.xMax : room.yMax;
            for (int tick = Mathf.CeilToInt(min / GridStep); tick <= Mathf.FloorToInt(max / GridStep); tick++)
            {
                bool major = tick % 5 == 0;

                float value = tick * GridStep;
                Rect line = OnPlan(axis == 0 ? new Rect(value, room.yMin, 0, room.height) : new Rect(room.xMin, value, room.width, 0));
                if (axis == 0) line.width = 1; else line.height = 1;
                Fill(line, new Color(.92f, .95f, .87f, major ? .3f : .08f));
            }
        }
    }
}
