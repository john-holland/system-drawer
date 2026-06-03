using UnityEngine;

namespace Locomotion.Spaceship
{
    public sealed class RadiationPathingOptions : MonoBehaviour
    {
        public bool useFastMoverToComplete;
        public bool avoidFastMover;
        public bool liveUpdateTracking = true;
        [Range(0f, 1f)] public float accuracy = 0.8f;
        public RadiationAwarePathingSolver solver = new RadiationAwarePathingSolver();
        public FastMoverRegistry fastMovers;
    }
}
