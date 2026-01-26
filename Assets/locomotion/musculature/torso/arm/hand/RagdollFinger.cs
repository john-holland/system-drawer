using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Musculature
{
    public enum FingerKind
    {
        Thumb,
        Index,
        Middle,
        Ring,
        Pinky,
        Extra
    }

    /// <summary>
    /// Marker for a finger root. Digits are ordered proximal->distal.
    /// </summary>
    public sealed class RagdollFinger : MonoBehaviour
    {
        public BodySide side;
        public FingerKind kind;

        [Tooltip("Digit components from proximal->distal.")]
        public List<RagdollDigit> digits = new List<RagdollDigit>(4);

        [Tooltip("If true, this finger is a 'caboose' partial finger placeholder.")]
        public bool isCaboose = false;
    }
}
