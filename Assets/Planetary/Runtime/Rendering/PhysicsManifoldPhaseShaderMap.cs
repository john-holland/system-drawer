using UnityEngine;
using Weather;

namespace Planetary.Rendering
{
    [CreateAssetMenu(fileName = "PhysicsManifoldPhaseShaderMap", menuName = "Planetary/Phase Shader Map")]
    public sealed class PhysicsManifoldPhaseShaderMap : ScriptableObject
    {
        [System.Serializable]
        public struct PhaseRange
        {
            public float tempMinC;
            public float tempMaxC;
            public float pressureMinHpa;
            public float pressureMaxHpa;
            public Color albedo;
            public string shaderKeyword;
        }

        public PhaseRange[] ranges = System.Array.Empty<PhaseRange>();

        public PhaseRange Sample(ManifoldCellData cell)
        {
            if (ranges == null || ranges.Length == 0)
                return default;
            for (int i = 0; i < ranges.Length; i++)
            {
                var r = ranges[i];
                if (cell.temperature >= r.tempMinC && cell.temperature <= r.tempMaxC
                    && cell.pressure >= r.pressureMinHpa && cell.pressure <= r.pressureMaxHpa)
                    return r;
            }
            return ranges[0];
        }
    }
}
