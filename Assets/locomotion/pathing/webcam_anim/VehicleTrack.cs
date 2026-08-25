using System;
using System.IO;
using UnityEngine;

/// <summary>Stable ids for the video→YOLO26→steering BT pipeline.</summary>
public static class VehicleVideoSteeringIds
{
    public const string Yolo26IntelSpec = "yolo26_vehicle@intel";
    public const string CabinCompositeSpec = "cabin_composite@v1";
}

[Serializable]
public sealed class VehicleTrackBBox
{
    public float x1;
    public float y1;
    public float x2;
    public float y2;

    public float Width => Mathf.Max(0f, x2 - x1);
    public float Height => Mathf.Max(0f, y2 - y1);
    public float Area => Width * Height;
    public Vector2 Centroid => new Vector2((x1 + x2) * 0.5f, (y1 + y2) * 0.5f);
}

[Serializable]
public sealed class VehicleTrackFrame
{
    public double tMs;
    public int trackId;
    public int classId;
    public string className;
    public float conf;
    public VehicleTrackBBox bbox = new VehicleTrackBBox();
    public float cx;
    public float cy;
    public int laneIndex = -1;
}

[Serializable]
public sealed class VehicleTrackSegment
{
    public double startMs;
    public double endMs;
    public float headingRad;
    public int subjectTrackId;
    public int subjectClassId;
    public bool hasFacingYawOverride;
    public float facingYawOverride;
    public int laneIndex = -1;
}

/// <summary>JSON from the yolo26_vehicle@intel hop (Unity JsonUtility).</summary>
[Serializable]
public sealed class VehicleTrack
{
    public string modelSpec = "";
    public VehicleTrackFrame[] frames = Array.Empty<VehicleTrackFrame>();
    public VehicleTrackSegment[] segments = Array.Empty<VehicleTrackSegment>();

    public int FrameCount => frames != null ? frames.Length : 0;

    public static VehicleTrack FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new VehicleTrack();
        try
        {
            var t = JsonUtility.FromJson<VehicleTrack>(json);
            return t ?? new VehicleTrack();
        }
        catch
        {
            return new VehicleTrack();
        }
    }

    public string ToJson() => JsonUtility.ToJson(this);

    public static VehicleTrack TryLoad(string pathOrUrl)
    {
        if (string.IsNullOrEmpty(pathOrUrl))
            return null;
        string path = pathOrUrl;
        if (path.StartsWith("file://"))
            path = path.Substring(7);
        if (!File.Exists(path))
        {
            if (File.Exists(path + ".vehicletrack.json"))
                path += ".vehicletrack.json";
            else if (File.Exists(Path.ChangeExtension(path, ".vehicletrack.json")))
                path = Path.ChangeExtension(path, ".vehicletrack.json");
            else
                return null;
        }
        return FromJson(File.ReadAllText(path));
    }
}
