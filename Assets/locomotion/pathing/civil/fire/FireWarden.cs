using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class FireTargetScore
{
    public GameObject target;
    public float heat01;
    public float windowLight01;
    public float destructionPotential01;
    public float victimRisk01;
    public float waterDemandLiters;
    public float Priority => heat01 * 0.35f + windowLight01 * 0.25f + destructionPotential01 * 0.2f + victimRisk01 * 0.2f;
}

/// <summary>Tracks water demand and releases trucks; bridges ThreatWarden fire agency.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Fire Warden")]
public sealed class FireWarden : MonoBehaviour
{
    public FirehouseBioRhythm bio;
    public FireStationBuildingRagdoll station;
    public ThreatWarden threatWarden;
    public string hospitalDispatchId = "hospital_ems";
    public float litersPerHeat01 = 800f;

    public readonly List<FireTargetScore> targets = new List<FireTargetScore>();
    public float totalWaterDemandLiters;
    public int trucksReleased;

    void Awake()
    {
        if (threatWarden == null)
            threatWarden = GetComponent<ThreatWarden>() ?? GetComponentInParent<ThreatWarden>();
    }

    public void Tick(float dt)
    {
        // Soft decay of alert when no targets
        if (targets.Count == 0 && bio != null)
            bio.alert01 = Mathf.MoveTowards(bio.alert01, 0f, dt * 0.02f);
    }

    public void OnThreatFire(GameObject source, float heat01, float windowLight01 = 0.5f)
    {
        var score = new FireTargetScore
        {
            target = source,
            heat01 = Mathf.Clamp01(heat01),
            windowLight01 = Mathf.Clamp01(windowLight01),
            destructionPotential01 = EstimateDestruction(source),
            victimRisk01 = EstimateVictimRisk(source),
            waterDemandLiters = Mathf.Clamp01(heat01) * litersPerHeat01
        };
        targets.Add(score);
        Retally();
        if (bio != null)
            bio.alert01 = Mathf.Clamp01(bio.alert01 + 0.3f);
        TryReleaseTrucks();
        RequestHospital();
    }

    public void IngestThreatCard(ThreatCard card)
    {
        if (card == null) return;
        if (card.threatKind != ThreatKind.Fire && card.threatKind != ThreatKind.SmokeDetectorAlarm)
            return;
        OnThreatFire(card.reportedSource, 0.7f);
    }

    void Retally()
    {
        targets.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        totalWaterDemandLiters = 0f;
        for (int i = 0; i < targets.Count; i++)
            totalWaterDemandLiters += targets[i].waterDemandLiters;
    }

    public bool TryReleaseTrucks()
    {
        if (bio == null || !bio.CanReleaseTruck(totalWaterDemandLiters * 0.5f))
            return false;
        var trucks = station != null ? station.trucks : null;
        if (trucks == null || trucks.Count == 0) return false;
        float remaining = totalWaterDemandLiters;
        trucksReleased = 0;
        for (int i = 0; i < trucks.Count && remaining > 0f; i++)
        {
            var t = trucks[i];
            if (t == null || !t.available) continue;
            t.DispatchToFire(targets.Count > 0 ? targets[0].target : null, remaining);
            float take = Mathf.Min(t.waterTankLiters, remaining);
            bio.ConsumeWater(take);
            remaining -= take;
            trucksReleased++;
        }
        return trucksReleased > 0;
    }

    /// <summary>Ordered spray stops — shorter sprays as priority drops (stub for SG4D).</summary>
    public List<Vector3> BuildSprayStops()
    {
        var stops = new List<Vector3>();
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].target == null) continue;
            stops.Add(targets[i].target.transform.position);
        }
        return stops;
    }

    public float SprayDurationSec(FireTargetScore t)
    {
        if (t == null) return 2f;
        return Mathf.Lerp(8f, 2f, 1f - t.Priority);
    }

    void RequestHospital()
    {
        var hub = CentralDispatchHub.Instance;
        if (hub == null) return;
        hub.RequestCrossDispatch(
            bio != null ? bio.serviceId : "fire_department",
            hospitalDispatchId,
            new DispatchRequest
            {
                kind = "passenger_pickup",
                priority01 = 0.8f,
                notes = "fire_scene_ems"
            });
    }

    static float EstimateDestruction(GameObject source)
    {
        if (source == null) return 0.4f;
        var br = source.GetComponentInParent<BuildingRagdoll>();
        if (br?.Health != null)
            return 1f - br.Health.integrity01;
        return 0.5f;
    }

    static float EstimateVictimRisk(GameObject source)
    {
        // Procedural stub — hearing/dialog can raise this later.
        return source != null ? 0.55f : 0.3f;
    }

    public void RegisterVictimHint(string personaKey, Vector3 worldPos)
    {
        targets.Add(new FireTargetScore
        {
            target = null,
            heat01 = 0.2f,
            windowLight01 = 0.1f,
            victimRisk01 = 0.9f,
            waterDemandLiters = 100f
        });
        Retally();
    }
}
