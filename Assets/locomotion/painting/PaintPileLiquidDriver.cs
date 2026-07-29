using UnityEngine;
using Weather;

/// <summary>
/// High-viscosity paint piles: hang from nozzle then fall via WeatherPhysicsManifold.
/// (Does not reference Locomotion.Liquid — Liquid.Runtime already depends on Locomotion.Runtime.)
/// </summary>
[AddComponentMenu("Locomotion/Painting/Paint Pile Liquid Driver")]
public sealed class PaintPileLiquidDriver : MonoBehaviour
{
    public WeatherPhysicsManifold manifold;
    [Range(0f, 1f)] public float paintBlend = 0.55f;
    public float paintDensity = 1200f;
    public float totalMass = 1f;
    public Color pileColor = new Color(0.8f, 0.15f, 0.1f, 1f);
    public Vector3 pileCenter;
    public float pileRadius = 0.04f;
    public float hangMass;
    public Vector3 hangPosition;
    public bool isHanging;
    float _surfaceTension = 0.85f;

    public float SurfaceTension => _surfaceTension;

    public void SetSurfaceTension(float t) => _surfaceTension = Mathf.Clamp01(t);

    public WeatherPhysicsManifold ResolveManifold()
    {
        if (manifold != null)
            return manifold;
        manifold = FindAnyObjectByType<WeatherPhysicsManifold>();
        return manifold;
    }

    public void SetHanging(float mass, Color color, Vector3 worldPos, PaintTubeConfig config)
    {
        hangMass = mass;
        pileColor = color;
        hangPosition = worldPos;
        isHanging = true;
        if (config != null)
            _surfaceTension = config.surfaceTension;
        float r = config != null ? config.nozzleRadiusM : 0.01f;
        PaintSphere(worldPos, r, Vector3.zero, 101325f);
    }

    public void ReleaseHang(PaintTubeConfig config)
    {
        if (!isHanging || hangMass <= 0f)
        {
            isHanging = false;
            return;
        }

        Vector3 g = config != null && config.useFakeGravity ? config.fakeGravity : Physics.gravity;
        Vector3 velocity = g.normalized * Mathf.Min(2f, hangMass * 10f);
        if (config != null)
            _surfaceTension = config.surfaceTension;
        float r = config != null ? Mathf.Max(config.nozzleRadiusM, 0.01f) : 0.015f;
        PaintSphere(hangPosition + g.normalized * 0.02f, r, velocity, 101325f);

        totalMass += hangMass;
        pileCenter = hangPosition + Vector3.down * 0.05f;
        pileRadius = Mathf.Max(pileRadius, 0.02f + hangMass * 0.5f);
        hangMass = 0f;
        isHanging = false;
    }

    public bool TrySampleContact(Vector3 worldPoint, out float depth, out Color color, out float mass)
    {
        color = pileColor;
        mass = totalMass;
        float d = Vector3.Distance(worldPoint, pileCenter);
        depth = Mathf.Max(0f, pileRadius - d);
        return depth > 0f && totalMass > 1e-5f;
    }

    public void ConsumeMass(float amount)
    {
        totalMass = Mathf.Max(0f, totalMass - Mathf.Max(0f, amount));
        pileRadius = Mathf.Max(0.01f, pileRadius * (0.85f + 0.15f * Mathf.Clamp01(totalMass)));
    }

    void PaintSphere(Vector3 center, float radius, Vector3 velocity, float pressurePa)
    {
        var m = ResolveManifold();
        if (m == null || radius <= 0f)
            return;

        float hPa = pressurePa > 0f ? pressurePa / 100f : 1013.25f;
        var existing = m.GetDataAtPosition(center);
        var target = new ManifoldCellData
        {
            velocity = velocity,
            pressure = hPa,
            temperature = existing.temperature,
            density = paintDensity,
            mode = WeatherMode.Water,
            surfaceTensionCoeff = _surfaceTension,
            surfaceFriction = 0.08f,
        };
        m.SetDataAtPosition(center, Blend(existing, target, paintBlend));

        if (radius <= m.cellResolution)
            return;

        int steps = Mathf.CeilToInt(radius / Mathf.Max(m.cellResolution, 0.01f));
        for (int i = -steps; i <= steps; i++)
        for (int j = -steps; j <= steps; j++)
        for (int k = -steps; k <= steps; k++)
        {
            var offset = new Vector3(i, j, k) * m.cellResolution;
            if (offset.magnitude > radius)
                continue;
            var ex = m.GetDataAtPosition(center + offset);
            m.SetDataAtPosition(center + offset, Blend(ex, target, paintBlend * 0.5f));
        }
    }

    static ManifoldCellData Blend(ManifoldCellData a, ManifoldCellData b, float t)
    {
        t = Mathf.Clamp01(t);
        return new ManifoldCellData
        {
            velocity = Vector3.Lerp(a.velocity, b.velocity, t),
            pressure = Mathf.Lerp(a.pressure, b.pressure, t),
            temperature = Mathf.Lerp(a.temperature, b.temperature, t),
            density = Mathf.Lerp(a.density, b.density, t),
            mode = t > 0.5f ? b.mode : (a.mode == WeatherMode.Air ? b.mode : a.mode),
            surfaceTensionCoeff = Mathf.Lerp(a.surfaceTensionCoeff, b.surfaceTensionCoeff, t),
            surfaceFriction = Mathf.Lerp(a.surfaceFriction, b.surfaceFriction, t),
        };
    }
}
