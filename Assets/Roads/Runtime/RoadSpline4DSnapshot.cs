using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roads
{
    [Serializable]
    public class RoadSpline4DSnapshot
    {
        public int seed;
        public float narrativeTime;
        public List<Vector3> controlPoints = new List<Vector3>();
        public float defaultWidth = 6f;
        public float gradeSlope;
        public AnimationCurve widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        public AnimationCurve gradeCurve = AnimationCurve.Constant(0f, 1f, 0f);
        public AnimationCurve bankingCurve = AnimationCurve.Constant(0f, 1f, 0f);
        public List<string> gatewayLeafBack = new List<string>();
        public List<string> gatewayLeafPause = new List<string>();
        public List<string> gatewayLeafForward = new List<string>();
        public string roadSegmentId;
    }
}
