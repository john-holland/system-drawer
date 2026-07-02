using UnityEngine;

namespace Planetary.Celestial
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public sealed class StarRenderer : MonoBehaviour
    {
        public StarRenderProfile profile;
        public CelestialAppearance appearance;

        MeshFilter _filter;
        MeshRenderer _renderer;
        MaterialPropertyBlock _mpb;

        void Awake()
        {
            _filter = GetComponent<MeshFilter>();
            _renderer = GetComponent<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();
            if (_filter.sharedMesh == null)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _filter.sharedMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(sphere);
            }
        }

        public void ApplyAppearance(CelestialAppearance app)
        {
            appearance = app;
            if (_renderer == null)
                return;
            Color baseColor = profile != null ? profile.color : Color.white;
            baseColor *= app.tint;
            float intensity = (profile != null ? profile.intensity : 1f) * app.intensity;
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_Color", baseColor * intensity);
            _mpb.SetFloat("_StareBackWeight", app.stareBackWeight);
            _renderer.SetPropertyBlock(_mpb);
        }

        void LateUpdate()
        {
            if (profile != null && profile.bypassBakeForNearbySun)
            {
                float scale = profile.coronaRadiusMultiplier;
                transform.localScale = Vector3.one * scale * (1f + appearance.stareBackWeight * 0.05f);
            }
        }
    }
}
