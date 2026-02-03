using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Locomotion.Narrative;

/// <summary>
/// One causality trigger event: (position, t) entered a narrative volume. Used when collectCausalityEvents is true.
/// </summary>
[Serializable]
public class CausalityTriggerTrippedDto
{
    public float gameTime;       // narrative seconds
    public float px, py, pz;     // position when tripped
    public string spatialNodeId; // marker name or instance id
    public string treeNodeId;    // optional narrative tree node id
    public string sequenceId;    // optional sequence index or id
    public string payloadLabel;  // optional payload from 4D volume (e.g. "Start", "Stop")
}

/// <summary>
/// DTO for serializing 4D spatial expressions to a flat file (JSON, YAML, or XML).
/// Used by the in-game Spatial 4D editor to save markers, start/stop, tool uses, and timeline bounds.
/// </summary>
[Serializable]
public class Spatial4DExpressionsDto
{
    public int schemaVersion = 1;
    public List<Spatial4DExpressionEntryDto> entries = new List<Spatial4DExpressionEntryDto>();
    /// <summary>Optional timeline end (narrative seconds). When set, scrubber can extend past generator tMax.</summary>
    public float? timelineEndT;
}

/// <summary>
/// Spacetime location ref: (position, t). Used when "use tool on location" means query what's there at that time.
/// </summary>
[Serializable]
public class SpacetimeLocationRefDto
{
    public float x, y, z;
    public float t;

    [JsonIgnore]
    public Vector3 Position => new Vector3(x, y, z);
    public static SpacetimeLocationRefDto From(Vector3 position, float t)
    {
        return new SpacetimeLocationRefDto { x = position.x, y = position.y, z = position.z, t = t };
    }
}

/// <summary>
/// One expression entry; kind discriminated by type string.
/// </summary>
[Serializable]
public class Spatial4DExpressionEntryDto
{
    public string type; // "Marker", "MarkedGameObject", "Start", "Stop", "ToolUse"
    public string id;
    public string label;

    // Position + time (for Marker, Start, Stop, SetLocation)
    public float px, py, pz;
    public float t; // narrative seconds
    public string dateTimeString; // optional display e.g. "2025-01-01 09:00:00Z"

    // Optional Bounds4 (size/duration) for Marker
    public float? sizeX, sizeY, sizeZ, tMin, tMax;

    // MarkedGameObject: scene path or instance id for file
    public string scenePath;
    public int instanceId;

    // ToolUse: tool and target refs (path or spacetime link)
    public string toolScenePath;
    public int toolInstanceId;
    public string targetScenePath;
    public int targetInstanceId;
    public bool targetIsSpacetimeLocation; // when true, use targetPosition + targetT instead of targetScenePath
    public float targetPx, targetPy, targetPz, targetT;

    [JsonIgnore]
    public Vector3 Position
    {
        get => new Vector3(px, py, pz);
        set { px = value.x; py = value.y; pz = value.z; }
    }

    [JsonIgnore]
    public Bounds4? Bounds4Value
    {
        get
        {
            if (!sizeX.HasValue || !sizeY.HasValue || !sizeZ.HasValue || !tMin.HasValue || !tMax.HasValue)
                return null;
            return new Bounds4(
                new Vector3(px, py, pz),
                new Vector3(sizeX.Value, sizeY.Value, sizeZ.Value),
                tMin.Value,
                tMax.Value
            );
        }
        set
        {
            if (!value.HasValue) return;
            var b = value.Value;
            px = b.center.x; py = b.center.y; pz = b.center.z;
            sizeX = b.size.x; sizeY = b.size.y; sizeZ = b.size.z;
            tMin = b.tMin; tMax = b.tMax;
        }
    }
}
