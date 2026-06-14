using Planetary.Field;
using UnityEngine;
using Weather;

namespace Planetary.Rendering
{
    public sealed class PlanetaryRegionMaterialController : MonoBehaviour
    {
        public PhysicsManifoldPhaseShaderMap phaseMap;
        public WeatherPhysicsManifold manifold;
        public CanonicalSpatiotemporalField canonicalField;
        public Material targetMaterial;
        public Transform player;

        void Awake()
        {
            if (canonicalField == null)
                canonicalField = CanonicalSpatiotemporalField.Resolve();
        }

        void Update()
        {
            if (phaseMap == null || targetMaterial == null || player == null)
                return;

            ManifoldCellData data;
            if (canonicalField != null && canonicalField.TrySampleBlended(player.position, Time.time, out SpatiotemporalSample sample))
                data = sample.cell;
            else
            {
                if (manifold == null)
                    return;
                data = manifold.GetDataAtPosition(player.position);
            }

            var phase = phaseMap.Sample(data);
            if (!string.IsNullOrEmpty(phase.shaderKeyword))
                targetMaterial.EnableKeyword(phase.shaderKeyword);
            targetMaterial.color = phase.albedo;
            if (manifold != null)
                WeatherShaderLibrary.SetupShaderProperties(manifold, targetMaterial);
        }
    }
}
