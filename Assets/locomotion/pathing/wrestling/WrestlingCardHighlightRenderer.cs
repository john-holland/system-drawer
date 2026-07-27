using UnityEngine;

/// <summary>Hover/select gizmo on opponent control points + move kind label.</summary>
[AddComponentMenu("Locomotion/Wrestling/Card Highlight Renderer")]
public sealed class WrestlingCardHighlightRenderer : MonoBehaviour
{
    public Color hoverColor = new Color(1f, 0.85f, 0.2f, 0.7f);
    public Color selectedColor = new Color(0.2f, 1f, 0.45f, 0.85f);
    public float gizmoRadius = 0.12f;

    WrestlingCard _hovered;
    WrestlingCard _selected;
    GameObject _opponent;

    public void SetHovered(WrestlingCard card, GameObject opponent)
    {
        _hovered = card;
        _opponent = opponent;
    }

    public void SetSelected(WrestlingCard card, GameObject opponent)
    {
        _selected = card;
        _opponent = opponent;
    }

    public void Clear()
    {
        _hovered = null;
        _selected = null;
    }

    void OnDrawGizmos()
    {
        DrawCard(_hovered, hoverColor);
        DrawCard(_selected, selectedColor);
    }

    void DrawCard(WrestlingCard card, Color color)
    {
        if (card == null) return;
        Gizmos.color = color;
        Vector3 p = card.ResolveAimAnchorWorld(_opponent != null ? _opponent : card.opponent);
        Gizmos.DrawWireSphere(p, gizmoRadius);
        Gizmos.DrawLine(p, p + Vector3.up * (gizmoRadius * 2f));
    }
}
