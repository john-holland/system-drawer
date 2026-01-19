using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Optional material override for audio transmission/occlusion heuristics.
    /// Attach to walls/doors/props to tune how much sound passes through.
    /// </summary>
    public class AcousticMaterial : MonoBehaviour
    {
        [Tooltip("0 = blocks almost all sound, 1 = passes sound freely.")]
        [Range(0f, 1f)]
        public float transmission = 0.5f;

        [Tooltip("Additional echo contribution when sound passes through/around this object.")]
        [Range(0f, 1f)]
        public float echoContribution = 0.1f;
    }
}

