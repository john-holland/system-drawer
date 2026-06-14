using UnityEngine;

/// <summary>
/// Service wizard component for Weather. Add to any GameObject; use slots and buttons to open the weather wizard or create examples.
/// Registers with SystemDrawerService when present. Use TryCompleteFromService to fill slot from the conglomerator.
/// </summary>
public class WeatherServiceWizardComponent : MonoBehaviour
{
    public const string ServiceKey = "WeatherSystem";

    [Tooltip("Weather system GameObject this wizard configures.")]
    public GameObject weatherSystemObject;

    /// <summary>Assign slot from SystemDrawerService if empty. Returns true if assigned.</summary>
    public bool TryCompleteFromService()
    {
        var service = SystemDrawerService.Instance;
        if (service == null) return false;
        var go = service.Get<GameObject>(ServiceKey);
        if (go != null)
        {
            weatherSystemObject = go;
            return true;
        }
        return false;
    }

    private void OnEnable()
    {
        if (weatherSystemObject != null && SystemDrawerService.Instance != null)
        {
            SystemDrawerService.Instance.Register(ServiceKey, weatherSystemObject);
            SystemDrawerService.Instance.Register(SystemDrawerServiceKeys.WeatherSystemLegacy, weatherSystemObject);
            SystemDrawerService.Instance.Register(SystemDrawerServiceKeys.WeatherSystem, weatherSystemObject);
            RegisterWeatherManifold();
        }
    }

    private void OnDisable()
    {
        if (SystemDrawerService.Instance != null)
        {
            SystemDrawerService.Instance.Unregister(ServiceKey);
            SystemDrawerService.Instance.Unregister(SystemDrawerServiceKeys.WeatherSystemLegacy);
            SystemDrawerService.Instance.Unregister(SystemDrawerServiceKeys.WeatherSystem);
            SystemDrawerService.Instance.Unregister(SystemDrawerServiceKeys.WeatherPhysicsManifold);
        }
    }

    void RegisterWeatherManifold()
    {
        if (weatherSystemObject == null || SystemDrawerService.Instance == null)
            return;
        foreach (MonoBehaviour mb in weatherSystemObject.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null && mb.GetType().Name == "WeatherPhysicsManifold")
            {
                SystemDrawerService.Instance.Register(SystemDrawerServiceKeys.WeatherPhysicsManifold, mb);
                break;
            }
        }
    }
}
