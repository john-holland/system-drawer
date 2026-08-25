using System;
using System.Collections.Generic;
using UnityEngine;

public enum PixelLightSyncMode
{
    Free = 0,
    BeatQuantized = 1,
    CompositionPhase = 2
}

public enum PixelLightLayerComposite
{
    Max = 0,
    Or = 1,
    Add = 2
}

[Serializable]
public sealed class PixelLightFrame
{
    [Tooltip("ASCII row glyphs, e.g. [( )|||] — space=off, other=on.")]
    public List<string> rows = new List<string>();
}

[Serializable]
public sealed class PixelLightLayer
{
    public string layerId = "main";
    public List<PixelLightFrame> frames = new List<PixelLightFrame>();
    [Range(0f, 1f)] public float weight01 = 1f;
}

[CreateAssetMenu(fileName = "PixelLightPattern", menuName = "Locomotion/Civil/Pixel Light Pattern")]
public sealed class PixelLightPatternAsset : ScriptableObject
{
    public int gridWidth = 8;
    public int gridHeight = 4;
    public float stepMs = 100f;
    public PixelLightLayerComposite composite = PixelLightLayerComposite.Max;
    public List<PixelLightLayer> layers = new List<PixelLightLayer>();

    public static PixelLightPatternAsset CreateChasePreset()
    {
        var a = CreateInstance<PixelLightPatternAsset>();
        a.gridWidth = 8;
        a.gridHeight = 4;
        a.stepMs = 80f;
        var layer = new PixelLightLayer { layerId = "chase" };
        string[] glyphs = { "[( )|||]", "[  ]", "[(   )]", "[|||||]", "[===]" };
        for (int g = 0; g < glyphs.Length; g++)
        {
            var frame = new PixelLightFrame();
            for (int y = 0; y < a.gridHeight; y++)
                frame.rows.Add(glyphs[g]);
            layer.frames.Add(frame);
        }
        a.layers.Add(layer);
        return a;
    }

    public static PixelLightPatternAsset CreateSolid(char fill, int w = 8, int h = 4)
    {
        var a = CreateInstance<PixelLightPatternAsset>();
        a.gridWidth = w;
        a.gridHeight = h;
        a.stepMs = 1000f;
        var layer = new PixelLightLayer { layerId = "solid" };
        var frame = new PixelLightFrame();
        string row = new string(fill, w);
        for (int y = 0; y < h; y++)
            frame.rows.Add(row);
        layer.frames.Add(frame);
        a.layers.Add(layer);
        return a;
    }

    public static PixelLightPatternAsset CreateWigWagPreset(bool leftOnFirst = true)
    {
        var a = CreateInstance<PixelLightPatternAsset>();
        a.gridWidth = 16;
        a.gridHeight = 2;
        a.stepMs = 250f;
        var layer = new PixelLightLayer { layerId = "wigwag" };
        string left = new string('#', 8) + new string(' ', 8);
        string right = new string(' ', 8) + new string('#', 8);
        var f0 = new PixelLightFrame();
        var f1 = new PixelLightFrame();
        for (int y = 0; y < 2; y++)
        {
            f0.rows.Add(leftOnFirst ? left : right);
            f1.rows.Add(leftOnFirst ? right : left);
        }
        layer.frames.Add(f0);
        layer.frames.Add(f1);
        a.layers.Add(layer);
        return a;
    }

    public static PixelLightPatternAsset CreateSplitChasePreset()
    {
        var a = CreateChasePreset();
        a.gridWidth = 16;
        a.gridHeight = 2;
        return a;
    }

    public static PixelLightPatternAsset CreateSteadyBurnPreset() => CreateSolid('#', 16, 2);

    public float[,] Evaluate(int frameIndex)
    {
        var grid = new float[gridHeight, gridWidth];
        if (layers == null) return grid;
        for (int li = 0; li < layers.Count; li++)
        {
            var layer = layers[li];
            if (layer?.frames == null || layer.frames.Count == 0) continue;
            int fi = ((frameIndex % layer.frames.Count) + layer.frames.Count) % layer.frames.Count;
            var frame = layer.frames[fi];
            if (frame?.rows == null) continue;
            for (int y = 0; y < gridHeight; y++)
            {
                string row = y < frame.rows.Count ? frame.rows[y] : "";
                for (int x = 0; x < gridWidth; x++)
                {
                    float cell = 0f;
                    if (x < row.Length)
                    {
                        char c = row[x];
                        cell = c == ' ' || c == '.' || c == '_' ? 0f : 1f;
                    }
                    cell *= layer.weight01;
                    switch (composite)
                    {
                        case PixelLightLayerComposite.Or:
                            grid[y, x] = Mathf.Max(grid[y, x], cell > 0.5f ? 1f : 0f);
                            break;
                        case PixelLightLayerComposite.Add:
                            grid[y, x] = Mathf.Clamp01(grid[y, x] + cell);
                            break;
                        default:
                            grid[y, x] = Mathf.Max(grid[y, x], cell);
                            break;
                    }
                }
            }
        }
        return grid;
    }
}
