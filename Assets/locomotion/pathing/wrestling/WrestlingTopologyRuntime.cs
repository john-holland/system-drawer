using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Active clinch graph: temporary tow links between actor/opponent control points + pin contact.
/// Not rope-swing semantics — body grappling topology.
/// </summary>
[AddComponentMenu("Locomotion/Wrestling/Topology Runtime")]
public sealed class WrestlingTopologyRuntime : MonoBehaviour
{
    public RagdollSystem actorRagdoll;
    public ClothUvStretchDriver clothDriver;

    readonly List<IkTowLink> _links = new List<IkTowLink>();
    GameObject _opponent;
    WrestlingCard _activeCard;
    Vector3 _pinNormal = Vector3.up;
    bool _pinning;

    public GameObject Opponent => _opponent;
    public WrestlingCard ActiveCard => _activeCard;
    public bool IsLocked => _links.Count > 0 || _activeCard != null;
    public IReadOnlyList<IkTowLink> ActiveLinks => _links;

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

        if (_pinning && _opponent != null)
            ApplyPinBias(dt);

        if (IsLocked && clothDriver != null)
            clothDriver.NotifyContact(_opponent, 0.75f);
    }

    public void BeginLock(GameObject actor, GameObject opponent, WrestlingCard card)
    {
        EndExchange();
        _opponent = opponent;
        _activeCard = card;
        if (actorRagdoll == null && actor != null)
            actorRagdoll = actor.GetComponent<RagdollSystem>();
        var oppRagdoll = opponent != null ? opponent.GetComponent<RagdollSystem>() : null;

        if (card?.requiredLimbBones == null || actorRagdoll == null || oppRagdoll == null)
            return;

        for (int i = 0; i < card.requiredLimbBones.Count; i++)
        {
            string bone = card.requiredLimbBones[i];
            if (string.IsNullOrEmpty(bone)) continue;
            Transform parent = actorRagdoll.GetBoneTransform(bone);
            // Pair actor limb to opponent torso / mirrored limb
            string oppBone = MapOpponentBone(bone, card);
            Transform child = oppRagdoll.GetBoneTransform(oppBone);
            if (parent == null || child == null) continue;

            var link = new IkTowLink
            {
                name = $"wrestle_{bone}_{oppBone}",
                parent = parent,
                child = child,
                childBody = child.GetComponent<Rigidbody>() ?? child.GetComponentInParent<Rigidbody>(),
                stiffness = card.mode == WrestlingMode.Pin ? 0.95f : 0.8f,
                maxErrorMeters = 0.45f,
                localOffsetFromParent = Vector3.zero,
                useJointAssist = true
            };
            _links.Add(link);
        }

        _pinning = card != null && card.mode == WrestlingMode.Pin;
        if (clothDriver != null)
            clothDriver.NotifyContact(opponent, 1f);
    }

    public void UpdatePin(Vector3 contactNormal, Collider surface)
    {
        _pinNormal = contactNormal.sqrMagnitude > 1e-6f ? contactNormal.normalized : Vector3.up;
        _pinning = true;
        if (surface != null && clothDriver != null)
            clothDriver.NotifyContact(_opponent, 1f);
    }

    public void EndExchange()
    {
        _links.Clear();
        _activeCard = null;
        _opponent = null;
        _pinning = false;
        if (clothDriver != null)
            clothDriver.ClearContact();
    }

    static string MapOpponentBone(string actorBone, WrestlingCard card)
    {
        if (card != null && card.moveKind == WrestlingMoveKind.DropOn &&
            !string.IsNullOrEmpty(card.dropHitBoneName))
            return card.dropHitBoneName;
        if (actorBone.Contains("Hand") || actorBone.Contains("ForeArm") || actorBone.Contains("Shoulder"))
            return "Chest";
        if (actorBone.Contains("Foot") || actorBone.Contains("Hip"))
            return "Hips";
        return "Chest";
    }

    void ApplyPinBias(float dt)
    {
        if (actorRagdoll == null || _opponent == null) return;
        Transform hips = actorRagdoll.GetBoneTransform("Hips");
        var oppRd = _opponent.GetComponent<RagdollSystem>();
        Transform oppHips = oppRd != null ? oppRd.GetBoneTransform("Hips") : _opponent.transform;
        if (hips == null || oppHips == null) return;

        // Bias opponent CoG toward down along pin normal (press into mat).
        var rb = oppHips.GetComponent<Rigidbody>() ?? oppHips.GetComponentInParent<Rigidbody>();
        if (rb == null || rb.isKinematic) return;
        Vector3 force = -_pinNormal * (rb.mass * 6f);
        rb.AddForce(force, ForceMode.Force);
    }
}
