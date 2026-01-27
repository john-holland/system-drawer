using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace BedogaGenerator.Editor
{
    /// <summary>
    /// Editor visualization for octree structure.
    /// Shows color-coded nodes when Shift is held and a component with an octree is selected.
    /// </summary>
    [InitializeOnLoad]
    public static class OctreeVisualizationEditor
    {
        private static bool isShiftHeld = false;
        private static SGOctTree.OctTreeNode selectedNode = null;
        private static Vector3 lastMousePosition = Vector3.zero;
        private static int maxDepthToShow = 8;
        private static float nodeAlpha = 0.3f;
        private static Color selectedNodeColor = Color.cyan;

        static OctreeVisualizationEditor()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            // Check if Shift is held using Input system
            isShiftHeld = (Event.current != null && (Event.current.modifiers & EventModifiers.Shift) != 0) ||
                         (UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift));
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!isShiftHeld) return;

            // Find selected object with octree
            GameObject selectedObj = Selection.activeGameObject;
            if (selectedObj == null) return;

            SGOctTreeSolver octSolver = selectedObj.GetComponent<SGOctTreeSolver>();
            SpatialGenerator spatialGen = selectedObj.GetComponent<SpatialGenerator>();

            SGOctTree octTree = null;
            Transform treeTransform = null;

            if (octSolver != null)
            {
                octTree = octSolver.GetOctTree();
                treeTransform = octSolver.transform;
            }
            else if (spatialGen != null)
            {
                // Try to get octree solver from spatial generator
                octSolver = spatialGen.GetComponent<SGOctTreeSolver>();
                if (octSolver != null)
                {
                    octTree = octSolver.GetOctTree();
                    treeTransform = octSolver.transform;
                }
            }

            if (octTree == null || treeTransform == null) return;

            // Handle mouse interaction for node selection
            HandleMouseInteraction(sceneView, octTree, treeTransform);

            // Draw octree nodes
            DrawOctreeNodes(octTree.GetRoot(), treeTransform, 0);

            // Update info window
            OctreeInfoWindow.UpdateNode(selectedNode);
        }

        private static void HandleMouseInteraction(SceneView sceneView, SGOctTree octTree, Transform treeTransform)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                lastMousePosition = e.mousePosition;
                
                // Raycast to find hovered node
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                selectedNode = FindNodeAtRay(octTree.GetRoot(), treeTransform, ray, 0);
                
                if (e.type == EventType.MouseMove)
                {
                    sceneView.Repaint();
                }
            }
        }

        private static SGOctTree.OctTreeNode FindNodeAtRay(SGOctTree.OctTreeNode node, Transform treeTransform, Ray ray, int depth)
        {
            if (node == null || depth > maxDepthToShow) return null;

            // Convert node bounds to world space
            Vector3 worldCenter = treeTransform.TransformPoint(node.bounds.center);
            Vector3 worldSize = Vector3.Scale(node.bounds.size, treeTransform.lossyScale);

            // Check if ray intersects bounds
            Bounds worldBounds = new Bounds(worldCenter, worldSize);
            if (worldBounds.IntersectRay(ray))
            {
                // Check children first (more specific)
                if (!node.isLeaf && node.children != null)
                {
                    for (int i = 0; i < node.children.Length; i++)
                    {
                        SGOctTree.OctTreeNode childResult = FindNodeAtRay(node.children[i], treeTransform, ray, depth + 1);
                        if (childResult != null)
                        {
                            return childResult;
                        }
                    }
                }

                // This node is intersected
                return node;
            }

            return null;
        }

        private static void DrawOctreeNodes(SGOctTree.OctTreeNode node, Transform treeTransform, int depth)
        {
            if (node == null || depth > maxDepthToShow) return;

            // Convert node bounds to world space
            Vector3 worldCenter = treeTransform.TransformPoint(node.bounds.center);
            Vector3 worldSize = Vector3.Scale(node.bounds.size, treeTransform.lossyScale);

            // Get color for node
            Color nodeColor = GetNodeColor(node, depth);
            
            // Highlight selected node
            if (node == selectedNode)
            {
                nodeColor = selectedNodeColor;
                nodeColor.a = 1.0f;
            }
            else
            {
                nodeColor.a = nodeAlpha;
            }

            // Draw wireframe box
            Handles.color = nodeColor;
            Handles.DrawWireCube(worldCenter, worldSize);

            // Draw child nodes
            if (!node.isLeaf && node.children != null)
            {
                for (int i = 0; i < node.children.Length; i++)
                {
                    DrawOctreeNodes(node.children[i], treeTransform, depth + 1);
                }
            }
        }

        private static Color GetNodeColor(SGOctTree.OctTreeNode node, int depth)
        {
            const int maxObjectsPerNode = 8;

            // Empty nodes: light gray
            if (node.objects == null || node.objects.Count == 0)
            {
                return Color.gray;
            }

            // Occupied nodes: color gradient based on occupancy
            float occupancyRatio = Mathf.Clamp01((float)node.objects.Count / maxObjectsPerNode);

            if (occupancyRatio < 0.5f)
            {
                // Low occupancy: green to yellow
                return Color.Lerp(Color.green, Color.yellow, occupancyRatio * 2f);
            }
            else
            {
                // High occupancy: yellow to red
                return Color.Lerp(Color.yellow, Color.red, (occupancyRatio - 0.5f) * 2f);
            }
        }

    }
}
