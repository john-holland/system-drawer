using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>
/// Hosts GameSessions inside a lobby. Switch re-enables tracked objects/BT without a full reload.
/// Close destroys session-created entities (adopt vs umbrella).
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("System Drawer/Networking/Game Session Host")]
public sealed class GameSessionHost : MonoBehaviour
{
    public string lobbySessionName = "Drawer 2";
    public List<GameSession> sessions = new List<GameSession>();
    public int activeIndex = -1;
    public NetworkTreeRegistry treeRegistry;
    public LockstepDecisionValidator lockstep;
    public LobbyPrefabParameters prefab = new LobbyPrefabParameters();

    public event Action SessionsChanged;

    readonly Dictionary<int, GameObject> _alive = new Dictionary<int, GameObject>();
    readonly Dictionary<string, List<int>> _sessionSpawns = new Dictionary<string, List<int>>();

    public GameSession Active =>
        sessions != null && activeIndex >= 0 && activeIndex < sessions.Count
            ? sessions[activeIndex]
            : null;

    public string ActiveId => Active != null ? Active.id : "";

    public GameSession CreateSession(string displayName = null)
    {
        if (sessions == null) sessions = new List<GameSession>();
        float narrative = 0f;
        var clock = FindFirstObjectByType<NarrativeClock>();
        if (clock != null)
            narrative = clock.SimulationSeconds;
        var session = GameSession.Create(lobbySessionName, narrative, displayName);
        var parent = Active;
        if (parent != null)
        {
            session.parentId = parent.id;
            session.peckingOrder = NextSiblingPecking(parent.id);
        }
        else
        {
            session.parentId = "";
            session.peckingOrder = NextSiblingPecking("");
        }
        if (prefab != null)
            session.prefab = prefab.Clone();
        sessions.Add(session);
        SwitchActive(sessions.Count - 1);
        BindVoteNodes();
        SessionsChanged?.Invoke();
        return session;
    }

    int NextSiblingPecking(string parentId)
    {
        int max = -1;
        if (sessions == null) return 0;
        for (int i = 0; i < sessions.Count; i++)
        {
            var s = sessions[i];
            if (s == null) continue;
            string pid = s.parentId ?? "";
            if (pid == (parentId ?? "") && s.peckingOrder > max)
                max = s.peckingOrder;
        }
        return max + 1;
    }

    public bool SwitchActive(int index)
    {
        if (sessions == null || index < 0 || index >= sessions.Count) return false;
        if (activeIndex == index)
        {
            var already = sessions[index];
            if (already != null) already.active = true;
            if (lockstep != null) lockstep.ActiveGameSessionId = already != null ? already.id : "";
            BindVoteNodes();
            return true;
        }
        var prev = Active;
        if (prev != null)
            SetDormant(prev, true);
        activeIndex = index;
        var next = sessions[index];
        next.active = true;
        SetDormant(next, false);
        if (lockstep != null)
            lockstep.ActiveGameSessionId = next.id;
        BindVoteNodes();
        SessionsChanged?.Invoke();
        return true;
    }

    public bool SwitchActiveById(string id)
    {
        if (sessions == null) return false;
        for (int i = 0; i < sessions.Count; i++)
            if (sessions[i] != null && sessions[i].id == id)
                return SwitchActive(i);
        return false;
    }

    public GameSession FindSession(string id)
    {
        int i = IndexOfSession(id);
        return i >= 0 ? sessions[i] : null;
    }

    int IndexOfSession(string id)
    {
        if (sessions == null || string.IsNullOrEmpty(id)) return -1;
        for (int i = 0; i < sessions.Count; i++)
            if (sessions[i] != null && sessions[i].id == id)
                return i;
        return -1;
    }

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
        float t = session.createdNarrativeTime;
        var clock = FindFirstObjectByType<NarrativeClock>();
        if (clock != null) t = clock.SimulationSeconds;
        return session.index.Add(go, treeId, prefabKey, t, isBt);
    }

    public void CleanupForSessionClose(GameSession session)
    {
        if (session == null) return;
        if (_sessionSpawns.TryGetValue(session.id, out var ids))
        {
            for (int i = 0; i < ids.Count; i++)
            {
                if (_alive.TryGetValue(ids[i], out var go) && go != null)
                {
                    if (Application.isPlaying)
                        Destroy(go);
                    else
                        DestroyImmediate(go);
                }
                _alive.Remove(ids[i]);
            }
            _sessionSpawns.Remove(session.id);
        }
        if (treeRegistry != null && session.treeIds != null)
        {
            for (int i = 0; i < session.treeIds.Count; i++)
                treeRegistry.Remove(session.treeIds[i]);
        }
        session.active = false;
        session.spawnedInstanceIds.Clear();
        session.treeIds.Clear();
        if (session.index != null && session.index.entries != null)
            session.index.entries.Clear();
    }

    public bool CloseSession(string id) => CloseSession(id, GameSessionCloseMode.AdoptToHigher);

    public bool CloseSession(string id, GameSessionCloseMode mode)
    {
        if (sessions == null || string.IsNullOrEmpty(id)) return false;
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
            {
                var s = FindSession(ids[i]);
                if (s != null)
                    CleanupForSessionClose(s);
            }
            sessions.RemoveAll(s => s != null && ids.Contains(s.id));
        }
        else
        {
            if (survivingActiveId == id)
                survivingActiveId = closedParentId;
            string parent = closedParentId;
            for (int i = 0; i < sessions.Count; i++)
            {
                var s = sessions[i];
                if (s != null && s.parentId == closed.id)
                    s.parentId = parent;
            }
            CleanupForSessionClose(closed);
            sessions.RemoveAll(s => s != null && s.id == id);
        }

        activeIndex = IndexOfSession(survivingActiveId);
        if (activeIndex < 0 && sessions.Count > 0)
            activeIndex = 0;
        if (Active != null)
        {
            SetDormant(Active, false);
            if (lockstep != null) lockstep.ActiveGameSessionId = Active.id;
        }
        else if (lockstep != null)
            lockstep.ActiveGameSessionId = "";
        BindVoteNodes();
        SessionsChanged?.Invoke();
        return true;
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

    public void SaveToLocalClient(string id = null)
    {
        GameSession session = null;
        if (!string.IsNullOrEmpty(id))
            session = FindSession(id);
        if (session == null) session = Active;
        if (session == null) return;
        if (prefab != null)
            session.prefab = prefab.Clone();
        GameSessionLocalSave.Save(session, prefab);
    }

    public void SaveAllToLocalClient()
    {
        if (sessions == null) return;
        for (int i = 0; i < sessions.Count; i++)
        {
            if (sessions[i] != null)
                SaveToLocalClient(sessions[i].id);
        }
    }

    public GameSession LoadFromLocalClient(string lobby, string gameSessionId)
    {
        var loaded = GameSessionLocalSave.Load(lobby, gameSessionId);
        if (loaded == null) return null;
        if (sessions == null) sessions = new List<GameSession>();
        sessions.Add(loaded);
        return loaded;
    }

    public void BindVoteNodes()
    {
        string sid = ActiveId;
        var nodes = FindObjectsByType<VoteBehaviorTreeNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null)
                nodes[i].gameSessionId = sid;
        }
        if (lockstep != null)
            lockstep.ActiveGameSessionId = sid;
    }

    void SetDormant(GameSession session, bool dormant)
    {
        if (session == null) return;
        session.active = !dormant;
        if (!_sessionSpawns.TryGetValue(session.id, out var ids)) return;
        for (int i = 0; i < ids.Count; i++)
        {
            if (!_alive.TryGetValue(ids[i], out var go) || go == null) continue;
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
}
