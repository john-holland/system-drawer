using System;
using UnityEngine;

/// <summary>
/// Marks an axis-aligned volume with a physical medium for authoring and rebuild invalidation (stub occupancy hook).
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicalMediumVolume : MonoBehaviour
{
    [SerializeField] Vector3 localCenter = Vector3.zero;
    [SerializeField] Vector3 localSize = Vector3.one * 8f;

    public PhysicalPathingMedium medium = PhysicalPathingMedium.Unspecified;

    public static event Action<PhysicalMediumVolume> Changed;

    public Bounds GetWorldBounds()
    {
        Bounds local = new Bounds(localCenter, localSize);
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.9f, 0.5f, 0.2f);
        Bounds b = GetWorldBounds();
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(0.3f, 0.85f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
#endif

    private void OnEnable() => Changed?.Invoke(this);
    private void OnDisable() => Changed?.Invoke(this);
}
