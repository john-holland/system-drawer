using System;
using UnityEngine;

namespace Planetary.Elemental
{
    [Serializable]
    public sealed class PlateDefinition
    {
        public int plateId;
        public Vector3 seedDirection;
        public MineralStack minerals = new MineralStack();
        public float thicknessMeters = 35000f;
        public Vector3 velocityTangent;
        public float stressAccumulator;
        public float boundaryCompatibility = 1f;
    }
}
