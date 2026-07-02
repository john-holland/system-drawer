using UnityEngine;

namespace Planetary.Celestial
{
    /// <summary>Auto-registers celestial bodies with relativity solver and large-object field array.</summary>
    [DisallowMultipleComponent]
    public sealed class CelestialManifoldHost : MonoBehaviour
    {
        public ICelestialBody celestialBody;
        public PhysicalManifold manifold;
        public float mass = 1e24f;
        public float influenceRadius = 1e7f;
        public PhysicalManifoldRelativitySolver relativitySolver;
        public LargeSpaceObjectFieldArray largeObjectField;

        bool _registered;

        void OnEnable() => Register();
        void OnDisable() => Unregister();

        public void Register()
        {
            if (_registered)
                return;
            if (celestialBody != null)
            {
                mass = celestialBody.Mass;
                manifold = celestialBody.Manifold;
            }
            if (relativitySolver == null)
                relativitySolver = FindAnyObjectByType<PhysicalManifoldRelativitySolver>();
            if (largeObjectField == null)
                largeObjectField = FindAnyObjectByType<LargeSpaceObjectFieldArray>();
            if (relativitySolver != null)
                relativitySolver.RegisterObject(transform, mass, influenceRadius);
            if (largeObjectField != null)
                largeObjectField.Track(transform, mass);
            if (manifold != null)
            {
                manifold.gravityWellStrength = celestialBody != null
                    ? Mathf.Max(0.01f, celestialBody.Manifold != null ? celestialBody.Manifold.gravityWellStrength : 1f)
                    : manifold.gravityWellStrength;
            }
            _registered = true;
        }

        public void Unregister()
        {
            if (!_registered)
                return;
            if (relativitySolver != null)
                relativitySolver.UnregisterObject(transform);
            if (largeObjectField != null)
                largeObjectField.Untrack(transform);
            _registered = false;
        }
    }
}
