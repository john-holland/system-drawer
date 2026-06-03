using UnityEngine;

namespace Locomotion.Spaceship
{
    public enum GravityTrainingProfile
    {
        OneG = 0,
        ZeroG = 1
    }

    public sealed class SpaceshipIkTrainingProfile : MonoBehaviour
    {
        public GravityTrainingProfile profile = GravityTrainingProfile.OneG;
        public RagdollSystem ragdoll;
        public bool completionOnly = true;
    }
}
