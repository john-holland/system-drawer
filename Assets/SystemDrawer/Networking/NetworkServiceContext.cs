/// <summary>Per-scene networking context resolved from orchestrators.</summary>
public sealed class NetworkServiceContext
{
    public NetworkServerMode Mode { get; set; } = NetworkServerMode.SinglePlayer;
    public string ClientId { get; set; } = "";
    public ClientOrchestrator Client { get; set; }
    public ServerOrchestrator Server { get; set; }
    public NetworkTreeRegistry TreeRegistry { get; } = new NetworkTreeRegistry();

    public bool IsServer => Server != null;
    public bool IsDedicated => Server != null && Server.IsDedicated;
}
