using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

public enum TheocraticWardenAction
{
    Allow = 0,
    Counsel = 1,
    Forbid = 2
}

[Serializable]
public sealed class NamedSg3dEntry
{
    public string key;
    public Vector3 value;
}

[Serializable]
public sealed class NamedSg4dEntry
{
    public string key;
    public Bounds4 value;
}

/// <summary>
/// Church/doctrine scorer shared with legal/court. Named SG3D (<c>string→Vector3</c>) and
/// SG4D (<c>string→Bounds4</c>) maps plus active scripture refs. One <see cref="Allow01"/> score
/// for intimacy and religious law.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Theocratic Warden")]
public sealed class TheocraticWarden : MonoBehaviour
{
    [Range(0f, 1f)] public float lastScore01 = 1f;
    public TheocraticWardenAction lastAction = TheocraticWardenAction.Allow;
    public List<NamedSg3dEntry> sg3d = new List<NamedSg3dEntry>();
    public List<NamedSg4dEntry> sg4d = new List<NamedSg4dEntry>();
    public List<string> activeScriptureRefs = new List<string>();
    public List<WardenLimitKv> limits = new List<WardenLimitKv>();

    public float Allow01()
    {
        RefreshAction();
        return lastScore01;
    }

    public TheocraticWardenAction SetDoctrineScore(float score01)
    {
        lastScore01 = Mathf.Clamp01(score01);
        RefreshAction();
        return lastAction;
    }

    public void SetAction(TheocraticWardenAction action)
    {
        lastAction = action;
        switch (action)
        {
            case TheocraticWardenAction.Allow:
                lastScore01 = Mathf.Max(lastScore01, 0.67f);
                break;
            case TheocraticWardenAction.Counsel:
                lastScore01 = Mathf.Clamp(lastScore01, 0.34f, 0.66f);
                break;
            default:
                lastScore01 = Mathf.Min(lastScore01, 0.32f);
                break;
        }
    }

    public Vector3 GetSg3d(string key)
    {
        if (string.IsNullOrEmpty(key) || sg3d == null) return Vector3.zero;
        for (int i = 0; i < sg3d.Count; i++)
        {
            if (sg3d[i] != null && string.Equals(sg3d[i].key, key, StringComparison.Ordinal))
                return sg3d[i].value;
        }
        return Vector3.zero;
    }

    public void SetSg3d(string key, Vector3 value)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (sg3d == null) sg3d = new List<NamedSg3dEntry>();
        for (int i = 0; i < sg3d.Count; i++)
        {
            if (sg3d[i] != null && string.Equals(sg3d[i].key, key, StringComparison.Ordinal))
            {
                sg3d[i].value = value;
                return;
            }
        }
        sg3d.Add(new NamedSg3dEntry { key = key, value = value });
    }

    public Bounds4 GetSg4d(string key)
    {
        if (string.IsNullOrEmpty(key) || sg4d == null) return default;
        for (int i = 0; i < sg4d.Count; i++)
        {
            if (sg4d[i] != null && string.Equals(sg4d[i].key, key, StringComparison.Ordinal))
                return sg4d[i].value;
        }
        return default;
    }

    public void SetSg4d(string key, Bounds4 value)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (sg4d == null) sg4d = new List<NamedSg4dEntry>();
        for (int i = 0; i < sg4d.Count; i++)
        {
            if (sg4d[i] != null && string.Equals(sg4d[i].key, key, StringComparison.Ordinal))
            {
                sg4d[i].value = value;
                return;
            }
        }
        sg4d.Add(new NamedSg4dEntry { key = key, value = value });
    }

    void RefreshAction()
    {
        if (lastScore01 >= 0.67f) lastAction = TheocraticWardenAction.Allow;
        else if (lastScore01 >= 0.34f) lastAction = TheocraticWardenAction.Counsel;
        else lastAction = TheocraticWardenAction.Forbid;
    }
}
