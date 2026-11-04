using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform viewTransform;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 3.5f;
    [SerializeField, Min(1f)] private float sprintMultiplier = 1.65f;
    [SerializeField, Min(0f)] private float jumpHeight = .65f;
    [SerializeField] private float gravity = -20f;

    [Header("Look")]
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
    [SerializeField, Range(1f, 89f)] private float maxLookAngle = 80f;

    private float pitch;
    private float verticalVelocity;
    public bool IsGrounded => characterController != null && characterController.isGrounded;
    public float VerticalSpeed => verticalVelocity;
    public float CurrentSpeed { get; private set; }

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (viewTransform == null && Camera.main != null)
        {
            viewTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        SetCursorLocked(true);
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private void Update()
    {
        if (ShopInputGate.Blocked) { CurrentSpeed = 0; SetCursorLocked(false); return; }
        if (!Application.isFocused) return;
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null || characterController == null || viewTransform == null)
        {
            return;
        }

        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            SetCursorLocked(true);
        }

        if (Cursor.lockState == CursorLockMode.Locked && mouse != null)
        {
            UpdateLook(mouse);
        }

        UpdateMovement(keyboard);
    }

    private void UpdateLook(Mouse mouse)
    {
        Vector2 mouseDelta = mouse.delta.ReadValue() * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - mouseDelta.y, -maxLookAngle, maxLookAngle);
        transform.Rotate(Vector3.up, mouseDelta.x, Space.World);
        viewTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void UpdateMovement(Keyboard keyboard)
    {
        Vector2 input = Vector2.zero;
        if (ShopBindings.Held(UnityEngine.InputSystem.Key.A)) input.x -= 1f;
        if (ShopBindings.Held(UnityEngine.InputSystem.Key.D)) input.x += 1f;
        if (ShopBindings.Held(UnityEngine.InputSystem.Key.S)) input.y -= 1f;
        if (ShopBindings.Held(UnityEngine.InputSystem.Key.W)) input.y += 1f;
        input = Cursor.lockState == CursorLockMode.Locked ? Vector2.ClampMagnitude(input, 1f) : Vector2.zero;

        bool locked = Cursor.lockState == CursorLockMode.Locked;
        StepMovement(input, locked && ShopBindings.Held(Key.LeftShift), locked && ShopBindings.Pressed(Key.Space), Time.deltaTime);
    }

    // Shared by actual input and scene checks; collision remains CharacterController-owned.
    public void StepMovement(Vector2 input, bool sprint, bool jump, float dt)
    {
        if (ShopInputGate.Blocked || ShopMenu.Active || Time.timeScale == 0 || characterController == null || !characterController.enabled || dt <= 0) return;
        input = Vector2.ClampMagnitude(input, 1f);
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        if (jump && characterController.isGrounded && gravity < 0)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        verticalVelocity += gravity * dt;
        CurrentSpeed = moveSpeed * (sprint ? sprintMultiplier : 1f) * input.magnitude;
        Vector3 planarMovement = (transform.right * input.x + transform.forward * input.y) * (moveSpeed * (sprint ? sprintMultiplier : 1f));
        Vector3 movement = planarMovement + Vector3.up * verticalVelocity;
        var collision = characterController.Move(movement * dt);
        if ((collision & CollisionFlags.Above) != 0 && verticalVelocity > 0) verticalVelocity = 0;
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
