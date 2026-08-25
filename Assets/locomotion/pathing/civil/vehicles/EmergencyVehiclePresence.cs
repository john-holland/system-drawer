using System.Collections.Generic;
using UnityEngine;
using Weather.Emergence;

/// <summary>Local hear/see radius around an emergency warning bar. Grafts yield/flee goals.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Vehicles/Emergency Vehicle Presence")]
public sealed class EmergencyVehiclePresence : MonoBehaviour, IWeatherEmergenceSource
{
    public EmergencyWarningBar bar;
    public float hearRadiusM = 40f;
    public float seeRadiusM = 50f;
    public float seeFovDeg = 90f;
    public float scanInterval = 0.2f;
    public bool showFleeingBirdsGizmo;
    public readonly List<TravelAgent> trackedActors = new List<TravelAgent>();
    public readonly List<EmergencyFleeBird> birds = new List<EmergencyFleeBird>();
    public EmergencyPullOverOverlay overlay = new EmergencyPullOverOverlay();
    public EmergencyFleeFlock flock = new EmergencyFleeFlock();

    readonly HashSet<int> _grafted = new HashSet<int>();
    readonly Dictionary<int, TravelAgent> _graftedAgents = new Dictionary<int, TravelAgent>();
    float _scanT;

    void Awake()
    {
        if (bar == null)
            bar = GetComponent<EmergencyWarningBar>();
    }

    void Update()
    {
        if (bar == null || !bar.barOn)
        {
            ClearAll();
            return;
        }
        _scanT -= Time.deltaTime;
        if (_scanT > 0f) return;
        _scanT = scanInterval;
        RefreshTracked();
        overlay.Refresh(this);
        flock.Integrate(this, Time.deltaTime);
        ApplyGrafts();
    }

    public void RefreshTracked()
    {
        trackedActors.Clear();
        Vector3 origin = transform.position;
        var agents = TravelAgentRegistry.All;
        for (int i = 0; i < agents.Count; i++)
        {
            var ta = agents[i];
            if (ta == null || ta.transform == transform || ta.transform.IsChildOf(transform)) continue;
            Vector3 to = ta.transform.position - origin;
            float dist = to.magnitude;
            bool hear = dist <= hearRadiusM;
            bool see = dist <= seeRadiusM && Vector3.Angle(transform.forward, to) <= seeFovDeg * 0.5f;
            if (hear || see)
                trackedActors.Add(ta);
        }
        SyncBirds();
    }

    void SyncBirds()
    {
        while (birds.Count < trackedActors.Count)
            birds.Add(new EmergencyFleeBird());
        while (birds.Count > trackedActors.Count)
            birds.RemoveAt(birds.Count - 1);
        for (int i = 0; i < trackedActors.Count; i++)
        {
            birds[i].actor = trackedActors[i];
            birds[i].world = trackedActors[i].transform.position + Vector3.up * 2f;
        }
    }

    void ApplyGrafts()
    {
        var seen = new HashSet<int>();
        Vector3 ev = transform.position;
        for (int i = 0; i < trackedActors.Count; i++)
        {
            var ta = trackedActors[i];
            int id = ta.GetInstanceID();
            seen.Add(id);
            Vector3 to = ta.transform.position - ev;
            bool hear = to.magnitude <= hearRadiusM;
            bool see = to.magnitude <= seeRadiusM;
            bool drive = ta.CachedPlan != null && ta.CachedPlan.segments != null
                         && ta.CachedPlan.segments.Count > 0
                         && ta.CachedPlan.segments[0].mode == TravelLegMode.Drive;
            string goalName = drive ? "emergency_yield" : "emergency_flee";
            if (PlayerVehicleTravelSlowOverride.ShouldApplyTravelSlow(ta))
            {
                ta.followTimeSec = Mathf.Max(ta.followTimeSec, 5f);
                ta.avoidRadius = Mathf.Max(ta.avoidRadius, hearRadiusM);
                if (!ta.avoidActors.Contains(transform))
                    ta.avoidActors.Add(transform);
                var slot = overlay.SlotFor(ta);
                if (slot != null)
                    ta.previewGoalWorld = slot.steeringWorld;
            }
            if (!_grafted.Contains(id))
            {
                Graft(ta.gameObject, goalName, hear, see);
                _grafted.Add(id);
                _graftedAgents[id] = ta;
            }
        }

        var toClear = new List<int>();
        foreach (int id in _grafted)
            if (!seen.Contains(id)) toClear.Add(id);
        for (int i = 0; i < toClear.Count; i++)
        {
            TryClearGraftById(toClear[i]);
            _grafted.Remove(toClear[i]);
        }
    }

    void Graft(GameObject actor, string goalName, bool hear, bool see)
    {
        if (actor == null) return;
        var bt = actor.GetComponent<BehaviorTree>() ?? actor.GetComponentInParent<BehaviorTree>();
        var nervous = actor.GetComponent<NervousSystem>() ?? actor.GetComponentInParent<NervousSystem>();
        var goal = new BehaviorTreeGoal
        {
            goalName = goalName,
            type = GoalType.TravelAgent,
            targetPosition = transform.position,
            priority = 7
        };
        goal.parameters["sourceId"] = gameObject.name;
        goal.parameters["hear"] = hear;
        goal.parameters["see"] = see;
        goal.parameters["kind"] = bar != null ? bar.kind.ToString() : "Police";
        if (bt != null) bt.SetGoal(goal);
        else nervous?.AddGoal(goal);
        if (goalName == "emergency_flee")
        {
            var card = TravelAgentCard.GenerateDefault(gameObject);
            card.preferFlee = true;
            card.ApplyToActor(actor, 1f);
        }
    }

    public void TryClearGraft(GameObject actor)
    {
        if (actor == null) return;
        var bt = actor.GetComponent<BehaviorTree>() ?? actor.GetComponentInParent<BehaviorTree>();
        var nervous = actor.GetComponent<NervousSystem>() ?? actor.GetComponentInParent<NervousSystem>();
        if (bt != null && bt.currentGoal != null
            && (bt.currentGoal.goalName == "emergency_yield" || bt.currentGoal.goalName == "emergency_flee"))
            bt.currentGoal = null;
        if (nervous != null && nervous.GetCurrentGoal() != null)
        {
            string n = nervous.GetCurrentGoal().goalName;
            if (n == "emergency_yield" || n == "emergency_flee")
                nervous.SetCurrentGoal(null);
        }
    }

    void TryClearGraftById(int id)
    {
        if (_graftedAgents.TryGetValue(id, out var ta) && ta != null)
            TryClearGraft(ta.gameObject);
        _graftedAgents.Remove(id);
    }

    void ClearAll()
    {
        for (int i = 0; i < trackedActors.Count; i++)
            TryClearGraft(trackedActors[i] != null ? trackedActors[i].gameObject : null);
        trackedActors.Clear();
        birds.Clear();
        _grafted.Clear();
        _graftedAgents.Clear();
    }

    public void CollectEmergenceVectors(List<EmergenceVector> into)
    {
        if (into == null || bar == null || !bar.barOn) return;
        into.Add(EmergenceVector.Point(transform.position, hearRadiusM, -1f, "emergency_bar"));
    }

    void OnDrawGizmos()
    {
        if (!showFleeingBirdsGizmo) return;
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, hearRadiusM);
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, seeRadiusM);
        for (int i = 0; i < birds.Count; i++)
        {
            var b = birds[i];
            if (b == null) continue;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(b.world, 0.2f);
            if (b.actor != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(b.world, b.actor.previewGoalWorld);
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(b.world, b.desiredWorld);
            }
        }
    }

    public int GizmoBirdCount => showFleeingBirdsGizmo ? birds.Count : 0;
}

[System.Serializable]
public sealed class EmergencyFleeBird
{
    public TravelAgent actor;
    public Vector3 world;
    public Vector3 desiredWorld;
    public Vector3 velocity;
}

[System.Serializable]
public sealed class EmergencyFleeFlock
{
    public float separation = 4f;
    public float seek = 1.2f;

    public void Integrate(EmergencyVehiclePresence presence, float dt)
    {
        if (presence == null) return;
        Vector3 ev = presence.transform.position;
        for (int i = 0; i < presence.birds.Count; i++)
        {
            var b = presence.birds[i];
            Vector3 sep = (b.world - ev).normalized * separation;
            Vector3 slot = presence.overlay.SlotFor(b.actor) != null
                ? presence.overlay.SlotFor(b.actor).chaosWorld
                : (b.world + sep);
            Vector3 desired = sep + (slot - b.world) * seek;
            b.velocity = Vector3.Lerp(b.velocity, desired, 0.3f);
            b.world += b.velocity * dt;
            b.desiredWorld = slot;
        }
    }
}

[System.Serializable]
public sealed class EmergencyPullOverSlot
{
    public TravelAgent agent;
    public string roadSegmentId;
    public int laneIndex;
    public int cellIndex;
    public Vector3 steeringWorld;
    public Vector3 chaosWorld;
}

[System.Serializable]
public sealed class EmergencyPullOverOverlay
{
    public readonly List<EmergencyPullOverSlot> slots = new List<EmergencyPullOverSlot>();

    public void Refresh(EmergencyVehiclePresence presence)
    {
        slots.Clear();
        if (presence == null) return;
        for (int i = 0; i < presence.trackedActors.Count; i++)
        {
            var ta = presence.trackedActors[i];
            Vector3 steer = ta.previewGoalWorld;
            Vector3 chaos = steer;
            var zones = ParkingZoneIndex.QueryNear(ta.transform.position, 30f);
            if (zones != null && zones.Count > 0)
                chaos = zones[0].transform.position;
            string seg = ta.CachedPlan?.segments != null && ta.CachedPlan.segments.Count > 0
                ? ta.CachedPlan.segments[0].roadSegmentId
                : "";
            var slot = new EmergencyPullOverSlot
            {
                agent = ta,
                roadSegmentId = seg,
                laneIndex = 0,
                cellIndex = i,
                steeringWorld = steer,
                chaosWorld = chaos
            };
            if (ta.laneOccupancy != null)
                ta.laneOccupancy.TryOccupy(RoadLaneOccupancy.SlotKey(seg, 0, i), ta);
            slots.Add(slot);
        }
    }

    public EmergencyPullOverSlot SlotFor(TravelAgent agent)
    {
        if (agent == null) return null;
        for (int i = 0; i < slots.Count; i++)
            if (slots[i].agent == agent) return slots[i];
        return null;
    }
}
