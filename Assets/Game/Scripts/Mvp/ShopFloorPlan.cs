using System.Linq;
using UnityEngine;

// Runtime migration preserves the authored scene and its materials/fixtures.
// Coordinates are shared by furnishing validation, navigation and interaction tests.
public sealed class ShopFloorPlan : MonoBehaviour
{
    public const float EntranceX = 1.2f;
    public static readonly Vector3 StaffSpawn = new Vector3(-1.4f, .05f, -2.3f);
    public static readonly Vector3 StaffExit = new Vector3(-1.35f, 0, .15f);
    public static readonly Vector3 CustomerPassage = new Vector3(.9f, 0, .15f);
    public static Rect PurchasedArea(bool expanded) => Rect.MinMaxRect(-2.4f, -4.3f, expanded ? 6.4f : 2.4f, 1.8f);
    private Transform[] authored;

    public Transform Find(string title) => authored.FirstOrDefault(t => t != null && t.name == title);

    public void Configure(MvpShopController shop, PlayerInteractor player)
    {
        authored = gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        Find("Counter Assembly").SetPositionAndRotation(new Vector3(-.35f, .55f, -2.25f), Quaternion.Euler(0, -90, 0));
        Find("Cigarette rack").SetPositionAndRotation(new Vector3(-2.18f, 0, -2.2f), Quaternion.Euler(0, -90, 0));
        Find("Extra Cigarette Shelf").SetPositionAndRotation(new Vector3(1.25f, 0, 1.48f), Quaternion.identity);
        var tobaccoSign = Find("Tobacco sign");
        if (tobaccoSign != null)
            tobaccoSign.SetPositionAndRotation(new Vector3(-2.38f, 2.35f, -2.1f), Quaternion.Euler(0, -90, 0));

        // Door opening: [0.4, 2.0], entirely inside the starter shop [-2.5, 2.5].
        ResizeWall("Front wall right", 2.25f, .5f);
        // Left wall ends at x=.4: centre (-2.5+.4)/2, width .4-(-2.5).
        ResizeWall("Front wall left", -1.05f, 2.9f);
        Find("Door lintel").position = new Vector3(EntranceX, 2.7f, -4.4f);
        Find("Open Close Sign").position = new Vector3(.05f, 1.5f, -4.25f);
        var hours = Find("Opening hours card");
        if (hours != null) hours.position = new Vector3(-.65f, 1.62f, -4.3f);
        var hoursText = Find("Opening hours");
        if (hoursText != null) hoursText.position = new Vector3(-.65f, 1.62f, -4.289f);
        Find("Entrance outside").position = new Vector3(EntranceX, 0, -4.95f);
        Find("Entrance inside").position = new Vector3(EntranceX, 0, -3.55f);
        Find("Exit doorway").position = new Vector3(EntranceX, 0, -4.05f);
        Find("Public aisle gate").position = new Vector3(1.4f, 0, -.2f);

        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.SetPositionAndRotation(StaffSpawn, Quaternion.Euler(0, 90, 0));
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    private void ResizeWall(string title, float x, float width)
    {
        var wall = Find(title);
        wall.position = new Vector3(x, wall.position.y, wall.position.z);
        wall.localScale = new Vector3(width, wall.localScale.y, wall.localScale.z);
    }
}
