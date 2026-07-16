using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime computer keyboard: keys, centroid, lookup.</summary>
[AddComponentMenu("Locomotion/Periphery/Computer Keyboard Runtime")]
public sealed class ComputerKeyboardRuntime : MonoBehaviour
{
    public ComputerKeyboardSpec spec = new ComputerKeyboardSpec();
    public List<ComputerKey> keys = new List<ComputerKey>();
    public Transform bodyTransform;
    public VolumeKnobRuntime volumeKnob;
    public Vector3 worldCentroid;

    public void RecalculateCentroid()
    {
        if (keys == null || keys.Count == 0)
        {
            worldCentroid = transform.position;
            return;
        }
        Vector3 sum = Vector3.zero;
        int n = 0;
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] == null) continue;
            sum += keys[i].WorldPressPoint;
            n++;
        }
        worldCentroid = n > 0 ? sum / n : transform.position;
    }

    public bool TryGetKey(ComputerKeyId id, out ComputerKey key)
    {
        key = null;
        if (keys == null) return false;
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] != null && keys[i].id == id)
            {
                key = keys[i];
                return true;
            }
        }
        return false;
    }

    public bool TryGetKeyByUnicode(char c, out ComputerKey key)
    {
        key = null;
        if (keys == null) return false;
        char lower = char.ToLowerInvariant(c);
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] != null && keys[i].unicode != '\0' &&
                char.ToLowerInvariant(keys[i].unicode) == lower)
            {
                key = keys[i];
                return true;
            }
        }
        return false;
    }

    public ComputerKey FindNearestKey(Vector3 worldPoint)
    {
        ComputerKey best = null;
        float bestD = float.MaxValue;
        if (keys == null) return null;
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] == null) continue;
            float d = Vector3.SqrMagnitude(keys[i].WorldPressPoint - worldPoint);
            if (d < bestD)
            {
                bestD = d;
                best = keys[i];
            }
        }
        return best;
    }
}
