using System;
using System.Collections.Generic;
using UnityEngine;

public enum SanitationSortIntegration
{
    Actor = 0,
    Machine = 1
}

public enum SanitationSortDownflowKind
{
    Bin = 0,
    Conveyor = 1
}

/// <summary>BT-enabled conveyor sorting — bag cut, loading, intermediate stations, downflow.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Sorting Station")]
public sealed class SanitationSortingStation : MonoBehaviour
{
    public string stationId = "sort_1";
    public BehaviorTree stationBt;
    public SanitationSortIntegration integration = SanitationSortIntegration.Machine;
    public SanitationSortDownflowKind downflowKind = SanitationSortDownflowKind.Conveyor;
    public Transform loadingArea;
    public Transform palletAnchor;
    public Transform conveyorAnchor;
    public Transform bagCutAnchor;
    public bool machineCutsBag = true;
    public string garbageBagConfigId = "random_garbage_bag";
    public List<string> intermediateStationIds = new List<string>();
    public List<SanitationDownflowSection> downflow = new List<SanitationDownflowSection>();
    public VehicleInstrumentPhysicsProxy scooperProxy;
    public SanitationFacilityRuntime facility;
    [Range(0f, 1f)] public float sortProgress01;

    void Awake()
    {
        if (facility == null)
            facility = GetComponentInParent<SanitationFacilityRuntime>();
        if (downflow.Count == 0)
        {
            downflow.Add(new SanitationDownflowSection
            {
                sectionId = "shake",
                stage = SanitationDownflowStage.ShakeFilter,
                commodityKey = "organic"
            });
            downflow.Add(new SanitationDownflowSection
            {
                sectionId = "extrude",
                stage = SanitationDownflowStage.FilterExtrude,
                commodityKey = "fertilizer",
                egress = SanitationEgressMode.Truck
            });
        }
    }

    public void Tick(float dt)
    {
        if (stationBt != null)
            stationBt.SendMessage("OnSanitationSortTick", dt, SendMessageOptions.DontRequireReceiver);
        sortProgress01 = Mathf.MoveTowards(sortProgress01, 1f, dt * 0.05f);
    }

    public bool CutBag(bool actorIk)
    {
        bool useActor = integration == SanitationSortIntegration.Actor || actorIk;
        string action = useActor ? "sanitation_bag_cut_actor" : "sanitation_bag_cut_machine";
        SendMessage("OnNarrativeSchedulerAction", action, SendMessageOptions.DontRequireReceiver);
        SendMessage("OnGarbageBagSplit", garbageBagConfigId, SendMessageOptions.DontRequireReceiver);
        return true;
    }

    public SanitationDownflowSection NextDownflow()
    {
        for (int i = 0; i < downflow.Count; i++)
            if (downflow[i] != null && downflow[i].throughput01 < 1f)
                return downflow[i];
        return downflow.Count > 0 ? downflow[downflow.Count - 1] : null;
    }
}
