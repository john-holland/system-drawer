using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Venue hours + open/close for restaurant; spawns retinue waypoint groups when open.
/// </summary>
[CreateAssetMenu(fileName = "RestaurantOpenCloseTopology", menuName = "Locomotion/Kitchen/Restaurant Open-Close Topology")]
public sealed class RestaurantOpenCloseTopologyAsset : ScriptableObject
{
    public string restaurantId;
    [CronExpr] public string hoursCron = "0 11-22 * * *";
    public bool continuousDeliveryStations = true;
    public List<string> waypointGroupIds = new List<string> { "kitchen-line", "kitchen-pass", "foh" };
    public List<string> sinkScanTags = new List<string> { "sink", "handwash" };
}

[AddComponentMenu("Locomotion/Kitchen/Restaurant Venue Runtime")]
public sealed class RestaurantVenueRuntime : MonoBehaviour
{
    public RestaurantOpenCloseTopologyAsset topology;
    public ThreatWarden threatWarden;
    public KitchenBioRhythmService kitchenBio;
    public bool isOpen;
    public List<RetinuePeckingEntry> retinue = new List<RetinuePeckingEntry>();
    public List<GameObject> waypointGroupRoots = new List<GameObject>();

    public event Action<bool> OpenStateChanged;

    void Awake()
    {
        if (threatWarden == null) threatWarden = GetComponent<ThreatWarden>() ?? gameObject.AddComponent<ThreatWarden>();
        if (kitchenBio == null) kitchenBio = GetComponent<KitchenBioRhythmService>() ?? gameObject.AddComponent<KitchenBioRhythmService>();
        threatWarden.contextOwner = gameObject;
        threatWarden.SetRetinuePeckingOrder(retinue);
    }

    public void SetOpen(bool open)
    {
        if (isOpen == open) return;
        isOpen = open;
        for (int i = 0; i < waypointGroupRoots.Count; i++)
            if (waypointGroupRoots[i] != null)
                waypointGroupRoots[i].SetActive(open);
        if (open)
            ActivateRetinueWaypoints();
        OpenStateChanged?.Invoke(open);
    }

    void ActivateRetinueWaypoints()
    {
        threatWarden?.SetRetinuePeckingOrder(retinue);
        for (int i = 0; i < retinue.Count; i++)
        {
            var e = retinue[i];
            if (e?.actor == null) continue;
            e.actor.SetActive(true);
            // TravelAgent / WaypointPlannerInput sync is scene-authored; nudge agents
            e.actor.SendMessage("OnRestaurantOpen", this, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void AttemptKitchenCleanliness()
    {
        kitchenBio?.NotifyCleanAttempt(0.15f);
        var consider = GetComponent<ConsiderChefCards>();
        if (consider != null)
        {
            consider.dutyMode = ChefDutyMode.Hygiene;
            consider.GenerateCards();
        }
    }

    public void OnKitchenLemmaPresent()
    {
        AttemptKitchenCleanliness();
    }
}
