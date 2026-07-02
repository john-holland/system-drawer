using System;
using Planetary.Composition;
using UnityEngine;

namespace Planetary.Celestial
{
    [Serializable]
    public struct GalacticTravelSnapshot
    {
        public Vector3 worldPos;
        public string nearestBodyId;
        public Vector3 surfaceAnchor;
        public float cellBlendWeight;
        public LodTier altitudeBand;
        public string activeLatticeCellId;
    }
}
