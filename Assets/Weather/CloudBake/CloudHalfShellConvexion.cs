using System;
using UnityEngine;

namespace Weather.CloudBake
{
    [Serializable]
    public sealed class CloudHalfShellConvexion
    {
        [Range(-1f, 1f)] public float bias;
        [Range(0f, 1f)] public float size = 0.5f;

        public bool IsNeutral => Mathf.Approximately(bias, 0f) || size <= 0.0001f;
    }
}
