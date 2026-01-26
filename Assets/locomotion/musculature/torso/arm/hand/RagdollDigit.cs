using UnityEngine;

namespace Locomotion.Musculature
{
    /// <summary>
    /// Marker for a digit bone in a finger chain.
    /// </summary>
    public sealed class RagdollDigit : MonoBehaviour
    {
        public int indexInFinger = 0;

        [Tooltip("If true, this is the last digit in the chain (caboose digit).")]
        public bool isCabooseDigit = false;

        [Tooltip("Optional nailbed mesh generated on caboose digit.")]
        public RagdollNailbed nailbed;
    }
}
