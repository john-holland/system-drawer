using System.Reflection;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>
/// Scene hub linking <see cref="SystemDrawerService"/> to typed wizard/service components with optional bulk sync.
/// Loose <see cref="Object"/> slots avoid asmdef cycles to BedogaGenerator (assign Spatial 4D wizard there manually).
/// </summary>
[AddComponentMenu("System Drawer/System Drawer Facilitator")]
[DisallowMultipleComponent]
public class SystemDrawerFacilitator : MonoBehaviour
{
    [Tooltip("Explicit service; when null uses FindInScene / Instance.")]
    [SerializeField] private SystemDrawerService service;

    [Header("Wizard components (prefer under child _Wizards)")]
    [SerializeField] private CalendarServiceWizard calendarWizard;
    [SerializeField] private NarrativePromptServiceWizard narrativePromptWizard;
    [SerializeField] private RagdollServiceWizard ragdollWizard;
    [SerializeField] private UscBuildServiceWizard uscBuildWizard;
    [SerializeField] private WeatherServiceWizardComponent weatherWizard;

    [Header("Other helpers (optional)")]
    [SerializeField] private BrainMessageService brainMessageService;
    [SerializeField] private SystemDrawerAnimator systemDrawerAnimator;
    [SerializeField] private AmbulatingActorRegistrar ambulatingActorRegistrar;

    [Tooltip("Assign a Bedoga Spatial4DServiceWizard reference (avoid typed field to prevent asmdef cycle).")]
    [SerializeField] private Object spatial4DServiceWizard;

    public SystemDrawerService Service => service;

    /// <summary>Resolve configured or scene <see cref="SystemDrawerService"/>.</summary>
    public SystemDrawerService ResolveService()
    {
        if (service != null)
            return service;
        return SystemDrawerService.FindInScene();
    }

    /// <summary>Wire serialized wizard fields from child objects if left empty.</summary>
    public void EnsureWizardReferencesFilled()
    {
        if (calendarWizard == null)
            calendarWizard = GetComponentInChildren<CalendarServiceWizard>();
        if (narrativePromptWizard == null)
            narrativePromptWizard = GetComponentInChildren<NarrativePromptServiceWizard>();
        if (ragdollWizard == null)
            ragdollWizard = GetComponentInChildren<RagdollServiceWizard>();
        if (uscBuildWizard == null)
            uscBuildWizard = GetComponentInChildren<UscBuildServiceWizard>();
        if (weatherWizard == null)
            weatherWizard = GetComponentInChildren<WeatherServiceWizardComponent>();
        if (brainMessageService == null)
            brainMessageService = GetComponentInChildren<BrainMessageService>();
        if (systemDrawerAnimator == null)
            systemDrawerAnimator = GetComponentInChildren<SystemDrawerAnimator>();
        if (ambulatingActorRegistrar == null)
            ambulatingActorRegistrar = GetComponentInChildren<AmbulatingActorRegistrar>();
        if (spatial4DServiceWizard == null)
        {
            var all = GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < all.Length; i++)
            {
                var mb = all[i];
                if (mb != null && mb.GetType().Name == "Spatial4DServiceWizard")
                {
                    spatial4DServiceWizard = mb;
                    break;
                }
            }
        }
    }

    /// <summary>Call each wizard's <see cref="TryCompleteFromService"/> plus reflection for Spatial loose ref.</summary>
    public int TryCacheFromService()
    {
        int n = 0;
        if (calendarWizard != null && calendarWizard.TryCompleteFromService())
            n++;
        if (narrativePromptWizard != null && narrativePromptWizard.TryCompleteFromService())
            n++;
        if (ragdollWizard != null && ragdollWizard.TryCompleteFromService())
            n++;
        if (uscBuildWizard != null && uscBuildWizard.TryCompleteFromService())
            n++;
        if (weatherWizard != null && weatherWizard.TryCompleteFromService())
            n++;
        if (TrySpatialLooseTryComplete())
            n++;
        return n;
    }

    /// <summary>
    /// Register known keys into <see cref="SystemDrawerService"/> (manual push; complements each wizard's OnEnable).
    /// Returns number of registrations performed.
    /// </summary>
    public int TryRegisterAllKnown()
    {
        var svc = ResolveService();
        if (svc == null)
            return 0;
        var count = 0;

        if (calendarWizard != null && calendarWizard.calendarAsset != null)
        {
            svc.Register(CalendarServiceWizard.ServiceKey, calendarWizard.calendarAsset);
            count++;
        }

        if (narrativePromptWizard != null && narrativePromptWizard.promptInterpreter != null)
        {
            svc.Register(NarrativePromptServiceWizard.ServiceKey, narrativePromptWizard.promptInterpreter);
            count++;
        }

        if (ragdollWizard != null && ragdollWizard.ragdollRoot != null)
        {
            svc.Register(RagdollServiceWizard.ServiceKey, ragdollWizard.ragdollRoot);
            if (!string.IsNullOrWhiteSpace(ragdollWizard.alsoRegisterAsPlayerKey))
                svc.Register(ragdollWizard.alsoRegisterAsPlayerKey.Trim(), ragdollWizard.ragdollRoot.gameObject);
            count++;
        }

        if (uscBuildWizard != null)
        {
            svc.Register(UscBuildServiceWizard.ServiceKey, uscBuildWizard);
            count++;
        }

        if (weatherWizard != null && weatherWizard.weatherSystemObject != null)
        {
            svc.Register(WeatherServiceWizardComponent.ServiceKey, weatherWizard.weatherSystemObject);
            count++;
        }

        if (brainMessageService != null)
        {
            var key = GetBrainMessageRegisterKey(brainMessageService);
            svc.Register(key, brainMessageService);
            count++;
        }

        count += TryRegisterSpatialLooseInternal(svc, spatial4DServiceWizard);

        return count;
    }

    private bool TrySpatialLooseTryComplete()
    {
        if (spatial4DServiceWizard == null)
            return false;
        if (spatial4DServiceWizard is not MonoBehaviour mb)
            return false;
        var m = mb.GetType().GetMethod("TryCompleteFromService", BindingFlags.Public | BindingFlags.Instance);
        if (m == null)
            return false;
        var r = m.Invoke(mb, null);
        return r is bool b && b;
    }

    private static int TryRegisterSpatialLooseInternal(SystemDrawerService svc, Object spatialLoose)
    {
        if (spatialLoose is not MonoBehaviour mb)
            return 0;
        var t = mb.GetType();
        if (t.Name != "Spatial4DServiceWizard")
            return 0;
        const BindingFlags bf = BindingFlags.Public | BindingFlags.Instance;
        var orchField = t.GetField("orchestrator", bf);
        if (orchField == null)
            return 0;
        var orch = orchField.GetValue(mb) as Object;
        if (orch == null)
            return 0;
        var keyField = t.GetField("ServiceKey", BindingFlags.Public | BindingFlags.Static);
        var key = keyField?.GetValue(null) as string;
        if (string.IsNullOrEmpty(key))
            key = "Spatial4DOrchestrator";
        svc.Register(key, orch);
        return 1;
    }

    private static string GetBrainMessageRegisterKey(BrainMessageService b)
    {
        if (b == null)
            return BrainMessageService.DefaultServiceKey;
        var fi = typeof(BrainMessageService).GetField("registerKey",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (fi == null)
            return BrainMessageService.DefaultServiceKey;
        var v = fi.GetValue(b) as string;
        return string.IsNullOrEmpty(v) ? BrainMessageService.DefaultServiceKey : v;
    }
}
