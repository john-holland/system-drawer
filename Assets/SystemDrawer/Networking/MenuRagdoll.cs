using UnityEngine;

/// <summary>Concrete main-menu MenuRagdoll root with lobby host/join fields.</summary>
[AddComponentMenu("System Drawer/Networking/Menu Ragdoll")]
public sealed class MenuRagdoll : MenuRagdollBase
{
    [Header("Lobby Host")]
    public string sessionName;
    public string hostLobbyPassword;
    public bool requireLobbyPassword;
    public bool allowSpectators = true;
    public int maxPlayers = 8;
    public int maxSpectators = 4;
    public string defaultHostAddress = "127.0.0.1";
    public int defaultLobbyPort;

    [Header("Lobby Join")]
    public string joinHostPort;
    public string joinLobbyPassword;

    public LobbyHostOptions BuildHostOptions()
    {
        var settings = NetworkSettings.Default;
        return new LobbyHostOptions
        {
            sessionName = string.IsNullOrEmpty(sessionName) ? settings.lobbySessionName : sessionName,
            maxPlayers = maxPlayers,
            maxSpectators = maxSpectators,
            allowSpectators = allowSpectators,
            password = requireLobbyPassword ? hostLobbyPassword : "",
            lobbyPort = defaultLobbyPort > 0 ? defaultLobbyPort : settings.lobbyPort
        };
    }

    public LobbyJoinPayload BuildJoinPayload(NetworkClientRole role, string payloadHostPort = null)
    {
        return new LobbyJoinPayload
        {
            hostPort = string.IsNullOrEmpty(payloadHostPort) ? joinHostPort : payloadHostPort,
            password = joinLobbyPassword,
            role = role
        };
    }
}
