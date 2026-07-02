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
        Assert.IsTrue(fragment.narrativeText.Contains("Empty"));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Buffer_PushWaveBatch_RetainsLatest()
    {
        var go = new GameObject("buf");
        var buf = go.AddComponent<DreamMemoryBuffer>();
        buf.capacity = 4;
        buf.PushWaveBatch(new[] { 0.1f, 0.2f, 0.3f }, 42, "abc");
        Assert.AreEqual(3, buf.Count);
        Assert.IsTrue(buf.TryPeekLatest(out var latest));
        Assert.AreEqual(0.3f, latest.waveSample, 0.001f);
        Object.DestroyImmediate(go);
    }
}
#endif
