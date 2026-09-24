using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Pushes lobby heartbeats and prefab params to Continuuuum; loads named ballots.</summary>
public static class GameLobbyContinuuuumClient
{
    /// <summary>Tests replace HTTP: (method, path, jsonBody) → response text.</summary>
    public static Func<string, string, string, string> TransportOverride;

    public static string BaseUrl => ContinuuuumApiConfig.GetApiBaseUrl().TrimEnd('/');

    public static string Heartbeat(ServerOrchestrator server)
    {
        if (server == null) return "";
        var body = BuildHeartbeat(server);
        return SendNow("POST", "/api/game-lobbies", JsonUtility.ToJson(body));
    }

    public static string CloseLobby(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        return SendNow("POST", "/api/game-lobbies/" + UnityWebRequest.EscapeURL(name) + "/close", "{}");
    }

    public static string PutPrefab(string name, LobbyPrefabParameters prefab)
    {
        if (string.IsNullOrEmpty(name) || prefab == null) return "";
        var dto = new LobbyPrefabPutDto
        {
            gameSize = prefab.gameSize,
            minPlayersToStart = prefab.minPlayersToStart,
            mode = prefab.mode.ToString(),
            requirePassword = prefab.requirePassword,
            allowSpectators = prefab.allowSpectators,
            maxSpectators = prefab.maxSpectators,
            lobbyTypeId = prefab.lobbyTypeId ?? "",
            contentKind = LobbyPrefabParameters.ContentKindToApi(prefab.contentKind),
            contentId = prefab.contentId ?? "",
            propertiesJson = prefab.propertiesJson ?? "{}",
            configId = prefab.configId ?? "",
            configName = prefab.configName ?? ""
        };
        return SendNow("PUT", "/api/game-lobbies/" + UnityWebRequest.EscapeURL(name) + "/prefab", JsonUtility.ToJson(dto));
    }

    public static string GetLobby(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        return SendNow("GET", "/api/game-lobbies/" + UnityWebRequest.EscapeURL(name), null);
    }

    public static string GetConfig(string configId)
    {
        if (string.IsNullOrEmpty(configId)) return "";
        return SendNow("GET", "/api/game-lobby-configs/" + UnityWebRequest.EscapeURL(configId), null);
    }

    public static bool TryApplyConfigJson(string json, NetworkSettings settings, ServerOrchestrator server)
    {
        if (string.IsNullOrEmpty(json) || settings == null)
            return false;
        var dto = JsonUtility.FromJson<LobbyConfigGetDto>(json);
        if (dto == null || string.IsNullOrEmpty(dto.id))
            return false;
        if (settings.prefab == null) settings.prefab = new LobbyPrefabParameters();
        ApplyPrefabDto(dto.gameSize, dto.maxPlayers, dto.minPlayersToStart, dto.mode, dto.requirePassword,
            dto.allowSpectators, dto.maxSpectators, dto.lobbyTypeId, dto.contentKind, dto.contentId,
            dto.propertiesJson, dto.id, dto.name, settings, server, keepInstanceName: true);
        return true;
    }

    public static bool TryApplyLobbyJson(string json, NetworkSettings settings, ServerOrchestrator server)
    {
        if (string.IsNullOrEmpty(json) || settings == null)
            return false;
        var dto = JsonUtility.FromJson<LobbyPrefabGetDto>(json);
        if (dto == null || string.IsNullOrEmpty(dto.name))
            return false;
        settings.lobbySessionName = dto.name;
        if (settings.prefab == null) settings.prefab = new LobbyPrefabParameters();
        ApplyPrefabDto(dto.gameSize, dto.maxPlayers, dto.minPlayersToStart, dto.mode, dto.requirePassword,
            dto.allowSpectators, dto.maxSpectators, dto.lobbyTypeId, dto.contentKind, dto.contentId,
            dto.propertiesJson, dto.configId, dto.configName, settings, server, keepInstanceName: false);
        return true;
    }

    static void ApplyPrefabDto(
        int gameSize, int maxPlayers, int minPlayersToStart, string mode, bool requirePassword,
        bool allowSpectators, int maxSpectators, string lobbyTypeId, string contentKind, string contentId,
        string propertiesJson, string configId, string configName, NetworkSettings settings,
        ServerOrchestrator server, bool keepInstanceName)
    {
        settings.maxPlayers = gameSize > 0 ? gameSize : maxPlayers;
        settings.maxSpectators = maxSpectators;
        settings.allowSpectators = allowSpectators;
        settings.prefab.gameSize = settings.maxPlayers;
        settings.prefab.minPlayersToStart = minPlayersToStart > 0 ? minPlayersToStart : 1;
        if (Enum.TryParse(mode, out NetworkServerMode parsed))
            settings.prefab.mode = parsed;
        settings.prefab.requirePassword = requirePassword;
        settings.prefab.allowSpectators = allowSpectators;
        settings.prefab.maxSpectators = maxSpectators;
        settings.prefab.lobbyTypeId = lobbyTypeId ?? "";
        settings.prefab.contentKind = LobbyPrefabParameters.ContentKindFromApi(contentKind);
        settings.prefab.contentId = contentId ?? "";
        settings.prefab.propertiesJson = string.IsNullOrEmpty(propertiesJson) ? "{}" : propertiesJson;
        settings.prefab.configId = configId ?? "";
        settings.prefab.configName = configName ?? "";
        if (!keepInstanceName)
            server?.ApplyLobbyPrefab(settings.prefab);
        else
            server?.ApplyLobbyPrefab(settings.prefab);
    }

    public static bool TryStartNamedBallot(string ballotName, VoteLedger ledger, string gameSessionId)
    {
        if (string.IsNullOrEmpty(ballotName) || ledger == null)
            return false;
        string json = SendNow("GET", "/api/votes/ballots/" + UnityWebRequest.EscapeURL(ballotName), null);
        if (string.IsNullOrEmpty(json))
            return false;
        var dto = JsonUtility.FromJson<NamedBallotDto>(json);
        if (dto == null || string.IsNullOrEmpty(dto.name))
            return false;
        var spec = ScriptableObject.CreateInstance<BallotSpec>();
        spec.ballotId = dto.name;
        spec.title = string.IsNullOrEmpty(dto.title) ? dto.name : dto.title;
        spec.prompt = dto.prompt ?? "";
        if (!string.IsNullOrEmpty(dto.kind) && Enum.TryParse(dto.kind, out BallotKind kind))
            spec.kind = kind;
        spec.EnsureQuestionDefaults();
        ledger.StartRun(gameSessionId ?? "", spec);
        return true;
    }

    public static GameLobbyHeartbeatDto BuildHeartbeat(ServerOrchestrator server)
    {
        server.EnsureReady();
        var settings = server.Settings != null ? server.Settings : NetworkSettings.Default;
        var prefab = settings.prefab ?? new LobbyPrefabParameters();
        var host = server.GameSessions;
        var playerIds = new List<string>();
        server.CopyHeartbeatPlayerIds(playerIds);
        var list = new List<GameLobbyHeartbeatSessionDto>();
        if (host != null && host.sessions != null)
        {
            for (int i = 0; i < host.sessions.Count; i++)
            {
                var s = host.sessions[i];
                if (s == null) continue;
                GameLobbyHeartbeatPlayerDto[] sessionPlayers = Array.Empty<GameLobbyHeartbeatPlayerDto>();
                if (s.active && playerIds.Count > 0)
                {
                    sessionPlayers = new GameLobbyHeartbeatPlayerDto[playerIds.Count];
                    for (int p = 0; p < playerIds.Count; p++)
                    {
                        sessionPlayers[p] = new GameLobbyHeartbeatPlayerDto
                        {
                            playerId = playerIds[p],
                            displayName = playerIds[p],
                            actorId = playerIds[p]
                        };
                    }
                }
                list.Add(new GameLobbyHeartbeatSessionDto
                {
                    id = s.id,
                    displayName = s.displayName,
                    active = s.active,
                    createdNarrativeTime = s.createdNarrativeTime,
                    parentId = s.parentId ?? "",
                    peckingOrder = s.peckingOrder,
                    players = sessionPlayers
                });
            }
        }
        return new GameLobbyHeartbeatDto
        {
            name = settings.lobbySessionName,
            displayName = settings.lobbySessionName,
            lobbyPort = server.ActiveLobbyPort,
            gamePort = server.ListenPort,
            playerCount = server.PlayerCount,
            maxPlayers = server.MaxPlayers,
            minPlayersToStart = prefab.minPlayersToStart,
            gameSize = prefab.gameSize > 0 ? prefab.gameSize : server.MaxPlayers,
            mode = (prefab.mode != NetworkServerMode.SinglePlayer ? prefab.mode : server.Mode).ToString(),
            requirePassword = prefab.requirePassword,
            allowSpectators = prefab.allowSpectators,
            maxSpectators = prefab.maxSpectators,
            lobbyTypeId = prefab.lobbyTypeId ?? "",
            contentKind = LobbyPrefabParameters.ContentKindToApi(prefab.contentKind),
            contentId = prefab.contentId ?? "",
            propertiesJson = prefab.propertiesJson ?? "{}",
            configId = prefab.configId ?? "",
            configName = prefab.configName ?? "",
            sessions = list.ToArray()
        };
    }

    public static IEnumerator HeartbeatRoutine(ServerOrchestrator server)
    {
        if (server == null) yield break;
        string body = JsonUtility.ToJson(BuildHeartbeat(server));
        yield return SendRoutine("POST", "/api/game-lobbies", body);
    }

    public static string SendNow(string method, string path, string jsonBody)
    {
        if (TransportOverride != null)
            return TransportOverride(method, path, jsonBody ?? "") ?? "";
        using var req = BuildRequest(method, path, jsonBody);
        var op = req.SendWebRequest();
        while (!op.isDone) { }
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("[GameLobbyContinuuuumClient] " + method + " " + path + " " + req.error);
            return req.downloadHandler != null ? req.downloadHandler.text : "";
        }
        return req.downloadHandler != null ? req.downloadHandler.text : "";
    }

    static IEnumerator SendRoutine(string method, string path, string jsonBody)
    {
        if (TransportOverride != null)
        {
            TransportOverride(method, path, jsonBody ?? "");
            yield break;
        }
        using var req = BuildRequest(method, path, jsonBody);
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning("[GameLobbyContinuuuumClient] " + method + " " + path + " " + req.error);
    }

    static UnityWebRequest BuildRequest(string method, string path, string jsonBody)
    {
        string url = BaseUrl + path;
        var req = new UnityWebRequest(url, method);
        if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && jsonBody != null)
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 2;
        return req;
    }
}

[Serializable]
public sealed class GameLobbyHeartbeatDto
{
    public string name;
    public string displayName;
    public int lobbyPort;
    public int gamePort;
    public int playerCount;
    public int maxPlayers;
    public int minPlayersToStart;
    public int gameSize;
    public string mode;
    public bool requirePassword;
    public bool allowSpectators;
    public int maxSpectators;
    public string lobbyTypeId;
    public string contentKind;
    public string contentId;
    public string propertiesJson;
    public string configId;
    public string configName;
    public GameLobbyHeartbeatSessionDto[] sessions;
}

[Serializable]
public sealed class GameLobbyHeartbeatSessionDto
{
    public string id;
    public string displayName;
    public bool active;
    public float createdNarrativeTime;
    public string parentId;
    public int peckingOrder;
    public GameLobbyHeartbeatPlayerDto[] players;
}

[Serializable]
public sealed class GameLobbyHeartbeatPlayerDto
{
    public string playerId;
    public string displayName;
    public string actorId;
}

[Serializable]
sealed class LobbyPrefabPutDto
{
    public int gameSize;
    public int minPlayersToStart;
    public string mode;
    public bool requirePassword;
    public bool allowSpectators;
    public int maxSpectators;
    public string lobbyTypeId;
    public string contentKind;
    public string contentId;
    public string propertiesJson;
    public string configId;
    public string configName;
}

[Serializable]
sealed class LobbyPrefabGetDto
{
    public string name;
    public int gameSize;
    public int maxPlayers;
    public int minPlayersToStart;
    public string mode;
    public bool requirePassword;
    public bool allowSpectators;
    public int maxSpectators;
    public string lobbyTypeId;
    public string contentKind;
    public string contentId;
    public string propertiesJson;
    public string configId;
    public string configName;
}

[Serializable]
sealed class LobbyConfigGetDto
{
    public string id;
    public string name;
    public int gameSize;
    public int maxPlayers;
    public int minPlayersToStart;
    public string mode;
    public bool requirePassword;
    public bool allowSpectators;
    public int maxSpectators;
    public string lobbyTypeId;
    public string contentKind;
    public string contentId;
    public string propertiesJson;
}

[Serializable]
sealed class NamedBallotDto
{
    public string name;
    public string kind;
    public string title;
    public string prompt;
}
