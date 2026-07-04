using UnityEngine;

namespace Planetary.Celestial
{
    /// <summary>Exposes PlanetBody as ICelestialBody for relativity and tractor systems.</summary>
    [RequireComponent(typeof(PlanetBody))]
    public sealed class PlanetCelestialBridge : MonoBehaviour, ICelestialBody
    {
        public string galacticBodyId;
        public float densityKgPerM3 = 5500f;
        public float influenceRadiusMultiplier = 3f;
        public bool immovable;
        public Vector3 galacticPosition;
        public PhysicalManifold manifold;

        PlanetBody _planet;
        CelestialManifoldHost _host;

        PlanetBody Planet
        {
            get
            {
                if (_planet == null)
                    _planet = GetComponent<PlanetBody>();
                return _planet;
            }
        }

        public string BodyId => !string.IsNullOrEmpty(galacticBodyId) ? galacticBodyId : Planet?.societyPlanetId ?? name;
        public GalacticBodyKind Kind => GalacticBodyKind.Planet;
        public float Mass => Planet != null ? VolumeMass(Planet.PlanetRadius, densityKgPerM3) : 0f;
        public float Radius => Planet != null ? Planet.PlanetRadius : 0f;
        public Vector3 GalacticPosition => galacticPosition;
        public PhysicalManifold Manifold => manifold;
        public bool Immovable => immovable;
        public Transform BodyTransform => transform;

        static float VolumeMass(float radius, float density) =>
            (4f / 3f) * Mathf.PI * radius * radius * radius * density;

        void Awake()
        {
            _planet = GetComponent<PlanetBody>();
            if (manifold == null)
                manifold = GetComponent<PhysicalManifold>();
            if (string.IsNullOrEmpty(galacticBodyId) && _planet != null)
                galacticBodyId = _planet.societyPlanetId;
        }

        void OnEnable()
        {
            EnsureHost();
            GalacticBodyRegistry.Instance?.RegisterSceneBody(this);
        }

        void OnDisable()
        {
            GalacticBodyRegistry.Instance?.UnregisterSceneBody(this);
        }

        void EnsureHost()
        {
            if (_host == null)
                _host = GetComponent<CelestialManifoldHost>();
            if (_host == null)
                _host = gameObject.AddComponent<CelestialManifoldHost>();
            _host.celestialBody = this;
            _host.manifold = manifold;
            _host.mass = Mass;
            _host.influenceRadius = Radius * influenceRadiusMultiplier;
        }
    }
}
