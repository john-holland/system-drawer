using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>SG3D/SG4D runway strip packing — large parallel/diagonal vs small single-lane profiles.</summary>
[Serializable]
public sealed class AirportRunwaySgPackSettings
{
    public enum AirportScale
    {
        SmallSingle = 0,
        LargeHub = 1
    }

    public AirportScale scale = AirportScale.LargeHub;
    public int parallelStripCount = 14;
    public float diagonalAngleDeg = 45f;
    public int diagonalParallelCount = 8;
    public float stripPaddingMeters = 12f;
    public bool useUniformQueue = true;
    public List<string> slotPriority = new List<string>
    {
        "runway", "taxiway", "apron", "gate", "terminal", "security", "hangar"
    };

    /// <summary>Parse lemma fragment e.g. scale=large,strips=14,diagonal=45,diag_strips=8</summary>
    public static AirportRunwaySgPackSettings FromLemmaFragment(string fragment)
    {
        var s = new AirportRunwaySgPackSettings();
        if (string.IsNullOrEmpty(fragment)) return s;
        string[] parts = fragment.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            string p = parts[i].Trim();
            int eq = p.IndexOf('=');
            if (eq <= 0) continue;
            string key = p.Substring(0, eq).Trim().ToLowerInvariant();
            string val = p.Substring(eq + 1).Trim().ToLowerInvariant();
            switch (key)
            {
                case "scale":
                    s.scale = val.Contains("small") || val.Contains("single")
                        ? AirportScale.SmallSingle
                        : AirportScale.LargeHub;
                    break;
                case "strips":
                case "parallel":
                    if (int.TryParse(val, out int n)) s.parallelStripCount = n;
                    break;
                case "diagonal":
                case "angle":
                    if (float.TryParse(val, out float a)) s.diagonalAngleDeg = a;
                    break;
                case "diag_strips":
                    if (int.TryParse(val, out int d)) s.diagonalParallelCount = d;
                    break;
                case "pad":
                    if (float.TryParse(val, out float pad)) s.stripPaddingMeters = pad;
                    break;
            }
        }
        if (s.scale == AirportScale.SmallSingle)
        {
            s.parallelStripCount = 1;
            s.diagonalParallelCount = 0;
        }
        return s;
    }

    public string DescribeLemma()
    {
        if (scale == AirportScale.SmallSingle)
            return "small Canadian 1 lane landing strip; the Sesna was like a rain drop in an ocean of airliners";
        return $"{parallelStripCount} parallel landing strips with {diagonalAngleDeg:0} degree angle diagonal {diagonalParallelCount} parallel strips";
    }

    public void ApplyTo(Component spatialGenerator)
    {
        if (spatialGenerator == null) return;
        Type t = spatialGenerator.GetType();
        TrySetFloat(t, spatialGenerator, "padding", stripPaddingMeters);
        TrySetFloat(t, spatialGenerator, "bufferPadding", stripPaddingMeters);
        TrySetBool(t, spatialGenerator, "useBufferPadding", true);
        if (useUniformQueue)
            TrySetEnum(t, spatialGenerator, "placementStrategy", 1);
    }

    static void TrySetEnum(Type t, object target, string fieldName, int value)
    {
        FieldInfo f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || !f.FieldType.IsEnum) return;
        try { f.SetValue(target, Enum.ToObject(f.FieldType, value)); } catch { /* ignore */ }
    }

    static void TrySetFloat(Type t, object target, string fieldName, float value)
    {
        FieldInfo f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || f.FieldType != typeof(float)) return;
        f.SetValue(target, value);
    }

    static void TrySetBool(Type t, object target, string fieldName, bool value)
    {
        FieldInfo f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || f.FieldType != typeof(bool)) return;
        f.SetValue(target, value);
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Airport/Airport Runway SG Generator")]
public sealed class AirportRunwaySgGenerator : MonoBehaviour
{
    public AirportRunwaySgPackSettings settings = new AirportRunwaySgPackSettings();
    public Component spatialGenerator3D;
    public Component spatialGenerator4D;
    public string lemmaPackFragment;

    public void ApplySettings()
    {
        if (!string.IsNullOrEmpty(lemmaPackFragment))
            settings = AirportRunwaySgPackSettings.FromLemmaFragment(lemmaPackFragment);
        if (settings == null)
            settings = new AirportRunwaySgPackSettings();
        settings.ApplyTo(spatialGenerator3D);
        settings.ApplyTo(spatialGenerator4D);
    }

    void Awake() => ApplySettings();
}
