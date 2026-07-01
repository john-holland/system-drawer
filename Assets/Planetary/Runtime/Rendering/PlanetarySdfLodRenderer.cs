using Planetary.Composition;
using UnityEngine;

namespace Planetary.Rendering
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class PlanetarySdfLodRenderer : MonoBehaviour
    {
        public Material lodMaterial;
        public PlanetarySdfLodProfile profile;
        public HorizonLodSettings horizonSettings;

        readonly PlanetarySdfLodBaker _baker = new PlanetarySdfLodBaker();
        PlanetarySdfLodController _controller;
        PlanetaryLodHandoffController _handoff;
        MaterialPropertyBlock _mpb;
        MeshFilter _filter;
        MeshRenderer _renderer;
        PlanetBody _body;

        static readonly int DetailCoeffId = Shader.PropertyToID("_DetailCoeff");
        static readonly int HorizonSdfWeightId = Shader.PropertyToID("_HorizonSdfWeight");
        static readonly int RevealNadirId = Shader.PropertyToID("_RevealAmountNadir");
        static readonly int PlanetCenterId = Shader.PropertyToID("_PlanetCenter");
        static readonly int CameraWorldId = Shader.PropertyToID("_CameraWorld");
        static readonly int HorizonStartId = Shader.PropertyToID("_HorizonStart");
        static readonly int HorizonEndId = Shader.PropertyToID("_HorizonEnd");

        void Awake()
        {
            _filter = GetComponent<MeshFilter>();
            _renderer = GetComponent<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();
            _body = GetComponentInParent<PlanetBody>();
            _controller = new PlanetarySdfLodController(profile, horizonSettings);
        }

        public void Rebake()
        {
            EnsureRenderRefs();
            if (_body == null || _filter == null)
                return;
            _baker.RebuildTiers(_body, profile);
            ApplyHighestTierMesh();
        }

        /// <summary>Bakes LOD tiers once when missing; safe to call from play-mode startup.</summary>
        public void EnsureLodMeshes()
        {
            EnsureRenderRefs();
            if (_body == null || _filter == null || profile == null || _body.composition == null)
                return;
            if (_baker.TierMeshes.Count == 0)
                Rebake();
            else
                ApplyHighestTierMesh();
        }

        void EnsureRenderRefs()
        {
            if (_filter == null)
                _filter = GetComponent<MeshFilter>();
            if (_renderer == null)
                _renderer = GetComponent<MeshRenderer>();
            if (_body == null)
                _body = GetComponentInParent<PlanetBody>();
        }

        void ApplyHighestTierMesh()
        {
            if (_filter == null || _baker.TierMeshes.Count == 0)
                return;
            _filter.sharedMesh = _baker.TierMeshes[_baker.TierMeshes.Count - 1];
        }

        void LateUpdate()
        {
            if (_body == null || _renderer == null)
                return;
            if (_baker.TierMeshes.Count == 0)
                EnsureLodMeshes();
            if (_handoff == null && _body.streamingService != null)
                _handoff = new PlanetaryLodHandoffController(_body.streamingService);
            _handoff?.Tick(_body, Camera.main);
            float reveal = _handoff != null ? _handoff.RevealNadir : 0f;
            var frame = _controller.Compute(
                Camera.main != null ? Camera.main.transform.position : transform.position,
                _body.PlanetCenter,
                _body.PlanetRadius,
                0f,
                1000f,
                3000f,
                reveal);
            int tierCount = _baker.TierMeshes.Count;
            if (tierCount > 0)
            {
                float idx = frame.detailCoeff * (tierCount - 1);
                int ti = Mathf.Clamp(Mathf.RoundToInt(idx), 0, tierCount - 1);
                _filter.sharedMesh = _baker.TierMeshes[ti];
            }
            _mpb.SetFloat(DetailCoeffId, frame.detailCoeff);
            _mpb.SetFloat(HorizonSdfWeightId, frame.horizonSdfWeight);
            _mpb.SetFloat(RevealNadirId, frame.revealNadir);
            _mpb.SetVector(PlanetCenterId, _body.PlanetCenter);
            _mpb.SetVector(CameraWorldId, Camera.main != null ? Camera.main.transform.position : transform.position);
            if (profile != null)
            {
                _mpb.SetFloat(HorizonStartId, profile.horizonStart);
                _mpb.SetFloat(HorizonEndId, profile.horizonEnd);
            }
            if (lodMaterial != null)
            {
                _renderer.SetPropertyBlock(_mpb);
                if (_renderer.sharedMaterial != lodMaterial)
                    _renderer.sharedMaterial = lodMaterial;
            }
        }
    }
}
