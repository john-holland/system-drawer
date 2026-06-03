using Planetary.Elemental;
using Planetary.Voxel;
using UnityEngine;

namespace Planetary.Tectonics
{
    public sealed class PlateTectonicsPhysicsSolver : MonoBehaviour
    {
        public PlanetBody planet;
        public PlateDefinition[] plates = System.Array.Empty<PlateDefinition>();
        public LoopEdgeMap loopEdges = new LoopEdgeMap();
        public float gravitationalConstant = 6.674e-11f;

        public void Step(float deltaTime, Vector3 playerWorld)
        {
            if (plates == null || plates.Length < 2)
                return;
            float planetMass = 5.97e24f;
            float r = planet != null ? planet.PlanetRadius : 6371000f;
            for (int i = 0; i < plates.Length; i++)
            {
                var p = plates[i];
                float localDensity = p.minerals?.GetWeight("silicate") ?? 0.5f;
                float gLocal = gravitationalConstant * planetMass / (r * r) * (1f + localDensity);
                p.velocityTangent += p.velocityTangent.normalized * gLocal * deltaTime * 1e-20f;
                for (int j = i + 1; j < plates.Length; j++)
                {
                    float compat = SphereVoronoiPlates.MineralCompatibility(p.minerals, plates[j].minerals);
                    float collisionStress = (1f - compat) * Vector3.Distance(p.velocityTangent, plates[j].velocityTangent);
                    p.stressAccumulator += collisionStress;
                    plates[j].stressAccumulator += collisionStress;
                    if (loopEdges != null && collisionStress > 1f)
                        loopEdges.TryDetectBreach(i, j, collisionStress, 1f - compat);
                }
                plates[i] = p;
            }
            System.Array.Sort(plates, (a, b) =>
            {
                float da = planet != null ? Vector3.Distance(playerWorld, a.seedDirection * planet.PlanetRadius) : 0f;
                float db = planet != null ? Vector3.Distance(playerWorld, b.seedDirection * planet.PlanetRadius) : 0f;
                return da.CompareTo(db);
            });
        }
    }
}
