using UnityEngine;

/// <summary>Stretch institution card markers / factory helpers (full BT later).</summary>
public static class InstitutionBuiltinCards
{
    public static CivilCard SoupKitchenGatherHomeless(string personaKey = null) =>
        CivilCard.Generate(CivilianDutyKind.GatherHomeless, personaKey);

    public static CivilCard SchoolGatherKids(string personaKey = null) =>
        CivilCard.Generate(CivilianDutyKind.GatherKids, personaKey);

    public static CivilCard LibraryFakeCardOptional(string personaKey = null) =>
        CivilCard.Generate(CivilianDutyKind.FakeLibraryCard, personaKey);

    public static CivicCard GenericBuildingRepair(GameObject building) =>
        CivicCard.Generate(CivicDutyKind.Repair, building);

    public static TravelAgentCard PolicePatrolFromViolenceHint()
    {
        var hint = ViolenceTelecomHint.Instance;
        return hint != null ? hint.MakePatrolCardFromLatest() : TravelAgentCard.GeneratePatrol(Vector3.zero);
    }
}
