using UnityEngine;

public enum TrafficWardenMode
{
    NormalLadder = 0,
    CongestedHold = 1,
    EmergencyPreempt = 2,
    PoliceDetailActive = 3,
    NarrativeLease = 4
}

/// <summary>Policy modes for TrafficWarden — light hold/priority and detail dispatch.</summary>
[System.Serializable]
public sealed class TrafficWardenStateMachine
{
    public TrafficWardenMode Mode { get; private set; } = TrafficWardenMode.NormalLadder;
    public Vector3 detailTargetWorld;
    public float congestedDemandThreshold = 8f;
    public float holdAllRedExtraSec = 2f;

    TrafficWarden _owner;

    public void Bind(TrafficWarden owner) => _owner = owner;

    public void Enter(TrafficWardenMode mode)
    {
        if (Mode == mode) return;
        Mode = mode;
        _owner?.OnStateEntered(mode);
    }

    public void Tick(float dt, float maxEdgeDemand, bool narrativeLeaseActive)
    {
        if (Mode == TrafficWardenMode.PoliceDetailActive || Mode == TrafficWardenMode.EmergencyPreempt)
            return;

        if (narrativeLeaseActive)
        {
            Enter(TrafficWardenMode.NarrativeLease);
            return;
        }

        if (maxEdgeDemand >= congestedDemandThreshold)
            Enter(TrafficWardenMode.CongestedHold);
        else if (Mode == TrafficWardenMode.CongestedHold || Mode == TrafficWardenMode.NarrativeLease)
            Enter(TrafficWardenMode.NormalLadder);
    }

    public void BeginPoliceDetail(Vector3 worldTarget)
    {
        detailTargetWorld = worldTarget;
        Enter(TrafficWardenMode.PoliceDetailActive);
    }

    public void ClearPoliceDetail()
    {
        if (Mode == TrafficWardenMode.PoliceDetailActive)
            Enter(TrafficWardenMode.NormalLadder);
    }

    /// <summary>Advise a TravelAgent for flow enforcement (MST goal / wait for light).</summary>
    public void AdviseAgent(TravelAgent agent, TrafficWarden warden)
    {
        if (agent == null || warden == null) return;
        Vector3 goal = warden.SuggestFlowGoal(agent.transform.position);
        agent.previewGoalWorld = goal;
        agent.ApplyAvoidHintsFromWarden();
        agent.RebuildCachedPlan();
    }
}
