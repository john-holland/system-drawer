/// <summary>Host-side lobby configuration passed to ServerOrchestrator.</summary>
public sealed class LobbyHostOptions
{
    public string sessionName;
    public int maxPlayers = 8;
    public int maxSpectators = 4;
    public bool allowSpectators = true;
    public string password;
    public int lobbyPort;
}
