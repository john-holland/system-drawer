using System;
using System.Collections.Generic;
using UnityEngine;

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
