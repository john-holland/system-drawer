using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// N-subject coordinator (not a <see cref="RagdollSystem"/> subclass). Subjects may be any
/// GameObject; ragdoll / heavy-petting keys are optional. Missing ragdoll → transform-only.
/// Open topology stays an untyped ScriptableObject.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Relationship Ragdoll")]
public sealed class RelationshipRagdoll : MonoBehaviour
{
    public List<GameObject> subjects = new List<GameObject>();
    public HeavyPettingIKActorRegistry ikRegistry;
    public ScriptableObject openTopology;

    public RagdollSystem RagdollFor(GameObject go)
    {
        if (go == null) return null;
        return go.GetComponent<RagdollSystem>() ?? go.GetComponentInChildren<RagdollSystem>();
    }

    public Vector3 PlacementFor(int index)
    {
        var go = SubjectAt(index);
        return go != null ? go.transform.position : Vector3.zero;
    }

    public GameObject SubjectAt(int index)
    {
        if (subjects == null || index < 0 || index >= subjects.Count) return null;
        return subjects[index];
    }

    public Vector3 Centroid()
    {
        if (subjects == null || subjects.Count == 0) return transform.position;
        Vector3 acc = Vector3.zero;
        int n = 0;
        for (int i = 0; i < subjects.Count; i++)
        {
            var go = subjects[i];
            if (go == null) continue;
            acc += go.transform.position;
            n++;
        }
        return n > 0 ? acc / n : transform.position;
    }

    public void Bind(IList<GameObject> next)
    {
        if (subjects == null) subjects = new List<GameObject>();
        subjects.Clear();
        if (next == null) return;
        for (int i = 0; i < next.Count; i++)
        {
            if (next[i] != null)
                subjects.Add(next[i]);
        }
    }

    public string IkKeyFor(GameObject go)
    {
        if (go == null || ikRegistry == null || ikRegistry.entries == null) return null;
        for (int i = 0; i < ikRegistry.entries.Count; i++)
        {
            var e = ikRegistry.entries[i];
            if (e != null && e.actor == go)
                return e.actorKey;
        }
        return null;
    }
}
