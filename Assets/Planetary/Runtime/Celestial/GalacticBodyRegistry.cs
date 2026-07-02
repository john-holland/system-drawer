using System.Collections.Generic;
using UnityEngine;

namespace Planetary.Celestial
{
    /// <summary>In-memory index of galactic bodies from API and scene hosts.</summary>
    public sealed class GalacticBodyRegistry : MonoBehaviour
    {
        static GalacticBodyRegistry _instance;
        public static GalacticBodyRegistry Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindAnyObjectByType<GalacticBodyRegistry>();
                return _instance;
            }
        }

        readonly Dictionary<string, GalacticBodyRecord> _byId = new Dictionary<string, GalacticBodyRecord>();
        readonly List<ICelestialBody> _sceneBodies = new List<ICelestialBody>();

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        public void LoadFromApi(IReadOnlyList<GalacticBodyRecord> records)
        {
            _byId.Clear();
            if (records == null)
                return;
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                if (!string.IsNullOrEmpty(r.bodyId))
                    _byId[r.bodyId] = r;
            }
        }

        public void RegisterSceneBody(ICelestialBody body)
        {
            if (body == null || _sceneBodies.Contains(body))
                return;
            _sceneBodies.Add(body);
        }

        public void UnregisterSceneBody(ICelestialBody body)
        {
            _sceneBodies.Remove(body);
        }

        public bool TryGetRecord(string bodyId, out GalacticBodyRecord record) =>
            _byId.TryGetValue(bodyId, out record);

        public IReadOnlyList<ICelestialBody> SceneBodies => _sceneBodies;

        public ICelestialBody FindNearestSceneBody(Vector3 worldPos, out float distance)
        {
            ICelestialBody best = null;
            distance = float.MaxValue;
            for (int i = 0; i < _sceneBodies.Count; i++)
            {
                var b = _sceneBodies[i];
                if (b?.BodyTransform == null)
                    continue;
                float d = Vector3.Distance(worldPos, b.BodyTransform.position);
                if (d < distance)
                {
                    distance = d;
                    best = b;
                }
            }
            return best;
        }

        public IEnumerable<GalacticBodyRecord> AllRecords => _byId.Values;
    }
}
