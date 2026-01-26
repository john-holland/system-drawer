// the neck has a number of muscle groups that connect the torso to the head
// traps -> pull back and to the sides
// front -> connect to ligaments and pull under the jaw

using UnityEngine;

namespace Locomotion.Musculature
{
    public sealed class RagdollNeck : RagdollBodyPart
    {
        [Header("Neck Properties")]
        [Tooltip("Reference to the left collarbone component")]
        public RagdollCollarbone leftCollarbone;

        [Tooltip("Reference to the right collarbone component")]
        public RagdollCollarbone rightCollarbone;
    }
}
