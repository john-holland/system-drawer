using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Canal ops: hours, staff pecking, lock-engine commodities, optional cafeteria/PA.</summary>
[AddComponentMenu("Locomotion/Civil/Canal Bio Rhythm")]
public class CanalBioRhythm : DispatchBioRhythm
{
    public CanalRibbonSpec ribbon;
    public CanalLockSpec lockSpec;
    public string lockFuelCommodity = "gas";
    public string lockSteamWoodCommodity = "wood";
    public string foodCommodity = "food";
    [Range(0f, 1f)] public float lockFuel01 = 1f;
    public StoreBase cafeteria;
    public PixelLightGridMountGameObject paMount;
    public StationShiftSchedule shifts;

    protected override void Awake()
    {
        if (string.IsNullOrEmpty(serviceId))
            serviceId = "canal";
        hoursCron = "* 6-22 * * *";
        base.Awake();
    }

    public override void Tick(DateTime utcNow, float dt)
    {
        if (shifts != null)
            hoursCron = shifts.ResolvedHoursCron(hoursCron);
        base.Tick(utcNow, dt);
        bool open = CronDue.IsActiveSchedule(hoursCron, utcNow);
        unitsAvailable01 = open ? Mathf.Clamp01(lockFuel01) : 0.05f;
        if (venueBio != null)
            venueBio.activity01 = open ? 0.45f + queueDepth01 * 0.3f : 0.05f;
    }

    public override List<GoodSection> FacilitateCards(DispatchRequest request)
    {
        var cards = base.FacilitateCards(request);
        if (request == null) return cards;
        string kind = (request.kind ?? "").ToLowerInvariant();
        if (kind == "lock" || kind == "canal_lock")
            cards.Add(CanalLockTransitCard.Generate(request, lockSpec));
        return cards;
    }
}

[Serializable]
public sealed class CanalLockTransitCard : TravelAgentCard
{
    public CanalLockSpec lockSpec;

    public static CanalLockTransitCard Generate(DispatchRequest request, CanalLockSpec lockSpec)
    {
        var c = new CanalLockTransitCard();
        c.lockSpec = lockSpec;
        c.sectionName = "canal_lock";
        c.description = "Canal lock transit";
        c.isTravelAgentGoal = true;
        c.isCivilGoal = true;
        c.goalWorld = request != null ? request.worldTarget : Vector3.zero;
        c.physicalPathingTag = "canal_lock";
        return c;
    }
}
