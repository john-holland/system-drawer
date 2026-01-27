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
    private double lastUpdateTime = 0f;
    private bool updateSubscribed = false;

    private void OnEnable()
    {
        if (!updateSubscribed)
        {
            EditorApplication.update += OnEditorUpdate;
            updateSubscribed = true;
            Debug.Log("[AnimationBehaviorTreeTimelineWindow] EditorApplication.update subscribed");
        }
    }

    private void OnDisable()
    {
        if (updateSubscribed)
        {
            EditorApplication.update -= OnEditorUpdate;
            updateSubscribed = false;
            Debug.Log("[AnimationBehaviorTreeTimelineWindow] EditorApplication.update unsubscribed");
        }
    }

    private void OnEditorUpdate()
    {
        // Update timeline when playing
        if (isPlaying && targetTree != null && targetTree.animationClip != null)
        {
            double currentRealTime = EditorApplication.timeSinceStartup;
            if (lastUpdateTime > 0f)
            {
                double deltaTime = currentRealTime - lastUpdateTime;
                if (deltaTime > 0f)
                {
                    currentTime += (float)deltaTime;
                    if (currentTime > targetTree.animationClip.length)
                    {
                        currentTime = 0f;
                        Debug.Log("[AnimationBehaviorTreeTimelineWindow] Timeline looped back to start");
                    }
                    Repaint();
                }
            }
            lastUpdateTime = currentRealTime;
        }
        else if (!isPlaying)
        {
            lastUpdateTime = 0f;
        }
    }

    [MenuItem("Window/Locomotion/Animation Behavior Tree Timeline")]
    public static void OpenWindow()
    {
        AnimationBehaviorTreeTimelineWindow window = GetWindow<AnimationBehaviorTreeTimelineWindow>();
        window.titleContent = new GUIContent("Animation Timeline");
        window.Show();
        Debug.Log("[AnimationBehaviorTreeTimelineWindow] Window opened (no target tree)");
    }

    public static void OpenWindow(AnimationBehaviorTree tree)
    {
        AnimationBehaviorTreeTimelineWindow window = GetWindow<AnimationBehaviorTreeTimelineWindow>();
        window.titleContent = new GUIContent("Animation Timeline");
        window.targetTree = tree;
        window.Show();
        Debug.Log($"[AnimationBehaviorTreeTimelineWindow] Window opened with target tree: {tree?.name ?? "null"}");
    }

    private void OnGUI()
    {
        if (targetTree == null)
        {
            EditorGUILayout.HelpBox("No AnimationBehaviorTree selected. Select one in the scene or assign it below.", MessageType.Info);
            var previousTree = targetTree;
            targetTree = (AnimationBehaviorTree)EditorGUILayout.ObjectField("Animation Behavior Tree", targetTree, typeof(AnimationBehaviorTree), true);
            if (targetTree != previousTree && targetTree != null)
            {
                Debug.Log($"[AnimationBehaviorTreeTimelineWindow] Target tree assigned: {targetTree.name}");
            }
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
            float previousTime = currentTime;
            currentTime = EditorGUILayout.Slider("Time", currentTime, 0f, clipLength);
            if (Mathf.Abs(currentTime - previousTime) > 0.001f)
            {
                Debug.Log($"[AnimationBehaviorTreeTimelineWindow] Time scrubbed to {currentTime:F3}s / {clipLength:F3}s");
            }

            // Playback controls
            EditorGUILayout.BeginHorizontal();
            bool previousPlaying = isPlaying;
            if (GUILayout.Button(isPlaying ? "Pause" : "Play"))
            {
                isPlaying = !isPlaying;
                lastUpdateTime = isPlaying ? EditorApplication.timeSinceStartup : 0f;
                Debug.Log($"[AnimationBehaviorTreeTimelineWindow] Playback {(isPlaying ? "started" : "paused")} at time {currentTime:F3}s (clip length: {clipLength:F3}s)");
            }
            if (GUILayout.Button("Stop"))
            {
                isPlaying = false;
                currentTime = 0f;
                lastUpdateTime = 0f;
                Debug.Log("[AnimationBehaviorTreeTimelineWindow] Playback stopped, time reset to 0");
            }
            EditorGUILayout.EndHorizontal();
            
            // Show current playback status
            if (isPlaying)
            {
                EditorGUILayout.LabelField($"Playing: {currentTime:F2}s / {clipLength:F2}s", EditorStyles.helpBox);
            }
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
                BehaviorTreeGoal goal = targetTree.toolUsageGoals[i];
                EditorGUILayout.LabelField($"Goal {i + 1}: {goal.goalName} ({goal.type})", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    var removedGoal = targetTree.toolUsageGoals[i];
                    Debug.Log($"[AnimationBehaviorTreeTimelineWindow] Removed tool usage goal: {removedGoal.goalName} ({removedGoal.type})");
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
                    Debug.Log($"[AnimationBehaviorTreeTimelineWindow] Restoring dropped frame {frame.frameIndex} at time {frame.time:F3}s");
                    targetTree.RestoreDroppedFrame(frame);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Restore All"))
            {
                var framesToRestore = new List<AnimationFrame>(targetTree.droppedFrames);
                Debug.Log($"[AnimationBehaviorTreeTimelineWindow] Restoring all {framesToRestore.Count} dropped frames");
                foreach (var frame in framesToRestore)
                {
                    targetTree.RestoreDroppedFrame(frame);
                }
            }
            if (GUILayout.Button("Clear"))
            {
                int clearedCount = targetTree.droppedFrames.Count;
                Debug.Log($"[AnimationBehaviorTreeTimelineWindow] Clearing {clearedCount} dropped frames");
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
