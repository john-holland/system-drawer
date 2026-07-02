using Locomotion.Narrative;
using SystemDrawer.Quest;
using UnityEngine;

/// <summary>Service wizard for QuestRunner + QuestMapRenderer. Registers with SystemDrawerService.</summary>
public class QuestServiceWizard : MonoBehaviour
{
    public const string ServiceKey = "QuestRunner";

    public QuestRunner runner;
    public QuestMapRenderer mapRenderer;
    [Tooltip("Loose ref to SpatialGenerator4DOrchestrator (BedogaGenerator) to avoid asmdef cycle.")]
    public Object spatialOrchestrator;

    public bool TryCompleteFromService()
    {
        var service = SystemDrawerService.Instance;
        if (service == null)
            return false;
        var r = service.Get<QuestRunner>(ServiceKey);
        if (r != null)
        {
            runner = r;
            return true;
        }
        return false;
    }

    void OnEnable()
    {
        if (runner != null && SystemDrawerService.Instance != null)
            SystemDrawerService.Instance.Register(ServiceKey, runner);
        if (mapRenderer != null && SystemDrawerService.Instance != null)
            SystemDrawerService.Instance.Register("QuestMapRenderer", mapRenderer);
    }

    void OnDisable()
    {
        if (SystemDrawerService.Instance == null)
            return;
        SystemDrawerService.Instance.Unregister(ServiceKey);
        SystemDrawerService.Instance.Unregister("QuestMapRenderer");
    }
}
