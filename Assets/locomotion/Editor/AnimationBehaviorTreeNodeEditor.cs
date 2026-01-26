using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for individual frame nodes.
/// </summary>
[CustomEditor(typeof(AnimationBehaviorTreeNode))]
public class AnimationBehaviorTreeNodeEditor : Editor
{
    private AnimationBehaviorTreeNode targetNode;

    private void OnEnable()
    {
        targetNode = (AnimationBehaviorTreeNode)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        // Go to root button
        if (targetNode.rootBehaviorTree != null)
        {
            if (GUILayout.Button("Go to Root AnimationBehaviorTree"))
            {
                Selection.activeGameObject = targetNode.rootBehaviorTree.gameObject;
            }
        }

        // Open in timeline window button
        if (GUILayout.Button("Open in Timeline Window"))
        {
            AnimationBehaviorTreeTimelineWindow.OpenWindow(targetNode.rootBehaviorTree);
        }

        // Physics card preview
        if (targetNode.physicsCard != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Physics Card", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Name: {targetNode.physicsCard.sectionName}");
            EditorGUILayout.LabelField($"Description: {targetNode.physicsCard.description}");
        }
    }
}
