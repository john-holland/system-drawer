#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class GameSessionTests
{
    [Test]
    public void TwoSessions_SwitchWithoutDestroy_CloseDeletesSpawns()
    {
        var hostGo = new GameObject("gs-host");
        var spawn = new GameObject("spawned");
        spawn.transform.SetParent(hostGo.transform);
        try
        {
            var host = hostGo.AddComponent<GameSessionHost>();
            host.lobbySessionName = "lobby-a";
            var a = host.CreateSession("A");
            var b = host.CreateSession("B");
            Assert.AreEqual(2, host.sessions.Count);
            host.SwitchActiveById(a.id);
            host.TrackSpawn(spawn, "tree.a", "prefab", false);
            Assert.IsTrue(spawn.activeSelf);
            host.SwitchActiveById(b.id);
            Assert.IsFalse(spawn.activeSelf);
            Assert.IsTrue(spawn != null);
            host.SwitchActiveById(a.id);
            Assert.IsTrue(spawn.activeSelf);
            host.CloseSession(a.id);
            Assert.IsTrue(spawn == null);
            Assert.AreEqual(1, host.sessions.Count);
            Assert.AreEqual(b.id, host.Active.id);
        }
        finally
        {
            Object.DestroyImmediate(hostGo);
        }
    }

    [Test]
    public void SaveToLocalClient_RoundTrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "gs-test-" + System.Guid.NewGuid().ToString("N"));
        GameSessionLocalSave.RootOverride = dir;
        var hostGo = new GameObject("gs-save");
        try
        {
            var host = hostGo.AddComponent<GameSessionHost>();
            host.lobbySessionName = "lobby-save";
            var s = host.CreateSession("Saved");
            host.SaveToLocalClient(s.id);
            Assert.IsTrue(File.Exists(GameSessionLocalSave.SessionPath(s.lobbySessionName, s.id)));
            var loaded = GameSessionLocalSave.Load(s.lobbySessionName, s.id);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(s.id, loaded.id);
            Assert.AreEqual("Saved", loaded.displayName);
            Assert.IsNotNull(loaded.prefab);
        }
        finally
        {
            GameSessionLocalSave.RootOverride = null;
            Object.DestroyImmediate(hostGo);
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }

    [Test]
    public void Lockstep_ScopedToActiveGameSession()
    {
        var registry = new NetworkTreeRegistry();
        registry.Register(new NetworkTreeDescriptor
        {
            TreeId = "t1",
            TransmitPolicy = TreeTransmitPolicy.PeerTransferable,
            CausalityLeafPrefix = "vote.gs1.run",
            GameSessionId = "gs1"
        });
        var lockstep = new LockstepDecisionValidator(registry);
        lockstep.ActiveGameSessionId = "gs2";
        Assert.IsFalse(lockstep.TryValidateDecision("c", "vote.gs1.run.actor", out _));
        lockstep.ActiveGameSessionId = "gs1";
        Assert.IsTrue(lockstep.TryValidateDecision("c", "vote.gs1.run.actor", out _));
    }

    [Test]
    public void VoteAccounting_TallyHashMatch()
    {
        var go = new GameObject("vote-hash");
        try
        {
            var ledger = go.AddComponent<VoteLedger>();
            var spec = ScriptableObject.CreateInstance<BallotSpec>();
            spec.EnsureQuestionDefaults();
            var run = ledger.StartRun("gs1", spec);
            ledger.Cast(run, "a", "yes", "dem");
            ledger.Cast(run, "b", "no", "rep");
            var local = run.Tally();
            var host = run.CloneForRecount().Tally();
            Assert.IsTrue(ledger.AccountingMatches(local, host));
            Object.DestroyImmediate(spec);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void GameSessionLemmas()
    {
        Assert.AreEqual("saving", GameSessionLemmaPropertyKeys.Saving);
        Assert.AreEqual("loading", GameSessionLemmaPropertyKeys.Loading);
        Assert.AreEqual("local-server", GameSessionLemmaPropertyKeys.LocalServer);
    }

    [Test]
    public void AdoptClose_ReparentsChildren_KeepsDescendantSpawns()
    {
        var hostGo = new GameObject("gs-adopt");
        var spawnC = new GameObject("spawn-c");
        spawnC.transform.SetParent(hostGo.transform);
        try
        {
            var host = hostGo.AddComponent<GameSessionHost>();
            host.lobbySessionName = "lobby-adopt";
            var a = host.CreateSession("A");
            var b = host.CreateSession("B");
            var c = host.CreateSession("C");
            Assert.AreEqual(a.id, b.parentId);
            Assert.AreEqual(b.id, c.parentId);
            host.SwitchActiveById(c.id);
            host.TrackSpawn(spawnC, "tree.c", "prefab", false);
            host.CloseSession(b.id, GameSessionCloseMode.AdoptToHigher);
            Assert.IsNull(host.FindSession(b.id));
            Assert.AreEqual(a.id, host.FindSession(c.id).parentId);
            Assert.AreEqual(c.id, host.Active.id);
            Assert.IsTrue(spawnC != null);
        }
        finally
        {
            Object.DestroyImmediate(hostGo);
        }
    }

    [Test]
    public void UmbrellaClose_DestroysDescendants()
    {
        var hostGo = new GameObject("gs-umb");
        var spawnC = new GameObject("spawn-c");
        spawnC.transform.SetParent(hostGo.transform);
        try
        {
            var host = hostGo.AddComponent<GameSessionHost>();
            host.lobbySessionName = "lobby-umb";
            var a = host.CreateSession("A");
            var b = host.CreateSession("B");
            var c = host.CreateSession("C");
            host.SwitchActiveById(c.id);
            host.TrackSpawn(spawnC, "tree.c", "prefab", false);
            host.CloseSession(b.id, GameSessionCloseMode.Umbrella);
            Assert.IsNull(host.FindSession(b.id));
            Assert.IsNull(host.FindSession(c.id));
            Assert.IsNotNull(host.FindSession(a.id));
            Assert.IsTrue(spawnC == null);
        }
        finally
        {
            Object.DestroyImmediate(hostGo);
        }
    }

    [Test]
    public void Prefab_RoundTripLocalSave()
    {
        var dir = Path.Combine(Path.GetTempPath(), "gs-prefab-" + System.Guid.NewGuid().ToString("N"));
        GameSessionLocalSave.RootOverride = dir;
        var hostGo = new GameObject("gs-prefab");
        try
        {
            var host = hostGo.AddComponent<GameSessionHost>();
            host.lobbySessionName = "lobby-prefab";
            host.prefab = new LobbyPrefabParameters
            {
                gameSize = 12,
                minPlayersToStart = 3,
                mode = NetworkServerMode.ClassicLockstep,
                propertiesJson = "{\"k\":1}"
            };
            var s = host.CreateSession("P");
            host.SaveToLocalClient(s.id);
            var loaded = GameSessionLocalSave.Load(s.lobbySessionName, s.id);
            Assert.IsNotNull(loaded.prefab);
            Assert.AreEqual(12, loaded.prefab.gameSize);
            Assert.AreEqual(3, loaded.prefab.minPlayersToStart);
            Assert.AreEqual(NetworkServerMode.ClassicLockstep, loaded.prefab.mode);
            Assert.AreEqual("{\"k\":1}", loaded.prefab.propertiesJson);
        }
        finally
        {
            GameSessionLocalSave.RootOverride = null;
            Object.DestroyImmediate(hostGo);
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }

    [Test]
    public void HeartbeatDto_IncludesSessionsAndLobbyName()
    {
        GameLobbyContinuuuumClient.TransportOverride = (m, p, b) => "{}";
        var go = new GameObject("gs-hb");
        try
        {
            var server = go.AddComponent<ServerOrchestrator>();
            server.EnsureReady();
            var s = server.GameSessions.CreateSession("HB");
            var dto = GameLobbyContinuuuumClient.BuildHeartbeat(server);
            Assert.AreEqual(server.Settings.lobbySessionName, dto.name);
            Assert.AreEqual(1, dto.sessions.Length);
            Assert.AreEqual(s.id, dto.sessions[0].id);
            Assert.AreEqual(s.peckingOrder, dto.sessions[0].peckingOrder);
        }
        finally
        {
            GameLobbyContinuuuumClient.TransportOverride = null;
            Object.DestroyImmediate(go);
        }
    }
}
#endif
