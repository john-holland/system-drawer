/// <summary>Synthetic vehicle track for editor iteration. Real detect is yolo26_vehicle@intel.</summary>
public static class LocalStubVehicleTrackDetector
{
    public const string DetectorId = "local-stub-vehicle";

    public static VehicleTrack Detect(string sourcePathOrUrl, string modelSpec)
    {
        _ = sourcePathOrUrl;
        return new VehicleTrack
        {
            modelSpec = string.IsNullOrEmpty(modelSpec) ? VehicleVideoSteeringIds.Yolo26IntelSpec : modelSpec,
            frames = new[]
            {
                Frame(0, 1, 2, "car", 0.2f, 0.45f),
                Frame(100, 1, 2, "car", 0.35f, 0.45f),
                Frame(200, 1, 2, "car", 0.5f, 0.45f),
                Frame(300, 1, 2, "car", 0.65f, 0.45f)
            },
            segments = new[]
            {
                new VehicleTrackSegment
                {
                    startMs = 0,
                    endMs = 300,
                    headingRad = 0f,
                    subjectTrackId = 1,
                    subjectClassId = 2
                }
            }
        };
    }

    static VehicleTrackFrame Frame(double tMs, int trackId, int classId, string name, float cx, float cy)
    {
        return new VehicleTrackFrame
        {
            tMs = tMs,
            trackId = trackId,
            classId = classId,
            className = name,
            conf = 0.9f,
            cx = cx,
            cy = cy,
            bbox = new VehicleTrackBBox
            {
                x1 = cx - 0.08f,
                y1 = cy - 0.06f,
                x2 = cx + 0.08f,
                y2 = cy + 0.06f
            }
        };
    }
}
