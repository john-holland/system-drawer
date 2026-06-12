using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

namespace Roads
{
    [AddComponentMenu("Roads/Placement/Road Layout Placement Solver")]
    public class RoadLayoutPlacementSolver : MonoBehaviour, IRoadLayoutPlacer
    {
        public HierarchicalPathingSolver pathingSolver;
        public RoadSpline3D roadSpline;
        public RoadSpline4D roadSpline4D;
        public RoadMeshBaker meshBaker;
        public bool restrictToExistingCorridors;
        public float greenfieldGoalDistance = 40f;

        public bool use4DGatewayBlend = true;

        public bool TryPlaceRoad(LayoutPlacementInstruction instruction, out string roadSegmentId)
        {
            roadSegmentId = null;
            if (!PathReplacementGate.CanReplacePath())
                return false;

            if (roadSpline == null)
                roadSpline = GetComponent<RoadSpline3D>();
            var node = GetComponent<RoadLayoutPlacementNode>();
            if (node != null && node.placementMode == RoadLayoutPlacementMode.HandAuthored && node.handPlacedControlPoints.Count >= 2)
                return PlaceHandAuthored(node, out roadSegmentId);

            if (pathingSolver == null)
                pathingSolver = FindAnyObjectByType<HierarchicalPathingSolver>();

            Vector3 start = instruction.startWorld;
            Vector3 goal = instruction.goalWorld;
            if (goal.sqrMagnitude < 1f)
                goal = start + Vector3.forward * greenfieldGoalDistance;

            var waypoints = SolvePath(start, goal);
            if (waypoints == null || waypoints.Count < 2)
                return false;

            if (roadSpline == null)
                roadSpline = gameObject.AddComponent<RoadSpline3D>();

            roadSpline.controlPoints = new List<Vector3>(waypoints);
            roadSpline.roadSegmentId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            roadSpline.conformToTerrain = true;
            roadSpline.RebuildBakedSamples(2f);

            if (use4DGatewayBlend && roadSpline4D != null)
            {
                var snap = roadSpline4D.ExportSnapshot();
                roadSpline.ApplySnapshot(snap);
            }

            roadSegmentId = roadSpline.roadSegmentId;
            if (meshBaker == null)
                meshBaker = GetComponent<RoadMeshBaker>();
            meshBaker?.Bake();
            return true;
        }

        bool PlaceHandAuthored(RoadLayoutPlacementNode node, out string roadSegmentId)
        {
            roadSegmentId = null;
            if (roadSpline == null)
                roadSpline = GetComponent<RoadSpline3D>() ?? gameObject.AddComponent<RoadSpline3D>();
            roadSpline.controlPoints = new List<Vector3>(node.handPlacedControlPoints);
            roadSpline.roadSegmentId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            roadSegmentId = roadSpline.roadSegmentId;
            meshBaker?.Bake();
            return true;
        }

        List<Vector3> SolvePath(Vector3 start, Vector3 goal)
        {
            if (pathingSolver == null)
                return new List<Vector3> { start, goal };

            var saved = pathingSolver.pathingMode;
            var savedRestrict = pathingSolver.restrictDriveToRoadCorridors;
            try
            {
                pathingSolver.pathingMode = PathingMode.Drive;
                pathingSolver.restrictDriveToRoadCorridors = restrictToExistingCorridors;
                pathingSolver.RebuildGrid();
                var path = pathingSolver.FindPath(start, goal);
                return path != null && path.Count >= 2 ? path : new List<Vector3> { start, goal };
            }
            finally
            {
                pathingSolver.pathingMode = saved;
                pathingSolver.restrictDriveToRoadCorridors = savedRestrict;
            }
        }

    }
}
