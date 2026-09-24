using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Kitchen/Consider Chef Cards")]
public sealed class ConsiderChefCards : MonoBehaviour
{
    public PhysicsCardSolver cardSolver;
    public RagdollSystem actorRagdoll;
    public ChefDutyMode dutyMode = ChefDutyMode.Line;
    public float scanRangeM = 6f;
    public LayerMask stationMask = ~0;
    readonly List<GoodSection> _generated = new List<GoodSection>();

    void Awake()
    {
        if (cardSolver == null) cardSolver = GetComponent<PhysicsCardSolver>();
        if (actorRagdoll == null) actorRagdoll = GetComponent<RagdollSystem>();
    }

    public List<GoodSection> GenerateCards(GameObject forcedStation = null)
    {
        _generated.Clear();
        RagdollState state = actorRagdoll != null ? actorRagdoll.GetCurrentState() : null;
        if (forcedStation != null)
            EmitForStation(forcedStation, state);
        else
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, scanRangeM, stationMask, QueryTriggerInteraction.Collide);
            var seen = new HashSet<GameObject>();
            for (int i = 0; i < hits.Length; i++)
            {
                var c = hits[i];
                if (c == null) continue;
                var root = c.transform.root != null ? c.transform.root.gameObject : c.gameObject;
                if (!seen.Add(root)) continue;
                if (root == gameObject) continue;
                EmitForStation(root, state);
            }
            if (_generated.Count == 0)
                _generated.Add(MakeDefaultCard());
        }
        if (cardSolver != null) cardSolver.AddCards(_generated);
        return _generated;
    }

    void EmitForStation(GameObject station, RagdollState state)
    {
        var activities = PreferredForMode(dutyMode);
        for (int i = 0; i < activities.Length; i++)
        {
            var card = ChefCard.Generate(dutyMode, activities[i], station, state);
            if (!card.MeetsChefRequirements(gameObject, station, actorRagdoll))
                continue;
            _generated.Add(card);
        }
    }

    public static ChefActivity[] PreferredForMode(ChefDutyMode mode)
    {
        switch (mode)
        {
            case ChefDutyMode.Prep:
                return new[] { ChefActivity.Cut, ChefActivity.Filet, ChefActivity.Pour, ChefActivity.Place };
            case ChefDutyMode.Pass:
            case ChefDutyMode.Expo:
                return new[] { ChefActivity.Plating, ChefActivity.Place };
            case ChefDutyMode.Hygiene:
                return new[] { ChefActivity.WashHands, ChefActivity.CleanStation, ChefActivity.SeasonPan };
            case ChefDutyMode.Dish:
                return new[] { ChefActivity.WashDish, ChefActivity.WashHands, ChefActivity.CleanStation };
            default:
                return new[] { ChefActivity.Sear, ChefActivity.Stir, ChefActivity.Place, ChefActivity.Pour };
        }
    }

    public static ChefCard MakeDefaultCard() =>
        ChefCard.Generate(ChefDutyMode.Line, ChefActivity.Place, null);
}
