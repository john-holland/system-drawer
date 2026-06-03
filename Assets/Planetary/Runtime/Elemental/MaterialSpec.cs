using System;
using UnityEngine;

namespace Planetary.Elemental
{
    [Serializable]
    public struct MaterialSpec
    {
        public string[] tags;
        public float densityKgM3;
        public float porosity;
        public Color albedo;
    }
}
