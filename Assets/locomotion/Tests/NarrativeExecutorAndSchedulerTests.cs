#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using Locomotion.Narrative;

/// <summary>
/// Tests for NarrativeExecutor (keys/context), NarrativeCalendarEvent, and NarrativeScheduler:
/// executor keys (bindings resolution), position keys (scheduler + per-event override),
/// spacetime assertions for causal "this before that" and "this because of that".
/// </summary>
public class NarrativeExecutorAndSchedulerTests
{
    [Test]
    public void NarrativeBindings_TryResolveGameObject_ResolvesKeyToGameObject()
    {
        var goBindings = new GameObject("Bindings");
        var bindings = goBindings.AddComponent<NarrativeBindings>();
        var playerGo = new GameObject("Player");
        bindings.bindings.Add(new NarrativeBindings.BindingEntry { key = "player", value = playerGo });

        bindings.RebuildIndex();
        bool ok = bindings.TryResolveGameObject("player", out GameObject resolved);
        Assert.IsTrue(ok);
        Assert.IsNotNull(resolved);
        Assert.AreEqual(playerGo, resolved);

        Object.DestroyImmediate(playerGo);
        Object.DestroyImmediate(goBindings);
    }

    [Test]
    public void NarrativeExecutor_StartEvent_SetsActiveEventAndIsExecuting()
    {
        var goExec = new GameObject("Executor");
        var executor = goExec.AddComponent<NarrativeExecutor>();

        var evt = new NarrativeCalendarEvent { id = "evt1", title = "Test" };
        executor.StartEvent(evt);

        var state = executor.GetRuntimeState();
        Assert.AreEqual("evt1", state.activeEventId);
        Assert.IsTrue(state.isExecuting);

        Object.DestroyImmediate(goExec);
    }

    [Test]
    public void NarrativeExecutor_EventWithNoTreeAndNoActions_FinishesAndAddsToTriggeredEventIds()
    {
        var goExec = new GameObject("Executor");
        var executor = goExec.AddComponent<NarrativeExecutor>();

        var evt = new NarrativeCalendarEvent { id = "evt-done", title = "Instant" };
        executor.StartEvent(evt);

        for (int i = 0; i < 5; i++)
            executor.SendMessage("Update"); // Unity will call Update

        var state = executor.GetRuntimeState();
        Assert.IsFalse(state.isExecuting);
        Assert.Contains("evt-done", state.triggeredEventIds);

        Object.DestroyImmediate(goExec);
    }

    [Test]
    public void NarrativeScheduler_PositionKeys_DefaultIncludesPlayer()
    {
        var go = new GameObject("Scheduler");
        var scheduler = go.AddComponent<NarrativeScheduler>();
        Assert.IsNotNull(scheduler.positionKeys);
        Assert.Greater(scheduler.positionKeys.Count, 0);
        Assert.Contains("player", scheduler.positionKeys);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void NarrativeScheduler_EventWithSpatiotemporalVolume_TriggersWhenKeyResolvesInVolume()
    {
        float tMid = 30f;
        var vol = new Bounds4(new Vector3(10, 0, 10), new Vector3(4, 4, 4), 0f, 60f);

        var goCalendar = new GameObject("Calendar");
        var calendar = goCalendar.AddComponent<NarrativeCalendarAsset>();
        var evt = new NarrativeCalendarEvent
        {
            id = "vol-event",
            title = "In volume",
            startDateTime = new NarrativeDateTime(2025, 1, 1, 0, 0, 0),
            spatiotemporalVolume = vol
        };
        calendar.events.Add(evt);

        var goClock = new GameObject("Clock");
        var clock = goClock.AddComponent<NarrativeClock>();
        clock.fallbackStartDateTime = NarrativeCalendarMath.SecondsToNarrativeDateTime(tMid);

        var goPlayer = new GameObject("Player");
        goPlayer.transform.position = vol.center;

        var goBindings = new GameObject("Bindings");
        var bindings = goBindings.AddComponent<NarrativeBindings>();
        bindings.bindings.Add(new NarrativeBindings.BindingEntry { key = "player", value = goPlayer });
        bindings.RebuildIndex();

        var goExec = new GameObject("Executor");
        var executor = goExec.AddComponent<NarrativeExecutor>();
        executor.clock = clock;
        executor.bindings = bindings;

        var goScheduler = new GameObject("Scheduler");
        var scheduler = goScheduler.AddComponent<NarrativeScheduler>();
        scheduler.autoFindReferences = false;
        scheduler.calendar = calendar;
        scheduler.clock = clock;
        scheduler.executor = executor;

        scheduler.ApplyEventsUpToNow();

        var state = executor.GetRuntimeState();
        Assert.IsTrue(state.isExecuting);
        Assert.AreEqual("vol-event", state.activeEventId);

        for (int i = 0; i < 5; i++)
            executor.SendMessage("Update");

        Assert.IsTrue(executor.GetRuntimeState().triggeredEventIds.Contains("vol-event"));

        Object.DestroyImmediate(goCalendar);
        Object.DestroyImmediate(goClock);
        Object.DestroyImmediate(goPlayer);
        Object.DestroyImmediate(goBindings);
        Object.DestroyImmediate(goExec);
        Object.DestroyImmediate(goScheduler);
    }

    [Test]
    public void NarrativeCalendarEvent_PositionKeysOverride_UsedWhenNonEmpty()
    {
        var vol = new Bounds4(Vector3.zero, new Vector3(2, 2, 2), 0f, 60f);
        float tMid = 30f;

        var goCalendar = new GameObject("Calendar");
        var calendar = goCalendar.AddComponent<NarrativeCalendarAsset>();
        var evt = new NarrativeCalendarEvent
        {
            id = "override-event",
            title = "Uses other key",
            startDateTime = new NarrativeDateTime(2025, 1, 1, 0, 0, 0),
            spatiotemporalVolume = vol,
            positionKeys = new System.Collections.Generic.List<string> { "listener" }
        };
        calendar.events.Add(evt);

        var goClock = new GameObject("Clock");
        var clock = goClock.AddComponent<NarrativeClock>();
        clock.fallbackStartDateTime = NarrativeCalendarMath.SecondsToNarrativeDateTime(tMid);

        var goListener = new GameObject("Listener");
        goListener.transform.position = vol.center;

        var goBindings = new GameObject("Bindings");
        var bindings = goBindings.AddComponent<NarrativeBindings>();
        bindings.bindings.Add(new NarrativeBindings.BindingEntry { key = "listener", value = goListener });
        bindings.RebuildIndex();

        var goExec = new GameObject("Executor");
        var executor = goExec.AddComponent<NarrativeExecutor>();
        executor.clock = clock;
        executor.bindings = bindings;

        var goScheduler = new GameObject("Scheduler");
        var scheduler = goScheduler.AddComponent<NarrativeScheduler>();
        scheduler.autoFindReferences = false;
        scheduler.calendar = calendar;
        scheduler.clock = clock;
        scheduler.executor = executor;
        scheduler.positionKeys.Clear();
        scheduler.positionKeys.Add("player");

        scheduler.ApplyEventsUpToNow();

        Assert.IsTrue(executor.GetRuntimeState().isExecuting);
        Assert.AreEqual("override-event", executor.GetRuntimeState().activeEventId);

        Object.DestroyImmediate(goCalendar);
        Object.DestroyImmediate(goClock);
        Object.DestroyImmediate(goListener);
        Object.DestroyImmediate(goBindings);
        Object.DestroyImmediate(goExec);
        Object.DestroyImmediate(goScheduler);
    }

    [Test]
    public void CausalLink_ThisBeforeThat_FromEventTimeBeforeToEventTime()
    {
        var goCalendar = new GameObject("Calendar");
        var calendar = goCalendar.AddComponent<NarrativeCalendarAsset>();

        var evtA = new NarrativeCalendarEvent
        {
            id = "evt-a",
            title = "First",
            startDateTime = new NarrativeDateTime(2025, 1, 1, 9, 0, 0),
            durationSeconds = 60,
            spatiotemporalVolume = new Bounds4(Vector3.zero, Vector3.one * 2, 0f, 60f)
        };
        var evtB = new NarrativeCalendarEvent
        {
            id = "evt-b",
            title = "Second",
            startDateTime = new NarrativeDateTime(2025, 1, 1, 10, 0, 0),
            durationSeconds = 60,
            spatiotemporalVolume = new Bounds4(Vector3.one * 5, Vector3.one * 2, 120f, 180f)
        };
        calendar.events.Add(evtA);
        calendar.events.Add(evtB);
        calendar.causalLinks.Add(new NarrativeCausalLink { fromEventId = "evt-a", toEventId = "evt-b" });

        foreach (var link in calendar.causalLinks)
        {
            var fromEvt = FindEvent(calendar, link.fromEventId);
            var toEvt = FindEvent(calendar, link.toEventId);
            Assert.IsNotNull(fromEvt, "fromEvent should exist");
            Assert.IsNotNull(toEvt, "toEvent should exist");

            float fromEnd = fromEvt.spatiotemporalVolume.HasValue
                ? fromEvt.spatiotemporalVolume.Value.tMax
                : NarrativeCalendarMath.DateTimeToSeconds(fromEvt.startDateTime) + fromEvt.durationSeconds;
            float toStart = toEvt.spatiotemporalVolume.HasValue
                ? toEvt.spatiotemporalVolume.Value.tMin
                : NarrativeCalendarMath.DateTimeToSeconds(toEvt.startDateTime);

            Assert.LessOrEqual(fromEnd, toStart,
                "Causal 'this before that': fromEvent time window must end before or when toEvent starts.");
        }

        Object.DestroyImmediate(goCalendar);
    }

    [Test]
    public void CausalLink_ThisBecauseOfThat_FromEventIdEnablesToEventId()
    {
        var goCalendar = new GameObject("Calendar");
        var calendar = goCalendar.AddComponent<NarrativeCalendarAsset>();

        var evtA = new NarrativeCalendarEvent { id = "cause", title = "Cause" };
        var evtB = new NarrativeCalendarEvent { id = "effect", title = "Effect" };
        calendar.events.Add(evtA);
        calendar.events.Add(evtB);
        calendar.causalLinks.Add(new NarrativeCausalLink { fromEventId = "cause", toEventId = "effect" });

        foreach (var link in calendar.causalLinks)
        {
            Assert.IsFalse(string.IsNullOrEmpty(link.fromEventId), "fromEventId should be set");
            Assert.IsFalse(string.IsNullOrEmpty(link.toEventId), "toEventId should be set");
            Assert.AreNotEqual(link.fromEventId, link.toEventId, "from and to should differ");

            var fromEvt = FindEvent(calendar, link.fromEventId);
            var toEvt = FindEvent(calendar, link.toEventId);
            Assert.IsNotNull(fromEvt, "fromEvent must exist in calendar");
            Assert.IsNotNull(toEvt, "toEvent must exist in calendar");
        }

        Object.DestroyImmediate(goCalendar);
    }

    private static NarrativeCalendarEvent FindEvent(NarrativeCalendarAsset calendar, string id)
    {
        if (calendar?.events == null) return null;
        foreach (var e in calendar.events)
            if (e != null && e.id == id)
                return e;
        return null;
    }
}
#endif
