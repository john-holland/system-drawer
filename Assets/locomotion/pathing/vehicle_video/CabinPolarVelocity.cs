using System;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class CabinPolarFrame
{
    public double tMs;
    public float radialExpand;
    public float azimuthalYaw;
    public float speedHint;
    public float yawRateHint;
}

/// <summary>Windshield polar VO JSON from cabin_composite hop (Unity JsonUtility).</summary>
[Serializable]
public sealed class CabinPolarVelocity
{
    public string modelSpec = "";
    public CabinPolarFrame[] frames = Array.Empty<CabinPolarFrame>();

    public int FrameCount => frames != null ? frames.Length : 0;

    public static CabinPolarVelocity FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new CabinPolarVelocity();
        try
        {
            var t = JsonUtility.FromJson<CabinPolarVelocity>(json);
            return t ?? new CabinPolarVelocity();
        }
        catch
        {
            return new CabinPolarVelocity();
        }
    }

    public string ToJson() => JsonUtility.ToJson(this);

    public static CabinPolarVelocity TryLoad(string pathOrUrl)
    {
        if (string.IsNullOrEmpty(pathOrUrl))
            return null;
        string path = pathOrUrl;
        if (path.StartsWith("file://"))
            path = path.Substring(7);
        if (!File.Exists(path))
        {
            if (File.Exists(path + ".polar.json"))
                path += ".polar.json";
            else if (File.Exists(Path.ChangeExtension(path, ".polar.json")))
                path = Path.ChangeExtension(path, ".polar.json");
            else
                return null;
        }
        return FromJson(File.ReadAllText(path));
    }

    public CabinPolarFrame FrameAt(double tMs)
    {
        if (frames == null || frames.Length == 0)
            return null;
        CabinPolarFrame best = frames[0];
        double bestD = Math.Abs(best.tMs - tMs);
        for (int i = 1; i < frames.Length; i++)
        {
            double d = Math.Abs(frames[i].tMs - tMs);
            if (d < bestD)
            {
                bestD = d;
                best = frames[i];
            }
        }
        return best;
    }

    public float AccelAt(double tMs)
    {
        if (frames == null || frames.Length < 2)
            return 0f;
        CabinPolarFrame a = null;
        CabinPolarFrame b = null;
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i].tMs <= tMs)
                a = frames[i];
            if (frames[i].tMs >= tMs && b == null)
                b = frames[i];
        }
        if (a == null)
            a = frames[0];
        if (b == null)
            b = frames[frames.Length - 1];
        if (ReferenceEquals(a, b))
        {
            int idx = Array.IndexOf(frames, a);
            if (idx > 0)
                a = frames[idx - 1];
            else if (idx + 1 < frames.Length)
                b = frames[idx + 1];
            else
                return 0f;
        }
        float dt = (float)((b.tMs - a.tMs) / 1000.0);
        if (Mathf.Abs(dt) < 1e-4f)
            return 0f;
        return (b.speedHint - a.speedHint) / dt;
    }

    /// <summary>First-frame chassis seed along camera/vehicle forward.</summary>
    public DimensionalPositionalSlot ToSeedSlot(Vector3 forwardWorld, Vector3 upWorld)
    {
        var slot = new DimensionalPositionalSlot { hasVelocity = true };
        if (frames == null || frames.Length == 0)
            return slot;
        CabinPolarFrame f = frames[0];
        Vector3 fwd = forwardWorld.sqrMagnitude > 1e-6f ? forwardWorld.normalized : Vector3.forward;
        Vector3 up = upWorld.sqrMagnitude > 1e-6f ? upWorld.normalized : Vector3.up;
        slot.linearVelocity = fwd * f.speedHint;
        slot.angularVelocity = up * f.yawRateHint;
        return slot;
    }

    public static CabinPolarVelocity Stub(string modelSpec = "cabin_polar@v1")
    {
        return new CabinPolarVelocity
        {
            modelSpec = modelSpec,
            frames = new[]
            {
                new CabinPolarFrame { tMs = 0, radialExpand = 0.4f, speedHint = 5f, yawRateHint = 0f },
                new CabinPolarFrame { tMs = 200, radialExpand = 0.5f, speedHint = 6f, yawRateHint = 0.05f },
                new CabinPolarFrame { tMs = 400, radialExpand = 0.45f, speedHint = 5.5f, yawRateHint = 0.02f }
            }
        };
    }
}
