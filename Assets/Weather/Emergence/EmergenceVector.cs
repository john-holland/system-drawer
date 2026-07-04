using System;
using UnityEngine;

namespace Weather.Emergence
{
    [Serializable]
    public struct EmergenceVector
    {
        public Vector3 origin;
        public Vector3 direction;
        public float length;
        public float influenceRadius;
        public float weight;
        public string sourceId;
        public float expiresAt;

        public bool IsExpired(float now) => expiresAt > 0f && now > expiresAt;

        public static EmergenceVector Segment(Vector3 a, Vector3 b, float radius, float weight, string sourceId, float ttlSeconds = 0f)
        {
            Vector3 delta = b - a;
            float len = delta.magnitude;
            Vector3 dir = len > 1e-4f ? delta / len : Vector3.forward;
            return new EmergenceVector
            {
                origin = a,
                direction = dir,
                length = len,
                influenceRadius = radius,
                weight = weight,
                sourceId = sourceId,
                expiresAt = ttlSeconds > 0f ? Time.time + ttlSeconds : 0f,
            };
        }

        public static EmergenceVector Point(Vector3 origin, float radius, float weight, string sourceId)
        {
            return new EmergenceVector
            {
                origin = origin,
                direction = Vector3.up,
                length = 0f,
                influenceRadius = radius,
                weight = weight,
                sourceId = sourceId,
            };
        }
    }
}
