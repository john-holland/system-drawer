using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DamagedObjectRecord
{
    public string objectId;
    public string buildingId;
    public float damage01;
    public BuildingMaterialClass materialClass;
    public Vector3 worldPos;
    public long reportedAtUnix;
    public string waypointGroup;
    public bool resolved;
    [NonSerialized] public GameObject source;
}

/// <summary>Queue of damaged objects for CivicCard repair crews.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Damaged Object Queue")]
public sealed class DamagedObjectQueue : MonoBehaviour
{
    public const string ServiceKey = "civil.damagedObjects";

    static DamagedObjectQueue _instance;
    public static DamagedObjectQueue Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<DamagedObjectQueue>();
            return _instance;
        }
    }

    public int maxRecords = 256;
    public readonly List<DamagedObjectRecord> records = new List<DamagedObjectRecord>();

    void Awake() => _instance = this;
    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public void Enqueue(DamagedObjectRecord rec)
    {
        if (rec == null) return;
        for (int i = 0; i < records.Count; i++)
        {
            var r = records[i];
            if (r == null || r.resolved) continue;
            if (r.objectId == rec.objectId && r.buildingId == rec.buildingId)
            {
                r.damage01 = Mathf.Max(r.damage01, rec.damage01);
                r.worldPos = rec.worldPos;
                r.source = rec.source ?? r.source;
                r.reportedAtUnix = rec.reportedAtUnix;
                return;
            }
        }
        records.Add(rec);
        while (records.Count > maxRecords)
            records.RemoveAt(0);
    }

    public List<DamagedObjectRecord> PeekOpen(int max)
    {
        var list = new List<DamagedObjectRecord>();
        for (int i = 0; i < records.Count && list.Count < max; i++)
        {
            if (records[i] != null && !records[i].resolved)
                list.Add(records[i]);
        }
        return list;
    }

    public bool TryDequeue(string objectId, string buildingId)
    {
        for (int i = 0; i < records.Count; i++)
        {
            var r = records[i];
            if (r == null || r.resolved) continue;
            if (r.objectId == objectId && (string.IsNullOrEmpty(buildingId) || r.buildingId == buildingId))
            {
                r.resolved = true;
                return true;
            }
        }
        return false;
    }

    public int OpenCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < records.Count; i++)
                if (records[i] != null && !records[i].resolved) n++;
            return n;
        }
    }
}
