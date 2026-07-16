using UnityEngine;

/// <summary>Simple highlight for hovered/selected pathing apertures (shader override optional).</summary>
public sealed class ApertureHighlightRenderer : MonoBehaviour
{
    public Material highlightMaterial;
    public Color hoverColor = new Color(0.3f, 0.9f, 1f, 0.55f);
    public Color selectedColor = new Color(0.2f, 1f, 0.45f, 0.7f);
    public bool drawPathRibbon = true;
    public LineRenderer pathRenderer;

    PathingAperture _hovered;
    PathingAperture _selected;

    public void SetHovered(PathingAperture aperture)
    {
        _hovered = aperture;
        Refresh();
    }

    public void SetSelected(PathingAperture aperture)
    {
        _selected = aperture;
        Refresh();
    }

    public void Clear()
    {
        _hovered = null;
        _selected = null;
        if (pathRenderer != null)
            pathRenderer.positionCount = 0;
    }

    void Refresh()
    {
        if (!drawPathRibbon || pathRenderer == null)
            return;
        var a = _selected != null ? _selected : _hovered;
        if (a == null)
        {
            pathRenderer.positionCount = 0;
            return;
        }
        pathRenderer.positionCount = 2;
        pathRenderer.SetPosition(0, a.ApproachPointWorld);
        pathRenderer.SetPosition(1, a.transform.position);
        pathRenderer.startColor = pathRenderer.endColor = _selected != null ? selectedColor : hoverColor;
        if (highlightMaterial != null)
            pathRenderer.material = highlightMaterial;
    }

    void OnDrawGizmos()
    {
        if (_hovered != null)
        {
            Gizmos.color = hoverColor;
            Gizmos.DrawWireSphere(_hovered.transform.position, _hovered.radius * 1.15f);
        }
        if (_selected != null)
        {
            Gizmos.color = selectedColor;
            Gizmos.DrawWireSphere(_selected.transform.position, _selected.radius * 1.25f);
        }
    }
}
