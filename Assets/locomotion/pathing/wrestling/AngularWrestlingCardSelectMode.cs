using System.Collections.Generic;
using UnityEngine;

/// <summary>Cone/ray pick among WrestlingCard aim anchors (opponent limbs), not pathing apertures.</summary>
[AddComponentMenu("Locomotion/Wrestling/Angular Card Select Mode")]
public sealed class AngularWrestlingCardSelectMode : MonoBehaviour
{
    public Camera viewCamera;
    [Range(1f, 45f)] public float coneHalfAngleDegrees = 14f;
    public WrestlingCardHighlightRenderer highlight;
    public GameObject opponent;

    readonly List<WrestlingCard> _candidates = new List<WrestlingCard>();
    WrestlingCard _hovered;
    WrestlingCard _selected;

    public WrestlingCard Hovered => _hovered;
    public WrestlingCard Selected => _selected;

    public void SetCandidates(IReadOnlyList<WrestlingCard> candidates)
    {
        _candidates.Clear();
        if (candidates == null) return;
        for (int i = 0; i < candidates.Count; i++)
            if (candidates[i] != null)
                _candidates.Add(candidates[i]);
    }

    public void SetHovered(WrestlingCard card)
    {
        _hovered = card;
        if (highlight != null)
            highlight.SetHovered(card, opponent);
    }

    public void SetSelected(WrestlingCard card)
    {
        _selected = card;
        if (highlight != null)
            highlight.SetSelected(card, opponent);
    }

    public void ClearSelection()
    {
        _hovered = null;
        _selected = null;
        if (highlight != null)
            highlight.Clear();
    }

    public bool TryScan(Vector2 screenPos, out WrestlingCard hit, out bool changed)
    {
        hit = null;
        changed = false;
        var cam = viewCamera != null ? viewCamera : Camera.main;
        if (cam == null || _candidates.Count == 0)
            return false;

        Ray ray = cam.pixelWidth >= 2
            ? cam.ScreenPointToRay(screenPos)
            : cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return TryScanRay(ray, out hit, out changed);
    }

    /// <summary>Cone pick from an explicit ray (Edit Mode tests / stick aim).</summary>
    public bool TryScanRay(Ray ray, out WrestlingCard hit, out bool changed)
    {
        hit = null;
        changed = false;
        if (_candidates.Count == 0)
            return false;

        float bestScore = float.MaxValue;
        WrestlingCard best = null;
        float cosLimit = Mathf.Cos(coneHalfAngleDegrees * Mathf.Deg2Rad);

        for (int i = 0; i < _candidates.Count; i++)
        {
            var c = _candidates[i];
            if (c == null) continue;
            Vector3 anchor = c.ResolveAimAnchorWorld(opponent != null ? opponent : c.opponent);
            var to = anchor - ray.origin;
            float dist = to.magnitude;
            if (dist < 1e-4f) continue;
            var dir = to / dist;
            float dot = Vector3.Dot(ray.direction, dir);
            if (dot < cosLimit) continue;
            float score = dist * (2f - dot);
            if (score < bestScore)
            {
                bestScore = score;
                best = c;
            }
        }

        hit = best;
        changed = !ReferenceEquals(best, _hovered);
        if (changed)
            SetHovered(best);
        return best != null;
    }
}
