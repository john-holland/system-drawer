using System;
using UnityEngine;

/// <summary>
/// Mass/extents gates so Superman can suplex a truck but The Rock cannot (easily) suplex a baby.
/// Gandhi's limits remain an open research question — document in WrestlingTopology.md.
/// </summary>
[Serializable]
public sealed class WrestlingBodySizeGate
{
    [Tooltip("Minimum opponent rigidbody mass (kg). 0 = no min.")]
    public float minOpponentMass;

    [Tooltip("Maximum opponent rigidbody mass (kg). 0 = no max.")]
    public float maxOpponentMass;

    [Tooltip("Min actorMass/opponentMass. 0 = unused.")]
    public float minActorToOpponentMassRatio;

    [Tooltip("Max actorMass/opponentMass. 0 = unused.")]
    public float maxActorToOpponentMassRatio;

    [Tooltip("Min opponent world AABB extent magnitude. 0 = unused.")]
    public float minOpponentExtentMagnitude;

    [Tooltip("Max opponent world AABB extent magnitude. 0 = unused.")]
    public float maxOpponentExtentMagnitude;

    public static WrestlingBodySizeGate Permissive => new WrestlingBodySizeGate();

    public static float EstimateMass(GameObject go)
    {
        if (go == null) return 0f;
        float sum = 0f;
        var bodies = go.GetComponentsInChildren<Rigidbody>();
        for (int i = 0; i < bodies.Length; i++)
            if (bodies[i] != null)
                sum += bodies[i].mass;
        if (sum > 1e-4f) return sum;
        var rb = go.GetComponentInParent<Rigidbody>();
        return rb != null ? rb.mass : 70f;
    }

    public static float EstimateExtentMagnitude(GameObject go)
    {
        if (go == null) return 0f;
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return 1f;
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            if (renderers[i] != null)
                b.Encapsulate(renderers[i].bounds);
        return b.size.magnitude;
    }

    public bool Passes(GameObject actor, GameObject opponent, out string failReason)
    {
        failReason = null;
        if (opponent == null)
        {
            failReason = "no opponent";
            return false;
        }

        float oppMass = EstimateMass(opponent);
        float actorMass = EstimateMass(actor);
        float oppExtent = EstimateExtentMagnitude(opponent);
        float ratio = oppMass > 1e-4f ? actorMass / oppMass : float.PositiveInfinity;

        bool massBandActive = minOpponentMass > 0f || maxOpponentMass > 0f;
        bool ratioBandActive = minActorToOpponentMassRatio > 0f || maxActorToOpponentMassRatio > 0f;
        bool massBandOk = !massBandActive ||
                          ((minOpponentMass <= 0f || oppMass >= minOpponentMass) &&
                           (maxOpponentMass <= 0f || oppMass <= maxOpponentMass));
        bool ratioBandOk = !ratioBandActive ||
                           ((minActorToOpponentMassRatio <= 0f || ratio >= minActorToOpponentMassRatio) &&
                            (maxActorToOpponentMassRatio <= 0f || ratio <= maxActorToOpponentMassRatio));

        // Locked: pass if mass band OR ratio band (when both authored); require the one that is active.
        if (massBandActive && ratioBandActive)
        {
            if (!massBandOk && !ratioBandOk)
            {
                failReason = $"mass {oppMass:F1} / ratio {ratio:F2} outside gates";
                return false;
            }
        }
        else if (massBandActive && !massBandOk)
        {
            failReason = $"opponent mass {oppMass:F1} outside [{minOpponentMass:F1},{maxOpponentMass:F1}]";
            return false;
        }
        else if (ratioBandActive && !ratioBandOk)
        {
            failReason = $"mass ratio {ratio:F2} outside gates";
            return false;
        }

        bool extentBandActive = minOpponentExtentMagnitude > 0f || maxOpponentExtentMagnitude > 0f;
        if (extentBandActive)
        {
            bool extentOk =
                (minOpponentExtentMagnitude <= 0f || oppExtent >= minOpponentExtentMagnitude) &&
                (maxOpponentExtentMagnitude <= 0f || oppExtent <= maxOpponentExtentMagnitude);
            if (!extentOk)
            {
                failReason = $"extent {oppExtent:F2} outside gates";
                return false;
            }
        }

        return true;
    }
}
