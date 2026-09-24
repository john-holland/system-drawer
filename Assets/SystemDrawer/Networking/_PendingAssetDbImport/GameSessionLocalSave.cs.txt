using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Persists GameSession server structure (index, trees, spawn list) to the local client.</summary>
public static class GameSessionLocalSave
{
    public static string RootOverride;

    public static string RootDir()
    {
        if (!string.IsNullOrEmpty(RootOverride))
            return RootOverride;
        return Path.Combine(Application.persistentDataPath, "game-sessions");
    }

    public static string SessionPath(string lobbySessionName, string gameSessionId, string playerId = null)
    {
        string lobby = Sanitize(lobbySessionName ?? "local");
        string id = Sanitize(gameSessionId ?? "session");
        string file = string.IsNullOrEmpty(playerId) ? id + ".json" : id + "." + Sanitize(playerId) + ".json";
        return Path.Combine(RootDir(), lobby, file);
    }

    public static void Save(GameSession session, LobbyPrefabParameters prefab = null, string playerId = null)
    {
        if (session == null) return;
        if (prefab != null)
            session.prefab = prefab.Clone();
        string path = SessionPath(session.lobbySessionName, session.id, playerId);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? RootDir());
        File.WriteAllText(path, JsonUtility.ToJson(session, true));
    }

    public static GameSession Load(string lobbySessionName, string gameSessionId)
    {
        string path = SessionPath(lobbySessionName, gameSessionId);
        if (!File.Exists(path)) return null;
        return JsonUtility.FromJson<GameSession>(File.ReadAllText(path));
    }

    static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "default";
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s;
    }
}
