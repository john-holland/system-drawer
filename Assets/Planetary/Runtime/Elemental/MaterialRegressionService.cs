using System.Collections.Generic;
using UnityEngine;

namespace Planetary.Elemental
{
    public sealed class MaterialRegressionService
    {
        readonly ElementalCompositionRulesEngine _engine = new ElementalCompositionRulesEngine();

        public ElementalCompositionRulesEngine Engine => _engine;

        public MineralStack RegressToMinerals(MaterialSpec spec) => _engine.RegressToMinerals(spec);

        public PlateDefinition[] RegressPlatesFromSurface(PlanetBody body, int plateCount, ElementalRule[] rules)
        {
            _engine.SetRules(rules);
            plateCount = Mathf.Clamp(plateCount, 2, 32);
            var plates = new List<PlateDefinition>();
            float golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
            for (int i = 0; i < plateCount; i++)
            {
                float y = 1f - (i + 0.5f) * 2f / plateCount;
                float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
                float theta = golden * i;
                var dir = new Vector3(Mathf.Cos(theta) * r, y, Mathf.Sin(theta) * r).normalized;
                var spec = SampleSurfaceMaterial(body, dir);
                plates.Add(new PlateDefinition
                {
                    plateId = i,
                    seedDirection = dir,
                    minerals = _engine.RegressToMinerals(spec),
                    thicknessMeters = 30000f + spec.densityKgM3 * 100f,
                    velocityTangent = Vector3.Cross(dir, Vector3.up).normalized * 0.01f
                });
            }
            for (int i = 0; i < plates.Count; i++)
            {
                float minCompat = 1f;
                for (int j = 0; j < plates.Count; j++)
                {
                    if (i == j)
                        continue;
                    float c = SphereVoronoiPlates.MineralCompatibility(plates[i].minerals, plates[j].minerals);
                    minCompat = Mathf.Min(minCompat, c);
                }
                plates[i].boundaryCompatibility = minCompat;
                plates[i].stressAccumulator = (1f - minCompat) * 10f;
            }
            return plates.ToArray();
        }

        static MaterialSpec SampleSurfaceMaterial(PlanetBody body, Vector3 dir)
        {
            if (body == null || body.planarBase == null)
            {
                return new MaterialSpec
                {
                    tags = new[] { "crust", "silicate" },
                    densityKgM3 = 2700f,
                    porosity = 0.1f,
                    albedo = new Color(0.4f, 0.35f, 0.3f)
                };
            }
            var sc = SphericalCoordinates.FromWorldPosition(dir * body.PlanetRadius, Vector3.zero, body.StablePoleAxis, body.PrimeMeridianOffsetDeg);
            float h = body.planarBase.SampleHeight(sc.LatitudeDeg, sc.LongitudeDeg);
            float slope = body.planarBase.SampleSlope(sc.LatitudeDeg, sc.LongitudeDeg);
            var tags = new List<string> { "crust", "silicate" };
            if (h < 0f)
                tags.Add("ocean");
            if (slope > 25f)
                tags.Add("mountain");
            return new MaterialSpec
            {
                tags = tags.ToArray(),
                densityKgM3 = h < 0f ? 1030f : 2700f,
                porosity = 0.05f + slope * 0.001f,
                albedo = h < 0f ? new Color(0.1f, 0.2f, 0.5f) : new Color(0.45f, 0.4f, 0.35f)
            };
        }
    }
}
