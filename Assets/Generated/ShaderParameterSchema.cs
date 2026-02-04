using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines common shader features and map slots so the LM prompt and dependency list align.
/// </summary>
[CreateAssetMenu(fileName = "ShaderParameterSchema", menuName = "Generated/Shader Parameter Schema", order = 51)]
public class ShaderParameterSchema : ScriptableObject
{
    public enum CoordSpace
    {
        UV,
        World,
        Object,
        Screen
    }

    [Serializable]
    public class MapSlot
    {
        [Tooltip("Slot id (e.g. albedo, normal, specular, displacement).")]
        public string id = "";
        [Tooltip("Suggested shader property name (e.g. _MainTex, _BumpMap).")]
        public string propertyName = "";
        [Tooltip("If true, dependency list should include this slot when material type uses it.")]
        public bool required;
    }

    [Header("Coord space")]
    [Tooltip("Coordinate space to use (included in prompt for LM).")]
    public CoordSpace coordSpace = CoordSpace.UV;

    [Header("Specular")]
    public bool specularEnabled = true;
    [Tooltip("Slot id for specular map.")]
    public string specularMapSlotId = "specular";

    [Header("Displacement")]
    [Tooltip("Slot id for displacement map.")]
    public string displacementMapSlotId = "displacement";
    [Tooltip("Scale parameter name in shader.")]
    public string displacementScalePropertyName = "_DisplacementScale";

    [Header("Map slots")]
    [Tooltip("Ordered list of map slots (albedo, normal, specular, etc.).")]
    public List<MapSlot> mapSlots = new List<MapSlot>();

    /// <summary>Get slot by id.</summary>
    public MapSlot GetSlot(string slotId)
    {
        if (mapSlots == null || string.IsNullOrEmpty(slotId)) return null;
        foreach (var s in mapSlots)
            if (string.Equals(s.id, slotId, StringComparison.OrdinalIgnoreCase))
                return s;
        return null;
    }

    /// <summary>Build a short spec string for the LM (coord space, which maps to include).</summary>
    public string ToPromptSpec()
    {
        var parts = new List<string> { "Coord space: " + coordSpace.ToString() };
        if (specularEnabled) parts.Add("Specular: on, slot " + specularMapSlotId);
        parts.Add("Displacement slot: " + displacementMapSlotId + ", scale: " + displacementScalePropertyName);
        if (mapSlots != null && mapSlots.Count > 0)
        {
            var slotIds = new List<string>();
            foreach (var s in mapSlots)
                if (!string.IsNullOrEmpty(s.id))
                    slotIds.Add(s.id + (s.required ? "(required)" : ""));
            parts.Add("Map slots: " + string.Join(", ", slotIds));
        }
        return "Schema: " + string.Join("; ", parts);
    }

    /// <summary>Return dependency slot ids for the given material type (e.g. Lit -> albedo, normal, specular).</summary>
    public List<string> GetDependencySlotIdsForMaterialType(string materialType)
    {
        var list = new List<string>();
        var slots = GetEffectiveMapSlots();
        bool isLit = !string.IsNullOrEmpty(materialType) && materialType.IndexOf("Lit", StringComparison.OrdinalIgnoreCase) >= 0;
        foreach (var s in slots)
        {
            if (string.IsNullOrEmpty(s.id)) continue;
            if (s.required) list.Add(s.id);
            else if (isLit && (s.id == "albedo" || s.id == "normal" || s.id == specularMapSlotId))
                list.Add(s.id);
        }
        if (list.Count == 0 && slots.Count > 0 && !string.IsNullOrEmpty(slots[0].id))
            list.Add(slots[0].id);
        return list;
    }

    /// <summary>Map slots to use; returns default slots when empty.</summary>
    private List<MapSlot> GetEffectiveMapSlots()
    {
        if (mapSlots != null && mapSlots.Count > 0) return mapSlots;
        return GetDefaultMapSlots();
    }

    private static List<MapSlot> GetDefaultMapSlots()
    {
        return new List<MapSlot>
        {
            new MapSlot { id = "albedo", propertyName = "_MainTex", required = true },
            new MapSlot { id = "normal", propertyName = "_BumpMap", required = false },
            new MapSlot { id = "specular", propertyName = "_SpecGlossMap", required = false },
            new MapSlot { id = "displacement", propertyName = "_DisplacementMap", required = false },
            new MapSlot { id = "emission", propertyName = "_EmissionMap", required = false },
            new MapSlot { id = "occlusion", propertyName = "_OcclusionMap", required = false }
        };
    }
}
