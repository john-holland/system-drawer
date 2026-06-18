using System;
using UnityEngine;

namespace Planetary.TimeTravel
{
    [Serializable]
    sealed class WeatherTimeTravelFrameDto
    {
        public float narrativeTime;
        public float waterLevelDelta;
        public string sparseManifoldDiffBase64;
        public float plateStressSnapshot;
        public string atmosphereSnapshotJson;
        public int altitudeBandMask;
        public string roadWearSnapshotJson;
    }

    public static class WeatherTimeTravelFrameSerializer
    {
        public static string ToJson(WeatherTimeTravelFrame frame)
        {
            if (frame == null)
                return "";
            var dto = new WeatherTimeTravelFrameDto
            {
                narrativeTime = frame.narrativeTime,
                waterLevelDelta = frame.waterLevelDelta,
                sparseManifoldDiffBase64 = frame.sparseManifoldDiff != null && frame.sparseManifoldDiff.Length > 0
                    ? Convert.ToBase64String(frame.sparseManifoldDiff) : "",
                plateStressSnapshot = frame.plateStressSnapshot,
                atmosphereSnapshotJson = frame.atmosphereSnapshot != null ? JsonUtility.ToJson(frame.atmosphereSnapshot) : "",
                altitudeBandMask = frame.altitudeBandMask,
                roadWearSnapshotJson = frame.roadWearSnapshot != null ? JsonUtility.ToJson(frame.roadWearSnapshot) : ""
            };
            return JsonUtility.ToJson(dto);
        }

        public static WeatherTimeTravelFrame FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            var dto = JsonUtility.FromJson<WeatherTimeTravelFrameDto>(json);
            if (dto == null)
                return null;
            var frame = new WeatherTimeTravelFrame
            {
                narrativeTime = dto.narrativeTime,
                waterLevelDelta = dto.waterLevelDelta,
                plateStressSnapshot = dto.plateStressSnapshot,
                altitudeBandMask = dto.altitudeBandMask
            };
            if (!string.IsNullOrEmpty(dto.sparseManifoldDiffBase64))
                frame.sparseManifoldDiff = Convert.FromBase64String(dto.sparseManifoldDiffBase64);
            if (!string.IsNullOrEmpty(dto.atmosphereSnapshotJson))
                frame.atmosphereSnapshot = JsonUtility.FromJson<Planetary.Composition.AtmosphereRegressionProfile>(dto.atmosphereSnapshotJson);
            if (!string.IsNullOrEmpty(dto.roadWearSnapshotJson))
                frame.roadWearSnapshot = JsonUtility.FromJson<RoadWearSnapshotDto>(dto.roadWearSnapshotJson);
            return frame;
        }
    }
}
