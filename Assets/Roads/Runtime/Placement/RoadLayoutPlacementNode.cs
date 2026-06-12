using System.Collections.Generic;
using UnityEngine;

namespace Roads
{
    public enum RoadLayoutPlacementMode
    {
        PathSolved,
        HandAuthored,
        InferredFromPrompt
    }

    [AddComponentMenu("Roads/Placement/Road Layout Placement Node")]
    public class RoadLayoutPlacementNode : MonoBehaviour
    {
        public RoadLayoutPlacementMode placementMode = RoadLayoutPlacementMode.PathSolved;
        public Transform startAnchor;
        public Transform endAnchor;
        public bool usePathSolver = true;
        public bool use4DGatewayBlend = true;
        public List<Vector3> handPlacedControlPoints = new List<Vector3>();
        public RoadSpline3D roadSpline;
        public RoadSpline4D roadSpline4D;
    }
}
