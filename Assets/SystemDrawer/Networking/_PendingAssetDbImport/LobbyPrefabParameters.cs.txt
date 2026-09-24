using System;
using UnityEngine;

public enum LobbyContentKind
{
    GameMode = 0,
    Expansion = 1,
    Mod = 2
}

/// <summary>Prefab config for a lobby: caps, mode, content binding, and freeform properties JSON.</summary>
[Serializable]
public sealed class LobbyPrefabParameters
{
    public int gameSize = 8;
    public int minPlayersToStart = 1;
    public NetworkServerMode mode = NetworkServerMode.SinglePlayer;
    public bool requirePassword;
    public bool allowSpectators = true;
    public int maxSpectators = 4;
    public string lobbyTypeId = "";
    public LobbyContentKind contentKind = LobbyContentKind.GameMode;
    public string contentId = "";
    public string propertiesJson = "{}";
    public string configId = "";
    public string configName = "";

    public static string ContentKindToApi(LobbyContentKind kind)
    {
        switch (kind)
        {
            case LobbyContentKind.Expansion: return "expansion";
            case LobbyContentKind.Mod: return "mod";
            default: return "game_mode";
        }
    }

    public static LobbyContentKind ContentKindFromApi(string value)
    {
        if (string.Equals(value, "expansion", StringComparison.OrdinalIgnoreCase))
            return LobbyContentKind.Expansion;
        if (string.Equals(value, "mod", StringComparison.OrdinalIgnoreCase))
            return LobbyContentKind.Mod;
        return LobbyContentKind.GameMode;
    }

    public bool TryValidateProperties(out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(propertiesJson))
        {
            propertiesJson = "{}";
            return true;
        }
        var trimmed = propertiesJson.Trim();
        if (trimmed.Length < 2 || trimmed[0] != '{' || trimmed[trimmed.Length - 1] != '}')
        {
            error = "propertiesJson must be object JSON";
            return false;
        }
        try
        {
            JsonUtility.FromJson<LobbyPrefabPropertiesDummy>(trimmed);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public LobbyPrefabParameters Clone()
    {
        return new LobbyPrefabParameters
        {
            gameSize = gameSize,
            minPlayersToStart = minPlayersToStart,
            mode = mode,
            requirePassword = requirePassword,
            allowSpectators = allowSpectators,
            maxSpectators = maxSpectators,
            lobbyTypeId = lobbyTypeId ?? "",
            contentKind = contentKind,
            contentId = contentId ?? "",
            propertiesJson = string.IsNullOrEmpty(propertiesJson) ? "{}" : propertiesJson,
            configId = configId ?? "",
            configName = configName ?? ""
        };
    }
}

[Serializable]
sealed class LobbyPrefabPropertiesDummy
{
}

/// <summary>Binds a MenuRagdoll or node to one lobby type (mode / expansion / mod).</summary>
[Serializable]
public sealed class LobbyTypeBinding
{
    public bool hasBinding;
    public string lobbyTypeId = "";
    public LobbyContentKind contentKind = LobbyContentKind.GameMode;
    public string contentId = "";

    public bool Matches(LobbyPrefabParameters active)
    {
        if (!hasBinding)
            return true;
        if (active == null)
            return false;
        if (!string.IsNullOrEmpty(lobbyTypeId) && lobbyTypeId != (active.lobbyTypeId ?? ""))
            return false;
        if (contentKind != active.contentKind)
            return false;
        if (!string.IsNullOrEmpty(contentId) && contentId != (active.contentId ?? ""))
            return false;
        return true;
    }
}

public enum GameSessionCloseMode
{
    AdoptToHigher = 0,
    Umbrella = 1
}
