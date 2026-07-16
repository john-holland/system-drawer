using System;
using System.Collections.Generic;
using UnityEngine;
using Locomotion.Musculature;

/// <summary>Caches last fingertip world poses per hand/finger for Consider press short-circuit.</summary>
[AddComponentMenu("Locomotion/Periphery/Finger Position Cache")]
public sealed class FingerPositionCache : MonoBehaviour
{
    [Serializable]
    public struct Entry
    {
        public KeyboardHandPicker.HandSide side;
        public FingerKind kind;
        public Vector3 worldPosition;
        public ComputerKeyId lastKeyId;
        public float time;
    }

    public float contactRadius = 0.03f;
    public List<Entry> entries = new List<Entry>();

    public void Remember(KeyboardHandPicker.HandSide side, FingerKind kind, Vector3 worldPos, ComputerKeyId keyId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].side == side && entries[i].kind == kind)
            {
                entries[i] = new Entry { side = side, kind = kind, worldPosition = worldPos, lastKeyId = keyId, time = Time.time };
                return;
            }
        }
        entries.Add(new Entry { side = side, kind = kind, worldPosition = worldPos, lastKeyId = keyId, time = Time.time });
    }

    public bool TryGet(KeyboardHandPicker.HandSide side, FingerKind kind, out Entry entry)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].side == side && entries[i].kind == kind)
            {
                entry = entries[i];
                return true;
            }
        }
        entry = default;
        return false;
    }

    public bool IsOverKey(ComputerKey key, KeyboardHandPicker.HandSide side, FingerKind kind)
    {
        if (key == null || !TryGet(side, kind, out var e))
            return false;
        return Vector3.Distance(e.worldPosition, key.WorldPressPoint) <= contactRadius;
    }
}
