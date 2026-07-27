using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scans for opponent ragdolls and emits mode-filtered WrestlingCards into PhysicsCardSolver.
/// Tags use wrestling_* — not rope_grapple semantics.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Wrestling/Consider Wrestling Cards")]
public sealed class ConsiderWrestlingCards : MonoBehaviour
{
    public PhysicsCardSolver cardSolver;
    public RagdollSystem actorRagdoll;
    public WrestlingMode mode = WrestlingMode.Play;
    public bool professionalStyle;
    public float scanRangeM = 4f;
    public LayerMask opponentMask = ~0;
    readonly List<GoodSection> _generated = new List<GoodSection>();

    void Awake()
    {
        if (cardSolver == null)
            cardSolver = GetComponent<PhysicsCardSolver>();
        if (actorRagdoll == null)
            actorRagdoll = GetComponent<RagdollSystem>();
    }

    public List<GoodSection> GenerateCards(GameObject forcedOpponent = null)
    {
        _generated.Clear();
        RagdollState state = actorRagdoll != null ? actorRagdoll.GetCurrentState() : null;

        if (forcedOpponent != null)
        {
            EmitForOpponent(forcedOpponent, state);
        }
        else
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, scanRangeM, opponentMask, QueryTriggerInteraction.Ignore);
            var seen = new HashSet<GameObject>();
            for (int i = 0; i < hits.Length; i++)
            {
                var c = hits[i];
                if (c == null) continue;
                var root = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;
                var rd = root.GetComponentInParent<RagdollSystem>();
                if (rd == null || rd == actorRagdoll) continue;
                GameObject opp = rd.gameObject;
                if (opp == gameObject || !seen.Add(opp)) continue;
                EmitForOpponent(opp, state);
            }
        }

        if (cardSolver != null)
            cardSolver.AddCards(_generated);
        return _generated;
    }

    void EmitForOpponent(GameObject opponent, RagdollState state)
    {
        WrestlingMoveKind[] kinds = PreferredMovesForMode(mode);
        for (int i = 0; i < kinds.Length; i++)
        {
            var card = WrestlingCard.Generate(mode, kinds[i], opponent, state, professionalStyle);
            if (!card.MeetsWrestlingRequirements(gameObject, opponent, actorRagdoll))
                continue;
            card.physicalPathingTag = $"wrestling_{kinds[i].ToString().ToLowerInvariant()}";
            _generated.Add(card);
        }
    }

    public static WrestlingMoveKind[] PreferredMovesForMode(WrestlingMode mode)
    {
        switch (mode)
        {
            case WrestlingMode.Subdue:
                return new[]
                {
                    WrestlingMoveKind.Pull, WrestlingMoveKind.Push, WrestlingMoveKind.LockGrapple,
                    WrestlingMoveKind.Pry, WrestlingMoveKind.Block
                };
            case WrestlingMode.Pin:
                return new[]
                {
                    WrestlingMoveKind.LockGrapple, WrestlingMoveKind.DropOn, WrestlingMoveKind.Push
                };
            default:
                return new[]
                {
                    WrestlingMoveKind.LungeShootIn, WrestlingMoveKind.LockGrapple, WrestlingMoveKind.Lift,
                    WrestlingMoveKind.Throw, WrestlingMoveKind.DropOn, WrestlingMoveKind.Counter
                };
        }
    }
}
