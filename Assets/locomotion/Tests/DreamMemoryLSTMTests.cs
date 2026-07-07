#if UNITY_EDITOR
using Locomotion.DreamCycle;
using NUnit.Framework;
using UnityEngine;

public class DreamMemoryLSTMTests
{
    [Test]
    public void RecallDreamFragment_EmptyBuffer_ReturnsLabel()
    {
        var go = new GameObject("dream_lstm");
        var lstm = go.AddComponent<DreamMemoryLSTM>();
        var fragment = lstm.RecallDreamFragment();
        Assert.IsTrue(fragment.isDreamMemory);
        Assert.IsTrue(fragment.narrativeText.Contains("Empty") || fragment.narrativeText.Contains("non-authoritative"));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Buffer_PushWaveBatch_RetainsLatest()
    {
        var go = new GameObject("buf");
        var buf = go.AddComponent<DreamMemoryBuffer>();
        buf.capacity = 4;
        buf.PushWaveBatch(new[] { 0.1f, 0.2f, 0.3f }, 42, "abc", 10, DreamMemoryLayer.DeveloperDream);
        Assert.AreEqual(3, buf.Count);
        Assert.IsTrue(buf.TryPeekLatest(out var latest));
        Assert.AreEqual(0.3f, latest.waveSample, 0.001f);
        Assert.AreEqual(10, latest.goodDayCollapseSeed);
        Assert.AreEqual(DreamMemoryLayer.DeveloperDream, latest.dreamLayer);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void SafeRefrain_CapsSeverity()
    {
        var go = new GameObject("buf");
        var buf = go.AddComponent<DreamMemoryBuffer>();
        buf.PushWaveBatch(new[] { 0.9f, 0.95f }, 1, "", 2, DreamMemoryLayer.DeveloperDream);

        var fragment = new DreamFragment
        {
            narrativeText = "intense recall",
            confidence = 0.9f,
            isDreamMemory = true
        };
        var settings = DreamSafeRefrainSettings.Default;
        settings.maxAlertSeverity = 0.35f;
        var result = DreamSafeRefrain.Apply(fragment, buf, settings);
        Assert.LessOrEqual(result.confidence, 0.35f);
        Assert.GreaterOrEqual(result.distanceFromBed, settings.minNarrativeDistanceFromBed);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Buffer_RemOnly_FiltersNonRemSamples()
    {
        var go = new GameObject("buf");
        var buf = go.AddComponent<DreamMemoryBuffer>();
        var samples = new float[100];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = i / 100f;
        buf.PushWaveBatch(samples, 5, "", 0, DreamMemoryLayer.DeveloperDream, remOnly: true);
        Assert.Greater(buf.Count, 0);
        Assert.Less(buf.Count, samples.Length);
        Object.DestroyImmediate(go);
    }
}
#endif
