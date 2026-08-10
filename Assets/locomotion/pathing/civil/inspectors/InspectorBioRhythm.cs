using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Inspector persona schedules (cron with month/year) across craft kinds.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Inspectors/Inspector Bio Rhythm")]
public sealed class InspectorBioRhythm : DispatchBioRhythm
{
    public List<InspectorPersonaSchedule> schedules = new List<InspectorPersonaSchedule>();
    public Transform inspectionStation;
    public string openCloseTopologyId = "inspect_station_gate";

    protected override void Awake()
    {
        if (string.IsNullOrEmpty(serviceId))
            serviceId = "inspector";
        governmentAssigned = true;
        base.Awake();
        if (schedules.Count == 0)
        {
            schedules.Add(new InspectorPersonaSchedule { craftKind = InspectableCraftKind.Elevator, hoursCron = "0 9 1 * *" });
            schedules.Add(new InspectorPersonaSchedule { craftKind = InspectableCraftKind.Train, hoursCron = "0 10 15 * *" });
            schedules.Add(new InspectorPersonaSchedule { craftKind = InspectableCraftKind.Bus, hoursCron = "0 11 * * 1" });
            schedules.Add(new InspectorPersonaSchedule { craftKind = InspectableCraftKind.Plane, hoursCron = "0 8 1 */3 *" });
            schedules.Add(new InspectorPersonaSchedule { craftKind = InspectableCraftKind.Helicopter, hoursCron = "0 14 1 * *" });
            schedules.Add(new InspectorPersonaSchedule { craftKind = InspectableCraftKind.Automobile, hoursCron = "0 13 * * 3" });
            schedules.Add(new InspectorPersonaSchedule { craftKind = InspectableCraftKind.Spaceship, hoursCron = "0 6 1 1 *" });
            schedules.Add(new InspectorPersonaSchedule { craftKind = InspectableCraftKind.Boat, hoursCron = "0 7 * * 5" });
        }
    }

    public override void Tick(DateTime utcNow, float dt)
    {
        base.Tick(utcNow, dt);
        int due = 0;
        for (int i = 0; i < schedules.Count; i++)
        {
            var s = schedules[i];
            if (s != null && s.enabled && CronDue.IsActiveSchedule(s.hoursCron, utcNow))
                due++;
        }
        alert01 = Mathf.Clamp01(due / 4f);
        unitsAvailable01 = due > 0 ? 1f : 0.35f;
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = new List<GoodSection>();
        if (request == null) return cards;
        var kind = ParseKind(request.notes);
        cards.Add(InspectorKnockCard.Generate(request, kind));
        cards.Add(InspectorTravelOptionCard.Generate(request, kind));
        if ((request.notes ?? "").IndexOf("dog", StringComparison.OrdinalIgnoreCase) >= 0)
            cards.Add(JusticeDogCard.Generate(request));
        cards.Add(DispatchConfirmCard.Generate(request));
        return cards;
    }

    static InspectableCraftKind ParseKind(string notes)
    {
        string n = (notes ?? "").ToLowerInvariant();
        if (n.Contains("plane") || n.Contains("airplane")) return InspectableCraftKind.Plane;
        if (n.Contains("train")) return InspectableCraftKind.Train;
        if (n.Contains("bus")) return InspectableCraftKind.Bus;
        if (n.Contains("heli")) return InspectableCraftKind.Helicopter;
        if (n.Contains("space")) return InspectableCraftKind.Spaceship;
        if (n.Contains("boat") || n.Contains("ship")) return InspectableCraftKind.Boat;
        if (n.Contains("auto") || n.Contains("car")) return InspectableCraftKind.Automobile;
        return InspectableCraftKind.Elevator;
    }
}
