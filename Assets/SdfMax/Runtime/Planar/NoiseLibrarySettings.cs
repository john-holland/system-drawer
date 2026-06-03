using System;
using UnityEngine;

namespace SdfMax
{
    [Serializable]
    public sealed class NoiseLibrarySettings
    {
        public int seed = 1;
        public float frequency = 0.01f;
        public int octaves = 4;
        public float persistence = 0.5f;
        public float lacunarity = 2f;
        public float amplitude = 1f;
    }
}
