using UnityEngine;

namespace DestructibleEnvironment
{
    [CreateAssetMenu(fileName = "DestructibleMaterialProfile", menuName = "Environment/Destructible Material Profile")]
    public class DestructibleMaterialProfile : ScriptableObject
    {
        [Tooltip("Base break threshold in Newtons before material/manifold modifiers.")]
        public float baseBreakThresholdN = 500f;

        [Tooltip("Material density used for mass estimates (kg/m³).")]
        public float densityKgPerM3 = 2400f;

        [Tooltip("Minimum multiplier applied from PhysicMaterial absorb heuristic.")]
        public float minMatScale = 0.5f;

        [Tooltip("Maximum multiplier applied from PhysicMaterial absorb heuristic.")]
        public float maxMatScale = 2f;

        [Tooltip("How much manifold surface tension increases break resistance.")]
        public float tensionScale = 0.25f;

        [Tooltip("How much manifold porosity weakens break resistance.")]
        public float porosityWeakening = 0.35f;
    }
}
