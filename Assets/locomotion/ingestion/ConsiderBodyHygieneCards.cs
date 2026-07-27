using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Emits default Eat / Toilet / Hygiene GoodSections into PhysicsCardSolver for goal matching.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Ingestion/Consider Body Hygiene Cards")]
public sealed class ConsiderBodyHygieneCards : MonoBehaviour
{
    public PhysicsCardSolver cardSolver;
    public bool emitEat = true;
    public bool emitToilet = true;
    public bool emitHygiene = true;
    readonly List<GoodSection> _generated = new List<GoodSection>();

    void Awake()
    {
        if (cardSolver == null)
            cardSolver = GetComponent<PhysicsCardSolver>();
    }

    public List<GoodSection> GenerateCards()
    {
        _generated.Clear();
        if (emitEat)
            _generated.Add(MakeEatCard());
        if (emitToilet)
            _generated.Add(MakeToiletCard());
        if (emitHygiene)
        {
            _generated.Add(MakeHygieneCard("brush_teeth"));
            _generated.Add(MakeHygieneCard("brush_tongue"));
            _generated.Add(MakeHygieneCard("floss"));
            _generated.Add(MakeHygieneCard("wash_hands"));
            _generated.Add(MakeHygieneCard("shower"));
        }
        if (cardSolver != null)
            cardSolver.AddCards(_generated);
        return _generated;
    }

    void Start() => GenerateCards();

    public static GoodSection MakeEatCard()
    {
        return new GoodSection
        {
            sectionName = "eat_food",
            description = "Bite, chew, swallow",
            isEatGoal = true,
            physicalPathingTag = "eat",
            traversabilityMode = TraversabilityMode.Custom,
            traversabilityTag = "eat"
        };
    }

    public static GoodSection MakeToiletCard()
    {
        return new GoodSection
        {
            sectionName = "toilet_visit",
            description = "Before sit → excrete → after sit",
            isToiletGoal = true,
            isSitGoal = true,
            physicalPathingTag = "toilet",
            traversabilityMode = TraversabilityMode.Custom,
            traversabilityTag = "toilet"
        };
    }

    public static GoodSection MakeHygieneCard(string kind)
    {
        return new GoodSection
        {
            sectionName = $"hygiene_{kind}",
            description = kind,
            isHygieneGoal = true,
            hygieneKind = kind,
            physicalPathingTag = $"hygiene_{kind}",
            traversabilityMode = TraversabilityMode.Custom,
            traversabilityTag = "hygiene"
        };
    }
}
