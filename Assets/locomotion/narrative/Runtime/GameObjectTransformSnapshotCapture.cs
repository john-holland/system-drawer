using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    [Serializable]
    public sealed class GameObjectSnapshotEntry
    {
        public string name;
        public bool activeSelf;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public bool hasRigidbody;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
        public bool hasRigidbody2D;
        public Vector2 linearVelocity2D;
        public float angularVelocity2D;
    }

    [Serializable]
    public sealed class GameObjectSnapshotBlob
    {
        public List<GameObjectSnapshotEntry> entries = new List<GameObjectSnapshotEntry>();
    }

    [Serializable]
    public sealed class GameObjectTransformSnapshotCapture : INodeExecStateCapture
    {
        [NonSerialized] GameObjectSnapshotBlob _lastBlob;
        [NonSerialized] List<GameObject> _targets = new List<GameObject>();

        public void SetTargets(IEnumerable<GameObject> objects)
        {
            _targets.Clear();
            if (objects == null)
                return;
            foreach (var go in objects)
            {
                if (go != null)
                    _targets.Add(go);
            }
        }

        public void CaptureBeforeExec(INodeExecContext ctx)
        {
            _lastBlob = CaptureObjects(ctx?.ResolvedObjects ?? _targets);
        }

        public void RestoreBeforeExec(INodeExecContext ctx)
        {
            if (_lastBlob != null)
                ApplyBlob(_lastBlob, ctx?.ResolvedObjects ?? _targets);
        }

        public void UndoOnRewind(INodeExecContext ctx, float targetNarrativeTime) =>
            RestoreBeforeExec(ctx);

        public static GameObjectSnapshotBlob CaptureObjects(IEnumerable<GameObject> objects)
        {
            var blob = new GameObjectSnapshotBlob();
            if (objects == null)
                return blob;
            foreach (var go in objects)
            {
                if (go == null)
                    continue;
                var entry = new GameObjectSnapshotEntry
                {
                    name = go.name,
                    activeSelf = go.activeSelf,
                    localPosition = go.transform.localPosition,
                    localRotation = go.transform.localRotation,
                    localScale = go.transform.localScale
                };
                if (go.TryGetComponent<Rigidbody>(out var rb))
                {
                    entry.hasRigidbody = true;
                    entry.linearVelocity = rb.linearVelocity;
                    entry.angularVelocity = rb.angularVelocity;
                }
                if (go.TryGetComponent<Rigidbody2D>(out var rb2))
                {
                    entry.hasRigidbody2D = true;
                    entry.linearVelocity2D = rb2.linearVelocity;
                    entry.angularVelocity2D = rb2.angularVelocity;
                }
                blob.entries.Add(entry);
            }
            return blob;
        }

        public static void ApplyBlob(GameObjectSnapshotBlob blob, IEnumerable<GameObject> objects)
        {
            if (blob?.entries == null || objects == null)
                return;
            var list = new List<GameObject>();
            foreach (var go in objects)
            {
                if (go != null)
                    list.Add(go);
            }
            for (int i = 0; i < blob.entries.Count && i < list.Count; i++)
            {
                ApplyEntry(list[i], blob.entries[i]);
            }
        }

        static void ApplyEntry(GameObject go, GameObjectSnapshotEntry entry)
        {
            if (go == null || entry == null)
                return;
            go.SetActive(entry.activeSelf);
            go.transform.localPosition = entry.localPosition;
            go.transform.localRotation = entry.localRotation;
            go.transform.localScale = entry.localScale;
            if (entry.hasRigidbody && go.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = entry.linearVelocity;
                rb.angularVelocity = entry.angularVelocity;
            }
            if (entry.hasRigidbody2D && go.TryGetComponent<Rigidbody2D>(out var rb2))
            {
                rb2.linearVelocity = entry.linearVelocity2D;
                rb2.angularVelocity = entry.angularVelocity2D;
            }
        }
    }
}
