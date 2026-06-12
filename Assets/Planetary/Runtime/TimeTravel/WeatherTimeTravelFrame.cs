using System;
using Planetary.Composition;
using UnityEngine;

namespace Planetary.TimeTravel
{
    [Serializable]
    public sealed class RoadWearSnapshotDto
    {
        public float[] flowArcLengths;
        public float[] flowIntensities;
        public float[] flowLateral;
        public bool debrisCached;
        public string roadSegmentId;
    }

    [Serializable]
    public sealed class WeatherTimeTravelFrame
    {
        public float narrativeTime;
        public float waterLevelDelta;
        public byte[] sparseManifoldDiff;
        public float plateStressSnapshot;
        public AtmosphereRegressionProfile atmosphereSnapshot;
        public int altitudeBandMask;
        public RoadWearSnapshotDto roadWearSnapshot;
    }
}
