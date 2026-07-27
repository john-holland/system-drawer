using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Embrace / kiss clinch graph: temporary tow links with gentler stiffness than wrestling.
/// </summary>
[AddComponentMenu("Locomotion/Love Making/Topology Runtime")]
public sealed class LoveMakingTopologyRuntime : MonoBehaviour
{
    public RagdollSystem actorRagdoll;
    public ClothUvStretchDriver clothDriver;
    [Range(0.2f, 0.95f)] public float embraceStiffness = 0.55f;

    readonly List<IkTowLink> _links = new List<IkTowLink>();
    GameObject _partner;
    LoveCard _activeCard;

    public GameObject Partner => _partner;
    public LoveCard ActiveCard => _activeCard;
    public bool IsLocked => _links.Count > 0 || _activeCard != null;

    void Awake()
    {
        if (actorRagdoll == null)
            actorRagdoll = GetComponent<RagdollSystem>();
        if (clothDriver == null)
            clothDriver = GetComponent<ClothUvStretchDriver>();
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        for (int i = 0; i < _links.Count; i++)
            _links[i]?.Tick(dt);
        if (IsLocked && clothDriver != null)
            clothDriver.NotifyContact(_partner, 0.45f);
    }

    public void BeginEmbrace(GameObject actor, GameObject partner, LoveCard card)
    {
        EndExchange();
        _partner = partner;
        _activeCard = card;
        if (actorRagdoll == null && actor != null)
            actorRagdoll = actor.GetComponent<RagdollSystem>();
        var partnerRagdoll = partner != null ? partner.GetComponent<RagdollSystem>() : null;
        if (card?.requiredLimbBones == null || actorRagdoll == null || partnerRagdoll == null)
            return;

        for (int i = 0; i < card.requiredLimbBones.Count; i++)
        {
            string bone = card.requiredLimbBones[i];
            if (string.IsNullOrEmpty(bone)) continue;
            Transform parent = actorRagdoll.GetBoneTransform(bone);
            string partnerBone = MapPartnerBone(bone, card);
            Transform child = partnerRagdoll.GetBoneTransform(partnerBone);
            if (parent == null || child == null) continue;

            _links.Add(new IkTowLink
            {
                name = $"love_{bone}_{partnerBone}",
                parent = parent,
                child = child,
                childBody = child.GetComponent<Rigidbody>() ?? child.GetComponentInParent<Rigidbody>(),
                stiffness = embraceStiffness * Mathf.Lerp(0.85f, 1.1f, card.physicality01),
                maxErrorMeters = 0.35f,
                localOffsetFromParent = Vector3.zero,
                useJointAssist = true
            });
        }

        if (clothDriver != null)
            clothDriver.NotifyContact(partner, 0.6f);
    }

    public void EndExchange()
    {
        _links.Clear();
        _activeCard = null;
        _partner = null;
        if (clothDriver != null)
            clothDriver.ClearContact();
    }

    static string MapPartnerBone(string actorBone, LoveCard card)
    {
        if (card != null && card.loveMoveKind == LoveMakingMoveKind.Kiss)
            return "Head";
        if (actorBone.Contains("Hand") || actorBone.Contains("Shoulder"))
            return "Chest";
        if (actorBone.Contains("Head"))
            return "Head";
        return "Chest";
    }
}
