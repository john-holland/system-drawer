using System.Collections.Generic;
using UnityEngine;

/// <summary>Cone/ray pick among pathing apertures while slow-time is active.</summary>
public sealed class AngularTargetSelectMode : MonoBehaviour
{
    public Camera viewCamera;
    [Range(1f, 45f)] public float coneHalfAngleDegrees = 12f;
    public ApertureHighlightRenderer highlight;

    readonly List<PathingAperture> _candidates = new List<PathingAperture>();
    PathingAperture _hovered;
    PathingAperture _selected;

    public PathingAperture Hovered => _hovered;
    public PathingAperture Selected => _selected;

    public void SetCandidates(IReadOnlyList<PathingAperture> candidates)
    {
        _candidates.Clear();
        if (candidates == null) return;
        for (int i = 0; i < candidates.Count; i++)
            if (candidates[i] != null)
                _candidates.Add(candidates[i]);
    }

    public void SetHovered(PathingAperture aperture)
    {
        _hovered = aperture;
        if (highlight != null)
            highlight.SetHovered(aperture);
    }

    public void SetSelected(PathingAperture aperture)
    {
        _selected = aperture;
        if (highlight != null)
            highlight.SetSelected(aperture);
    }

    public void ClearSelection()
    {
        _hovered = null;
        _selected = null;
        if (highlight != null)
            highlight.Clear();
    }

    /// <summary>Pick best aperture under screen ray / view cone. Returns true if hover changed.</summary>
    public bool TryScan(Vector2 screenPos, out PathingAperture hit, out bool changed)
    {
        hit = null;
        changed = false;
        var cam = viewCamera != null ? viewCamera : Camera.main;
        if (cam == null || _candidates.Count == 0)
            return false;

        var ray = cam.ScreenPointToRay(screenPos);
        float bestScore = float.MaxValue;
        PathingAperture best = null;
        float cosLimit = Mathf.Cos(coneHalfAngleDegrees * Mathf.Deg2Rad);

        for (int i = 0; i < _candidates.Count; i++)
        {
            var a = _candidates[i];
            if (a == null) continue;
            var to = a.transform.position - ray.origin;
            float dist = to.magnitude;
            if (dist < 1e-4f) continue;
            var dir = to / dist;
            float dot = Vector3.Dot(ray.direction, dir);
            if (dot < cosLimit) continue;
            // Prefer closer + more centered.
            float score = dist * (2f - dot);
            if (score < bestScore)
            {
                bestScore = score;
                best = a;
            }
        }

        // Also accept direct raycast hits on aperture colliders.
        if (Physics.Raycast(ray, out var rh, 500f))
        {
            var onAperture = rh.collider.GetComponentInParent<PathingAperture>();
            if (onAperture != null && _candidates.Contains(onAperture))
                best = onAperture;
        }

        hit = best;
        changed = !ReferenceEquals(best, _hovered);
        if (changed)
            SetHovered(best);
        return best != null;
    }
}
