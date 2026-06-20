using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    [Serializable]
    public sealed class NarrativeExecutionLedgerEntry
    {
        public float time;
        public float finishTime;
        public string eventId;
        public string nodeId;
        public string storeKey;
        public string actionTypeName;
        public int undoTargetInstanceId;
    }

    [Serializable]
    public sealed class StoredNodeExecSnapshot
    {
        public string storeKey;
        public float narrativeTime;
        public string eventId;
        public string nodeId;
        public string blobJson;
    }

    /// <summary>Scene store for pre-exec GO snapshots keyed by event/node.</summary>
    [AddComponentMenu("Locomotion/Narrative/Node Exec State Store")]
    public sealed class NarrativeNodeExecStateStore : MonoBehaviour
    {
        public const string ServiceKey = "narrative.nodeExecStateStore";

        static NarrativeNodeExecStateStore _instance;
        readonly Dictionary<string, StoredNodeExecSnapshot> _snapshots = new Dictionary<string, StoredNodeExecSnapshot>();
        int _sequence;

        public static NarrativeNodeExecStateStore Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindAnyObjectByType<NarrativeNodeExecStateStore>();
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public string Capture(string eventId, string nodeId, IEnumerable<GameObject> objects, float narrativeTime)
        {
            string key = BuildKey(eventId, nodeId, ++_sequence);
            var blob = GameObjectTransformSnapshotCapture.CaptureObjects(objects);
            _snapshots[key] = new StoredNodeExecSnapshot
            {
                storeKey = key,
                narrativeTime = narrativeTime,
                eventId = eventId,
                nodeId = nodeId,
                blobJson = JsonUtility.ToJson(blob)
            };
            return key;
        }

        public bool Restore(string storeKey)
        {
            if (string.IsNullOrEmpty(storeKey) || !_snapshots.TryGetValue(storeKey, out var snap))
                return false;
            var blob = JsonUtility.FromJson<GameObjectSnapshotBlob>(snap.blobJson);
            var objects = ResolveObjectsFromSnapshot(snap);
            GameObjectTransformSnapshotCapture.ApplyBlob(blob, objects);
            return true;
        }

        public bool TryGet(string storeKey, out StoredNodeExecSnapshot snapshot) =>
            _snapshots.TryGetValue(storeKey, out snapshot);

        public void PopUndoStackUntil(float targetTime, List<NarrativeExecutionLedgerEntry> ledger)
        {
            if (ledger == null)
                return;
            for (int i = ledger.Count - 1; i >= 0; i--)
            {
                var entry = ledger[i];
                if (entry.time <= targetTime)
                    break;
                if (!string.IsNullOrEmpty(entry.storeKey))
                    Restore(entry.storeKey);
            }
        }

        static string BuildKey(string eventId, string nodeId, int sequence) =>
            (eventId ?? "evt") + ":" + (nodeId ?? "node") + ":" + sequence;

        static List<GameObject> ResolveObjectsFromSnapshot(StoredNodeExecSnapshot snap)
        {
            var list = new List<GameObject>();
            if (snap?.blobJson == null)
                return list;
            var blob = JsonUtility.FromJson<GameObjectSnapshotBlob>(snap.blobJson);
            if (blob?.entries == null)
                return list;
            for (int i = 0; i < blob.entries.Count; i++)
            {
                var go = GameObject.Find(blob.entries[i].name);
                if (go != null)
                    list.Add(go);
            }
            return list;
        }
    }
}
