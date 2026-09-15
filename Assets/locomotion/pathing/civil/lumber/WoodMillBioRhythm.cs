using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Mill ops: hours, load/unload, vehicle/river/canal delivery.</summary>
[AddComponentMenu("Locomotion/Civil/Wood Mill Bio Rhythm")]
public sealed class WoodMillBioRhythm : DispatchBioRhythm
{
    public FactoryRuntime factory;
    public string deliveryMechanism = "vehicle";
    public StationShiftSchedule shifts;
    public CanalRibbonSpec canalRibbon;
    [Range(0f, 1f)] public float throughput01 = 0.7f;

    protected override void Awake()
    {
        if (string.IsNullOrEmpty(serviceId))
            serviceId = "wood_mill";
        hoursCron = "* 6-22 * * *";
        base.Awake();
    }

    public override void Tick(DateTime utcNow, float dt)
    {
        if (shifts != null)
            hoursCron = shifts.ResolvedHoursCron(hoursCron);
        base.Tick(utcNow, dt);
        bool open = CronDue.IsActiveSchedule(hoursCron, utcNow);
        unitsAvailable01 = open ? Mathf.Clamp01(throughput01) : 0.05f;
        if (venueBio != null)
            venueBio.activity01 = open ? 0.4f + queueDepth01 * 0.35f : 0.05f;
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = base.FacilitateCards(request);
        if (request == null) return cards;
        string kind = (request.kind ?? "").ToLowerInvariant();
        if (kind == "load" || kind == "mill_load")
        {
            var load = FactoryLoadZoneCard.GenerateLoad(request, factory);
            cards.Add(load);
            cards.Add(load.ToTruckUnload(request));
        }
        if (kind == "unload" || kind == "mill_unload")
            cards.Add(FactoryTruckUnloadCard.GenerateUnload(request, factory));
        if (kind == "plank" || kind == "plank_cut" || kind == "kerf")
            cards.Add(WoodMillPlankCutCard.Generate(request, 3, false));
        if (kind == "section" || kind == "plank_section")
            cards.Add(WoodMillPlankCutCard.Generate(request, 3, true));
        if (kind == "delivery" || kind == "ship" || kind == "vehicle" || kind == "river" || kind == "canal"
            || kind == "river_canal_dam")
        {
            string mech = kind == "delivery" || kind == "ship" ? deliveryMechanism : kind;
            cards.Add(TAVehicleDeliveryCard.Generate(request, mech));
        }
        return cards;
    }
}
