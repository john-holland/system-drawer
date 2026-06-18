/// <summary>Client join/spectate payload for MenuRagdoll events.</summary>
public sealed class LobbyJoinPayload
{
    public string hostPort;
    public string password;
    public NetworkClientRole role = NetworkClientRole.Player;
}
