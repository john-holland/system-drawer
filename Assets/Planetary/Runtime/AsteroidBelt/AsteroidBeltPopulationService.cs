using System.Collections.Generic;
using UnityEngine;

namespace Planetary.AsteroidBelt
{
    public sealed class AsteroidBeltPopulationService : MonoBehaviour
    {
        public AsteroidBeltStatisticalManifold manifold;
        public AsteroidBeltMutationLog mutationLog;
        public GameObject asteroidPrefab;
        public float spawnRadiusM = 2e8f;
        public int slotsPerSector = 12;
        public int sectorCount = 64;

        readonly Dictionary<int, List<GameObject>> _spawnedBySector = new Dictionary<int, List<GameObject>>();
        bool _active;

        void Awake()
        {
            if (manifold == null)
                manifold = GetComponent<AsteroidBeltStatisticalManifold>();
            if (mutationLog == null)
                mutationLog = ScriptableObject.CreateInstance<AsteroidBeltMutationLog>();
        }

        public void SetActive(bool active, Vector3 observerWorld)
        {
            if (active == _active && !active)
                return;
            _active = active;
            if (!active)
            {
                DespawnAll();
                return;
            }
            int sector = SectorFromWorld(observerWorld);
            EnsureSectorSpawned(sector);
            EnsureSectorSpawned((sector + 1) % sectorCount);
            EnsureSectorSpawned((sector - 1 + sectorCount) % sectorCount);
        }

        public Vector3 ComputeSlotPosition(int sectorIndex, int slotIndex)
        {
            int seed = manifold != null ? manifold.seed : 12345;
            if (mutationLog != null)
                seed = mutationLog.beltSeed;
            var rng = new System.Random(SeedCombine(seed, sectorIndex, slotIndex));
            float t = slotIndex / (float)Mathf.Max(1, slotsPerSector);
            float ang = (sectorIndex + (float)rng.NextDouble()) / sectorCount * Mathf.PI * 2f;
            float inner = manifold != null ? manifold.innerRadiusM : 1e8f;
            float outer = manifold != null ? manifold.outerRadiusM : 4e8f;
            float r = Mathf.Lerp(inner, outer, t);
            Vector3 center = manifold != null && manifold.parentPlanet != null
                ? manifold.parentPlanet.position
                : transform.position;
            Vector3 pos = center + new Vector3(Mathf.Cos(ang) * r, ((float)rng.NextDouble() * 2f - 1f) * 1000f, Mathf.Sin(ang) * r);
            if (mutationLog != null && mutationLog.TryGetMutation(sectorIndex, slotIndex, out var mut))
                pos += mut.deltaPosition;
            return pos;
        }

        void EnsureSectorSpawned(int sectorIndex)
        {
            if (_spawnedBySector.ContainsKey(sectorIndex))
                return;
            var list = new List<GameObject>();
            for (int slot = 0; slot < slotsPerSector; slot++)
            {
                if (mutationLog != null && mutationLog.IsSlotDestroyed(sectorIndex, slot))
                    continue;
                Vector3 pos = ComputeSlotPosition(sectorIndex, slot);
                GameObject go = asteroidPrefab != null
                    ? Instantiate(asteroidPrefab, pos, Random.rotation, transform)
                    : CreateDefaultAsteroid(pos);
                var body = go.GetComponent<AsteroidBody>();
                if (body == null)
                    body = go.AddComponent<AsteroidBody>();
                body.beltSectorIndex = sectorIndex;
                body.beltSlotIndex = slot;
                body.mutationLog = mutationLog;
                body.compositionSeed = SeedCombine(manifold != null ? manifold.seed : 0, sectorIndex, slot);
                list.Add(go);
            }
            _spawnedBySector[sectorIndex] = list;
        }

        static GameObject CreateDefaultAsteroid(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * Random.Range(20f, 80f);
            return go;
        }

        void DespawnAll()
        {
            foreach (var kv in _spawnedBySector)
            {
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    if (kv.Value[i] != null)
                        Destroy(kv.Value[i]);
                }
            }
            _spawnedBySector.Clear();
        }

        int SectorFromWorld(Vector3 world)
        {
            Vector3 center = manifold != null && manifold.parentPlanet != null
                ? manifold.parentPlanet.position
                : transform.position;
            Vector3 d = world - center;
            float ang = Mathf.Atan2(d.z, d.x);
            if (ang < 0f)
                ang += Mathf.PI * 2f;
            return Mathf.Clamp(Mathf.FloorToInt(ang / (Mathf.PI * 2f) * sectorCount), 0, sectorCount - 1);
        }

        static int SeedCombine(int a, int b, int c) => a ^ (b * 73856093) ^ (c * 19349663);
    }
}
