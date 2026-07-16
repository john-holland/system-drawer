using System.Collections.Generic;
using UnityEngine;

/// <summary>Stairwell railing with strike ring, acoustic material hook, and optional resonance proxy.</summary>
public sealed class StairwellRailingNode : MonoBehaviour
{
    public string railingId;
    public int floorIndex;
    public Collider railingCollider;
    public List<Transform> strikePoints = new List<Transform>();
    [Range(0f, 1f)] public float manifoldFriction = 0.35f;
    public float massHint = 40f;
    public PathingAperture pathingAperture;
    public MonoBehaviour resonanceProxy;
    public string dingCacheKey;

    public Vector3 SampleStrikePoint(int index)
    {
        if (strikePoints != null && strikePoints.Count > 0)
        {
            var t = strikePoints[Mathf.Clamp(index, 0, strikePoints.Count - 1)];
            if (t != null) return t.position;
        }
        return transform.position;
    }

    public void EnsureAperture()
    {
        if (pathingAperture == null)
            pathingAperture = GetComponent<PathingAperture>();
        if (pathingAperture == null)
            pathingAperture = gameObject.AddComponent<PathingAperture>();
        pathingAperture.apertureId = string.IsNullOrEmpty(railingId) ? name : railingId;
        pathingAperture.mode = PathingApertureMode.Walk;
        if (!pathingAperture.tags.Contains("stair_rail"))
            pathingAperture.tags.Add("stair_rail");
    }
}
