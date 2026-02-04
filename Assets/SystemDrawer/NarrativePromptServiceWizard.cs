using UnityEngine;

/// <summary>
/// Service wizard for the Narrative LSTM prompt interpreter and summarizer.
/// Register with SystemDrawerService; use the custom editor to assign from the drawer, create the LSTM rig, or open export/training flows.
/// </summary>
public class NarrativePromptServiceWizard : MonoBehaviour
{
    public const string ServiceKey = "NarrativeLSTMPrompt";

    [Header("Prompt & summarizer (assign from scene or Create LSTM Rig in editor)")]
    [Tooltip("NarrativeLSTMPromptInterpreter: runs ONNX to turn natural language into events.")]
    public MonoBehaviour promptInterpreter;
    [Tooltip("NarrativeLSTMSummarizer: runs ONNX to summarize calendar into 'what's going on'.")]
    public MonoBehaviour summarizer;

    [Header("Optional")]
    [Tooltip("Calendar to summarize or to apply interpreted events to.")]
    public MonoBehaviour calendarAsset;

    /// <summary>Assign slots from SystemDrawerService if empty. Returns true if any assigned.</summary>
    public bool TryCompleteFromService()
    {
        var service = SystemDrawerService.Instance;
        if (service == null) return false;
        bool any = false;
        if (promptInterpreter == null)
        {
            var obj = service.Get<MonoBehaviour>(ServiceKey);
            if (obj != null) { promptInterpreter = obj; any = true; }
        }
        if (calendarAsset == null)
        {
            var cal = service.Get<MonoBehaviour>(CalendarServiceWizard.ServiceKey);
            if (cal != null) { calendarAsset = cal; any = true; }
        }
        return any;
    }

    private void OnEnable()
    {
        if (SystemDrawerService.Instance == null) return;
        if (promptInterpreter != null)
            SystemDrawerService.Instance.Register(ServiceKey, promptInterpreter);
    }

    private void OnDisable()
    {
        if (SystemDrawerService.Instance != null)
            SystemDrawerService.Instance.Unregister(ServiceKey);
    }
}
