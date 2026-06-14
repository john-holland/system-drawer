using System;
using UnityEngine;
/// <summary>
/// Path cost / locomotion modifier zone (ice, mud, low traction). Sampled at edge midpoints during A* and by ambulation solvers.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsPathingZone : MonoBehaviour
{
    [SerializeField] Vector3 localCenter = Vector3.zero;
    [SerializeField] Vector3 localSize = Vector3.one * 4f;

    [Tooltip("Multiplies A* step cost (>= 1).")]
    [Min(0.01f)]
    public float pathCostMultiplier = 1.5f;

    [Tooltip("Grip multiplier applied by vehicle / ambulation solvers (0..1 = slippery).")]
    [Range(0f, 2f)]
    public float gripMultiplier = 1f;

    [Tooltip("Optional medium tag (stub). When sampling with a medium filter, zones with a set medium must match.")]
    public PhysicalPathingMedium pathingMedium = PhysicalPathingMedium.Unspecified;

    public static event Action<PhysicsPathingZone> Changed;

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

    public bool ContainsWorldPosition(Vector3 world)
    {
        return GetWorldBounds().Contains(world);
    }

    /// <summary>Aggregates multipliers from all overlapping zones (multiplicative).</summary>
    public static void SampleAt(Vector3 world, out float pathCostMul, out float gripMul)
    {
        SampleAt(world, PhysicalPathingMedium.Unspecified, out pathCostMul, out gripMul);
    }

    /// <summary>
    /// When <paramref name="mediumFilter"/> is not <see cref="PhysicalPathingMedium.Unspecified"/>,
    /// only zones whose <see cref="pathingMedium"/> is Unspecified or matches the filter contribute.
    /// </summary>
    public static void SampleAt(Vector3 world, PhysicalPathingMedium mediumFilter, out float pathCostMul, out float gripMul)
    {
        pathCostMul = 1f;
        gripMul = 1f;
        foreach (var z in UnityEngine.Object.FindObjectsByType<PhysicsPathingZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (z == null || !z.isActiveAndEnabled)
                continue;
            if (mediumFilter != PhysicalPathingMedium.Unspecified
                && z.pathingMedium != PhysicalPathingMedium.Unspecified
                && z.pathingMedium != mediumFilter)
                continue;
            if (!z.ContainsWorldPosition(world))
                continue;
            pathCostMul *= Mathf.Max(0.01f, z.pathCostMultiplier);
            gripMul *= Mathf.Clamp(z.gripMultiplier, 0f, 2f);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.2f);
        Bounds b = GetWorldBounds();
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.85f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
#endif

    private void OnEnable() => Changed?.Invoke(this);
    private void OnDisable() => Changed?.Invoke(this);
}
