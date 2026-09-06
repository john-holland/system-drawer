using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>Bin-pack / SG settings for garage door carpentry (lemma or bespoke).</summary>
[Serializable]
public sealed class GarageDoorSgPackSettings
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
    public float paddingMeters = 0.04f;
    public int mouldingSides = 4;
    public string lemmaId = DoorCarpentryLemmaPropertyKeys.LemmaPackPanels;
    public List<string> slotPriority = new List<string>
    {
        DoorCarpentryLemmaPropertyKeys.LemmaPlaceStile,
        DoorCarpentryLemmaPropertyKeys.LemmaPlaceRail,
        DoorCarpentryLemmaPropertyKeys.LemmaPackPanels,
        DoorCarpentryLemmaPropertyKeys.LemmaWrapMoulding
    };

    public static GarageDoorSgPackSettings FromLemmaFragment(string fragment)
    {
        var s = new GarageDoorSgPackSettings();
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
                case "sides":
                case "moulding-sides":
                    if (int.TryParse(val, out int sides))
                        s.mouldingSides = Mathf.Max(3, sides);
                    break;
                case "lemma":
                case "op":
                    s.lemmaId = val;
                    break;
            }
        }
        return s;
    }

    public void ApplyTo(Component spatialGenerator)
    {
        if (spatialGenerator == null) return;
        Type t = spatialGenerator.GetType();
        TrySetEnum(t, spatialGenerator, "mode", (int)dimension);
        TrySetEnum(t, spatialGenerator, "generationMode", (int)dimension);
        TrySetEnum(t, spatialGenerator, "placementStrategy", (int)placement);
        TrySetFloat(t, spatialGenerator, "padding", paddingMeters);
        TrySetFloat(t, spatialGenerator, "bufferPadding", paddingMeters);
    }

    public void ApplyToDoor(DoorAssemblySpec door)
    {
        if (door == null) return;
        door.mouldingSides = Mathf.Max(3, mouldingSides);
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
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Housing/Garage Door SG Pack")]
public sealed class GarageDoorSgPackHost : MonoBehaviour
{
    public GarageDoorSgPackSettings settings = new GarageDoorSgPackSettings();
    public DoorAssemblySpec door;
    public Component spatialGenerator;
    public string lemmaPackFragment;

    public void ApplySettings()
    {
        if (!string.IsNullOrEmpty(lemmaPackFragment))
            settings = GarageDoorSgPackSettings.FromLemmaFragment(lemmaPackFragment);
        settings ??= new GarageDoorSgPackSettings();
        settings.ApplyTo(spatialGenerator);
        settings.ApplyToDoor(door);
    }

    void Awake() => ApplySettings();
}
