using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Consider Civil Cards")]
public sealed class ConsiderCivilCards : MonoBehaviour
{
    public PhysicsCardSolver cardSolver;
    public PersonalSchedule schedule;
    public string personaKey;
    [Tooltip("When false, hide crassOrOptional CivilCards from generation.")]
    public bool includeOptionalDuties;
    readonly List<GoodSection> _generated = new List<GoodSection>();

    void Awake()
    {
        if (cardSolver == null) cardSolver = GetComponent<PhysicsCardSolver>();
        if (schedule == null) schedule = GetComponent<PersonalSchedule>();
    }

    public List<GoodSection> GenerateCards()
    {
        _generated.Clear();
        var duty = schedule != null ? schedule.CurrentDuty : CivilianDutyKind.Leisure;
        var venue = schedule != null ? schedule.CurrentVenueTarget : null;
        var card = CivilCard.Generate(duty, personaKey, venue);
        if (!includeOptionalDuties && card.crassOrOptional)
            card = CivilCard.Generate(CivilianDutyKind.Leisure, personaKey, venue);
        _generated.Add(card);
        if (cardSolver != null) cardSolver.AddCards(_generated);
        return _generated;
    }

    public static CivilCard MakeDefaultCard() =>
        CivilCard.Generate(CivilianDutyKind.Leisure);
}
