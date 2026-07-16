using UnityEngine;

/// <summary>
/// Scene authoring: regenerates gambit stops when a vehicle narrowly passes an aperture.
/// Draws gizmos for octree bucket / position goal / spline nodes.
/// </summary>
public sealed class VehicleGambitPathAuthoring : MonoBehaviour
{
    public VehicleGambitPathAsset pathAsset;
    public PathingApertureRegistry registry;
    public Transform vehicleRoot;
    public Vector3 vehicleHalfExtents = new Vector3(1f, 1f, 2f);
    public bool autoReenterSlowTime = true;
    [Range(0f, 1f)] public float timeScaleCoefficient = 0.2f;

    void Update()
    {
        if (pathAsset == null || registry == null || vehicleRoot == null)
            return;
        var apertures = registry.Query(PathingApertureMode.Vehicle);
        for (int i = 0; i < apertures.Count; i++)
        {
            var a = apertures[i];
            if (a == null) continue;
            float clearance = EstimateClearance(a);
            if (clearance < pathAsset.narrowClearanceThreshold && IsNearAperture(a))
                pathAsset.UpsertStopFromAperture(a, clearance);
        }
    }

    public float EstimateClearance(PathingAperture aperture)
    {
        if (aperture == null) return float.MaxValue;
        // Clearance ≈ aperture radius minus vehicle half-width projected on opening plane.
        float vehicleWidth = Mathf.Max(vehicleHalfExtents.x, vehicleHalfExtents.z);
        return Mathf.Max(0f, aperture.radius - vehicleWidth);
    }

    bool IsNearAperture(PathingAperture a)
    {
        return (vehicleRoot.position - a.transform.position).sqrMagnitude <=
               (a.radius + vehicleHalfExtents.magnitude) * (a.radius + vehicleHalfExtents.magnitude);
    }

    void OnDrawGizmosSelected()
    {
        if (pathAsset == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < pathAsset.stops.Count; i++)
        {
            var s = pathAsset.stops[i];
            Gizmos.DrawWireSphere(s.positionGoal, 0.35f);
            if (i + 1 < pathAsset.stops.Count)
                Gizmos.DrawLine(s.positionGoal, pathAsset.stops[i + 1].positionGoal);
        }
        Gizmos.color = new Color(1f, 0.4f, 0.9f, 0.9f);
        for (int i = 0; i < pathAsset.splineNodes.Count; i++)
        {
            var n = pathAsset.splineNodes[i];
            Gizmos.DrawSphere(n.position, 0.2f);
            Gizmos.DrawRay(n.position, n.tangent.normalized);
            if (i + 1 < pathAsset.splineNodes.Count)
                Gizmos.DrawLine(n.position, pathAsset.splineNodes[i + 1].position);
        }
    }
}
