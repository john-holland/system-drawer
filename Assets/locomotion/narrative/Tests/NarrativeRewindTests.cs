using System.Collections.Generic;
using Locomotion.Narrative;
using NUnit.Framework;
using UnityEngine;

public class NarrativeRewindTests
{
    GameObject _prefab;

    [SetUp]
    public void SetUp()
    {
        _prefab = new GameObject("spawnPrefab");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_prefab);
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go != null && go.name == "spawnPrefab(Clone)")
                Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void SpawnPrefabAction_Undo_DestroysInstance()
    {
        var parent = new GameObject("parent");
        var bindings = parent.AddComponent<NarrativeBindings>();
        var action = new SpawnPrefabAction { prefab = _prefab, parentKey = "parent" };
        bindings.bindings = new List<NarrativeBindings.BindingEntry>
        {
            new NarrativeBindings.BindingEntry { key = "parent", value = parent }
        };
        bindings.RebuildIndex();
        var ctx = new NarrativeExecutionContext(null, bindings, null);
        var state = new NarrativeRuntimeState();

        Assert.AreEqual(BehaviorTreeStatus.Success, action.Execute(ctx, state));

        bool hadClone = false;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.gameObject.name == "spawnPrefab(Clone)")
                hadClone = true;
        }
        Assert.IsTrue(hadClone);

        action.Undo(ctx, state);
        int clones = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.gameObject.name == "spawnPrefab(Clone)")
                clones++;
        }
        Assert.AreEqual(0, clones);
        Object.DestroyImmediate(parent);
    }

    [Test]
    public void RewindWalker_TrimsTriggeredEventsAfterTarget()
    {
        var state = new NarrativeRuntimeState();
        state.triggeredEventIds.Add("evt1");
        state.triggeredEventIds.Add("evt2");
        state.executionLedger.Add(new NarrativeExecutionLedgerEntry
        {
            eventId = "evt1",
            time = 5f,
            finishTime = 8f
        });
        state.executionLedger.Add(new NarrativeExecutionLedgerEntry
        {
            eventId = "evt2",
            time = 12f,
            finishTime = 15f
        });

        NarrativeRewindUndoWalker.RewindToTime(10f, state, null, null);

        CollectionAssert.Contains(state.triggeredEventIds, "evt1");
        CollectionAssert.DoesNotContain(state.triggeredEventIds, "evt2");
        Assert.AreEqual(1, state.executionLedger.Count);
    }
}
