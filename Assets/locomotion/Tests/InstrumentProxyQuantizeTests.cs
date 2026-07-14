#if UNITY_EDITOR
using Locomotion.Audio;
using NUnit.Framework;
using UnityEngine;

public class InstrumentProxyQuantizeTests
{
    [Test]
    public void QuantizeTime_ZeroMeansFree()
    {
        float t = 1.37f;
        Assert.AreEqual(t, PlayerInteractionQuantizer.QuantizeTime(t, 120f, 0f), 0.0001f);
    }

    [Test]
    public void QuantizeTime_OneSnapsToGrid()
    {
        float snapped = PlayerInteractionQuantizer.QuantizeTime(0.3f, 120f, 1f, 4);
        // 120 bpm → beat 0.5s → 16th = 0.125; 0.3 → 0.25 or 0.375
        Assert.AreEqual(0.25f, snapped, 0.0001f);
    }

    [Test]
    public void TraditionalFlag_FiltersGuitarDistortion()
    {
        var curves = InstrumentProfileCurves.CreateRuntimeDefault(InstrumentFamily.Strings);
        Assert.IsTrue(curves.IsOptionAllowed("pluck"));
        Assert.IsFalse(curves.IsOptionAllowed("super-powered-distortion"));
        curves.enforceTraditionalDefaults = false;
        Assert.IsTrue(curves.IsOptionAllowed("super-powered-distortion"));
    }

    [Test]
    public void ElectronicFamily_OwnsDryWetLfoPwm()
    {
        var strings = InstrumentProfileCurves.CreateRuntimeDefault(InstrumentFamily.Strings);
        Assert.IsFalse(strings.IsOptionAllowed("drywet"));
        Assert.IsFalse(strings.IsOptionAllowed("lfo"));
        Assert.IsFalse(strings.IsOptionAllowed("pwm"));

        var electronic = InstrumentProfileCurves.CreateRuntimeDefault(InstrumentFamily.Electronic);
        Assert.IsTrue(electronic.IsOptionAllowed("drywet"));
        Assert.IsTrue(electronic.IsOptionAllowed("lfo"));
        Assert.IsTrue(electronic.IsOptionAllowed("pwm"));
        Assert.IsTrue(electronic.IsOptionAllowed("dac"));
        Assert.IsTrue(electronic.IsOptionAllowed("drummachine"));
        Assert.AreEqual(InstrumentWaveShape.Square, electronic.waveShape);
    }

    [Test]
    public void ProxyArticulate_AppliesInstrumentationCurve()
    {
        var go = new GameObject("proxy");
        var proxy = go.AddComponent<InstrumentProxy>();
        proxy.family = InstrumentFamily.Keyboard;
        proxy.playerInteractionQuantize01 = 0f;
        proxy.enforceTraditionalDefaults = true;
        bool ok = proxy.TryArticulate("key", 0.8f, 0f, 120f, out var dsp);
        Assert.IsTrue(ok);
        Assert.Greater(dsp.baseFrequency, 0f);
        Object.DestroyImmediate(go);
    }
}
#endif
