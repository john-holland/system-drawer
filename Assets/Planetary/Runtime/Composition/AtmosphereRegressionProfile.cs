using System;
using UnityEngine;

namespace Planetary.Composition
{
    [CreateAssetMenu(fileName = "AtmosphereRegressionProfile", menuName = "Planetary/Atmosphere Regression Profile")]
    public sealed class AtmosphereRegressionProfile : ScriptableObject
    {
        public float pressureScaleHeightM = 8500f;
        public float cloudBaseM = 1000f;
        public float cloudTopM = 3000f;
        public float cloudDensityCoeff = 0.5f;
        public float troposphereTopM = 12000f;
        public float stratosphereShellM = 50000f;
        public float[] cloudTypeWeights = { 1f, 0.5f, 0.3f, 0.8f, 0.6f, 0.4f, 0.4f };
    }
}
