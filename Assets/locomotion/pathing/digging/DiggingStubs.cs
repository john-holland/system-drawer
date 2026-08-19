using System.Collections.Generic;
using UnityEngine;

/// <summary>Fake-curve scoop capacity: moved particles / (distance / time).</summary>
public static class ScoopCapacityEstimator
{
    public static float Estimate(float movedParticles, float distance, float time)
    {
        float dt = Mathf.Max(1e-4f, time);
        float speed = Mathf.Max(1e-4f, distance) / dt;
        return movedParticles / speed;
    }

    public static float FromAnimationCurve(AnimationCurve shovelDescend, float dt)
    {
        shovelDescend ??= AnimationCurve.Linear(0f, 1f, 1f, 0.35f);
        float a = shovelDescend.Evaluate(0f);
        float b = shovelDescend.Evaluate(Mathf.Clamp01(dt));
        float curvatureDrop = Mathf.Max(0f, a - b);
        return Estimate(curvatureDrop * 16f, 0.4f, Mathf.Max(0.05f, dt));
    }
}

/// <summary>Tip particles off until scoop completes a 360° turn.</summary>
public static class TipMinimumSimulator
{
    public static float TipOff(float load01, float rotationDeg)
    {
        float turns = Mathf.Abs(rotationDeg) / 360f;
        return Mathf.Clamp01(load01 * (1f - Mathf.Clamp01(turns)));
    }

    public static float RemainingAfterFullTurn(float load01) => TipOff(load01, 360f);
}

/// <summary>Developer in-paint for tunnel stress on PixelLight cells.</summary>
public static class TunnelStressPainter
{
    public static void Paint(CityPixelGrid grid, string layerId, int x, int y, int frame, byte stress)
    {
        if (grid?.layers == null) return;
        for (int i = 0; i < grid.layers.Count; i++)
        {
            var layer = grid.layers[i];
            if (layer == null || layer.layerId != layerId) continue;
            if (layer.kind != CityPixelLayerKind.TunnelStress && layer.kind != CityPixelLayerKind.Custom)
                continue;
            if (layer.frames == null || frame < 0 || frame >= layer.frames.Count) return;
            layer.frames[frame].Set(x, y, grid.width, stress);
            return;
        }
    }
}

/// <summary>Kalman-style 1D filter for SDF fall cache picks.</summary>
public static class DigKalmanFallFilter
{
    public static float Update(float estimate, float measurement, float processVar = 0.05f, float measureVar = 0.2f)
    {
        float k = processVar / (processVar + measureVar);
        return estimate + k * (measurement - estimate);
    }

    public static List<float> PickFalls(IList<float> forces, float threshold = 0.15f)
    {
        var falls = new List<float>();
        if (forces == null) return falls;
        float est = forces.Count > 0 ? forces[0] : 0f;
        for (int i = 0; i < forces.Count; i++)
        {
            est = Update(est, forces[i]);
            if (i > 0 && forces[i - 1] - forces[i] > threshold)
                falls.Add(est);
        }
        return falls;
    }
}
