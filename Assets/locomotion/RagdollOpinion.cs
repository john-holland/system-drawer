using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Rich opinion set for ConsentWarden / PetWarden / ThreatWarden.</summary>
[Serializable]
public sealed class RagdollOpinion
{
    public string subjectId;
    [Range(-1f, 1f)] public float affinity01;
    [Range(0f, 1f)] public float trust01 = 0.5f;
    [Range(0f, 1f)] public float fear01;
    [Range(0f, 1f)] public float ownership01;
    [Range(0f, 1f)] public float like01 = 0.5f;
    [Range(0f, 1f)] public float dislike01;
    public string interpretationTag;
    public List<string> likes = new List<string>();
    public List<string> dislikes = new List<string>();

    public float OwnerCoefficient =>
        Mathf.Clamp01(ownership01 * 0.5f + like01 * 0.35f - dislike01 * 0.25f + affinity01 * 0.2f);
}

/// <summary>Stores opinions on a ragdoll; <see cref="RagdollSystem.OpinionFor"/> reads this.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Ragdoll Opinion Book")]
public sealed class RagdollOpinionBook : MonoBehaviour
{
    public List<RagdollOpinion> opinions = new List<RagdollOpinion>();

    public RagdollOpinion OpinionFor(UnityEngine.Object actorOrObject)
    {
        string id = ResolveId(actorOrObject);
        for (int i = 0; i < opinions.Count; i++)
            if (opinions[i] != null && opinions[i].subjectId == id)
                return opinions[i];
        var created = new RagdollOpinion { subjectId = id };
        opinions.Add(created);
        return created;
    }

    public static string ResolveId(UnityEngine.Object actorOrObject)
    {
        if (actorOrObject == null) return "null";
        if (actorOrObject is Component c)
            return c.gameObject.name + ":" + c.GetInstanceID();
        if (actorOrObject is GameObject go)
            return go.name + ":" + go.GetInstanceID();
        return actorOrObject.name + ":" + actorOrObject.GetInstanceID();
    }
}

public static class RagdollOpinionExtensions
{
    public static RagdollOpinion OpinionFor(this RagdollSystem ragdoll, UnityEngine.Object actorOrObject)
    {
        if (ragdoll == null) return new RagdollOpinion { subjectId = "null" };
        var book = ragdoll.GetComponent<RagdollOpinionBook>()
                   ?? ragdoll.gameObject.AddComponent<RagdollOpinionBook>();
        return book.OpinionFor(actorOrObject);
    }
}
