using System.Collections.Generic;
using UnityEngine;

namespace Planetary.AsteroidBelt
{
    public enum AsteroidDebrisFadeMode
    {
        Dust,
        TeleportMine
    }

    public sealed class AsteroidDebrisTracker : MonoBehaviour
    {
        public AsteroidDebrisFadeMode fadeMode = AsteroidDebrisFadeMode.Dust;
        public float shardLifetimeS = 8f;
        public ParticleSystem dustParticlePrefab;
        public ParticleSystem teleportParticlePrefab;
        public Material teleportFadeMaterial;

        readonly List<Transform> _shards = new List<Transform>();
        float _startTime;
        MaterialPropertyBlock _mpb;

        void Awake()
        {
            _startTime = Time.time;
            _mpb = new MaterialPropertyBlock();
            if (fadeMode == AsteroidDebrisFadeMode.Dust && dustParticlePrefab != null)
                Instantiate(dustParticlePrefab, transform.position, Quaternion.identity, transform);
            if (fadeMode == AsteroidDebrisFadeMode.TeleportMine && teleportParticlePrefab != null)
                Instantiate(teleportParticlePrefab, transform.position, Quaternion.identity, transform);
        }

        public void TrackShard(Transform shard)
        {
            if (shard != null)
                _shards.Add(shard);
        }

        void Update()
        {
            float t = (Time.time - _startTime) / Mathf.Max(0.01f, shardLifetimeS);
            float fade = 1f - Mathf.Clamp01(t);
            for (int i = _shards.Count - 1; i >= 0; i--)
            {
                if (_shards[i] == null)
                {
                    _shards.RemoveAt(i);
                    continue;
                }
                _shards[i].localScale = Vector3.one * fade * 0.5f;
                if (fadeMode == AsteroidDebrisFadeMode.TeleportMine)
                {
                    var r = _shards[i].GetComponent<Renderer>();
                    if (r != null && teleportFadeMaterial != null)
                    {
                        r.sharedMaterial = teleportFadeMaterial;
                        r.GetPropertyBlock(_mpb);
                        _mpb.SetFloat("_Fade", fade);
                        r.SetPropertyBlock(_mpb);
                    }
                }
            }
            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}
