using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// During TravelAgent / multimodality plan prep, layer open/close topology cards and
/// desk sit/stand occupy segments into the plan.
/// Open/close types live in Locomotion.Open — resolved via component scan without hard asm ref.
/// </summary>
public static class ConsiderPathingPrep
{
    public const string TagOpenClose = "pathing_prep_open_close";
    public const string TagDeskSit = "pathing_prep_desk_sit";
    public const string TagPlaceBuild = "pathing_prep_place_build";

    public static void EnrichPlan(GenericMultiModalPathPlan plan, GameObject actor, float scanRange = 4f)
    {
        if (plan == null || plan.segments == null || actor == null)
            return;

        Collider[] hits = Physics.OverlapSphere(actor.transform.position, scanRange, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            var mb = hits[i].GetComponentInParent<MonoBehaviour>();
            if (mb == null) continue;
            // OpenableJointDriver (Locomotion.Open) — detect by type name
            var drivers = hits[i].GetComponentsInParent<MonoBehaviour>(true);
            bool found = false;
            for (int d = 0; d < drivers.Length; d++)
            {
                if (drivers[d] == null) continue;
                if (drivers[d].GetType().Name != "OpenableJointDriver") continue;
                found = true;
                var card = new GoodSection
                {
                    sectionName = "PrepOpen_" + drivers[d].name,
                    description = TagOpenClose,
                    impulseStack = new List<ImpulseAction>
                    {
                        new ImpulseAction { muscleGroup = "Arm", activation = 0.5f, duration = 0.2f }
                    }
                };
                plan.segments.Add(MultiModalSegment.FromToolBridge(card, null, actor.transform.position, drivers[d].transform.position));
                break;
            }
            if (found) break;
        }

        var station = Object.FindFirstObjectByType<ComputerPeripheryStation>();
        if (station != null)
        {
            station.EnsureSeatContact();
            Vector3 approach = station.ApproachPosition;
            var sitCard = SitCard.GenerateSitCard(
                station.seat.contact.WorldPlanePoint,
                station.seat.contact.WorldPlaneNormal,
                null,
                actor);
            sitCard.BindSurface(station.seat.contact);
            if (station.seat.defaultMode == SurfaceOccupancyMode.StandOn)
            {
                var stand = StandOnSurfaceCard.Generate(station.seat.contact, null);
                plan.segments.Add(MultiModalSegment.FromToolBridge(stand, null, approach, station.seat.contact.WorldPlanePoint));
            }
            else
            {
                plan.segments.Add(MultiModalSegment.FromWalk(new List<Vector3> { actor.transform.position, approach }));
                plan.segments.Add(MultiModalSegment.FromToolBridge(sitCard, null, approach, station.seat.contact.WorldPlanePoint));
            }
        }
    }
}
