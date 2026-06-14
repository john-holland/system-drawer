using System.Reflection;
using UnityEngine;

/// <summary>
/// Loose planet service wizard slot for System Drawer facilitator (avoids hard Planetary.Editor dependency).
/// </summary>
public class PlanetServiceWizardComponent : MonoBehaviour
{
    public const string ServiceKey = SystemDrawerServiceKeys.PlanetSystemLegacy;

    [Tooltip("Planet host GameObject this wizard configures.")]
    public GameObject planetSystemObject;

    public bool TryCompleteFromService()
    {
        var service = SystemDrawerService.Instance;
        if (service == null)
            return false;
        var go = service.Get<GameObject>(ServiceKey);
        if (go == null)
            go = service.Get<GameObject>(SystemDrawerServiceKeys.PlanetBody);
        if (go != null)
        {
            planetSystemObject = go;
            return true;
        }

        return false;
    }

    void OnEnable() => RegisterAll();

    void OnDisable() => UnregisterAll();

    public void RegisterAll()
    {
        if (planetSystemObject == null || SystemDrawerService.Instance == null)
            return;

        SystemDrawerService.Instance.Register(ServiceKey, planetSystemObject);
        SystemDrawerService.Instance.Register(SystemDrawerServiceKeys.PlanetBody, planetSystemObject);

        Component planetBody = FindPlanetBody(planetSystemObject);
        if (planetBody != null)
            SystemDrawerService.Instance.Register(SystemDrawerServiceKeys.PlanetBody, planetBody);

        Component shellGrid = FindShellGrid(planetSystemObject);
        if (shellGrid != null)
            SystemDrawerService.Instance.Register(SystemDrawerServiceKeys.PlanetShellGrid, shellGrid);

        Component physicalManifold = FindPhysicalManifold(planetSystemObject);
        if (physicalManifold != null)
            SystemDrawerService.Instance.Register(SystemDrawerServiceKeys.PhysicalManifold, physicalManifold);
    }

    void UnregisterAll()
    {
        if (SystemDrawerService.Instance == null)
            return;
        SystemDrawerService.Instance.Unregister(ServiceKey);
        SystemDrawerService.Instance.Unregister(SystemDrawerServiceKeys.PlanetBody);
        SystemDrawerService.Instance.Unregister(SystemDrawerServiceKeys.PlanetShellGrid);
        SystemDrawerService.Instance.Unregister(SystemDrawerServiceKeys.PhysicalManifold);
    }

    static Component FindPlanetBody(GameObject root)
    {
        foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null && mb.GetType().Name == "PlanetBody")
                return mb;
        }
        return null;
    }

    static Component FindShellGrid(GameObject root)
    {
        foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null && mb.GetType().Name == "PlanetShellManifoldGrid")
                return mb;
        }
        return null;
    }

    static Component FindPhysicalManifold(GameObject root)
    {
        foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null && mb.GetType().Name == "PhysicalManifoldRelativitySolver")
                return mb;
        }
        return null;
    }
}
