using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Combat/Consider Combat Cards")]
public sealed class ConsiderCombatCards : MonoBehaviour
{
    public PhysicsCardSolver cardSolver;
    public RagdollSystem actorRagdoll;
    public CombatMode mode = CombatMode.Melee;
    public float scanRangeM = 8f;
    public LayerMask hostileMask = ~0;
    readonly List<GoodSection> _generated = new List<GoodSection>();

    void Awake()
    {
        if (cardSolver == null) cardSolver = GetComponent<PhysicsCardSolver>();
        if (actorRagdoll == null) actorRagdoll = GetComponent<RagdollSystem>();
    }

    public List<GoodSection> GenerateCards(GameObject forcedTarget = null)
    {
        _generated.Clear();
        RagdollState state = actorRagdoll != null ? actorRagdoll.GetCurrentState() : null;
        if (forcedTarget != null)
            EmitForTarget(forcedTarget, state);
        else
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, scanRangeM, hostileMask, QueryTriggerInteraction.Ignore);
            var seen = new HashSet<GameObject>();
            for (int i = 0; i < hits.Length; i++)
            {
                var c = hits[i];
                if (c == null) continue;
                var root = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;
                var rd = root.GetComponentInParent<RagdollSystem>();
                if (rd == null || rd == actorRagdoll) continue;
                if (!seen.Add(rd.gameObject)) continue;
                EmitForTarget(rd.gameObject, state);
            }
        }
        if (cardSolver != null) cardSolver.AddCards(_generated);
        return _generated;
    }

    void EmitForTarget(GameObject target, RagdollState state)
    {
        var kinds = PreferredMovesForMode(mode);
        for (int i = 0; i < kinds.Length; i++)
        {
            var card = CombatCard.Generate(mode, kinds[i], target, state);
            if (!card.MeetsCombatRequirements(gameObject, target, actorRagdoll))
                continue;
            _generated.Add(card);
        }
    }

    public static CombatMoveKind[] PreferredMovesForMode(CombatMode mode)
    {
        switch (mode)
        {
            case CombatMode.Cqc:
                return new[] { CombatMoveKind.Strike, CombatMoveKind.Block, CombatMoveKind.GrappleBreak, CombatMoveKind.Stab };
            case CombatMode.Ranged:
                return new[] { CombatMoveKind.Aim, CombatMoveKind.Fire, CombatMoveKind.Reload, CombatMoveKind.Suppress };
            case CombatMode.VehicleWeapon:
                return new[] { CombatMoveKind.Aim, CombatMoveKind.Fire, CombatMoveKind.Suppress };
            case CombatMode.Explosive:
                return new[] { CombatMoveKind.Throw, CombatMoveKind.Fire };
            default:
                return new[] { CombatMoveKind.Strike, CombatMoveKind.Slash, CombatMoveKind.Block, CombatMoveKind.Parry };
        }
    }

    public static CombatCard MakeDefaultCard() =>
        CombatCard.Generate(CombatMode.Melee, CombatMoveKind.Strike, null);
}
