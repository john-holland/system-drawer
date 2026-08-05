using System;
using UnityEngine;

/// <summary>Generic timed pixel light rig — firetruck, stage, stop-light heads.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Pixel Light Rig")]
public sealed class PixelLightRig : MonoBehaviour
{
    public int gridWidth = 8;
    public int gridHeight = 4;
    public float stepMs = 100f;
    public PixelLightPatternAsset pattern;
    public PixelLightColorPackage colorPackage;
    public PixelLightOptic optic;
    public PixelLightSyncMode syncMode = PixelLightSyncMode.Free;
    public BeatQuantizedActionBinder beatBinder;
    public MusicAmbianceSchedule ambiance;

    [Range(0f, 1f)] public float masterBrightness01 = 1f;
    public bool playing = true;
    public int frameIndex;
    public Color solidOverride = Color.clear;

    Texture2D _lumRt;
    float _accum;
    float _avgLum;

    void Awake()
    {
        if (optic == null)
            optic = GetComponent<PixelLightOptic>() ?? gameObject.AddComponent<PixelLightOptic>();
        optic.EnsureBreadPanMesh();
        EnsureLuminanceTexture();
        if (pattern == null)
            pattern = PixelLightPatternAsset.CreateChasePreset();
        if (colorPackage == null)
            colorPackage = PixelLightColorPackage.CreateEmergencyRed();
        ApplyGridFromPattern();
    }

    void Update()
    {
        if (!playing) return;
        float step = Mathf.Max(1f, EffectiveStepMs()) / 1000f;
        _accum += Time.deltaTime;
        if (_accum < step) return;
        _accum = 0f;
        if (syncMode == PixelLightSyncMode.BeatQuantized && beatBinder != null
            && beatBinder.QuantizeDelaySec() > 0.001f)
            return;
        frameIndex++;
        PushFrame();
    }

    float EffectiveStepMs()
    {
        float ms = pattern != null ? pattern.stepMs : stepMs;
        if (syncMode != PixelLightSyncMode.Free && ambiance != null)
        {
            beatBinder?.ApplyBpmFromSchedule(ambiance);
            if (beatBinder != null && beatBinder.bpm > 1f)
                ms = 60000f / beatBinder.bpm / Mathf.Max(1, beatBinder.subdivision);
        }
        return ms;
    }

    public void BindBeatBinder(BeatQuantizedActionBinder binder) => beatBinder = binder;
    public void BindAmbiance(MusicAmbianceSchedule schedule) => ambiance = schedule;

    public void SetPattern(PixelLightPatternAsset asset)
    {
        pattern = asset;
        if (pattern != null)
        {
            gridWidth = pattern.gridWidth;
            gridHeight = pattern.gridHeight;
            stepMs = pattern.stepMs;
        }
        EnsureLuminanceTexture();
        PushFrame();
    }

    public void SetSolidChannel(Color channelColor, bool on = true)
    {
        solidOverride = on ? channelColor : Color.clear;
        colorPackage = PixelLightColorPackage.CreateSignal(channelColor);
        var solid = PixelLightPatternAsset.CreateSolid(on ? '#' : ' ', gridWidth, gridHeight);
        SetPattern(solid);
        PushFrame();
    }

    public void SetEnabledEmission(bool on)
    {
        playing = on;
        if (!on && optic != null)
            optic.ApplyEmission(Color.black, 0f, 0f);
        else
            PushFrame();
    }

    void ApplyGridFromPattern()
    {
        if (pattern != null)
        {
            gridWidth = Mathf.Max(1, pattern.gridWidth);
            gridHeight = Mathf.Max(1, pattern.gridHeight);
            stepMs = pattern.stepMs;
        }
        EnsureLuminanceTexture();
        PushFrame();
    }

    void EnsureLuminanceTexture()
    {
        if (_lumRt != null && _lumRt.width == gridWidth && _lumRt.height == gridHeight)
            return;
        if (_lumRt != null)
            Destroy(_lumRt);
        _lumRt = new Texture2D(gridWidth, gridHeight, TextureFormat.RFloat, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "PixelLightLuminance"
        };
        if (optic != null)
            optic.luminanceTexture = _lumRt;
    }

    public void PushFrame()
    {
        EnsureLuminanceTexture();
        float[,] grid;
        if (pattern != null)
            grid = pattern.Evaluate(frameIndex);
        else
            grid = new float[gridHeight, gridWidth];

        _avgLum = 0f;
        int n = gridWidth * gridHeight;
        var pixels = new Color[n];
        for (int y = 0; y < gridHeight; y++)
        for (int x = 0; x < gridWidth; x++)
        {
            float v = grid[y, x] * masterBrightness01;
            _avgLum += v;
            pixels[y * gridWidth + x] = new Color(v, v, v, 1f);
        }
        _avgLum = n > 0 ? _avgLum / n : 0f;
        _lumRt.SetPixels(pixels);
        _lumRt.Apply(false);

        var pkg = colorPackage != null ? colorPackage : PixelLightColorPackage.CreateEmergencyRed();
        Color em = solidOverride.a > 0.01f ? solidOverride : pkg.emissionColor;
        optic?.ApplyEmission(em, pkg.emissionIntensity, _avgLum);
    }

    public float AverageLuminance01 => _avgLum;
}
