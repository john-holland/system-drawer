/// <summary>
/// Canonical keys for <see cref="SystemDrawerService"/> and <see cref="SystemDrawerSceneServices"/>.
/// </summary>
public static class SystemDrawerServiceKeys
{
    public const string WeatherExecutor = "weather.executor";
    public const string WeatherPhysicsManifold = "weather.physicsManifold";
    public const string WeatherSystem = "weather.system";
    public const string PlanetBody = "planet.body";
    public const string PlanetShellGrid = "planet.shellGrid";
    public const string PhysicalManifold = "planet.physicalManifold";
    public const string HierarchicalPathingSolver = "pathing.hierarchical";
    public const string SystemDrawerAnimator = "animation.systemDrawerAnimator";

    public const string NetworkClientOrchestrator = "network.clientOrchestrator";
    public const string NetworkServerOrchestrator = "network.serverOrchestrator";
    public const string NetworkServerMode = "network.serverMode";
    public const string NetworkLobbyServer = "network.lobbyServer";
    public const string MenuRagdoll = "menu.ragdoll";
    public const string NarrativeNodeExecStateStore = "narrative.nodeExecStateStore";

    /// <summary>Legacy wizard key; prefer <see cref="PlanetBody"/>.</summary>
    public const string PlanetSystemLegacy = "PlanetSystem";

    /// <summary>Legacy wizard key; prefer <see cref="WeatherSystem"/>.</summary>
    public const string WeatherSystemLegacy = "WeatherSystem";
}
