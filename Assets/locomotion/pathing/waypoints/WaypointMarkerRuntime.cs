using UnityEngine;

/// <summary>Mesh or SDF Max stamp hook for a waypoint / attack mark.</summary>
[AddComponentMenu("Locomotion/Waypoints/Marker Runtime")]
public sealed class WaypointMarkerRuntime : MonoBehaviour
{
    public WaypointMarker marker;
    public Mesh meshOverride;
    public Material meshMaterial;
    public bool enableSdfStampHooks = true;
    public string sdfCompositionKey;
    public string animationBtKey;

    MeshFilter _filter;
    MeshRenderer _renderer;

    public void Bind(WaypointMarker m)
    {
        marker = m;
        if (m == null) return;
        transform.position = m.worldPosition;
        animationBtKey = m.EffectiveAnimKey;
        sdfCompositionKey = enableSdfStampHooks
            ? (m.attackMark ? $"wp_attack_{m.id}" : $"wp_idle_{m.id}")
            : null;
        EnsureVisual();
    }

    void EnsureVisual()
    {
        if (marker != null && marker.visualMode == WaypointVisualMode.SdfMax)
        {
            // Stamp key authored; particle/mesh stub for scaffold
            EnsureMesh(PrimitiveType.Sphere, marker.attackMark ? Color.red : Color.yellow);
            transform.localScale = Vector3.one * (marker.attackMark ? 0.45f : 0.3f);
            return;
        }
        EnsureMesh(PrimitiveType.Cylinder, marker != null && marker.attackMark ? Color.red : new Color(0.9f, 0.8f, 0.3f));
        transform.localScale = new Vector3(0.25f, 0.05f, 0.25f);
    }

    void EnsureMesh(PrimitiveType type, Color color)
    {
        if (_filter == null)
        {
            var temp = GameObject.CreatePrimitive(type);
            _filter = gameObject.AddComponent<MeshFilter>();
            _filter.sharedMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            Destroy(temp);
        }
        if (meshOverride != null) _filter.sharedMesh = meshOverride;
        if (_renderer != null)
        {
            if (meshMaterial != null)
                _renderer.sharedMaterial = meshMaterial;
            else
            {
                _renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                _renderer.sharedMaterial.color = color;
            }
        }
    }
}
