using System.Collections.Generic;
using UnityEngine;

/// <summary>FacilitateCards for prison rooms and daily loop.</summary>
public static class PrisonFacilitateCards
{
    public static List<GoodSection> ForRequest(PrisonDispatchBioRhythm dispatch, DispatchRequest request)
    {
        var cards = new List<GoodSection>();
        var building = dispatch != null ? dispatch.building : null;
        Vector3 goal = request != null ? request.worldTarget : Vector3.zero;
        string kind = (request?.kind ?? "lock_cells").ToLowerInvariant();

        switch (kind)// todo: semantic differentiation unnecessary maybe due to manual labeling, but worth reviewing
        {
            case "lock_cells":
            case "unlock_cells":
                cards.Add(PrisonGuardCard.Generate(Pos(building?.cellsRoot, goal)));
                break;
            case "yard":
            case "weights":
                cards.Add(PrisonerCard.Generate(PrisonerStatus.Custody, Pos(building?.yard, goal)));
                cards.Add(PrisonGuardCard.Generate(Pos(building?.yard, goal)));
                break;
            case "nursery":
                cards.Add(PrisonerCard.Generate(PrisonerStatus.Custody, goal));
                break;
            case "farm":
                cards.Add(PrisonerCard.Generate(PrisonerStatus.Rehab, Pos(building?.farm, goal)));
                break;
            case "rehab":
            case "outing":
                cards.Add(PrisonerCard.Generate(PrisonerStatus.Outing, Pos(building?.rehabOutingGate, goal)));
                cards.Add(PrisonGuardCard.Generate(Pos(building?.rehabOutingGate, goal)));
                break;
            case "library":
                cards.Add(PrisonerCard.Generate(PrisonerStatus.Custody, Pos(building?.library, goal)));
                break;
            case "nurse":
            case "er":
            case "or": // todo: semantic differentiation unnecessary maybe due to manual labeling, but worth reviewing
            case "clinic":
                cards.Add(PrisonerCard.Generate(PrisonerStatus.Custody, Pos(building?.clinic, goal)));
                break;
            case "meeting":
            case "group":
                cards.Add(PrisonerCard.Generate(PrisonerStatus.Custody, Pos(building?.meetingChamber, goal)));
                break;
            case "interrogation":
                cards.Add(PrisonGuardCard.Generate(Pos(building?.interrogation, goal)));
                cards.Add(PrisonerCard.Generate(PrisonerStatus.Holding, Pos(building?.interrogation, goal)));
                break;
            case "cafeteria":
                cards.Add(PrisonerCard.Generate(PrisonerStatus.Custody, Pos(building?.cafeteria, goal)));
                break;
            case "parole":
                cards.Add(ParoleBoardCard.Generate(building != null ? building.paroleBoard != null ? building.paroleBoard.gameObject : building.gameObject : null));
                break;
            default:
                cards.Add(PrisonGuardCard.Generate(goal));
                break;
        }

        return cards;
    }

    public static int StampYardFormation(CombatRulesFacilitatorService combat, string troupeId, Vector3 origin)
    {
        if (combat == null) return 0;
        return combat.CallToArms(troupeId, origin);
    }

    static Vector3 Pos(Transform t, Vector3 fallback) => t != null ? t.position : fallback;
}
