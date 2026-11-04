using UnityEngine;

public sealed class CashRegisterDrawer : MonoBehaviour
{
    [SerializeField] private Transform movingDrawer;
    [SerializeField] private Transform moneyContents;
    [SerializeField] private Vector3 closedPosition;
    [SerializeField] private float travel = .44f;
    [SerializeField] private float duration = .35f;
    public bool TargetOpen { get; private set; }
    public float Progress { get; private set; }
    public bool IsOpen => Progress >= .999f;
    public Transform MovingDrawer => movingDrawer;
    public Vector3 OpenPosition => movingDrawer.parent.TransformPoint(closedPosition + Vector3.forward * travel);
    public void Configure(Transform tray, Transform contents)
    {
        movingDrawer = tray; moneyContents = contents; closedPosition = tray.localPosition;
        Progress = 0; TargetOpen = false; ApplyPose();
    }
    private void Awake() { Progress = 0; TargetOpen = false; ApplyPose(); }
    private void Update() => Advance(Time.deltaTime);
    public void SetOpen(bool value) { TargetOpen = value; ApplyPose(); }
    public void Advance(float seconds)
    {
        Progress = Mathf.MoveTowards(Progress, TargetOpen ? 1f : 0f, Mathf.Max(0, seconds) / duration);
        ApplyPose();
    }
    private void ApplyPose()
    {
        if (movingDrawer == null || moneyContents == null) return;
        movingDrawer.localPosition = closedPosition + Vector3.forward * (travel * Mathf.SmoothStep(0, 1, Progress));
        moneyContents.gameObject.SetActive(Progress > .01f);
        foreach (var collider in moneyContents.GetComponentsInChildren<Collider>(true)) collider.enabled = TargetOpen && IsOpen;
    }
    private void OnDisable() { TargetOpen = false; Progress = 0; ApplyPose(); }
}
