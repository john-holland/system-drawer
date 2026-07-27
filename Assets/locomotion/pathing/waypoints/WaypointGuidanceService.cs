using System.Collections.Generic;
using UnityEngine;

/// <summary>Drives a troupe along a shared WaypointRoute (PC guide line / NPC TravelAgent).</summary>
[AddComponentMenu("Locomotion/Waypoints/Guidance Service")]
public sealed class WaypointGuidanceService : MonoBehaviour
{
    public WaypointRoute route = new WaypointRoute();
    public FormationCatalog formationCatalog;
    public CombatRulesFacilitatorService facilitator;
    public string troupeId = "default";
    public LineRenderer guideLine;
    public bool enableGambitForPlayerGuide = true;
    public Color guideColor = new Color(0.85f, 0.75f, 0.35f, 0.9f);

    readonly List<WaypointMarkerRuntime> _spawned = new List<WaypointMarkerRuntime>();

    void Awake()
    {
        EnsureGuideLine();
    }

    void LateUpdate()
    {
        RefreshGuideLine();
    }

    public void BindRoute(WaypointRoute r)
    {
        route = r ?? new WaypointRoute();
        OnRouteChanged();
    }

    public void OnRouteChanged()
    {
        RebuildMarkerVisuals();
        ApplyFormationToTroupe(0);
        DriveAgentsTowardActive();
        RefreshGuideLine();
    }

    public void DriveAgentsTowardActive()
    {
        if (route == null || route.Count == 0) return;
        var wp = route.Active ?? route.markers[0];
        if (wp == null) return;
        var members = ResolveMembers();
        for (int i = 0; i < members.Count; i++)
        {
            var m = members[i];
            if (m.actor == null) continue;
            if (m.mode == TravelGuidanceMode.PlayerGuide)
                continue; // guide line only; optional gambit elsewhere
            var agent = m.actor.GetComponent<TravelAgent>() ?? m.actor.GetComponentInChildren<TravelAgent>();
            if (agent == null) continue;
            ApplyCoeffs(agent, m.coeffs);
            ApplyFormationAsset(agent, wp.formationId);
            agent.previewGoalWorld = wp.worldPosition;
            agent.RebuildCachedPlan(wp.targetActorOrObject);
        }
    }

    public void ApplyFormationToTroupe(int waypointIndex)
    {
        if (route?.markers == null || waypointIndex < 0 || waypointIndex >= route.markers.Count) return;
        var wp = route.markers[waypointIndex];
        if (wp == null) return;
        var members = ResolveMembers();
        for (int i = 0; i < members.Count; i++)
        {
            var agent = members[i].actor != null
                ? members[i].actor.GetComponent<TravelAgent>() ?? members[i].actor.GetComponentInChildren<TravelAgent>()
                : null;
            if (agent == null) continue;
            ApplyFormationAsset(agent, wp.formationId);
            if (!string.IsNullOrEmpty(troupeId))
                agent.multibodyFormationGroupId = troupeId;
        }
    }

    void ApplyFormationAsset(TravelAgent agent, string formationId)
    {
        if (agent?.multibody == null || formationCatalog == null) return;
        if (formationCatalog.TryGet(formationId, out var asset))
            agent.multibody.formation = asset;
    }

    void ApplyCoeffs(TravelAgent agent, TravelFeatureCoefficients coeffs)
    {
        if (agent == null || coeffs == null) return;
        agent.waypointFeatureCoeffs = coeffs;
        if (agent.multibody != null && !coeffs.AllowMultibody)
            agent.multibody.enableMultibody = false;
    }

    List<(GameObject actor, TravelGuidanceMode mode, TravelFeatureCoefficients coeffs)> ResolveMembers()
    {
        var list = new List<(GameObject, TravelGuidanceMode, TravelFeatureCoefficients)>();
        if (facilitator != null && facilitator.TryGetTroupe(troupeId, out var troupe) && troupe != null)
        {
            for (int i = 0; i < troupe.members.Count; i++)
            {
                var mem = troupe.members[i];
                if (mem?.actor == null) continue;
                list.Add((mem.actor, mem.guidanceMode, mem.coeffs ?? troupe.defaultCoeffs));
            }
            return list;
        }
        // Fallback: this GameObject only
        list.Add((gameObject, TravelGuidanceMode.NpcFull, new TravelFeatureCoefficients()));
        return list;
    }

    void EnsureGuideLine()
    {
        if (guideLine != null) return;
        guideLine = GetComponent<LineRenderer>();
        if (guideLine != null) return;
        guideLine = gameObject.AddComponent<LineRenderer>();
        guideLine.widthMultiplier = 0.08f;
        guideLine.material = new Material(Shader.Find("Sprites/Default"));
        guideLine.startColor = guideColor;
        guideLine.endColor = guideColor;
        guideLine.positionCount = 0;
    }

    void RefreshGuideLine()
    {
        EnsureGuideLine();
        if (route == null || route.Count == 0)
        {
            guideLine.positionCount = 0;
            return;
        }
        var pts = route.Polyline();
        guideLine.positionCount = pts.Count;
        for (int i = 0; i < pts.Count; i++)
            guideLine.SetPosition(i, pts[i] + Vector3.up * 0.15f);
    }

    void RebuildMarkerVisuals()
    {
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
        _spawned.Clear();
        if (route?.markers == null) return;
        for (int i = 0; i < route.markers.Count; i++)
        {
            var m = route.markers[i];
            if (m == null) continue;
            var go = new GameObject($"Waypoint_{m.name}");
            go.transform.SetParent(transform, false);
            go.transform.position = m.worldPosition;
            var runtime = go.AddComponent<WaypointMarkerRuntime>();
            runtime.Bind(m);
            _spawned.Add(runtime);
        }
    }
}
