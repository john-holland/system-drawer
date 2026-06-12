using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Roads.Editor
{
    [InitializeOnLoad]
    public static class RoadHandPlacementTool
    {
        static RoadLayoutPlacementNode _activeNode;
        static bool _painting;

        static RoadHandPlacementTool()
        {
            SceneView.duringSceneGui += OnSceneGui;
        }

        [MenuItem("Roads/Hand Placement/Enable Painting")]
        static void EnablePainting()
        {
            _activeNode = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<RoadLayoutPlacementNode>()
                : null;
            if (_activeNode == null)
            {
                Debug.LogWarning("Select a GameObject with RoadLayoutPlacementNode.");
                return;
            }
            _activeNode.placementMode = RoadLayoutPlacementMode.HandAuthored;
            _painting = true;
        }

        [MenuItem("Roads/Hand Placement/Disable Painting")]
        static void DisablePainting()
        {
            _painting = false;
        }

        [MenuItem("Roads/Hand Placement/Clear Control Points")]
        static void ClearPoints()
        {
            var node = Selection.activeGameObject?.GetComponent<RoadLayoutPlacementNode>();
            if (node != null)
                node.handPlacedControlPoints.Clear();
        }

        static void OnSceneGui(SceneView view)
        {
            if (!_painting || _activeNode == null)
                return;

            Handles.color = Color.yellow;
            var pts = _activeNode.handPlacedControlPoints;
            for (int i = 0; i < pts.Count; i++)
            {
                pts[i] = Handles.PositionHandle(pts[i], Quaternion.identity);
                if (i > 0)
                    Handles.DrawLine(pts[i - 1], pts[i]);
            }

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && e.control)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out var hit, 500f))
                {
                    pts.Add(hit.point);
                    e.Use();
                    if (_activeNode.roadSpline == null)
                        _activeNode.roadSpline = _activeNode.GetComponent<RoadSpline3D>();
                    if (_activeNode.roadSpline != null)
                    {
                        _activeNode.roadSpline.controlPoints = new List<Vector3>(pts);
                        _activeNode.roadSpline.RebuildBakedSamples(2f);
                    }
                }
            }
        }
    }
}
