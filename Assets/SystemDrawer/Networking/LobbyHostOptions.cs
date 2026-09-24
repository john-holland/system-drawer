using System;
using System.Collections.Generic;
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

[Serializable]
public sealed class GameSessionIndexEntry
{
    public int index;
    public string netId;
    public int instanceId;
    public string treeId;
    public string prefabKey;
    public string parentPath;
    public float createdNarrativeTime;
    public bool isBehaviorTree;
}

[Serializable]
public sealed class GameSessionIndex
{
    public List<GameSessionIndexEntry> entries = new List<GameSessionIndexEntry>();

    public GameSessionIndexEntry Add(
        GameObject go,
        string treeId,
        string prefabKey,
        float narrativeTime,
        bool isBt)
    {
        if (entries == null) entries = new List<GameSessionIndexEntry>();
        var e = new GameSessionIndexEntry
        {
            index = entries.Count,
            instanceId = go != null ? go.GetInstanceID() : 0,
            netId = go != null ? go.name : "",
            treeId = treeId ?? "",
            prefabKey = prefabKey ?? "",
            parentPath = go != null && go.transform.parent != null ? go.transform.parent.name : "",
            createdNarrativeTime = narrativeTime,
            isBehaviorTree = isBt
        };
        entries.Add(e);
        return e;
    }
}

[Serializable]
public sealed class GameSession
{
    public string id;
    public string displayName = "Session";
    public string lobbySessionName = "Drawer 2";
    public string createdUtc;
    public float createdNarrativeTime;
    public bool active;
    public string parentId;
    public int peckingOrder;
    public LobbyPrefabParameters prefab = new LobbyPrefabParameters();
    public GameSessionIndex index = new GameSessionIndex();
    public List<int> spawnedInstanceIds = new List<int>();
    public List<string> treeIds = new List<string>();
    public List<GameSessionPlayer> players = new List<GameSessionPlayer>();

    public static GameSession Create(string lobbySessionName, float narrativeTime, string displayName = null)
    {
        return new GameSession
        {
            id = Guid.NewGuid().ToString("N"),
            displayName = string.IsNullOrEmpty(displayName) ? "Session" : displayName,
            lobbySessionName = lobbySessionName ?? "",
            createdUtc = DateTime.UtcNow.ToString("o"),
            createdNarrativeTime = narrativeTime,
            active = false,
            parentId = "",
            peckingOrder = 0,
            prefab = new LobbyPrefabParameters(),
            index = new GameSessionIndex()
        };
    }
}

[Serializable]
public sealed class GameSessionPlayer
{
    public string playerId;
    public string displayName;
    public string actorId;
}

/// <summary>AssetDB-host stub until GameSessionHost.cs is reimported.</summary>
[DisallowMultipleComponent]
public sealed class GameSessionHost : MonoBehaviour
{
    public string lobbySessionName = "Drawer 2";
    public List<GameSession> sessions = new List<GameSession>();
    public int activeIndex = -1;
    public NetworkTreeRegistry treeRegistry;
    public LockstepDecisionValidator lockstep;
    public LobbyPrefabParameters prefab = new LobbyPrefabParameters();
    public event Action SessionsChanged;

    public GameSession Active =>
        sessions != null && activeIndex >= 0 && activeIndex < sessions.Count
            ? sessions[activeIndex]
            : null;

    public string ActiveId => Active != null ? Active.id : "";

    public GameSession CreateSession(string displayName = null)
    {
        if (sessions == null) sessions = new List<GameSession>();
        var session = GameSession.Create(lobbySessionName, 0f, displayName);
        if (prefab != null)
            session.prefab = prefab.Clone();
        sessions.Add(session);
        activeIndex = sessions.Count - 1;
        if (session != null) session.active = true;
        SessionsChanged?.Invoke();
        return session;
    }

    public bool SwitchActiveById(string id)
    {
        if (sessions == null || string.IsNullOrEmpty(id)) return false;
        for (int i = 0; i < sessions.Count; i++)
        {
            if (sessions[i] != null && sessions[i].id == id)
            {
                activeIndex = i;
                SessionsChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    public bool CloseSession(string id) => CloseSession(id, GameSessionCloseMode.AdoptToHigher);

    public bool CloseSession(string id, GameSessionCloseMode mode)
    {
        if (sessions == null || string.IsNullOrEmpty(id)) return false;
        int idx = -1;
        for (int i = 0; i < sessions.Count; i++)
            if (sessions[i] != null && sessions[i].id == id) { idx = i; break; }
        if (idx < 0) return false;
        sessions.RemoveAt(idx);
        if (activeIndex >= sessions.Count) activeIndex = sessions.Count - 1;
        _ = mode;
        SessionsChanged?.Invoke();
        return true;
    }

    public void SaveToLocalClient(string id = null) { _ = id; }
}



[System.Serializable]
public sealed class ChatComposeDeltaPayload
{
    public string treeId;
    public string[] tokens;
    public string text;
    public bool committed;
}

public sealed class StructuredChatRagdoll : MenuRagdollBase
{
    public ChatComposeDeltaPayload LastStreamed;
    public void OnRemoteCommitted(string text, string[] tokens, string clientId) { _ = text; _ = tokens; _ = clientId; }
}

public static class GameLobbyContinuuuumClient
{
    public static System.Func<string, string, string, string> TransportOverride;

    public static string Heartbeat(ServerOrchestrator server) { _ = server; return ""; }
    public static System.Collections.IEnumerator HeartbeatRoutine(ServerOrchestrator server)
    {
        _ = server;
        yield break;
    }
    public static string CloseLobby(string name) { _ = name; return ""; }
    public static string PutPrefab(string name, LobbyPrefabParameters prefab) { _ = name; _ = prefab; return ""; }
    public static string GetLobby(string name) { _ = name; return ""; }
    public static string GetConfig(string configId) { _ = configId; return ""; }
    public static bool TryApplyConfigJson(string json, NetworkSettings settings, ServerOrchestrator server) => false;
    public static bool TryApplyLobbyJson(string json, NetworkSettings settings, ServerOrchestrator server) => false;
    public static object BuildHeartbeat(ServerOrchestrator server) { _ = server; return null; }
}

/// <summary>Host-side lobby configuration passed to ServerOrchestrator.</summary>
public sealed class LobbyHostOptions
{
    public string sessionName;
    public int maxPlayers = 8;
    public int maxSpectators = 4;
    public bool allowSpectators = true;
    public string password;
    public int lobbyPort;
    public int minPlayersToStart = 1;
    public LobbyPrefabParameters prefab = new LobbyPrefabParameters();
}
