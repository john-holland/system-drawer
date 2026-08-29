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
    public int minPlayersToStart = 1;
    public string defaultHostAddress = "127.0.0.1";
    public int defaultLobbyPort;
    public LobbyTypeBinding lobbyTypeBinding = new LobbyTypeBinding();
    public LobbyPrefabParameters prefab = new LobbyPrefabParameters();

    [Header("Lobby Join")]
    public string joinHostPort;
    public string joinLobbyPassword;

    public LobbyHostOptions BuildHostOptions()
    {
        var settings = NetworkSettings.Default;
        var p = prefab ?? new LobbyPrefabParameters();
        p.gameSize = maxPlayers;
        p.minPlayersToStart = minPlayersToStart;
        p.requirePassword = requireLobbyPassword;
        p.allowSpectators = allowSpectators;
        p.maxSpectators = maxSpectators;
        if (lobbyTypeBinding != null && lobbyTypeBinding.hasBinding)
        {
            p.lobbyTypeId = lobbyTypeBinding.lobbyTypeId;
            p.contentKind = lobbyTypeBinding.contentKind;
            p.contentId = lobbyTypeBinding.contentId;
        }
        return new LobbyHostOptions
        {
            sessionName = string.IsNullOrEmpty(sessionName) ? settings.lobbySessionName : sessionName,
            maxPlayers = maxPlayers,
            maxSpectators = maxSpectators,
            allowSpectators = allowSpectators,
            password = requireLobbyPassword ? hostLobbyPassword : "",
            lobbyPort = defaultLobbyPort > 0 ? defaultLobbyPort : settings.lobbyPort,
            minPlayersToStart = minPlayersToStart,
            prefab = p
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
