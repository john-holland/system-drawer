using System;
using UnityEngine;

/// <summary>
/// Marks a world volume as off-limits to hierarchical pathing systems.
/// Provides runtime notifications for solvers to rebuild caches when the volume changes.
/// </summary>
public class OffLimitsSpace : MonoBehaviour
{
    public enum VolumeMode
    {
        UseColliderOrRendererBounds,
        SphereRadius
        // Future: MeshVolume, Capsule, ConvexDecomposition, etc.
    }

    [Header("Volume")]
    public VolumeMode volumeMode = VolumeMode.UseColliderOrRendererBounds;

    [Tooltip("If VolumeMode == SphereRadius, this radius is used.")]
    public float radius = 5f;

    [Header("Runtime Updates")]
    public bool notifyOnTransformChange = true;

    public static event Action<OffLimitsSpace> Changed;

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
        switch (volumeMode)
        {
            case VolumeMode.SphereRadius:
                return new Bounds(transform.position, Vector3.one * Mathf.Max(0f, radius) * 2f);

            case VolumeMode.UseColliderOrRendererBounds:
            default:
            {
                Collider c = GetComponent<Collider>();
                if (c != null)
                    return c.bounds;

                Renderer r = GetComponent<Renderer>();
                if (r != null)
                    return r.bounds;

                return new Bounds(transform.position, Vector3.one);
            }
        }
    }

    private void NotifyChanged()
    {
        Changed?.Invoke(this);
    }
}
