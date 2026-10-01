using UnityEngine;

/// <summary>Walkable sidewalk ribbon snap target for TravelAgent walk legs.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Roads/Sidewalk Ribbon")]
public sealed class SidewalkRibbon : MonoBehaviour
{
    public string roadLotId;
    public float widthM = 1.8f;
    public float paddingM = 0.2f;
    [Range(0f, 1f)] public float mattingWidth01;
    public bool walkOpen = true;
    public Vector3 along = Vector3.forward;

    static readonly System.Collections.Generic.List<SidewalkRibbon> Registry = new System.Collections.Generic.List<SidewalkRibbon>();
    public static System.Collections.Generic.IReadOnlyList<SidewalkRibbon> All => Registry;

    public float WalkableWidthM => Mathf.Max(0.1f, widthM - 2f * paddingM);
    public bool HasMatting => mattingWidth01 > 1e-4f;

    void OnEnable()
    {
        if (!Registry.Contains(this)) Registry.Add(this);
    }

    void OnDisable() => Registry.Remove(this);

    public bool TrySampleWalk(Vector3 near, out Vector3 world)
    {
        world = transform.position;
        if (!walkOpen) return false;
        Vector3 a = Vector3.ProjectOnPlane(near - transform.position, Vector3.up);
        Vector3 dir = Vector3.ProjectOnPlane(along, Vector3.up);
        if (dir.sqrMagnitude < 1e-4f) dir = transform.forward;
        dir.Normalize();
        float t = Vector3.Dot(a, dir);
        world = transform.position + dir * t;
        world.y = transform.position.y;
        return true;
    }

    public static SidewalkRibbon FindNearest(Vector3 world, float maxDist)
    {
        SidewalkRibbon best = null;
        float bestSq = maxDist * maxDist;
        for (int i = 0; i < Registry.Count; i++)
        {
            var r = Registry[i];
            if (r == null || !r.walkOpen) continue;
            float sq = (r.transform.position - world).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = r;
            }
        }
        return best;
    }
}
