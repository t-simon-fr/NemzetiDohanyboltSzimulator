using UnityEngine;
using UnityEngine.InputSystem;

public enum CustomerVisitState { Arriving, WaitingForProduct, Thanking, Leaving, Completed }

[DisallowMultipleComponent]
public sealed class CustomerOrderController : MonoBehaviour
{
    [SerializeField] private CharacterController character;
    [SerializeField] private GameObject visual;
    [SerializeField] private Transform entrance;
    [SerializeField] private Transform servicePoint;
    [SerializeField] private ProductSlot deliverySlot;
    [SerializeField] private string requestedProductId = "water";
    [SerializeField] private string requestedProductName = "Buborék ásványvíz";
    [SerializeField, Min(0.1f)] private float walkSpeed = 1.3f;
    private float stateTime;
    private float feedbackTime;
    private string dialogue;
    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    public CustomerVisitState State { get; private set; }
    public int CompletedOrders { get; private set; }
    public int WrongAttempts { get; private set; }
    public string Dialogue => dialogue;
    public ProductSlot DeliverySlot => deliverySlot;

    public void Configure(CharacterController body, GameObject model, Transform entry, Transform counter, ProductSlot slot)
    {
        character = body; visual = model; entrance = entry; servicePoint = counter; deliverySlot = slot;
    }
    private void OnEnable() { if (deliverySlot != null) deliverySlot.ProductPlaced += OnProductPlaced; }
    private void OnDisable() { if (deliverySlot != null) deliverySlot.ProductPlaced -= OnProductPlaced; }
    private void Start() => BeginVisit();

    public void BeginVisit()
    {
        if (character == null || entrance == null || servicePoint == null || deliverySlot == null)
        {
            Debug.LogError("Customer visit needs a character, waypoints and delivery slot.", this);
            enabled = false;
            return;
        }
        character.enabled = false;
        transform.SetPositionAndRotation(entrance.position, entrance.rotation);
        character.enabled = true;
        if (visual != null) visual.SetActive(true);
        State = CustomerVisitState.Arriving;
        stateTime = 0f; feedbackTime = 0f; WrongAttempts = 0;
        dialogue = "Józsi belép a boltba…";
    }
    private void Update()
    {
        Tick(Time.deltaTime);
        if (State == CustomerVisitState.Completed && Application.isFocused && Cursor.lockState == CursorLockMode.Locked && Keyboard.current != null && ShopBindings.Pressed(UnityEngine.InputSystem.Key.R)) BeginVisit();
    }
    public void Tick(float deltaTime)
    {
        if (character == null || entrance == null || servicePoint == null || deliverySlot == null) return;
        deltaTime = Mathf.Max(0f, deltaTime);
        stateTime += deltaTime;
        switch (State)
        {
            case CustomerVisitState.Arriving:
                if (MoveTo(servicePoint.position, deltaTime))
                {
                    State = CustomerVisitState.WaitingForProduct; stateTime = 0f;
                    transform.rotation = servicePoint.rotation;
                    dialogue = "Józsi: A szokásosat kérem.";
                    if (deliverySlot.Occupant != null) OnProductPlaced(deliverySlot.Occupant);
                }
                break;
            case CustomerVisitState.WaitingForProduct:
                feedbackTime = Mathf.Max(0f, feedbackTime - deltaTime);
                if (feedbackTime <= 0f)
                    dialogue = stateTime < 2f ? "Józsi: A szokásosat kérem." : stateTime < 4f ? "Te: Most látom először." : stateTime < 6f ? "Józsi: Attól még nekem ez a szokásos." : "Józsi: Egy Buborék ásványvizet kérek. A kéket.";
                break;
            case CustomerVisitState.Thanking:
                if (stateTime >= 2.8f) { State = CustomerVisitState.Leaving; stateTime = 0f; }
                break;
            case CustomerVisitState.Leaving:
                if (MoveTo(entrance.position, deltaTime))
                {
                    State = CustomerVisitState.Completed; character.enabled = false;
                    if (visual != null) visual.SetActive(false);
                    dialogue = "Józsi távozott. Az első kiszolgálás kész!";
                }
                break;
        }
    }
    private bool MoveTo(Vector3 destination, float deltaTime)
    {
        Vector3 offset = destination - transform.position; offset.y = 0f;
        if (offset.magnitude <= 0.06f) return true;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(offset), 360f * deltaTime);
        character.Move(Vector3.ClampMagnitude(offset, walkSpeed * deltaTime));
        return Vector3.ProjectOnPlane(destination - transform.position, Vector3.up).magnitude <= 0.06f;
    }
    private void OnProductPlaced(ShopProduct product) => TryServe(product);
    public bool TryServe(ShopProduct product)
    {
        if (State != CustomerVisitState.WaitingForProduct || product == null || product.IsHeld || product.CurrentSlot != deliverySlot || deliverySlot.Occupant != product) return false;
        if (product.ProductId != requestedProductId)
        {
            WrongAttempts++; feedbackTime = 5f;
            dialogue = "Józsi: Én vizet kértem. Ezt még felvizezni is nehéz lenne.";
            return false;
        }
        if (!product.AcceptDelivery()) return false;
        CompletedOrders++; State = CustomerVisitState.Thanking; stateTime = 0f;
        dialogue = "Józsi: Na, ez az! Mondtam én, hogy ismeri a szokásosat.";
        return true;
    }
    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, wordWrap = true };
            titleStyle.normal.textColor = Color.white;
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            textStyle.normal.textColor = Color.white;
        }
        float width = Mathf.Min(460f, Screen.width - 24f);
        float left = Screen.width - width - 12f;
        Color previous = GUI.color;
        GUI.color = new Color(0.04f, 0.09f, 0.14f, 0.9f);
        GUI.DrawTexture(new Rect(left, 12f, width, 154f), Texture2D.whiteTexture);
        GUI.color = previous;
        string title = State == CustomerVisitState.WaitingForProduct ? "Józsi rendelése: " + requestedProductName : State == CustomerVisitState.Completed ? "Kiszolgálás kész" : State == CustomerVisitState.Thanking || State == CustomerVisitState.Leaving ? "Áru átadva" : "Első vásárló: Józsi";
        GUI.Label(new Rect(left + 12f, 20f, width - 24f, 44f), title, titleStyle);
        GUI.Label(new Rect(left + 12f, 66f, width - 24f, 46f), dialogue, textStyle);
        string task = State == CustomerVisitState.WaitingForProduct ? "Tedd a kék ásványvizet a pult KÉK átadóhelyére. (E)" : State == CustomerVisitState.Completed ? "R – Új látogatás  •  Kiszolgálva: " + CompletedOrders : State == CustomerVisitState.Arriving ? "A vásárló a pulthoz sétál." : "A vásárló megköszöni és távozik.";
        GUI.Label(new Rect(left + 12f, 118f, width - 24f, 40f), task, textStyle);
    }
}
