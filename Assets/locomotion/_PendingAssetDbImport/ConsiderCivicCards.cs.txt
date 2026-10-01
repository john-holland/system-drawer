using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Consider Civic Cards")]
public sealed class ConsiderCivicCards : MonoBehaviour
{
    public PhysicsCardSolver cardSolver;
    public RagdollSystem actorRagdoll;
    public DamagedObjectQueue damageQueue;
    public float scanRangeM = 24f;
    readonly List<GoodSection> _generated = new List<GoodSection>();

    void Awake()
    {
        if (cardSolver == null) cardSolver = GetComponent<PhysicsCardSolver>();
        if (actorRagdoll == null) actorRagdoll = GetComponent<RagdollSystem>();
        if (damageQueue == null) damageQueue = FindFirstObjectByType<DamagedObjectQueue>();
    }

    public List<GoodSection> GenerateCards()
    {
        _generated.Clear();
        RagdollState state = actorRagdoll != null ? actorRagdoll.GetCurrentState() : null;

        if (damageQueue != null)
        {
            var peek = damageQueue.PeekOpen(8);
            for (int i = 0; i < peek.Count; i++)
            {
                var rec = peek[i];
                var building = string.IsNullOrEmpty(rec.buildingId)
                    ? null
                    : GameObject.Find(rec.buildingId);
                var damaged = rec.source != null ? rec.source : (string.IsNullOrEmpty(rec.objectId) ? null : GameObject.Find(rec.objectId));
                var card = CivicCard.Generate(CivicDutyKind.Repair, building, damaged, state);
                card.buildingStableId = rec.buildingId;
                card.damagedObjectId = rec.objectId;
                card.waypointGroup = rec.waypointGroup;
                _generated.Add(card);
            }
        }

        if (_generated.Count == 0)
            _generated.Add(MakeDefaultCard());

        if (cardSolver != null) cardSolver.AddCards(_generated);
        return _generated;
    }

    public static CivicCard MakeDefaultCard() =>
        CivicCard.Generate(CivicDutyKind.Inspect, null);
}
