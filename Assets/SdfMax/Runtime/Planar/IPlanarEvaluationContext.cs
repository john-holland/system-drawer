using UnityEngine;

namespace SdfMax
{
    /// <summary>Planet/planar sampling for SDF primitives (stamps, lat/lon shell).</summary>
    public interface IPlanarEvaluationContext
    {
        bool TryWorldToPlanarUV(Vector3 worldPos, out int featureIndex, out Vector2 uv);
        float SampleStampHeight(int featureIndex, Vector2 uv);
        float SampleProceduralNoise(Vector2 uv, float narrativeTime, NoiseKind kind);
        bool TryWorldToLatLon(Vector3 worldPos, out float latDeg, out float lonDeg);
        float SampleHeightAtLatLon(float latDeg, float lonDeg);
    }
}
