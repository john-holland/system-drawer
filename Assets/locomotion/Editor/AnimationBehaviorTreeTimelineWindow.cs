using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Custom EditorWindow combining timeline scrubber with state machine graph visualization.
/// </summary>
public class AnimationBehaviorTreeTimelineWindow : EditorWindow
{
    private AnimationBehaviorTree targetTree;
    private float currentTime = 0f;
    private bool isPlaying = false;
    private Vector2 scrollPosition;

    [MenuItem("Window/Locomotion/Animation Behavior Tree Timeline")]
    public static void OpenWindow()
    {
        AnimationBehaviorTreeTimelineWindow window = GetWindow<AnimationBehaviorTreeTimelineWindow>();
        window.titleContent = new GUIContent("Animation Timeline");
        window.Show();
    }

    public static void OpenWindow(AnimationBehaviorTree tree)
    {
        AnimationBehaviorTreeTimelineWindow window = GetWindow<AnimationBehaviorTreeTimelineWindow>();
        window.titleContent = new GUIContent("Animation Timeline");
        window.targetTree = tree;
        window.Show();
    }

    private void OnGUI()
    {
        if (targetTree == null)
        {
            EditorGUILayout.HelpBox("No AnimationBehaviorTree selected. Select one in the scene or assign it below.", MessageType.Info);
            targetTree = (AnimationBehaviorTree)EditorGUILayout.ObjectField("Animation Behavior Tree", targetTree, typeof(AnimationBehaviorTree), true);
            return;
        }

        EditorGUILayout.LabelField("Animation Behavior Tree Timeline", EditorStyles.boldLabel);

        // Timeline scrubber section
        DrawTimelineScrubber();

        EditorGUILayout.Space();

        // Tool usage goals section
        DrawToolUsageGoalsSection();

        EditorGUILayout.Space();

        // Dropped frames panel
        DrawDroppedFramesPanel();
    }

    private void DrawTimelineScrubber()
    {
        EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);

        if (targetTree.animationClip != null)
        {
            float clipLength = targetTree.animationClip.length;
            EditorGUILayout.LabelField($"Clip Length: {clipLength:F2}s");

            // Time scrubber
            currentTime = EditorGUILayout.Slider("Time", currentTime, 0f, clipLength);

            // Playback controls
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(isPlaying ? "Pause" : "Play"))
            {
                isPlaying = !isPlaying;
            }
            if (GUILayout.Button("Stop"))
            {
                isPlaying = false;
                currentTime = 0f;
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("No animation clip assigned.", MessageType.Warning);
        }
    }

    private void DrawToolUsageGoalsSection()
    {
        EditorGUILayout.LabelField("Tool Usage Goals", EditorStyles.boldLabel);

        if (targetTree.toolUsageGoals != null && targetTree.toolUsageGoals.Count > 0)
        {
            for (int i = 0; i < targetTree.toolUsageGoals.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(targetTree.toolUsageGoals[i], typeof(BehaviorTreeGoal), false);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    targetTree.toolUsageGoals.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No tool usage goals set.", MessageType.Info);
        }
    }

    private void DrawDroppedFramesPanel()
    {
        EditorGUILayout.LabelField("Dropped Frames", EditorStyles.boldLabel);

        if (targetTree.droppedFrames != null && targetTree.droppedFrames.Count > 0)
        {
            EditorGUILayout.LabelField($"Count: {targetTree.droppedFrames.Count}");

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            for (int i = 0; i < targetTree.droppedFrames.Count; i++)
            {
                var frame = targetTree.droppedFrames[i];
                if (frame == null)
                    continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Frame {frame.frameIndex} (t={frame.time:F2}s)");
                if (GUILayout.Button("Restore", GUILayout.Width(60)))
                {
                    targetTree.RestoreDroppedFrame(frame);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Restore All"))
            {
                var framesToRestore = new List<AnimationFrame>(targetTree.droppedFrames);
                foreach (var frame in framesToRestore)
                {
                    targetTree.RestoreDroppedFrame(frame);
                }
            }
            if (GUILayout.Button("Clear"))
            {
                targetTree.droppedFrames.Clear();
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("No dropped frames.", MessageType.Info);
        }
    }
}
