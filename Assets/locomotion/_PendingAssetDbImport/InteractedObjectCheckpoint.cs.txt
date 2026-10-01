using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// First-seen snapshots of objects involved in edit-mode contact / physics.
/// Reset restores those poses; live webcam bone writes do not dirty the set.
/// </summary>
public sealed class InteractedObjectCheckpoint
{
    public struct Entry
    {
        public GameObject go;
        public bool activeSelf;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
        public bool hasRigidbody;
    }

    readonly Dictionary<int, Entry> _firstSeen = new Dictionary<int, Entry>();
    readonly List<GoodSection> _enabledSections = new List<GoodSection>();
    PhysicsCardSolver _solver;
    bool _dirty;

    public bool CanReset => _dirty && _firstSeen.Count > 0;

    public int SnapshotCount => _firstSeen.Count;

    public void RememberFirstSeen(GameObject go)
    {
        if (go == null)
            return;
        int id = go.GetInstanceID();
        if (_firstSeen.ContainsKey(id))
            return;
        var e = Capture(go);
        _firstSeen[id] = e;
    }

    public void MarkDirtyFromGoodSection(GoodSection section, PhysicsCardSolver solver)
    {
        if (section == null)
            return;
        _solver = solver;
        if (!_enabledSections.Contains(section))
            _enabledSections.Add(section);
        _dirty = true;
    }

    public void MarkDirtyFromPhysicsTranslation(GameObject go)
    {
        if (go == null)
            return;
        RememberFirstSeen(go);
        int id = go.GetInstanceID();
        if (!_firstSeen.TryGetValue(id, out var e))
            return;
        var t = go.transform;
        if ((t.localPosition - e.localPosition).sqrMagnitude > 1e-8f ||
            Quaternion.Angle(t.localRotation, e.localRotation) > 0.01f)
            _dirty = true;
    }

    public void Reset()
    {
        foreach (var kv in _firstSeen)
        {
            var e = kv.Value;
            if (e.go == null)
                continue;
            e.go.SetActive(e.activeSelf);
            e.go.transform.localPosition = e.localPosition;
            e.go.transform.localRotation = e.localRotation;
            e.go.transform.localScale = e.localScale;
            var rb = e.go.GetComponent<Rigidbody>();
            if (rb != null && e.hasRigidbody)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        if (_solver != null && _enabledSections.Count > 0)
            _solver.RemoveCards(_enabledSections);
        _enabledSections.Clear();
        _firstSeen.Clear();
        _dirty = false;
        _solver = null;
    }

    public static Entry Capture(GameObject go)
    {
        var e = new Entry
        {
            go = go,
            activeSelf = go.activeSelf,
            localPosition = go.transform.localPosition,
            localRotation = go.transform.localRotation,
            localScale = go.transform.localScale
        };
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            e.hasRigidbody = true;
            e.linearVelocity = rb.linearVelocity;
            e.angularVelocity = rb.angularVelocity;
        }
        return e;
    }
}
