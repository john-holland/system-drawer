#if UNITY_EDITOR
using NUnit.Framework;
using SystemDrawer.DreamCycle;
using UnityEngine;

public class SleepWaveStatRendererTests
{
    [Test]
    public void SetWaveSamples_BuildsTexture()
    {
        var go = new GameObject("sleep_wave");
        var r = go.AddComponent<SleepWaveStatRenderer>();
        r.textureWidth = 32;
        r.textureHeight = 16;
        r.SetWaveSamples(new[] { -0.5f, 0f, 0.5f, 1f });
        r.RenderWave();
        Assert.AreEqual(4, r.waveSamples.Length);
        UnityEngine.Object.DestroyImmediate(go);
    }
}
#endif
