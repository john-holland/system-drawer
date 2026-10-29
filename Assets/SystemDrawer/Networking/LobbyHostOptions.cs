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

    public void BindVoteNodes() { }

    public GameSession CreateSession(string displayName = null)
    {
        if (sessions == null) sessions = new List<GameSession>();
        var session = GameSession.Create(lobbySessionName, 0f, displayName);
        var parent = Active;
        session.parentId = parent != null ? parent.id : "";
        session.peckingOrder = NextSiblingPecking(session.parentId);
        if (prefab != null)
            session.prefab = prefab.Clone();
        sessions.Add(session);
        SetDormant(parent, true);
        activeIndex = sessions.Count - 1;
        session.active = true;
        if (lockstep != null) lockstep.ActiveGameSessionId = session.id;
        SessionsChanged?.Invoke();
        return session;
    }

    int NextSiblingPecking(string parentId)
    {
        int max = -1;
        for (int i = 0; i < sessions.Count; i++)
        {
            var s = sessions[i];
            if (s != null && (s.parentId ?? "") == (parentId ?? "") && s.peckingOrder > max)
                max = s.peckingOrder;
        }
        return max + 1;
    }

    int IndexOfSession(string id)
    {
        if (sessions == null || string.IsNullOrEmpty(id)) return -1;
        for (int i = 0; i < sessions.Count; i++)
            if (sessions[i] != null && sessions[i].id == id)
                return i;
        return -1;
    }

    void CollectDescendants(string id, List<string> ids)
    {
        ids.Add(id);
        for (int i = 0; i < sessions.Count; i++)
        {
            var s = sessions[i];
            if (s != null && s.parentId == id && !ids.Contains(s.id))
                CollectDescendants(s.id, ids);
        }
    }

    readonly Dictionary<int, GameObject> _alive = new Dictionary<int, GameObject>();
    readonly Dictionary<string, List<int>> _sessionSpawns = new Dictionary<string, List<int>>();

    public GameSessionIndexEntry TrackSpawn(
        GameObject go,
        string treeId = null,
        string prefabKey = null,
        bool isBt = false)
    {
        var session = Active;
        if (session == null || go == null) return null;
        int id = go.GetInstanceID();
        _alive[id] = go;
        if (!_sessionSpawns.TryGetValue(session.id, out var list))
        {
            list = new List<int>();
            _sessionSpawns[session.id] = list;
        }
        list.Add(id);
        session.spawnedInstanceIds.Add(id);
        if (!string.IsNullOrEmpty(treeId))
            session.treeIds.Add(treeId);
        return session.index.Add(go, treeId, prefabKey, session.createdNarrativeTime, isBt);
    }

    void SetDormant(GameSession session, bool dormant)
    {
        if (session == null) return;
        session.active = !dormant;
        if (_sessionSpawns.TryGetValue(session.id, out var ids))
        {
            for (int i = 0; i < ids.Count; i++)
                if (_alive.TryGetValue(ids[i], out var go) && go != null)
                    go.SetActive(!dormant);
        }
        if (treeRegistry == null || session.treeIds == null) return;
        for (int i = 0; i < session.treeIds.Count; i++)
        {
            if (!treeRegistry.TryGet(session.treeIds[i], out var d) || d == null) continue;
            d.TransmitPolicy = dormant ? TreeTransmitPolicy.LocalOnly : TreeTransmitPolicy.PeerTransferable;
            d.GameSessionId = session.id;
            treeRegistry.Register(d);
        }
    }

    void CleanupForSessionClose(GameSession session)
    {
        if (session == null) return;
        if (_sessionSpawns.TryGetValue(session.id, out var spawnIds))
        {
            for (int i = 0; i < spawnIds.Count; i++)
            {
                if (_alive.TryGetValue(spawnIds[i], out var go) && go != null)
                {
                    if (Application.isPlaying) Destroy(go);
                    else DestroyImmediate(go);
                }
                _alive.Remove(spawnIds[i]);
            }
            _sessionSpawns.Remove(session.id);
        }
        if (treeRegistry != null && session.treeIds != null)
        {
            for (int i = 0; i < session.treeIds.Count; i++)
                treeRegistry.Remove(session.treeIds[i]);
        }
        session.active = false;
        session.spawnedInstanceIds?.Clear();
        session.treeIds?.Clear();
        session.index?.entries?.Clear();
    }

    public bool SwitchActiveById(string id)
    {
        int index = IndexOfSession(id);
        if (index < 0) return false;
        if (index != activeIndex)
        {
            SetDormant(Active, true);
            activeIndex = index;
        }
        SetDormant(Active, false);
        if (lockstep != null) lockstep.ActiveGameSessionId = ActiveId;
        SessionsChanged?.Invoke();
        return true;
    }

    public void SwitchActive(int index)
    {
        if (sessions == null || index < 0 || index >= sessions.Count) return;
        SwitchActiveById(sessions[index] != null ? sessions[index].id : null);
    }

    public void SaveAllToLocalClient()
    {
        if (sessions == null) return;
        for (int i = 0; i < sessions.Count; i++)
            if (sessions[i] != null)
                GameSessionLocalSave.Save(sessions[i], prefab);
    }

    public bool CloseSession(string id) => CloseSession(id, GameSessionCloseMode.AdoptToHigher);

    public bool CloseSession(string id, GameSessionCloseMode mode)
    {
        var closed = FindSession(id);
        if (closed == null) return false;

        string survivingActiveId = ActiveId;
        string closedParentId = closed.parentId ?? "";

        if (mode == GameSessionCloseMode.Umbrella)
        {
            var ids = new List<string>();
            CollectDescendants(id, ids);
            if (ids.Contains(survivingActiveId))
                survivingActiveId = closedParentId;
            for (int i = ids.Count - 1; i >= 0; i--)
                CleanupForSessionClose(FindSession(ids[i]));
            sessions.RemoveAll(s => s != null && ids.Contains(s.id));
        }
        else
        {
            if (survivingActiveId == id)
                survivingActiveId = closedParentId;
            for (int i = 0; i < sessions.Count; i++)
            {
                var s = sessions[i];
                if (s != null && s.parentId == closed.id)
                    s.parentId = closedParentId;
            }
            CleanupForSessionClose(closed);
            sessions.RemoveAll(s => s != null && s.id == id);
        }

        activeIndex = IndexOfSession(survivingActiveId);
        if (activeIndex < 0 && sessions.Count > 0)
            activeIndex = 0;
        SetDormant(Active, false);
        if (lockstep != null) lockstep.ActiveGameSessionId = ActiveId;
        SessionsChanged?.Invoke();
        return true;
    }

    public GameSession FindSession(string id)
    {
        if (sessions == null || string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < sessions.Count; i++)
            if (sessions[i] != null && sessions[i].id == id)
                return sessions[i];
        return null;
    }

    public void SaveToLocalClient(string id = null)
    {
        GameSession session = null;
        if (!string.IsNullOrEmpty(id))
            session = FindSession(id);
        if (session == null) session = Active;
        if (session == null) return;
        GameSessionLocalSave.Save(session, prefab);
    }
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
    public string productId;
    public string sessionId;
    public ChatLexiconWord[] LexiconWords = System.Array.Empty<ChatLexiconWord>();
    public bool autoCloseOnExit = true;
    public string LastDenyCode;
    public StructuredChatChannel Channel = new StructuredChatChannel();
    public bool IsOpen { get; private set; }
    public bool SentFlashVisible { get; private set; }
    public StructuredChatComposer Composer { get; } = new StructuredChatComposer();
    public ChatComposeDeltaPayload LastStreamed;

    public void SetOpen(bool open) => IsOpen = open;

    public void ApplyLexicon(ChatLexiconWord[] words, string composeMode)
    {
        LexiconWords = words ?? System.Array.Empty<ChatLexiconWord>();
        Composer.ComposeMode = string.IsNullOrEmpty(composeMode) ? "preview" : composeMode;
        Composer.SetAllowedWords(LexiconWords);
    }

    public bool AppendWord(string word)
    {
        if (!Composer.TryAppend(word, out string deny))
        {
            LastDenyCode = deny;
            return false;
        }
        if (Composer.StreamOnAppend)
            LastStreamed = Composer.BuildDelta(false);
        return true;
    }

    public bool Commit()
    {
        if (Composer.Tokens.Count == 0) return false;
        LastStreamed = Composer.BuildDelta(true);
        Composer.ClearAfterSend();
        SentFlashVisible = true;
        return true;
    }

    public override bool HandleBubble(MenuRagdollEvent e)
    {
        if (e.Name == "chat.word")
        {
            AppendWord(e.Payload as string);
            Composer.ClearAfterSend();
            return true;
        }
        return base.HandleBubble(e);
    }

    public void OnRemoteCommitted(string text, string[] tokens, string clientId) { _ = text; _ = tokens; _ = clientId; }
}


public static class GameSessionLocalSave
{
    public static string RootOverride;

    public static string RootDir()
    {
        if (!string.IsNullOrEmpty(RootOverride))
            return RootOverride;
        return System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "game-sessions");
    }

    public static string SessionPath(string lobbySessionName, string gameSessionId, string playerId = null)
    {
        string lobby = Sanitize(lobbySessionName ?? "local");
        string id = Sanitize(gameSessionId ?? "session");
        string file = string.IsNullOrEmpty(playerId) ? id + ".json" : id + "." + Sanitize(playerId) + ".json";
        return System.IO.Path.Combine(RootDir(), lobby, file);
    }

    public static void Save(GameSession session, LobbyPrefabParameters prefab = null, string playerId = null)
    {
        if (session == null) return;
        if (prefab != null)
            session.prefab = prefab.Clone();
        string path = SessionPath(session.lobbySessionName, session.id, playerId);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) ?? RootDir());
        System.IO.File.WriteAllText(path, UnityEngine.JsonUtility.ToJson(session, true));
    }

    public static GameSession Load(string lobbySessionName, string gameSessionId)
    {
        string path = SessionPath(lobbySessionName, gameSessionId);
        if (!System.IO.File.Exists(path)) return null;
        return UnityEngine.JsonUtility.FromJson<GameSession>(System.IO.File.ReadAllText(path));
    }

    static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "default";
        foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s;
    }
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
    public static GameLobbyHeartbeatDto BuildHeartbeat(ServerOrchestrator server)
    {
        var host = server != null ? server.GameSessions : null;
        var sessions = new System.Collections.Generic.List<GameLobbyHeartbeatSession>();
        if (host != null && host.sessions != null)
        {
            for (int i = 0; i < host.sessions.Count; i++)
            {
                var s = host.sessions[i];
                if (s == null) continue;
                sessions.Add(new GameLobbyHeartbeatSession { id = s.id, peckingOrder = s.peckingOrder });
            }
        }
        string name = server != null && server.Settings != null ? server.Settings.lobbySessionName : "";
        return new GameLobbyHeartbeatDto { name = name, sessions = sessions.ToArray() };
    }
}

public sealed class GameLobbyHeartbeatSession
{
    public string id;
    public int peckingOrder;
}

public sealed class GameLobbyHeartbeatDto
{
    public string name;
    public GameLobbyHeartbeatSession[] sessions = System.Array.Empty<GameLobbyHeartbeatSession>();
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
