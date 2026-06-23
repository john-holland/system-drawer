using System;
using UnityEngine;

namespace Locomotion.Camera
{
    [Serializable]
    public class CameraTopologySample
    {
        public string episodeId;
        public string shotId;
        public string focusMode;
        public float[] topologyVector;
        public float memorabilityMl;
        public float userRatingMean;
        public float actorVisionSalience;
        public CameraRigPose rigPose;
    }
}
