using System.Collections.Generic;
using UnityEngine;

// todo: review: would this be better off inheriting from something?
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Voting Place Bootstrap")]
public sealed class VotingPlaceBootstrap : MonoBehaviour
{
    public CivilInstitutionStub stub;

    void Awake()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        Ensure();
    }

    public void Ensure()
    {
        if (stub == null) stub = GetComponent<CivilInstitutionStub>();
        if (stub != null && stub.kind != CivilSystemKind.TownHall)
            stub.kind = CivilSystemKind.VotingPlace;

        if (GetComponent<CompanyRegistration>() == null)
            gameObject.AddComponent<CompanyRegistration>();
        var company = GetComponent<CompanyRegistration>();
        if (string.IsNullOrEmpty(company.companyId))
            company.companyId = stub != null && stub.kind == CivilSystemKind.TownHall
                ? "town_hall"
                : "voting_place";

        if (GetComponent<CivilVenueAmenities>() == null)
            gameObject.AddComponent<CivilVenueAmenities>();
        if (GetComponent<CivilVenueBioRhythmService>() == null)
            gameObject.AddComponent<CivilVenueBioRhythmService>();
        if (GetComponent<LaneGrid>() == null)
            gameObject.AddComponent<LaneGrid>();
        if (GetComponent<VotingQueueHub>() == null)
            gameObject.AddComponent<VotingQueueHub>();
        if (GetComponent<VoteLedger>() == null)
            gameObject.AddComponent<VoteLedger>();
        if (GetComponent<VotingPlaceBioRhythm>() == null)
            gameObject.AddComponent<VotingPlaceBioRhythm>();
        if (GetComponent<VoteBehaviorTreeNode>() == null)
            gameObject.AddComponent<VoteBehaviorTreeNode>();

        var grid = GetComponent<LaneGrid>();
        grid.EnsureCells();
        var hub = GetComponent<VotingQueueHub>();
        hub.centralQueue = grid;
        var bio = GetComponent<VotingPlaceBioRhythm>();
        bio.laneGrid = grid;
        bio.queueHub = hub;
        bio.ledger = GetComponent<VoteLedger>();
        bio.ActivatePerimeter();
        hub.placeCard = bio.perimeter;
        hub.EnsureBooths();
        hub.CollectFeeders();
        if (string.IsNullOrEmpty(hub.inpaintPrompt))
            hub.inpaintPrompt = VoteLemmaPropertyKeys.DefaultInpaintPrompt;
        hub.ExecuteInpaintPrompt();

        var bt = GetComponent<VoteBehaviorTreeNode>();
        bt.ledger = bio.ledger;
        bt.voter = new VoterCard { place = bio.perimeter };
    }
}
