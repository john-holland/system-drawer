using System;
using UnityEngine;

namespace Planetary.AsteroidBelt
{
    public static class AsteroidDestroyedEvent
    {
        public static event Action<AsteroidBody, AsteroidHitInfo> OnDestroyed;

        public static void Raise(AsteroidBody body, AsteroidHitInfo hit) =>
            OnDestroyed?.Invoke(body, hit);
    }

    public sealed class ProceduralAsteroidDestruction : MonoBehaviour
    {
        public AsteroidBody asteroidBody;
        public int shardCount = 6;
        public float shardImpulse = 5f;
        public GameObject shardPrefab;

        public void TriggerDestruction(AsteroidHitInfo hit)
        {
            if (asteroidBody == null)
                return;
            if (asteroidBody.mutationLog != null)
            {
                asteroidBody.mutationLog.Record(new AsteroidBeltMutation
                {
                    sectorIndex = asteroidBody.beltSectorIndex,
                    slotIndex = asteroidBody.beltSlotIndex,
                    kind = AsteroidMutationKind.Destroyed,
                    timestamp = Time.time,
                    destructionSeed = asteroidBody.compositionSeed
                });
            }

            var root = new GameObject($"{name}_Debris");
            root.transform.position = transform.position;
            var tracker = root.AddComponent<AsteroidDebrisTracker>();
            tracker.fadeMode = AsteroidDebrisFadeMode.Dust;

            for (int i = 0; i < shardCount; i++)
            {
                float ang = i / (float)shardCount * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(ang), UnityEngine.Random.Range(-0.3f, 0.3f), Mathf.Sin(ang)) * asteroidBody.radius * 0.5f;
                GameObject shard = shardPrefab != null
                    ? Instantiate(shardPrefab, root.transform)
                    : GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.transform.SetParent(root.transform, false);
                shard.transform.localPosition = offset;
                shard.transform.localScale = Vector3.one * asteroidBody.radius * 0.2f;
                var rb = shard.GetComponent<Rigidbody>();
                if (rb == null)
                    rb = shard.AddComponent<Rigidbody>();
                rb.AddForce((offset.normalized + hit.incomingDirection) * shardImpulse, ForceMode.Impulse);
                tracker.TrackShard(shard.transform);
            }

            AsteroidDestroyedEvent.Raise(asteroidBody, hit);
            DestroyAsteroidObject(gameObject);
        }

        static void DestroyAsteroidObject(GameObject go)
        {
            if (go == null)
                return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(go);
                return;
            }
#endif
            Destroy(go);
        }
    }
}
