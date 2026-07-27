using System.Collections.Generic;
using UnityEngine;

/// <summary>CQC clinch topology with medium IkTow stiffness (between love and wrestling).</summary>
[AddComponentMenu("Locomotion/Combat/Topology Runtime")]
public sealed class CombatTopologyRuntime : MonoBehaviour
{
    public RagdollSystem actorRagdoll;
    [Range(0.2f, 0.95f)] public float clinchStiffness = 0.72f;
    readonly List<IkTowLink> _links = new List<IkTowLink>();
    CombatCard _active;
    GameObject _opponent;

    public CombatCard ActiveCard => _active;
    public bool IsLocked => _links.Count > 0 || _active != null;

    void Awake()
    {
        if (actorRagdoll == null)
            actorRagdoll = GetComponent<RagdollSystem>();
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        for (int i = 0; i < _links.Count; i++)
            _links[i]?.Tick(dt);
    }

    public void BeginClinch(GameObject actor, GameObject opponent, CombatCard card)
    {
        EndExchange();
        _active = card;
        _opponent = opponent;
        if (actorRagdoll == null && actor != null)
            actorRagdoll = actor.GetComponent<RagdollSystem>();
        var oppRd = opponent != null ? opponent.GetComponent<RagdollSystem>() : null;
        if (actorRagdoll == null || oppRd == null) return;

        string bone = card?.impact != null ? card.impact.primaryLimbBone : "RightHand";
        if (string.IsNullOrEmpty(bone)) bone = "RightHand";
        Transform parent = actorRagdoll.GetBoneTransform(bone) ?? actorRagdoll.GetBoneTransform("RightHand");
        Transform child = oppRd.GetBoneTransform("Chest") ?? oppRd.transform;
        if (parent == null || child == null) return;

        _links.Add(new IkTowLink
        {
            name = $"combat_{bone}_Chest",
            parent = parent,
            child = child,
            childBody = child.GetComponent<Rigidbody>() ?? child.GetComponentInParent<Rigidbody>(),
            stiffness = clinchStiffness,
            maxErrorMeters = 0.4f,
            useJointAssist = true
        });
    }

    public void EndExchange()
    {
        _links.Clear();
        _active = null;
        _opponent = null;
    }
}
