using UnityEngine;

public sealed class CounterLayout : MonoBehaviour
{
    [SerializeField] private Transform servicePoint;
    [SerializeField] private float queueSpacing = .85f;
    public Vector3 ServicePoint => servicePoint.position;
    public Vector3 QueuePoint(int index)
    {
        Vector3 point = servicePoint.position - transform.right * (queueSpacing * index);
        if (index > 0) point -= transform.forward * .25f;
        point.y = 0;
        return point;
    }
    public void Configure(Transform service) => servicePoint = service;
    private void OnDrawGizmosSelected()
    {
        if (servicePoint == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < 3; i++) Gizmos.DrawWireSphere(QueuePoint(i), .2f);
    }
}
