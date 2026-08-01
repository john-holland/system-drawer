using System;
using System.Collections.Generic;
using UnityEngine;
using Locomotion.Narrative;

/// <summary>
/// Civil-system day manager: persona request, biorhythm bias, causality-gated retinue wake, LOD.
/// </summary>
[AddComponentMenu("Locomotion/Persona/Persona Day Manager")]
public sealed class PersonaDayManager : MonoBehaviour
{
    public const string ServiceKey = "persona.day";

    static PersonaDayManager _instance;
    public static PersonaDayManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<PersonaDayManager>();
            return _instance;
        }
    }

    [Header("Lattice")]
    public CivilSystemLattice lattice = new CivilSystemLattice();
    public CivilLodController lodController = new CivilLodController();
    public RateLimitedWouldHaveBeenTracker wouldHaveBeen = new RateLimitedWouldHaveBeenTracker();
    public SpatialRetinueWakeSource wakeSource;

    [Header("Time / player")]
    public NarrativeClock narrativeClock;
    public Transform playerTransform;
    public Rigidbody playerBody;
    public float tickIntervalSeconds = 0.5f;
    public string apiBaseUrl = "http://127.0.0.1:5050";
    public string cityId = "demo-city";

    [Header("Prefs")]
    public bool applyGovGloveBias = true;
    public bool driveTravelAgentOnWake = true;

    float _accum;
    readonly Dictionary<string, PersonaRequestBundle> _bundleCache = new Dictionary<string, PersonaRequestBundle>(StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
        _instance = this;
        if (wakeSource == null) wakeSource = GetComponent<SpatialRetinueWakeSource>() ?? gameObject.AddComponent<SpatialRetinueWakeSource>();
        if (lodController == null) lodController = new CivilLodController();
        if (wouldHaveBeen == null) wouldHaveBeen = new RateLimitedWouldHaveBeenTracker();
        if (lattice == null) lattice = new CivilSystemLattice();
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    void Update()
    {
        _accum += Time.deltaTime;
        if (_accum < tickIntervalSeconds) return;
        float dt = _accum;
        _accum = 0f;
        Tick(dt);
    }

    public void Tick(float dt)
    {
        float speed = ResolvePlayerSpeed();
        float combined = lodController.ComputeCombinedScale(speed);
        float narrativeT = ResolveNarrativeSeconds();
        var ordered = lattice.OrderedByPriority();
        int fullSimUsed = 0;
        int wokenActors = 0;
        var utcNow = DateTime.UtcNow;

        for (int i = 0; i < ordered.Count; i++)
        {
            var venue = ordered[i];
            if (venue == null) continue;

            bool causalOk = PassesCausality(venue, narrativeT);
            bool cronOk = CronDue.IsActiveSchedule(venue.hoursCron, utcNow);
            var tier = lodController.ResolveTier(combined, i, fullSimUsed);
            if (!causalOk || !cronOk)
                tier = CivilLodTier.Culled;

            venue.currentTier = tier;
            bool shouldWake = tier == CivilLodTier.FullSim || tier == CivilLodTier.Proxy;
            wouldHaveBeen.NoteWake(shouldWake && causalOk && cronOk);

            if (tier == CivilLodTier.FullSim)
                fullSimUsed++;

            if (shouldWake)
            {
                EnsureRetinueBound(venue);
                ApplyPersona(venue);
                WakeVenue(venue, tier);
                wokenActors += venue.CountWokenActors();
                if (wokenActors > lodController.maxWokenActors)
                {
                    // Soft-cap: demote remaining to ghost accounting
                    wouldHaveBeen.NoteWake(false);
                }
                wouldHaveBeen.NoteBioTick(true);
                TickVenueBio(venue, dt);
                wouldHaveBeen.NoteBtTick(tier == CivilLodTier.FullSim);
                wouldHaveBeen.NoteReplan(tier == CivilLodTier.FullSim && driveTravelAgentOnWake);
            }
            else
            {
                SleepVenue(venue);
                wouldHaveBeen.NoteBioTick(false);
                wouldHaveBeen.NoteBtTick(false);
                wouldHaveBeen.NoteReplan(false);
            }
        }
    }

    public void RegisterVenue(CivilVenueNode node) => lattice.Register(node);

    public void ApplySettings(
        IList<CivilSystemKind> kindOrder,
        float developerMaxSpeedMps,
        float logFalloffBase,
        float lodFloor,
        int maxFullSim,
        int maxWoken)
    {
        if (kindOrder != null && kindOrder.Count > 0)
            lattice.kindPriorityOrder = new List<CivilSystemKind>(kindOrder);
        lodController.speedPolicy.developerMaxSpeedMps = developerMaxSpeedMps;
        lodController.speedPolicy.logFalloffBase = logFalloffBase;
        lodController.speedPolicy.lodFloor = lodFloor;
        lodController.maxFullSimVenues = maxFullSim;
        lodController.maxWokenActors = maxWoken;
    }

    public void CacheBundle(PersonaRequestBundle bundle)
    {
        if (bundle == null || string.IsNullOrEmpty(bundle.personaKey)) return;
        _bundleCache[bundle.personaKey] = bundle;
    }

    float ResolvePlayerSpeed()
    {
        if (playerBody != null)
            return playerBody.linearVelocity.magnitude;
        if (playerTransform != null)
        {
            // Approximate from TravelAgent if present
            var ta = playerTransform.GetComponent<TravelAgent>();
            if (ta != null)
                return 0f; // unknown without kinematics; treat as in-bounds
        }
        return 0f;
    }

    float ResolveNarrativeSeconds()
    {
        if (narrativeClock != null)
            return narrativeClock.SimulationSeconds;
        return Time.time;
    }

    bool PassesCausality(CivilVenueNode venue, float t)
    {
        if (venue.minCausalDepth <= 0f)
            return true;
        if (!NarrativeVolumeQuery.Sample4D(venue.WorldPosition, t, out _, out float depth))
            return venue.minCausalDepth <= 0f;
        return depth >= venue.minCausalDepth;
    }

    void EnsureRetinueBound(CivilVenueNode venue)
    {
        if (venue.retinue == null || venue.retinue.Count == 0)
            wakeSource?.CollectNearby(venue);
    }

    void ApplyPersona(CivilVenueNode venue)
    {
        if (!applyGovGloveBias) return;
        PersonaRequestBundle bundle = venue.lastBundle;
        if (bundle == null && venue.retinue != null && venue.retinue.Count > 0)
        {
            var key = venue.retinue[0].personaKey;
            if (!string.IsNullOrEmpty(key) && _bundleCache.TryGetValue(key, out var cached))
                bundle = cached;
        }
        if (bundle == null)
            bundle = PersonaRequestBundle.CreateDefault(venue.stableId, venue.kind);

        venue.lastBundle = bundle;
        for (int i = 0; i < venue.retinue.Count; i++)
        {
            var e = venue.retinue[i];
            if (e?.actor == null) continue;
            var sheet = e.actor.GetComponent<LifeSystemsSheet>() ?? e.actor.AddComponent<LifeSystemsSheet>();
            sheet.EnsureDefaults();
            LifeSystemsGovGloveBias.ApplyBaselineBias(sheet, bundle.societyFeatures, bundle.needSatisfied01);
            sheet.bioRhythm?.ApplyAmplitudeDelta((bundle.biorhythmAmplitudeSeed - 0.5f) * 0.1f);
        }
        if (venue.venueBio != null)
            venue.venueBio.ApplyPersonaSeed(bundle.biorhythmAmplitudeSeed);
    }

    void WakeVenue(CivilVenueNode venue, CivilLodTier tier)
    {
        venue.isOpen = true;
        if (venue.kind == CivilSystemKind.Kitchen && venue.kitchenRuntime != null)
        {
            if (venue.retinue != null)
                venue.kitchenRuntime.retinue = venue.retinue;
            venue.kitchenRuntime.SetOpen(true);
        }
        else
        {
            venue.venueBio?.NotifyOpen();
            if (venue.retinue != null)
            {
                for (int i = 0; i < venue.retinue.Count; i++)
                {
                    var a = venue.retinue[i]?.actor;
                    if (a != null && !a.activeSelf)
                        a.SetActive(true);
                }
            }
        }

        if (tier == CivilLodTier.FullSim && driveTravelAgentOnWake)
            DriveTroupe(venue);
    }

    void SleepVenue(CivilVenueNode venue)
    {
        if (!venue.isOpen) return;
        venue.isOpen = false;
        if (venue.kind == CivilSystemKind.Kitchen && venue.kitchenRuntime != null)
            venue.kitchenRuntime.SetOpen(false);
        else if (venue.retinue != null)
        {
            for (int i = 0; i < venue.retinue.Count; i++)
            {
                var a = venue.retinue[i]?.actor;
                if (a != null && a.activeSelf)
                    a.SetActive(false);
            }
        }
    }

    void DriveTroupe(CivilVenueNode venue)
    {
        if (string.IsNullOrEmpty(venue.troupeId) && venue.waypointPlanner == null)
        {
            if (venue.retinue == null) return;
            for (int i = 0; i < venue.retinue.Count; i++)
            {
                var a = venue.retinue[i]?.actor;
                if (a == null) continue;
                a.SendMessage("OnCivilVenueOpen", venue, SendMessageOptions.DontRequireReceiver);
            }
            return;
        }
        // Soft hooks — facilitator / guidance may be scene-authored
        var facilitator = FindFirstObjectByType<CombatRulesFacilitatorService>();
        if (facilitator != null && !string.IsNullOrEmpty(venue.troupeId))
            facilitator.CallToArms(venue.troupeId, venue.WorldPosition);
        var guidance = FindFirstObjectByType<WaypointGuidanceService>();
        guidance?.DriveAgentsTowardActive();
        venue.waypointPlanner?.SendMessage("OnCivilVenueOpen", venue, SendMessageOptions.DontRequireReceiver);
    }

    void TickVenueBio(CivilVenueNode venue, float dt)
    {
        if (venue.kind == CivilSystemKind.Kitchen)
            venue.kitchenBio?.Tick(dt);
        else
            venue.venueBio?.Tick(dt);
    }
}
