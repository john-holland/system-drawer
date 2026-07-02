using System.Collections.Generic;
using Planetary.Celestial;
using UnityEngine;

namespace Planetary
{
    [CreateAssetMenu(fileName = "QuantumTractorBeamPolicy", menuName = "Planetary/Quantum Tractor Beam Policy")]
    public sealed class QuantumTractorBeamPolicy : ScriptableObject
    {
        public bool enforceLimits = true;
        public List<string> blacklistedBodyIds = new List<string> { "sol" };
        public List<GalacticBodyKind> blacklistedKinds = new List<GalacticBodyKind> { GalacticBodyKind.Star };
        [Tooltip("0 = unlimited (Invader Zim planet-racing mode).")]
        public float maxTargetRadiusM;
        [Tooltip("0 = unlimited.")]
        public float maxTargetMassKg;
        public float maxCouplingForceN = 1e7f;

        public bool CanTarget(ICelestialBody body, out string reason)
        {
            reason = null;
            if (body == null)
            {
                reason = "no body";
                return false;
            }
            if (body.Immovable)
            {
                reason = "immovable";
                return false;
            }
            if (blacklistedBodyIds != null && blacklistedBodyIds.Contains(body.BodyId))
            {
                reason = "blacklisted id";
                return false;
            }
            if (blacklistedKinds != null && blacklistedKinds.Contains(body.Kind))
            {
                reason = "blacklisted kind";
                return false;
            }
            if (!enforceLimits)
                return true;
            if (maxTargetRadiusM > 0f && body.Radius > maxTargetRadiusM)
            {
                reason = "radius limit";
                return false;
            }
            if (maxTargetMassKg > 0f && body.Mass > maxTargetMassKg)
            {
                reason = "mass limit";
                return false;
            }
            return true;
        }
    }
}
