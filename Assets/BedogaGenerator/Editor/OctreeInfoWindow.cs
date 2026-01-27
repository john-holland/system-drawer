using UnityEngine;
using UnityEditor;

namespace BedogaGenerator.Editor
{
    /// <summary>
    /// Editor window that displays information about the currently selected octree node.
    /// Appears when Shift is held and updates in real-time.
    /// </summary>
    public class OctreeInfoWindow : EditorWindow
    {
        private static OctreeInfoWindow instance;
        private SGOctTree.OctTreeNode currentNode;
        private Vector2 scrollPosition;

        [MenuItem("Window/BedogaGenerator/Octree Info")]
        public static void ShowWindow()
        {
            instance = GetWindow<OctreeInfoWindow>("Octree Node Info");
            instance.minSize = new Vector2(300, 200);
        }

        public static void UpdateNode(SGOctTree.OctTreeNode node)
        {
            if (instance == null)
            {
                // Create window if it doesn't exist
                ShowWindow();
            }

            if (instance != null)
            {
                instance.currentNode = node;
                instance.Repaint();
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Octree Node Information", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (currentNode == null)
            {
                EditorGUILayout.HelpBox("No node selected. Hold Shift and hover over octree nodes in the scene view.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            // Position information
            EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Center:", $"{currentNode.bounds.center.x:F3}, {currentNode.bounds.center.y:F3}, {currentNode.bounds.center.z:F3}");
            EditorGUILayout.LabelField("Min:", $"{currentNode.bounds.min.x:F3}, {currentNode.bounds.min.y:F3}, {currentNode.bounds.min.z:F3}");
            EditorGUILayout.LabelField("Max:", $"{currentNode.bounds.max.x:F3}, {currentNode.bounds.max.y:F3}, {currentNode.bounds.max.z:F3}");
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            // Size information
            EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("X:", $"{currentNode.bounds.size.x:F3}");
            EditorGUILayout.LabelField("Y:", $"{currentNode.bounds.size.y:F3}");
            EditorGUILayout.LabelField("Z:", $"{currentNode.bounds.size.z:F3}");
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            // Node properties
            EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Objects:", (currentNode.objects?.Count ?? 0).ToString());
            EditorGUILayout.LabelField("Is Leaf:", currentNode.isLeaf.ToString());
            
            if (currentNode.children != null && currentNode.children.Length > 0)
            {
                EditorGUILayout.LabelField("Children:", currentNode.children.Length.ToString());
                
                // Show which children exist
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Child Octants:", EditorStyles.miniLabel);
                EditorGUI.indentLevel++;
                for (int i = 0; i < currentNode.children.Length; i++)
                {
                    if (currentNode.children[i] != null)
                    {
                        string octantName = ((SGOctTree.Octant)i).ToString();
                        EditorGUILayout.LabelField(octantName, "Present");
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            // Object list
            if (currentNode.objects != null && currentNode.objects.Count > 0)
            {
                EditorGUILayout.LabelField("Objects in Node", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                for (int i = 0; i < currentNode.objects.Count; i++)
                {
                    GameObject obj = currentNode.objects[i];
                    if (obj != null)
                    {
                        EditorGUILayout.ObjectField($"Object {i + 1}", obj, typeof(GameObject), true);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Object {i + 1}", "null (destroyed)");
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView();

            // Update instructions
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Hold Shift in Scene View to visualize octree nodes.", MessageType.None);
        }

        private void OnEnable()
        {
            instance = this;
        }

        private void OnDisable()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
