using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>JsonUtility-friendly DTO for formation JSON files (wrapper object, not a bare array root).</summary>
[Serializable]
public class TravelFormationJsonV1
{
    public int version = 1;
    public TravelFormationJsonSlot[] slots;
}

[Serializable]
public class TravelFormationJsonSlot
{
    public float x;
    public float y;
    public float z;
}

/// <summary>Loads <see cref="TravelFormationJsonV1"/> from JSON strings or <see cref="TextAsset"/>.</summary>
public static class TravelFormationJsonLoader
{
    /// <summary>Parses JSON into slot local offsets. Returns false if JSON invalid or no slots.</summary>
    public static bool TryParseSlots(string json, out List<Vector3> localOffsets, out string error)
    {
        localOffsets = null;
        error = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Empty JSON.";
            return false;
        }

        try
        {
            var dto = JsonUtility.FromJson<TravelFormationJsonV1>(json);
            if (dto == null || dto.slots == null || dto.slots.Length == 0)
            {
                error = "Missing or empty 'slots' array.";
                return false;
            }

            localOffsets = new List<Vector3>(dto.slots.Length);
            for (int i = 0; i < dto.slots.Length; i++)
            {
                var s = dto.slots[i];
                localOffsets.Add(new Vector3(s.x, s.y, s.z));
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryParseSlots(TextAsset textAsset, out List<Vector3> localOffsets, out string error)
    {
        if (textAsset == null)
        {
            error = "TextAsset is null.";
            localOffsets = null;
            return false;
        }
        return TryParseSlots(textAsset.text, out localOffsets, out error);
    }

    /// <summary>Replaces <paramref name="asset"/> slot list from JSON (Editor or runtime).</summary>
    public static bool TryApplyToAsset(TravelFormationAsset asset, string json, out string error)
    {
        error = null;
        if (asset == null)
        {
            error = "Asset is null.";
            return false;
        }
        if (!TryParseSlots(json, out var list, out error))
            return false;
        asset.slots ??= new List<TravelFormationSlot>();
        asset.slots.Clear();
        for (int i = 0; i < list.Count; i++)
            asset.slots.Add(new TravelFormationSlot { localOffset = list[i] });
        return true;
    }
}
