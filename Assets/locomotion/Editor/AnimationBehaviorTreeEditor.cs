using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for AnimationBehaviorTree component (Inspector view).
/// </summary>
[CustomEditor(typeof(AnimationBehaviorTree))]
public class AnimationBehaviorTreeEditor : Editor
{
    private AnimationBehaviorTree targetComponent;

    private void OnEnable()
    {
        targetComponent = (AnimationBehaviorTree)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        // Generate/Regenerate buttons
        if (GUILayout.Button("Generate Behavior Tree"))
        {
            targetComponent.GenerateBehaviorTree();
        }

        if (GUILayout.Button("Open Timeline Window"))
        {
            AnimationBehaviorTreeTimelineWindow.OpenWindow(targetComponent);
        }

        // Tool usage goals section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tool Usage Goals", EditorStyles.boldLabel);

        if (GUILayout.Button("Auto-Detect Tool Usage"))
        {
            targetComponent.autoDetectToolUsage = true;
            targetComponent.DetectToolUsageRequirements();
        }

        // Dropped frames section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dropped Frames", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Dropped Frames Count: {targetComponent.droppedFrames?.Count ?? 0}");

        if (targetComponent.droppedFrames != null && targetComponent.droppedFrames.Count > 0)
        {
            if (GUILayout.Button("Restore All Dropped Frames"))
            {
                var framesToRestore = new System.Collections.Generic.List<AnimationFrame>(targetComponent.droppedFrames);
                foreach (var frame in framesToRestore)
                {
                    targetComponent.RestoreDroppedFrame(frame);
                }
            }

            if (GUILayout.Button("Clear Dropped Frames"))
            {
                targetComponent.droppedFrames.Clear();
            }
        }
    }
}
