using UnityEngine;
using Weather;

namespace Roads
{
    /// <summary>Stamps road surface / shoulder / dirt cells into WeatherPhysicsManifold during bake.</summary>
    [AddComponentMenu("Roads/Road Physics Manifold Bridge")]
    public class RoadPhysicsManifoldBridge : MonoBehaviour
    {
        public WeatherPhysicsManifold manifold;
        public RoadSpline3D spline;
        public SplinePathMeshSampler sampler;

        [Header("Surface Coefficients")]
        public float roadFriction = 0.85f;
        public float shoulderFriction = 0.6f;
        public float dirtFriction = 0.45f;
        public float roadPorosity = 0.05f;
        public float dirtPorosity = 0.35f;

        public void StampFromBake()
        {
            if (manifold == null)
                manifold = FindAnyObjectByType<WeatherPhysicsManifold>();
            if (manifold == null || spline == null)
                return;

            spline.RebuildBakedSamples(sampler != null ? sampler.sampleSpacingMeters : 1f);
            var samples = spline.BakedSamples;
            if (samples == null)
                return;

            foreach (var s in samples)
            {
                StampZone(s.position, s.normal, s.binormal, s.width, RoadSurfaceType.RoadSurface, roadFriction, roadPorosity);
                StampZone(s.position + s.binormal * s.width * 0.35f, s.normal, s.binormal, s.width * 0.2f, RoadSurfaceType.RoadShoulder, shoulderFriction, roadPorosity * 2f);
                StampZone(s.position + s.binormal * s.width * 0.55f, s.normal, s.binormal, s.width * 0.25f, RoadSurfaceType.RoadDirt, dirtFriction, dirtPorosity);
                StampZone(s.position - s.binormal * s.width * 0.35f, s.normal, s.binormal, s.width * 0.2f, RoadSurfaceType.RoadShoulder, shoulderFriction, roadPorosity * 2f);
                StampZone(s.position - s.binormal * s.width * 0.55f, s.normal, s.binormal, s.width * 0.25f, RoadSurfaceType.RoadDirt, dirtFriction, dirtPorosity);
            }
        }

        void StampZone(Vector3 pos, Vector3 normal, Vector3 binormal, float width, RoadSurfaceType type, float friction, float porosity)
        {
            var data = manifold.GetDataAtPosition(pos);
            data.roadSurfaceType = type;
            data.surfaceFriction = friction;
            data.surfacePorosity = porosity;
            data.mode = type switch
            {
                RoadSurfaceType.RoadSurface => WeatherMode.RoadSurface,
                RoadSurfaceType.RoadShoulder => WeatherMode.RoadShoulder,
                _ => WeatherMode.RoadDirt
            };
            manifold.SetDataAtPosition(pos, data);
        }
    }
}
