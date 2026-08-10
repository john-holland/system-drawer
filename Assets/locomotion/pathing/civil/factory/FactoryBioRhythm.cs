using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Factory bio — hours + line FacilitateCards.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Factory/Factory Bio Rhythm")]
public class FactoryBioRhythm : DispatchBioRhythm
{
    public FactoryRuntime factory;
    public bool isOpen = true;

    protected override void Awake()
    {
        if (string.IsNullOrEmpty(serviceId))
            serviceId = "factory";
        if (factory == null)
            factory = GetComponent<FactoryRuntime>();
        governmentAssigned = factory != null && factory.governmentAssigned;
        base.Awake();
        CentralDispatchHub.Instance?.Subscribe(serviceId, this);
    }

    public override void Tick(DateTime utcNow, float dt)
    {
        base.Tick(utcNow, dt);
        bool due = CronDue.IsActiveSchedule(hoursCron, utcNow);
        isOpen = due;
        unitsAvailable01 = isOpen ? 1f : 0.1f;
        if (venueBio != null)
        {
            venueBio.activity01 = isOpen ? 0.5f : 0.05f;
            venueBio.stress01 = isOpen ? 0.25f : 0.05f;
        }
        factory?.SetOpen(isOpen);
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = new List<GoodSection>();
        if (request == null) return cards;
        cards.Add(FactoryLineCard.Generate(request, factory));
        cards.Add(DispatchConfirmCard.Generate(request));
        return cards;
    }
}

[Serializable]
public class FactoryLineCard : TravelAgentCard
{
    public FactoryRuntime factory;

    public static FactoryLineCard Generate(DispatchRequest request, FactoryRuntime factory = null)
    {
        var c = new FactoryLineCard();
        c.factory = factory;
        c.sectionName = "factory_line";
        c.description = request != null ? request.kind : "factory_line";
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        c.goalWorld = request != null ? request.worldTarget
            : (factory != null && factory.lineAnchor != null ? factory.lineAnchor.position
                : factory != null ? factory.transform.position : Vector3.zero);
        return c;
    }
}
