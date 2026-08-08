using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>Bin-pack / SG3D+SG4D settings for bus station space solve (lemma or bespoke).</summary>
[Serializable]
public sealed class BusStationSgPackSettings
{
    public enum PackDimension
    {
        TwoDimensional = 0,
        ThreeDimensional = 1
    }

    public enum PackPlacement
    {
        Immediate = 0,
        UniformQueue = 1
    }

    public PackDimension dimension = PackDimension.ThreeDimensional;
    public PackPlacement placement = PackPlacement.UniformQueue;
    public float paddingMeters = 0.35f;
    public bool useBufferPadding = true;
    public float schedulePaddingSeconds = 30f;
    [Tooltip("Priority slot ids: waiting, platform, bay, cafeteria, bathroom, parking, trash, telecom")]
    public List<string> slotPriority = new List<string>
    {
        "platform", "waiting", "bay", "telecom", "cafeteria", "bathroom", "parking", "trash"
    };

    /// <summary>Parse developer lemma fragment e.g. pack=3d,placement=uniform,pad=0.4</summary>
    public static BusStationSgPackSettings FromLemmaFragment(string fragment)
    {
        var s = new BusStationSgPackSettings();
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
                case "pack":
                case "dim":
                case "dimension":
                    s.dimension = val.Contains("2") || val.Contains("quad")
                        ? PackDimension.TwoDimensional
                        : PackDimension.ThreeDimensional;
                    break;
                case "placement":
                case "place":
                    s.placement = val.Contains("immediate") || val == "dfs"
                        ? PackPlacement.Immediate
                        : PackPlacement.UniformQueue;
                    break;
                case "pad":
                case "padding":
                    if (float.TryParse(val, out float pad))
                        s.paddingMeters = pad;
                    break;
                case "schedulepad":
                    if (float.TryParse(val, out float sp))
                        s.schedulePaddingSeconds = sp;
                    break;
            }
        }
        return s;
    }

    /// <summary>Apply to a SpatialGenerator / SpatialGenerator4D component without hard-asmdef coupling.</summary>
    public void ApplyTo(Component spatialGenerator)
    {
        if (spatialGenerator == null) return;
        Type t = spatialGenerator.GetType();
        TrySetEnum(t, spatialGenerator, "generationMode", (int)dimension);
        TrySetEnum(t, spatialGenerator, "placementStrategy", (int)placement);
        TrySetFloat(t, spatialGenerator, "padding", paddingMeters);
        TrySetFloat(t, spatialGenerator, "bufferPadding", paddingMeters);
        TrySetBool(t, spatialGenerator, "useBufferPadding", useBufferPadding);
        TrySetFloat(t, spatialGenerator, "schedulePadding", schedulePaddingSeconds);
        TrySetFloat(t, spatialGenerator, "scheduleBuffer", schedulePaddingSeconds);
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

/// <summary>Applies <see cref="BusStationSgPackSettings"/> to assigned SG3D/SG4D hosts.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Transit/Bus Station SG Generator")]
public sealed class BusStationSgGenerator : MonoBehaviour
{
    public BusStationSgPackSettings settings = new BusStationSgPackSettings();
    public Component spatialGenerator3D;
    public Component spatialGenerator4D;
    [Tooltip("Optional lemma fragment: pack=3d,placement=uniform,pad=0.35")]
    public string lemmaPackFragment;

    public void ApplySettings()
    {
        if (!string.IsNullOrEmpty(lemmaPackFragment))
            settings = BusStationSgPackSettings.FromLemmaFragment(lemmaPackFragment);
        if (settings == null)
            settings = new BusStationSgPackSettings();
        settings.ApplyTo(spatialGenerator3D);
        settings.ApplyTo(spatialGenerator4D);
    }

    void Awake() => ApplySettings();
}
