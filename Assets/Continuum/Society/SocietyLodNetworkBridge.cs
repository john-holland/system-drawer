using UnityEngine;

/// <summary>Registers society.* network trees (server-authoritative snapshots and routing).</summary>
[AddComponentMenu("Continuum/Society/Society LOD Network Bridge")]
public sealed class SocietyLodNetworkBridge : MonoBehaviour
{
    public ServerOrchestrator serverOrchestrator;
    public string planetId = "earth";
    public string cityId;

    void Awake()
    {
        if (serverOrchestrator == null)
            serverOrchestrator = FindAnyObjectByType<ServerOrchestrator>();
    }

    void OnEnable()
    {
        RegisterTrees();
    }

    void RegisterTrees()
    {
        if (serverOrchestrator == null || string.IsNullOrEmpty(cityId))
            return;

        serverOrchestrator.TreeRegistry.Register(new NetworkTreeDescriptor
        {
            TreeId = $"society.political.{cityId}",
            Dimension = TreeDimension.Spatial3D,
            TransmitPolicy = TreeTransmitPolicy.ServerAuthoritative,
            StreamForOwnership = true,
            CausalityLeafPrefix = "society"
        });
        serverOrchestrator.TreeRegistry.Register(new NetworkTreeDescriptor
        {
            TreeId = $"society.buildings.{cityId}",
            Dimension = TreeDimension.Spatial3D,
            TransmitPolicy = TreeTransmitPolicy.ServerAuthoritative,
            StreamForOwnership = true,
            CausalityLeafPrefix = "society.buildings"
        });
        serverOrchestrator.TreeRegistry.Register(new NetworkTreeDescriptor
        {
            TreeId = $"society.network.{cityId}",
            Dimension = TreeDimension.Spatial3D,
            TransmitPolicy = TreeTransmitPolicy.ServerAuthoritative,
            StreamForOwnership = false,
            CausalityLeafPrefix = "society.network"
        });
        if (!string.IsNullOrEmpty(planetId))
        {
            serverOrchestrator.TreeRegistry.Register(new NetworkTreeDescriptor
            {
                TreeId = $"society.planet.{planetId}",
                Dimension = TreeDimension.Spatial3D,
                TransmitPolicy = TreeTransmitPolicy.ServerAuthoritative,
                StreamForOwnership = true,
                CausalityLeafPrefix = "society.planet"
            });
        }
    }
}
