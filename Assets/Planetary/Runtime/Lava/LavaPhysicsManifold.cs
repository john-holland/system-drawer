using System.Collections.Generic;
using Planetary.Voxel;
using UnityEngine;
using Weather;

namespace Planetary.Lava
{
    public struct VolcanoCandidate
    {
        public Vector3 worldPosition;
        public float stress;
        public float gasPressure;
    }

    public sealed class LavaPhysicsManifold : MonoBehaviour
    {
        [Range(0f, 1f)]
        public float surfaceTensionCoeff = 0.5f;

        public WeatherPhysicsManifold weatherManifold;
        public LoopEdgeMap loopEdges = new LoopEdgeMap();

        void Awake()
        {
            if (weatherManifold == null)
                SceneServiceLookup.TryResolve("weather.physicsManifold", out weatherManifold);
        }

        readonly List<VolcanoCandidate> _candidates = new List<VolcanoCandidate>();
        readonly HashSet<int> _reached = new HashSet<int>();

        public IReadOnlyList<VolcanoCandidate> Candidates => _candidates;

        public void AdvectStep(float deltaTime)
        {
            if (weatherManifold == null)
                return;
            var bounds = weatherManifold.worldBounds;
            Vector3 step = new Vector3(
                bounds.size.x / Mathf.Max(1, weatherManifold.cellCount.x),
                bounds.size.y / Mathf.Max(1, weatherManifold.cellCount.y),
                bounds.size.z / Mathf.Max(1, weatherManifold.cellCount.z));
            for (int z = 0; z < weatherManifold.cellCount.z; z++)
            for (int y = 0; y < weatherManifold.cellCount.y; y++)
            for (int x = 0; x < weatherManifold.cellCount.x; x++)
            {
                Vector3 p = bounds.min + new Vector3(x * step.x, y * step.y, z * step.z);
                var d = weatherManifold.GetDataAtPosition(p);
                if (d.mode != WeatherMode.Lava && d.mode != WeatherMode.MagmaPlume)
                    continue;
                d.lavaVelocity += Vector3.down * 9.81f * deltaTime * 0.01f;
                d.surfaceTensionCoeff = surfaceTensionCoeff;
                VolcanicEmissionThermodynamics.ApplyConvectionRadiation(ref d, deltaTime, 20f);
                weatherManifold.SetDataAtPosition(p, d);
            }
        }

        public void ScanBreaches(DualEncodedVoxelField field, int startCell, float stress)
        {
            _candidates.Clear();
            if (field == null)
                return;
            loopEdges.FloodLiquidThroughput(field, startCell, _reached);
            foreach (int cell in _reached)
            {
                if (loopEdges.TryDetectBreach(cell, cell + 1, stress, surfaceTensionCoeff))
                {
                    _candidates.Add(new VolcanoCandidate
                    {
                        worldPosition = transform.position,
                        stress = stress,
                        gasPressure = stress * 10f
                    });
                }
            }
        }
    }
}
