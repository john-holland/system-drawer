using UnityEngine;

/// <summary>
/// Service wizard component for Narrative Calendar. Add to any GameObject; use slots and buttons to open the calendar wizard or create examples.
/// Registers with SystemDrawerService when present. Use TryCompleteFromService to fill slot from the conglomerator.
/// </summary>
public class CalendarServiceWizard : MonoBehaviour
{
    public const string ServiceKey = "NarrativeCalendar";

    [Tooltip("Calendar asset this wizard configures.")]
    public MonoBehaviour calendarAsset;

    /// <summary>Assign slot from SystemDrawerService if empty. Returns true if assigned.</summary>
    public bool TryCompleteFromService()
    {
        var service = SystemDrawerService.Instance;
        if (service == null) return false;
        var obj = service.Get<Object>(ServiceKey);
        if (obj is MonoBehaviour mb)
        {
            calendarAsset = mb;
            return true;
        }
        return false;
    }

    private void OnEnable()
    {
        if (calendarAsset != null && SystemDrawerService.Instance != null)
            SystemDrawerService.Instance.Register(ServiceKey, calendarAsset);
    }

    private void OnDisable()
    {
        if (SystemDrawerService.Instance != null)
            SystemDrawerService.Instance.Unregister(ServiceKey);
    }
}
