using System.Collections.Generic;
using UnityEngine;

public enum PathingApertureMode
{
    Vehicle,
    Walk,
    Either
}

/// <summary>Pathing opening / narrow passage marker (not drink aperture).</summary>
public sealed class PathingAperture : MonoBehaviour
{
    public string apertureId;
    public PathingApertureMode mode = PathingApertureMode.Either;
    [Min(0.05f)] public float radius = 1.2f;
    public Vector3 approachOffset = new Vector3(0f, 0f, -2f);
    [Tooltip("Optional authoring tag filter (e.g. stair_rail, garage_door).")]
    public List<string> tags = new List<string>();
    public int octreeLeafIndex = -1;

    public Vector3 ApproachPointWorld => transform.TransformPoint(approachOffset);
    public Vector3 OpeningNormal => transform.forward;

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.DrawLine(transform.position, ApproachPointWorld);
        Gizmos.DrawRay(transform.position, OpeningNormal * radius);
    }

    public bool MatchesTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return true;
        if (tags == null) return false;
        for (int i = 0; i < tags.Count; i++)
            if (string.Equals(tags[i], tag, System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}

/// <summary>Scene registry of pathing apertures for gambit selection.</summary>
public sealed class PathingApertureRegistry : MonoBehaviour
{
    public List<PathingAperture> apertures = new List<PathingAperture>();

    public void RefreshFromChildren()
    {
        apertures = new List<PathingAperture>(GetComponentsInChildren<PathingAperture>(true));
    }

    public IReadOnlyList<PathingAperture> Query(PathingApertureMode modeFilter, string tagFilter = null)
    {
        if (apertures == null || apertures.Count == 0)
            RefreshFromChildren();
        var list = new List<PathingAperture>();
        for (int i = 0; i < apertures.Count; i++)
        {
            var a = apertures[i];
            if (a == null) continue;
            if (modeFilter != PathingApertureMode.Either &&
                a.mode != PathingApertureMode.Either &&
                a.mode != modeFilter)
                continue;
            if (!a.MatchesTag(tagFilter)) continue;
            list.Add(a);
        }
        return list;
    }

    public PathingAperture FindById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (apertures == null || apertures.Count == 0) RefreshFromChildren();
        for (int i = 0; i < apertures.Count; i++)
            if (apertures[i] != null && apertures[i].apertureId == id)
                return apertures[i];
        return null;
    }
}
