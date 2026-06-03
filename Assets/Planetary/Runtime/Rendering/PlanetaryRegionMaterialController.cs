using UnityEngine;
using Weather;

namespace Planetary.Rendering
{
    public sealed class PlanetaryRegionMaterialController : MonoBehaviour
    {
        public PhysicsManifoldPhaseShaderMap phaseMap;
        public WeatherPhysicsManifold manifold;
        public Material targetMaterial;
        public Transform player;

        void Update()
        {
            if (phaseMap == null || manifold == null || targetMaterial == null || player == null)
                return;
            var data = manifold.GetDataAtPosition(player.position);
            var phase = phaseMap.Sample(data);
            if (!string.IsNullOrEmpty(phase.shaderKeyword))
                targetMaterial.EnableKeyword(phase.shaderKeyword);
            targetMaterial.color = phase.albedo;
            WeatherShaderLibrary.SetupShaderProperties(manifold, targetMaterial);
        }
    }
}
