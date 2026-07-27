using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared kiss IK execution: lip-midpoint IkTow, jaw drive from kissAnimationIntensity.
/// Owned by KissingBehaviorTreeNode; LoveMakeObjectNode delegates Kiss here (no Embrace Head↔Head).
/// </summary>
public static class KissingExecution
{
    sealed class State
    {
        public readonly List<IkTowLink> links = new List<IkTowLink>();
        public MouthInteriorRuntime mouthA;
        public MouthInteriorRuntime mouthB;
        public float savedJawA;
        public float savedJawB;
        public float targetJaw01;
        public LoveCard card;
        public GameObject actor;
        public GameObject partner;
        public bool active;
    }

    static readonly Dictionary<int, State> s_byActor = new Dictionary<int, State>();

    public static bool Begin(GameObject actor, GameObject partner, LoveCard card, HeavyPettingIKActorRegistry registry = null)
    {
        End(actor);
        if (actor == null || partner == null || card == null)
            return false;

        registry = registry ?? HeavyPettingIKActorRegistry.FindActive();
        HeavyPettingIKActorEntry entryA = null;
        HeavyPettingIKActorEntry entryB = null;
        if (registry != null)
        {
            if (!string.IsNullOrEmpty(card.selfActorKey))
                registry.TryGet(card.selfActorKey, out entryA);
            if (!string.IsNullOrEmpty(card.partnerActorKey))
                registry.TryGet(card.partnerActorKey, out entryB);
            if (entryA == null) registry.TryGet(actor, out entryA);
            if (entryB == null) registry.TryGet(partner, out entryB);
        }
        entryA ??= HeavyPettingIKActorRegistry.InventEphemeral(actor, card.selfActorKey);
        entryB ??= HeavyPettingIKActorRegistry.InventEphemeral(partner, card.partnerActorKey);

        Transform lipA = registry != null
            ? registry.ResolveLipMidAnchor(entryA)
            : entryA.mouth != null ? entryA.mouth.EnsureLipMidAnchor() : null;
        Transform lipB = registry != null
            ? registry.ResolveLipMidAnchor(entryB)
            : entryB.mouth != null ? entryB.mouth.EnsureLipMidAnchor() : null;

        // Fallback Head↔Head when mouths missing
        if (lipA == null)
        {
            var rag = entryA.ragdoll ?? actor.GetComponent<RagdollSystem>();
            lipA = rag != null ? rag.GetBoneTransform("Head") : actor.transform;
        }
        if (lipB == null)
        {
            var rag = entryB.ragdoll ?? partner.GetComponent<RagdollSystem>();
            lipB = rag != null ? rag.GetBoneTransform("Head") : partner.transform;
        }
        if (lipA == null || lipB == null)
            return false;

        float intensity = Mathf.Clamp01(card.kissAnimationIntensity);
        float stiffness = Mathf.Lerp(0.45f, 0.72f, intensity);
        float jaw = card.kissJawOpen01 >= 0f
            ? Mathf.Clamp01(card.kissJawOpen01)
            : Mathf.Lerp(0.05f, 0.25f, intensity);

        var state = new State
        {
            actor = actor,
            partner = partner,
            card = card,
            mouthA = entryA.mouth,
            mouthB = entryB.mouth,
            targetJaw01 = jaw,
            active = true
        };
        if (state.mouthA != null) state.savedJawA = state.mouthA.jawOpen01;
        if (state.mouthB != null) state.savedJawB = state.mouthB.jawOpen01;

        state.links.Add(new IkTowLink
        {
            name = "kiss_lip_a_to_b",
            parent = lipB,
            child = lipA,
            childBody = lipA.GetComponent<Rigidbody>() ?? lipA.GetComponentInParent<Rigidbody>(),
            stiffness = stiffness,
            maxErrorMeters = 0.28f,
            localOffsetFromParent = Vector3.zero,
            useJointAssist = true
        });
        state.links.Add(new IkTowLink
        {
            name = "kiss_lip_b_to_a",
            parent = lipA,
            child = lipB,
            childBody = lipB.GetComponent<Rigidbody>() ?? lipB.GetComponentInParent<Rigidbody>(),
            stiffness = stiffness * 0.92f,
            maxErrorMeters = 0.28f,
            localOffsetFromParent = Vector3.zero,
            useJointAssist = true
        });

        s_byActor[actor.GetInstanceID()] = state;
        return true;
    }

    public static void Tick(GameObject actor, float dt)
    {
        if (actor == null || !s_byActor.TryGetValue(actor.GetInstanceID(), out var state) || !state.active)
            return;
        for (int i = 0; i < state.links.Count; i++)
            state.links[i]?.Tick(dt);
        if (state.mouthA != null)
            state.mouthA.jawOpen01 = Mathf.MoveTowards(state.mouthA.jawOpen01, state.targetJaw01, dt * 2.5f);
        if (state.mouthB != null)
            state.mouthB.jawOpen01 = Mathf.MoveTowards(state.mouthB.jawOpen01, state.targetJaw01 * 0.9f, dt * 2.5f);
    }

    public static float LipDistance(GameObject actor)
    {
        if (actor == null || !s_byActor.TryGetValue(actor.GetInstanceID(), out var state) || state.links.Count == 0)
            return float.MaxValue;
        return state.links[0].ErrorMeters;
    }

    public static void End(GameObject actor)
    {
        if (actor == null) return;
        int id = actor.GetInstanceID();
        if (!s_byActor.TryGetValue(id, out var state)) return;
        if (state.mouthA != null) state.mouthA.jawOpen01 = state.savedJawA;
        if (state.mouthB != null) state.mouthB.jawOpen01 = state.savedJawB;
        state.links.Clear();
        state.active = false;
        s_byActor.Remove(id);
    }

    public static bool IsActive(GameObject actor) =>
        actor != null && s_byActor.TryGetValue(actor.GetInstanceID(), out var s) && s.active;
}
