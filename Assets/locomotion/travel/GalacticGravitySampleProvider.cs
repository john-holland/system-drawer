using Planetary;
using Planetary.Celestial;
using UnityEngine;

/// <summary>Aggregates gravity wells from registered celestial bodies.</summary>
public sealed class GalacticGravitySampleProvider : IGravitySampleProvider
{
    readonly IGravitySampleProvider _fallback;
    readonly GalacticBodyRegistry _registry;
    readonly PhysicalManifoldRelativitySolver _relativity;

    public GalacticGravitySampleProvider(
        GalacticBodyRegistry registry = null,
        PhysicalManifoldRelativitySolver relativity = null,
        IGravitySampleProvider fallback = null)
    {
        _registry = registry ?? GalacticBodyRegistry.Instance;
        _relativity = relativity ?? Object.FindAnyObjectByType<PhysicalManifoldRelativitySolver>();
        _fallback = fallback ?? new UnityGravityProvider();
    }

    public GravitySample Sample(Vector3 worldPos)
    {
        GravitySample s = _fallback.Sample(worldPos);
        if (_registry == null)
            return s;

        Vector3 sum = Vector3.zero;
        float strengthSum = 0f;
        var bodies = _registry.SceneBodies;
        for (int i = 0; i < bodies.Count; i++)
        {
            var b = bodies[i];
            if (b?.BodyTransform == null)
                continue;
            Vector3 toBody = b.BodyTransform.position - worldPos;
            float distSq = toBody.sqrMagnitude;
            if (distSq < 1e-4f)
                continue;
            float dist = Mathf.Sqrt(distSq);
            float g = b.Mass * 6.674e-11f / distSq;
            float well = b.Manifold != null ? b.Manifold.gravityWellStrength : 1f;
            strengthSum += g * well;
            sum += toBody.normalized * g * well;
        }

        if (strengthSum > 1e-6f)
        {
            s.up = (-sum.normalized);
            s.strength = strengthSum;
        }

        if (_relativity != null)
        {
            float metric = _relativity.SampleMetricFactor(worldPos, sum.normalized);
            s.strength *= Mathf.Max(0.01f, metric);
        }

        return s;
    }
}
