using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Date-windowed road deformation: damage active between start/end; after end resets geometry
/// unless repair-decal memory is kept. Optional cron gates crew BT availability.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Road Deformation Repair Window")]
public sealed class RoadDeformationRepairWindow : MonoBehaviour
{
    [Tooltip("Inclusive damage window start (UTC).")]
    public string startDateIso = "1996-11-11";
    [Tooltip("Inclusive damage window end (UTC).")]
    public string endDateIso = "1997-08-08";
    public string crewCron = "* 8-18 * * 1-5";
    public bool keepRepairDecalMemory = true;
    public RoadRepairDecal repairDecal;
    public List<Behaviour> damageFeatures = new List<Behaviour>();
    public List<string> crewActorPresets = new List<string>
    {
        "jackhammer", "bulldozer", "cement_truck", "steamroller"
    };

    public bool damageActive;
    public bool repaired;

    public DateTime StartUtc => ParseDate(startDateIso, new DateTime(1996, 11, 11));
    public DateTime EndUtc => ParseDate(endDateIso, new DateTime(1997, 8, 8));

    public void Tick(DateTime utcNow)
    {
        bool inWindow = utcNow.Date >= StartUtc.Date && utcNow.Date <= EndUtc.Date;
        bool crewOk = string.IsNullOrEmpty(crewCron) || CronDue.IsActiveSchedule(crewCron, utcNow);

        if (inWindow)
        {
            damageActive = true;
            repaired = false;
            SetFeaturesEnabled(true);
        }
        else if (damageActive || !repaired)
        {
            // Past end: repair / reset
            damageActive = false;
            SetFeaturesEnabled(false);
            if (crewOk)
                CompleteRepair();
        }
    }

    public void CompleteRepair()
    {
        repaired = true;
        SetFeaturesEnabled(false);
        if (keepRepairDecalMemory)
        {
            if (repairDecal == null)
                repairDecal = GetComponent<RoadRepairDecal>() ?? gameObject.AddComponent<RoadRepairDecal>();
            repairDecal.Apply();
        }
        // Soft hook: RoadWeatherIntegration / wear restore (cross-asm, message + optional component name).
        SendMessage("RestoreWearFromTimeTravelFrame", null, SendMessageOptions.DontRequireReceiver);
        SendMessage("OnRoadDeformationRepaired", this, SendMessageOptions.DontRequireReceiver);
        var weather = GetComponent("RoadWeatherIntegration");
        if (weather != null)
            weather.SendMessage("OnRoadDeformationRepaired", this, SendMessageOptions.DontRequireReceiver);
    }

    void SetFeaturesEnabled(bool on)
    {
        for (int i = 0; i < damageFeatures.Count; i++)
            if (damageFeatures[i] != null)
                damageFeatures[i].enabled = on;
    }

    public bool IsInDamageWindow(DateTime utcNow) =>
        utcNow.Date >= StartUtc.Date && utcNow.Date <= EndUtc.Date;

    static DateTime ParseDate(string iso, DateTime fallback)
    {
        if (DateTime.TryParse(iso, out DateTime d))
            return d.ToUniversalTime();
        return fallback;
    }
}
