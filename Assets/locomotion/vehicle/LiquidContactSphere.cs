using Planetary.Field;
using UnityEngine;
using Weather;

/// <summary>
/// Per-tire liquid contact sphere: overrides friction/tension when manifold cell is water.
/// </summary>
[DisallowMultipleComponent]
public sealed class LiquidContactSphere : MonoBehaviour
{
    public CanonicalSpatiotemporalField canonicalField;
    public WeatherPhysicsManifold manifoldFallback;
    public float contactRadius = 0.12f;

    [Tooltip("Very low mu when aquaplaning on water.")]
    [Range(0.01f, 0.2f)] public float aquaplaneMu = 0.05f;

    public bool IsInLiquid { get; private set; }
    public float CurrentMu { get; private set; } = 0.7f;
    public float CurrentSurfaceTension { get; private set; }

    void Awake()
    {
        if (canonicalField == null)
            canonicalField = CanonicalSpatiotemporalField.Resolve();
        if (manifoldFallback == null)
            manifoldFallback = FindAnyObjectByType<WeatherPhysicsManifold>();
    }

    void FixedUpdate() => RefreshContact();

    public void RefreshContact()
    {
        Vector3 samplePos = transform.position;
        ManifoldCellData cell;
        if (canonicalField != null && canonicalField.TrySampleBlended(samplePos, Time.time, out SpatiotemporalSample s))
        {
            cell = s.cell;
            CurrentMu = s.surfaceFriction;
            CurrentSurfaceTension = s.surfaceTensionCoeff;
        }
        else if (manifoldFallback != null)
        {
            cell = manifoldFallback.GetDataAtPosition(samplePos);
            CurrentMu = cell.surfaceFriction > 1e-6f ? cell.surfaceFriction : 0.7f;
            CurrentSurfaceTension = cell.surfaceTensionCoeff;
        }
        else
        {
            IsInLiquid = false;
            return;
        }

        IsInLiquid = cell.mode == WeatherMode.Water;
        if (IsInLiquid)
        {
            CurrentMu = aquaplaneMu;
            if (CurrentSurfaceTension <= 1e-6f)
                CurrentSurfaceTension = 0.02f;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = IsInLiquid ? new Color(0.2f, 0.5f, 1f, 0.5f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, contactRadius);
    }
#endif
}
