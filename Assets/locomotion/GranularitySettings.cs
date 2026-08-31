using System;
using UnityEngine;

/// <summary>
/// Spatial granulation for PixelLight / Modly / VoxelRagdollActor.
/// Distinct from webcam timeline ticks (<c>WebcamAnimTimelineGranularity</c>).
/// </summary>
[Serializable]
public sealed class GranularitySettings
{
    public const string PresetMinecraft = "minecraft";
    public const string PresetContinuuuum = "continuuuum";
    public const string PresetCustom = "custom";

    public string preset = PresetMinecraft;
    public int pixelGrid = 16;
    public float blockMeters = 1f;
    public int texelsPerMeter = 16;
    public float voxelCell = 1f / 16f;
    public string skinLayout = "64x64";
    public int maxBones = 33;
    public bool snapToGrid = true;

    /// <summary>Derived inverse of <see cref="blockMeters"/>. Minecraft default is 1 block per meter.</summary>
    public float BlocksPerMeter
    {
        get => blockMeters <= 0f ? 1f : 1f / blockMeters;
        set
        {
            if (value <= 0f)
                return;
            blockMeters = 1f / value;
        }
    }

    /// <summary>Steve-like MediaPipe Holistic / Human:* cap used by the Minecraft preset.</summary>
    public static readonly string[] MinecraftHumanoidTraits =
    {
        "Human:Hips", "Human:Spine", "Human:Chest", "Human:Neck", "Human:Head",
        "Human:LeftShoulder", "Human:RightShoulder", "Human:LeftUpperArm", "Human:RightUpperArm",
        "Human:LeftLowerArm", "Human:RightLowerArm", "Human:LeftHand", "Human:RightHand",
        "Human:LeftUpperLeg", "Human:RightUpperLeg", "Human:LeftLowerLeg", "Human:RightLowerLeg",
        "Human:LeftFoot", "Human:RightFoot", "Human:LeftToes", "Human:RightToes",
        "Human:LeftThumb", "Human:RightThumb", "Human:LeftIndex", "Human:RightIndex",
        "Human:LeftPinky", "Human:RightPinky", "Human:Jaw", "Human:LeftEye", "Human:RightEye",
        "Human:LeftEar", "Human:RightEar", "Human:Nose"
    };

    public static GranularitySettings Minecraft()
    {
        return new GranularitySettings();
    }

    /// <summary>1 m world unit, uncapped bones, snap off until voxel-ragdoll is enabled.</summary>
    public static GranularitySettings Continuuuum()
    {
        return new GranularitySettings
        {
            preset = PresetContinuuuum,
            pixelGrid = 16,
            blockMeters = 1f,
            texelsPerMeter = 16,
            voxelCell = 1f / 16f,
            skinLayout = "custom",
            maxBones = 256,
            snapToGrid = false
        };
    }

    public float VoxelCellMeters => voxelCell * blockMeters;

    public Vector3 SnapWorld(Vector3 p)
    {
        if (!snapToGrid)
            return p;
        float cell = VoxelCellMeters;
        if (cell <= 0f)
            return p;
        return new Vector3(
            Mathf.Round(p.x / cell) * cell,
            Mathf.Round(p.y / cell) * cell,
            Mathf.Round(p.z / cell) * cell);
    }

    public void MarkCustomIfEdited()
    {
        if (Matches(Minecraft()))
            preset = PresetMinecraft;
        else if (Matches(Continuuuum()))
            preset = PresetContinuuuum;
        else
            preset = PresetCustom;
    }

    bool Matches(GranularitySettings other)
    {
        return pixelGrid == other.pixelGrid
               && Mathf.Approximately(blockMeters, other.blockMeters)
               && texelsPerMeter == other.texelsPerMeter
               && Mathf.Approximately(voxelCell, other.voxelCell)
               && skinLayout == other.skinLayout
               && maxBones == other.maxBones
               && snapToGrid == other.snapToGrid;
    }
}
