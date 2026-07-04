using UnityEngine;

namespace Locomotion.Drink
{
    /// <summary>Slosh mass and material hardness for flow attenuation.</summary>
    public sealed class DrinkLiquidContent : MonoBehaviour
    {
        [Range(0f, 1f)]
        [Tooltip("Higher = harder material, lower flow deformation.")]
        public float materialHardness = 0.3f;

        [Tooltip("Slosh mass in kg.")]
        public float sloshMassKg = 0.5f;
    }
}
