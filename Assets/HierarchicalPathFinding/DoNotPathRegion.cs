using System;
using UnityEngine;

/// <summary>
/// Marks an axis-aligned volume agents must not path through. Integrated into hierarchical path rebuild and card solvers.
/// </summary>
[DisallowMultipleComponent]
public sealed class DoNotPathRegion : MonoBehaviour
{
    [SerializeField] Vector3 localCenter = Vector3.zero;
    [SerializeField] Vector3 localSize = Vector3.one;

    public static event Action<DoNotPathRegion> Changed;

    public Bounds LocalBounds => new Bounds(localCenter, localSize);

    public Bounds GetWorldBounds()
    {
        Bounds local = LocalBounds;
        Vector3 worldCenter = transform.TransformPoint(local.center);
        Vector3 axisX = transform.TransformVector(new Vector3(local.extents.x, 0f, 0f));
        Vector3 axisY = transform.TransformVector(new Vector3(0f, local.extents.y, 0f));
        Vector3 axisZ = transform.TransformVector(new Vector3(0f, 0f, local.extents.z));
        Vector3 worldExtents = new Vector3(
            Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
            Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
            Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
        return new Bounds(worldCenter, worldExtents * 2f);
    }

    /// <summary>Axis-aligned world-space containment (conservative OBB check).</summary>
    public bool ContainsWorldPosition(Vector3 world)
    {
        return GetWorldBounds().Contains(world);
    }

    public static bool AnyContainsWorld(Vector3 world)
    {
        foreach (var r in UnityEngine.Object.FindObjectsByType<DoNotPathRegion>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (r != null && r.isActiveAndEnabled && r.ContainsWorldPosition(world))
                return true;
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
        Bounds b = GetWorldBounds();
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
#endif

    private void OnEnable() => Changed?.Invoke(this);
    private void OnDisable() => Changed?.Invoke(this);
}
