using System.Collections.Generic;
using UnityEngine;

public sealed class ShopCoffeeDisplay : MonoBehaviour
{
    private ShopActivities activities;
    private MvpShopController shop;
    private TextMesh title, status, supplies, actionLabel;
    private readonly List<Material> materials = new List<Material>();
    private readonly List<ShopWorldControl> controls = new List<ShopWorldControl>();
    private readonly List<ShopWorldControl> recipeControls = new List<ShopWorldControl>();
    private readonly List<TextMesh> recipeLabels = new List<TextMesh>();
    private Material paper, green, disabled;
    public IReadOnlyList<ShopWorldControl> Controls => controls;
    public ShopWorldControl ActionControl => controls[3];
    public string DisplayStatus => status.text;
    public string DisplayRecipe => title.text;
    public string DisplaySupplies => supplies.text;

    public void Configure(ShopActivities owner, MvpShopController game, Transform machine)
    {
        activities = owner; shop = game;
        transform.SetParent(machine, false);
        transform.localPosition = new Vector3(0, 1.36f, -.155f);
        var bezel = Material("Coffee display bezel", new Color(.055f, .075f, .065f));
        paper = Material("Coffee display key", new Color(.79f, .80f, .72f));
        green = Material("Coffee display active", ShopUi.Green);
        disabled = Material("Coffee display disabled", new Color(.44f, .48f, .43f));
        var lcd = Material("Coffee display LCD", ShopUi.Paper);
        Box("Coffee display bezel", Vector3.zero, new Vector3(.44f, .36f, .036f), bezel);
        var screen = Box("Coffee display LCD", new Vector3(0, 0, -.022f), new Vector3(.416f, .336f, .008f), lcd);
        screen.GetComponent<Collider>().enabled = false;
        title = Text("Selected coffee", new Vector3(0, .146f, -.031f), .015f);
        title.fontStyle = FontStyle.Bold;
        status = Text("Coffee status", new Vector3(0, .114f, -.031f), .013f);
        status.fontStyle = FontStyle.Bold;
        supplies = Text("Coffee supplies", new Vector3(0, .073f, -.031f), .011f);
        string[] captions = { "RÖVID", "HOSSZÚ", "LATTE", "TEA", "FORRÓ CSOKI" };
        for (int i = 0; i < captions.Length; i++)
        {
            Vector3 p = new Vector3(i < 3 ? (i - 1) * .133f : (i - 3.5f) * .20f, i < 3 ? .008f : -.060f, -.034f);
            var button = Box("Coffee recipe " + captions[i], p, new Vector3(i < 3 ? .121f : .184f, .047f, .012f), paper);
            var control = button.AddComponent<ShopWorldControl>();
            control.Kind = WorldControlKind.CoffeeSelect;
            control.Value = ShopHotDrinkRecipes.Types[i];
            recipeControls.Add(control);
            if (i < 3) controls.Add(control);
            var text = Text("Recipe label", p + Vector3.back * .01f, .012f);
            text.text = captions[i];
            recipeLabels.Add(text);
        }
        var action = Box("Coffee action", new Vector3(0, -.137f, -.034f), new Vector3(.388f, .047f, .012f), green).AddComponent<ShopWorldControl>();
        action.Kind = WorldControlKind.CoffeeBrew;
        controls.Add(action);
        controls.AddRange(recipeControls.GetRange(3, 2));
        actionLabel = Text("Coffee action label", new Vector3(0, -.137f, -.044f), .013f);
        gameObject.AddComponent<ShopStation>().Configure(shop, ShopStationKind.CoffeeControl);
        Refresh();
    }
    private Material Material(string name, Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name, color = color };
        materials.Add(material);
        return material;
    }
    private GameObject Box(string name, Vector3 position, Vector3 size, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }
    private TextMesh Text(string name, Vector3 p, float size) => ProductVisuals.Text(transform, name, "", p, Quaternion.identity, size, Color.black);

    private void Update() => Refresh();
    public void Refresh()
    {
        if (activities == null || title == null) return;
        bool brewing = activities.BrewRemaining > 0;
        var cup = activities.CoffeeOutput.Occupant;
        bool ready = cup != null && !cup.IsHeld;
        bool enough = activities.CanPrepareDrink(activities.SelectedRecipe);
        title.text = ShopCatalog.ShortNames[activities.SelectedRecipe];
        status.text = brewing ? "FŐZÉS " + activities.BrewRemaining.ToString("0.0") + " MP" :
            ready ? "ELKÉSZÜLT: " + ShopCatalog.ShortNames[ShopCatalog.Index(cup.ProductId)] :
            cup != null ? "ITAL ÁTADÁS ALATT" :
            !enough ? activities.MissingDrinkIngredients : "KÉSZENLÉT";
        supplies.text = activities.CoffeeSupplies + " KÁVÉ / " + activities.MilkSupplies + " TEJ\n" +
            activities.LoadedIngredient(ShopDrinkIngredient.Tea) + " TEA / " + activities.LoadedIngredient(ShopDrinkIngredient.Cocoa) + " KAKAÓ";
        for (int i = 0; i < recipeControls.Count; i++)
        {
            bool selected = recipeControls[i].Value == activities.SelectedRecipe;
            recipeControls[i].Interactable = !brewing;
            recipeControls[i].GetComponent<Renderer>().sharedMaterial = brewing ? disabled : selected ? green : paper;
            recipeLabels[i].color = selected && !brewing ? ShopUi.Paper : Color.black;
        }
        ActionControl.Kind = cup != null ? WorldControlKind.CoffeeTake : WorldControlKind.CoffeeBrew;
        ActionControl.Interactable = !brewing && activities.CoffeeOwned && shop.Phase != ShopPhase.Summary && (ready || cup == null && enough);
        ActionControl.GetComponent<Renderer>().sharedMaterial = ActionControl.Interactable ? green : disabled;
        actionLabel.text = brewing ? "FŐZÉS FOLYAMATBAN" : cup != null ? "ÁTVESZEM AZ ITALT" : "FŐZÉS INDÍTÁSA";
        actionLabel.color = ActionControl.Interactable ? ShopUi.Paper : Color.black;
    }
    public void Focus(Camera camera)
    {
        float distance = .82f / (2 * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f));
        Vector3 target = transform.TransformPoint(new Vector3(0, -.16f, 0));
        camera.transform.position = target - transform.forward * distance;
        camera.transform.LookAt(target, transform.up);
    }
    public Rect Bounds(Camera camera, int index)
    {
        if (!gameObject.activeInHierarchy) return Rect.zero;
        var b = controls[index].GetComponent<Collider>().bounds;
        Vector2 min = Vector2.one * float.MaxValue, max = Vector2.one * float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var p = camera.WorldToScreenPoint(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
            if (p.z <= .01f) return Rect.zero;
            Vector2 point = new Vector2(p.x, Screen.height - p.y);
            min = Vector2.Min(min, point); max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    private void OnDestroy()
    {
        foreach (var material in materials) if (material != null) Destroy(material);
    }
}
