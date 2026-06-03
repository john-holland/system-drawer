using UnityEngine;

namespace Weather
{
    /// <summary>Land mass height/slope for weather modulation and liquid placement.</summary>
    public sealed class WeatherLandMass : MonoBehaviour
    {
        public MeshTerrainSampler meshTerrainSampler;
        public float slopeRainThresholdDeg = 15f;
        public float slopeRunoffMultiplier = 1.5f;
        [Range(0f, 2f)] public float precipitationSlopeScale = 1f;

        IExternalHeightProvider _external;

        void Awake() => _external = GetComponentInParent<IExternalHeightProvider>();

        public float GetPrecipitationScaleAt(Vector3 worldPos)
        {
            float slope = SampleSlope(worldPos);
            if (slope < slopeRainThresholdDeg)
                return 1f;
            return Mathf.Max(0f, 1f - (slope - slopeRainThresholdDeg) * 0.01f * precipitationSlopeScale * slopeRunoffMultiplier);
        }

        public float SampleSlope(Vector3 worldPos)
        {
            if (_external != null && _external.TrySampleHeightAtWorld(worldPos, out _, out float slopeExt))
                return slopeExt;
            if (meshTerrainSampler != null)
            {
                float h0 = meshTerrainSampler.SampleHeight(worldPos);
                float hx = meshTerrainSampler.SampleHeight(worldPos + Vector3.right) - h0;
                float hz = meshTerrainSampler.SampleHeight(worldPos + Vector3.forward) - h0;
                return Mathf.Atan(Mathf.Sqrt(hx * hx + hz * hz)) * Mathf.Rad2Deg;
            }
            return 0f;
        }
    }
}
