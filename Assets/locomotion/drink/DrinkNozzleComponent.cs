using UnityEngine;

namespace Locomotion.Drink
{
    /// <summary>Nozzle outlet on a drink vessel.</summary>
    public sealed class DrinkNozzleComponent : MonoBehaviour
    {
        [Header("Geometry")]
        public Transform tip;
        public float apertureRadiusM = 0.004f;
        public float maxThroughputLitersPerSecond = 0.05f;

        [Header("Loop")]
        public bool loopPourActive;

        public Vector3 TipPosition => tip != null ? tip.position : transform.position;
        public Vector3 TipForward => tip != null ? tip.forward : transform.forward;

        public float EffectiveApertureArea(float angleEfficiency = 1f) =>
            Mathf.PI * apertureRadiusM * apertureRadiusM * Mathf.Clamp01(angleEfficiency);
    }
}
