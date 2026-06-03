using UnityEngine;

namespace Locomotion.Spaceship
{
    public sealed class SpaceshipWeaponBehaviorTree : MonoBehaviour
    {
        public SpaceshipIkTrainingProfile trainingProfile;
        public bool fireOnComplete = true;

        public bool EvaluateComplete()
        {
            if (trainingProfile != null && trainingProfile.completionOnly)
                return true;
            return fireOnComplete;
        }
    }
}
