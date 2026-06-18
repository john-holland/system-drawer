using UnityEngine;

/// <summary>Runtime networking defaults (ports, LOD radii, lobby).</summary>
[CreateAssetMenu(menuName = "System Drawer/Network Settings", fileName = "NetworkSettings")]
public sealed class NetworkSettings : ScriptableObject
{
    static NetworkSettings _default;

    [Header("Ports")]
    public int gamePort = 7777;
    public int lobbyPort = 7780;
    public string bindAddress = "0.0.0.0";
    public string lobbySessionName = "Drawer 2";

    [Header("Lobby")]
    public int maxPlayers = 8;
    public int maxSpectators = 4;
    public bool allowSpectators = true;
    public bool allowLobbyPassword = true;

    [Header("LOD streaming (world units)")]
    public float clientLodRadius = 50f;
    public float serverLodRadius = 80f;

    [Header("Capabilities")]
    public bool hasMultiplayer = true;
    public bool allowSaveLoadInMultiplayer = false;

    public static NetworkSettings Default
    {
        get
        {
            if (_default == null)
            {
                _default = CreateInstance<NetworkSettings>();
                _default.hideFlags = HideFlags.HideAndDontSave;
            }
            return _default;
        }
    }
}
