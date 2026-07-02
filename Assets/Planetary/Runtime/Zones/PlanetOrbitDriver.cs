using Planetary.Celestial;
using UnityEngine;

namespace Planetary
{
    /// <summary>Lightweight orbit shift for planetoids under tractor coupling.</summary>
    public sealed class PlanetOrbitDriver : MonoBehaviour
    {
        public ICelestialBody celestialBody;
        public bool immovable;
        Vector3 _accumulatedDelta;

        void Awake()
        {
            if (celestialBody == null)
                celestialBody = GetComponent<ICelestialBody>();
        }

        public void ApplyDelta(Vector3 worldDelta)
        {
            if (immovable || (celestialBody != null && celestialBody.Immovable))
                return;
            _accumulatedDelta += worldDelta;
            transform.position += worldDelta;
        }

        public Vector3 AccumulatedDelta => _accumulatedDelta;
    }
}
