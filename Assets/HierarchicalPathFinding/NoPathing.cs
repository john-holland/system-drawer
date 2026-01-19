using System;
using UnityEngine;

/// <summary>
/// Marks an object (and optionally its children) as non-traversable for hierarchical pathing systems.
/// Also serves as a runtime change notifier so solvers can rebuild caches when this object moves/toggles.
/// </summary>
public class NoPathing : MonoBehaviour
{
    public enum AdjacencyMode
    {
        None,
        Radius
        // Future: OctTreeNeighbors, BoundsIntersections, etc.
    }

    [Header("Scope")]
    public bool includeChildren = true;

    [Header("Adjacency")]
    public AdjacencyMode adjacencyMode = AdjacencyMode.None;
    public float adjacencyRadius = 0f;

    [Header("Runtime Updates")]
    [Tooltip("If true, changes in transform will fire notifications.")]
    public bool notifyOnTransformChange = true;

    public static event Action<NoPathing> Changed;

    private void OnEnable()
    {
        NotifyChanged();
    }

    private void OnDisable()
    {
        NotifyChanged();
    }

    private void OnValidate()
    {
        NotifyChanged();
    }

    private void Update()
    {
        if (!notifyOnTransformChange)
            return;

        if (transform.hasChanged)
        {
            transform.hasChanged = false;
            NotifyChanged();
        }
    }

    public Bounds GetWorldBounds()
    {
        // Prefer colliders/renderers for accurate bounds.
        Collider c = GetComponent<Collider>();
        if (c != null)
            return c.bounds;

        Renderer r = GetComponent<Renderer>();
        if (r != null)
            return r.bounds;

        // Fallback: small bounds around transform.
        return new Bounds(transform.position, Vector3.one);
    }

    private void NotifyChanged()
    {
        Changed?.Invoke(this);
    }
}
