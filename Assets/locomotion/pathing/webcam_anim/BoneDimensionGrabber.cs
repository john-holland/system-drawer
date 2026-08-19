using System;
using System.Collections.Generic;
using Locomotion.Rig;
using UnityEngine;

/// <summary>Per-bone dimension offsets (position/rotation) authored with scene gizmos.</summary>
[Serializable]
public sealed class BoneDimensionOffset
{
    public string traitId;
    public Vector3 position;
    public Vector3 euler;
}

[AddComponentMenu("Locomotion/Animation/Bone Dimension Grabber")]
public sealed class BoneDimensionGrabber : MonoBehaviour
{
    public BoneMap boneMap;
    public List<BoneDimensionOffset> offsets = new List<BoneDimensionOffset>();
    public float gizmoRadius = 0.04f;
    public bool drawWhenUnselected;

    public BoneMap ResolveBoneMap()
    {
        if (boneMap != null)
            return boneMap;
        boneMap = GetComponent<BoneMap>() ?? GetComponentInChildren<BoneMap>(true);
        return boneMap;
    }

    public BoneDimensionOffset GetOrCreate(string traitId)
    {
        if (string.IsNullOrEmpty(traitId))
            return null;
        if (offsets == null)
            offsets = new List<BoneDimensionOffset>();
        for (int i = 0; i < offsets.Count; i++)
        {
            if (offsets[i] != null && offsets[i].traitId == traitId)
                return offsets[i];
        }
        var o = new BoneDimensionOffset { traitId = traitId };
        offsets.Add(o);
        return o;
    }

    public void ApplyOffsets()
    {
        var map = ResolveBoneMap();
        if (map == null || offsets == null)
            return;
        for (int i = 0; i < offsets.Count; i++)
        {
            var o = offsets[i];
            if (o == null || string.IsNullOrEmpty(o.traitId))
                continue;
            if (!map.TryGet(o.traitId, out var t) || t == null)
                continue;
            t.localPosition = o.position;
            t.localRotation = Quaternion.Euler(o.euler);
        }
    }

    void OnDrawGizmos()
    {
        if (drawWhenUnselected)
            DrawGizmosInternal();
    }

    void OnDrawGizmosSelected()
    {
        DrawGizmosInternal();
    }

    void DrawGizmosInternal()
    {
        var map = ResolveBoneMap();
        if (map == null || map.entries == null)
            return;
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.85f);
        for (int i = 0; i < map.entries.Count; i++)
        {
            var e = map.entries[i];
            if (e == null || e.transform == null)
                continue;
            Gizmos.DrawWireSphere(e.transform.position, gizmoRadius);
        }
    }
}
