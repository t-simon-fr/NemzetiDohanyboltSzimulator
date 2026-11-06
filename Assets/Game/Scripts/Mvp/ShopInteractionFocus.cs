using UnityEngine;

// Shared camera restoration for devices and the arrangement editor.
public sealed class ShopCameraSnapshot
{
    private readonly Camera camera;
    private readonly Vector3 position;
    private readonly Quaternion rotation;
    private readonly float fieldOfView, size;
    private readonly Rect rect;
    private readonly bool orthographic;
    public ShopCameraSnapshot(Camera camera)
    {
        this.camera = camera;
        position = camera.transform.localPosition;
        rotation = camera.transform.localRotation;
        fieldOfView = camera.fieldOfView;
        rect = camera.rect;
        orthographic = camera.orthographic;
        size = camera.orthographicSize;
    }
    public void Restore()
    {
        if (camera == null) return;
        camera.transform.SetLocalPositionAndRotation(position, rotation);
        camera.fieldOfView = fieldOfView;
        camera.rect = rect;
        camera.orthographic = orthographic;
        camera.orthographicSize = size;
    }
}

public sealed class ShopDeviceFocus
{
    private ShopCameraSnapshot camera;
    public ShopDeviceKind Kind { get; private set; }
    public void Enter(ShopDeviceKind kind, Camera view)
    {
        camera = new ShopCameraSnapshot(view);
        Kind = kind;
        ShopInputGate.SetBlocked(true);
        ShopInputGate.ConsumeWorldAction();
    }
    public void Exit(bool restoreCursor)
    {
        if (Kind == ShopDeviceKind.None) return;
        camera?.Restore();
        camera = null;
        Kind = ShopDeviceKind.None;
        ShopInputGate.SetBlocked(false, restoreCursor);
        ShopInputGate.ConsumeWorldAction();
    }
}

// A menu/editor owns one pause lease, including the state to restore after nesting over a device.
public sealed class ShopPauseScope
{
    private bool active, blocked, visible;
    private CursorLockMode cursor;
    private float scale;
    public void Enter()
    {
        if (active) return;
        active = true;
        blocked = ShopInputGate.Blocked;
        visible = Cursor.visible;
        cursor = Cursor.lockState;
        scale = Time.timeScale;
        Time.timeScale = 0;
        ShopInputGate.SetBlocked(true);
        ShopInputGate.ConsumeWorldAction();
    }
    public void Exit()
    {
        if (!active) return;
        active = false;
        Time.timeScale = scale;
        ShopInputGate.Blocked = blocked;
        Cursor.lockState = blocked ? cursor : CursorLockMode.Locked;
        Cursor.visible = blocked && visible;
        ShopInputGate.ConsumeWorldAction();
    }
}
