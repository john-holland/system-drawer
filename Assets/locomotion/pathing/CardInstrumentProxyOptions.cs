using System;
using UnityEngine;

/// <summary>
/// Shared proxy-instrument options for Combat / Love / Wrestling cards
/// (vehicle weapon fire, cockpit triggers, etc.).
/// </summary>
[Serializable]
public sealed class CardInstrumentProxyOptions
{
    public bool useProxyInstrument;
    public VehicleInstrumentMap sourceMap;
    [Tooltip("Local instrument map slot id, e.g. weapon.trigger or jet.fire_sw.")]
    public string localSurfaceId = "weapon.trigger";
    [Tooltip("~22 N ≈ 5 lbf safety-lock pressure.")]
    public float safetyLockForceN = 22.24f;
    [Tooltip("Author flavor, e.g. car door spring.")]
    public string hardwareFlavorNote = "all we had was car door spring";
    [Range(0f, 1f)] public float appliedForce01 = 1f;

    /// <summary>Maps 0–1 authoring slider onto Newtons (full scale = 50 N).</summary>
    public float AppliedForceN => Mathf.Clamp01(appliedForce01) * 50f;

    public bool SafetyLockSatisfied =>
        !useProxyInstrument || AppliedForceN + 1e-3f >= safetyLockForceN;

    /// <summary>Try routing via a proxy on actor (or children). Returns false if unused or blocked.</summary>
    public bool TryRoute(GoodSection card, GameObject actor, float dt)
    {
        if (!useProxyInstrument || card == null || actor == null)
            return false;
        if (!SafetyLockSatisfied)
            return false;
        var proxy = actor.GetComponentInChildren<VehicleInstrumentPhysicsProxy>()
                    ?? actor.GetComponentInParent<VehicleInstrumentPhysicsProxy>();
        if (proxy == null)
            return false;
        if (sourceMap != null)
            proxy.sourceMap = sourceMap;
        return proxy.TryFirePulse(localSurfaceId, card, dt);
    }
}
