using UnityEngine;

namespace Planetary
{
    public sealed class OrbitTrajectoryCache : MonoBehaviour
    {
        public Transform center;
        public float orbitalRadius = 100f;
        public float angularSpeedDegPerSec = 5f;

        public Vector3 SamplePosition(float t)
        {
            if (center == null)
                return transform.position;
            float ang = t * angularSpeedDegPerSec * Mathf.Deg2Rad;
            return center.position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * orbitalRadius;
        }
    }
}
