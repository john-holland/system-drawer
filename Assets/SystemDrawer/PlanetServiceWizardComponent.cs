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

    [Tooltip("Optional asteroid belt host spawned around the planet.")]
    public GameObject asteroidBeltHost;

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

        if (asteroidBeltHost != null)
            SystemDrawerService.Instance.Register("planet.asteroidBelt", asteroidBeltHost);
    }

    void UnregisterAll()
    {
        if (SystemDrawerService.Instance == null)
            return;
        SystemDrawerService.Instance.Unregister(ServiceKey);
        SystemDrawerService.Instance.Unregister(SystemDrawerServiceKeys.PlanetBody);
        SystemDrawerService.Instance.Unregister(SystemDrawerServiceKeys.PlanetShellGrid);
        SystemDrawerService.Instance.Unregister(SystemDrawerServiceKeys.PhysicalManifold);
        SystemDrawerService.Instance.Unregister("planet.asteroidBelt");
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

    /// <summary>Spawns AsteroidBeltHost sibling; uses reflection to avoid Planetary.Editor dependency.</summary>
    public GameObject SpawnAsteroidBeltAroundPlanet()
    {
        if (planetSystemObject == null)
            return null;
        foreach (MonoBehaviour mb in planetSystemObject.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null && mb.GetType().Name == "PlanetBody")
            {
                var hostType = System.Type.GetType("Planetary.AsteroidBelt.AsteroidBeltHost, Planetary");
                if (hostType == null)
                    return null;
                var go = new GameObject("AsteroidBeltHost");
                go.transform.SetParent(planetSystemObject.transform.parent);
                var host = go.AddComponent(hostType);
                var planetField = hostType.GetField("parentPlanet");
                planetField?.SetValue(host, mb);
                var ensure = hostType.GetMethod("EnsureComponents");
                ensure?.Invoke(host, null);
                asteroidBeltHost = go;
                RegisterAll();
                return go;
            }
        }
        return null;
    }
}
