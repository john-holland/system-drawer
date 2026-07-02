using UnityEngine;

namespace Planetary.Celestial
{
    /// <summary>Star host: physics manifold, galactic position, relativity registration.</summary>
    [AddComponentMenu("Planetary/Celestial/Star Body")]
    public sealed class StarBody : MonoBehaviour, ICelestialBody
    {
        public string galacticBodyId = "sol";
        public float mass = 1.989e30f;
        public float radius = 696340000f;
        public float influenceRadius = 1e12f;
        public bool immovable = true;
        public Vector3 galacticPosition;
        public PhysicalManifold manifold;
        public StarRenderProfile renderProfile;
        public StarRenderer starRenderer;
        public string lemmaColorId;
        public string lemmaVisibilityId;

        CelestialManifoldHost _manifoldHost;

        public string BodyId => galacticBodyId;
        public GalacticBodyKind Kind => GalacticBodyKind.Star;
        public float Mass => mass;
        public float Radius => radius;
        public Vector3 GalacticPosition => galacticPosition;
        public PhysicalManifold Manifold => manifold;
        public bool Immovable => immovable;
        public Transform BodyTransform => transform;

        void Awake()
        {
            if (manifold == null)
                manifold = GetComponent<PhysicalManifold>();
            if (starRenderer == null)
                starRenderer = GetComponentInChildren<StarRenderer>();
            if (starRenderer != null && renderProfile != null)
                starRenderer.profile = renderProfile;
            EnsureManifoldHost();
        }

        void OnEnable()
        {
            EnsureManifoldHost();
            GalacticBodyRegistry.Instance?.RegisterSceneBody(this);
        }

        void OnDisable()
        {
            GalacticBodyRegistry.Instance?.UnregisterSceneBody(this);
        }

        void EnsureManifoldHost()
        {
            if (_manifoldHost == null)
                _manifoldHost = GetComponent<CelestialManifoldHost>();
            if (_manifoldHost == null)
                _manifoldHost = gameObject.AddComponent<CelestialManifoldHost>();
            _manifoldHost.celestialBody = this;
            _manifoldHost.manifold = manifold;
            _manifoldHost.mass = mass;
            _manifoldHost.influenceRadius = influenceRadius;
        }

        public void ApplyAppearance(CelestialAppearance appearance)
        {
            if (starRenderer != null)
                starRenderer.ApplyAppearance(appearance);
        }
    }
}
