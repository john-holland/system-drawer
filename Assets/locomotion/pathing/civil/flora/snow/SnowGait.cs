using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SnowPolarFrame
{
    public float time;
    public float radius = 0.4f;
    public float azimuthDeg;
    public float depth = 0.15f;
}

/// <summary>One stride in polar time. A run, a stampede, a snowshoe step, and a snow angel are all gaits.</summary>
[CreateAssetMenu(fileName = "SnowPolarGait", menuName = "Locomotion/Civil/Snow Polar Gait")]
public sealed class SnowPolarGait : ScriptableObject
{
    public string gaitId = "run";
    public SnowPolarFrame[] frames = Array.Empty<SnowPolarFrame>();
}

/// <summary>Azimuth and radius bins shared by every actor using the same gait.</summary>
public sealed class SnowRadialCache
{
    public int azimuthBins = 8;
    public int radiusBins = 4;
    public string[] bins = Array.Empty<string>();
    public int lastBin = -1;

    public int Store(SnowPolarGait gait)
    {
        if (gait == null)
            return -1;
        int count = Mathf.Max(1, azimuthBins * radiusBins);
        if (bins == null || bins.Length != count)
            bins = new string[count];
        int bin = Mathf.Abs(gait.gaitId != null ? gait.gaitId.GetHashCode() : 0) % count;
        bins[bin] = gait.gaitId;
        lastBin = bin;
        return bin;
    }

    public bool Reuses(SnowPolarGait gait)
    {
        int bin = Store(gait);
        return bin >= 0 && bins[bin] == gait.gaitId;
    }

    public int Stamp(SnowVolume volume, Vector3 origin, SnowPolarGait gait)
    {
        int bin = Store(gait);
        if (volume == null || gait == null || gait.frames == null)
            return bin;
        for (int i = 0; i < gait.frames.Length; i++)
        {
            SnowPolarFrame frame = gait.frames[i];
            if (frame == null)
                continue;
            float rad = frame.azimuthDeg * Mathf.Deg2Rad;
            Vector3 at = origin + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * frame.radius;
            volume.ApplyScoop(null, at, Mathf.Max(0.05f, frame.depth));
        }
        return bin;
    }
}

/// <summary>Tool sealed-skin tracks run before the ambulation cache.</summary>
public static class SnowStampSequence
{
    public static void Apply(SnowVolume volume, SdfSealedClothSkin tool, SnowRadialCache cache, SnowPolarGait gait, Vector3 origin, List<string> log)
    {
        if (tool != null && tool.fragments != null && volume != null)
        {
            log?.Add("tool");
            for (int i = 0; i < tool.fragments.Count; i++)
            {
                SdfSealedClothFragment fragment = tool.fragments[i];
                if (fragment == null)
                    continue;
                Vector3 at = volume.transform.TransformPoint(fragment.centroid);
                volume.ApplyScoop(null, at, 0.2f);
            }
        }
        if (cache != null && gait != null)
        {
            log?.Add("gait");
            cache.Stamp(volume, origin, gait);
        }
    }
}

public static class SnowPile
{
    public static float LipHeight(float s, float peak)
    {
        s = Mathf.Clamp01(s);
        peak = Mathf.Max(0f, peak);
        const float knee = 0.35f;
        if (s < knee)
        {
            float u = s / knee;
            return peak * (1f - Mathf.Exp(-4f * u));
        }
        float rise = peak * (1f - Mathf.Exp(-4f));
        float t = (s - knee) / (1f - knee);
        return Mathf.Lerp(rise, 0f, t);
    }

    public static int BeadCount(float edgeLength, float granularity)
    {
        return Mathf.Max(1, Mathf.RoundToInt(edgeLength / Mathf.Max(0.01f, granularity)));
    }
}

/// <summary>Near-field beads along a fresh subtraction lip. Rain, snow, and hail select the particle count.</summary>
public sealed class SnowNearfieldSph
{
    public List<Vector3> beads = new List<Vector3>();
    public float flowSpeed;

    public void BuildLip(Vector3 origin, Vector3 along, float edgeLength, float granularity, float peak, Weather.PrecipitationType type, float windSpeed)
    {
        beads.Clear();
        int n = BeadCountFor(type, SnowPile.BeadCount(edgeLength, granularity));
        Vector3 side = Vector3.Cross(Vector3.up, along.sqrMagnitude > 1e-6f ? along.normalized : Vector3.forward);
        if (side.sqrMagnitude < 1e-6f)
            side = Vector3.right;
        side.Normalize();
        for (int i = 0; i < n; i++)
        {
            float s = n == 1 ? 1f : i / (float)(n - 1);
            float h = SnowPile.LipHeight(s, peak);
            beads.Add(origin + along.normalized * (s * edgeLength) + Vector3.up * h + side * (h * 0.35f));
        }
        flowSpeed = Mathf.Max(0f, windSpeed);
    }

    public static int BeadCountFor(Weather.PrecipitationType type, int baseCount)
    {
        if (type == Weather.PrecipitationType.Hail)
            return Mathf.Max(1, baseCount / 2);
        if (type == Weather.PrecipitationType.Rain)
            return baseCount + 2;
        return baseCount;
    }

    public float FlowFactor(float referenceSpeed)
    {
        return Mathf.Clamp01(flowSpeed / Mathf.Max(0.01f, referenceSpeed));
    }
}
