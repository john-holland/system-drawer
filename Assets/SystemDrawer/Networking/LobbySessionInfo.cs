/// <summary>Parsed lobby QUERY response.</summary>
public sealed class LobbySessionInfo
{
    public bool Ok;
    public string Error;
    public string sessionName;
    public int gamePort;
    public int playerCount;
    public int maxPlayers;
    public int spectatorCount;
    public int maxSpectators;
    public bool allowSpectators;
    public bool passwordRequired;
}
