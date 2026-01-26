using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility class for detecting tool usage requirements from animations.
/// Analyzes animation frames to detect when tools are needed.
/// </summary>
public static class AnimationToolUsageDetector
{
    /// <summary>
    /// Detect tool usage from animation clip and frames.
    /// </summary>
    public static List<BehaviorTreeGoal> DetectToolUsage(AnimationClip clip, List<AnimationFrame> frames, RagdollSystem ragdoll)
    {
        List<BehaviorTreeGoal> goals = new List<BehaviorTreeGoal>();

        if (clip == null || frames == null || frames.Count == 0)
            return goals;

        // Analyze each frame for tool usage
        foreach (var frame in frames)
        {
            if (frame == null || frame.isDropped)
                continue;

            GameObject detectedTool = AnalyzeFrameForToolUsage(frame, ragdoll);
            if (detectedTool != null)
            {
                frame.requiresTool = true;
                frame.detectedTool = detectedTool;

                // Create goal for this tool
                BehaviorTreeGoal goal = CreateToolUsageGoal(detectedTool, frame);
                if (goal != null && !goals.Contains(goal))
                {
                    goals.Add(goal);
                }
            }
        }

        return goals;
    }

    /// <summary>
    /// Analyze a frame for tool usage requirements.
    /// </summary>
    public static GameObject AnalyzeFrameForToolUsage(AnimationFrame frame, RagdollSystem ragdoll)
    {
        if (frame == null || ragdoll == null || frame.boneTransforms == null)
            return null;

        // Check hand positions for tool-like poses
        // Simplified detection: check if hands are in grasping positions
        bool leftHandGrasping = IsHandInGraspPose(frame, "LeftHand");
        bool rightHandGrasping = IsHandInGraspPose(frame, "RightHand");

        if (leftHandGrasping || rightHandGrasping)
        {
            // Try to find a tool in the scene near the hand position
            Vector3 handPosition = GetHandPosition(frame, leftHandGrasping ? "LeftHand" : "RightHand");
            GameObject tool = FindToolInScene("", 2f, handPosition);

            return tool;
        }

        return null;
    }

    /// <summary>
    /// Create a BehaviorTreeGoal for tool usage.
    /// </summary>
    public static BehaviorTreeGoal CreateToolUsageGoal(GameObject tool, AnimationFrame frame)
    {
        if (tool == null)
            return null;

        BehaviorTreeGoal goal = new BehaviorTreeGoal
        {
            goalName = $"use_{tool.name}",
            type = GoalType.ToolUsage,
            target = tool,
            targetPosition = tool.transform.position,
            priority = 5
        };

        goal.toolsUsed.Add(tool);
        goal.requiresCleanup = true;
        goal.cleanupUrgency = CleanupUrgency.AfterTask;

        return goal;
    }

    /// <summary>
    /// Find tool GameObject in scene.
    /// </summary>
    public static GameObject FindToolInScene(string toolName, float range, Vector3 searchPosition)
    {
        // Search for tools in the scene
        // This is a simplified implementation - actual implementation would use
        // a more sophisticated tool detection system
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        
        foreach (var obj in allObjects)
        {
            // Check if object name contains tool keywords
            string objName = obj.name.ToLower();
            if (objName.Contains("tool") || objName.Contains("weapon") || objName.Contains("item"))
            {
                // Check if within range
                float distance = Vector3.Distance(searchPosition, obj.transform.position);
                if (distance <= range)
                {
                    return obj;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Match tools to animation requirements.
    /// </summary>
    public static Dictionary<AnimationFrame, GameObject> MatchToolToAnimation(AnimationClip clip, List<AnimationFrame> frames, List<GameObject> availableTools)
    {
        Dictionary<AnimationFrame, GameObject> matches = new Dictionary<AnimationFrame, GameObject>();

        if (frames == null || availableTools == null || availableTools.Count == 0)
            return matches;

        foreach (var frame in frames)
        {
            if (frame == null || frame.isDropped)
                continue;

            GameObject matchedTool = null;
            float bestDistance = float.MaxValue;

            Vector3 handPosition = GetHandPosition(frame, "LeftHand");
            if (handPosition == Vector3.zero)
            {
                handPosition = GetHandPosition(frame, "RightHand");
            }

            if (handPosition != Vector3.zero)
            {
                foreach (var tool in availableTools)
                {
                    if (tool == null)
                        continue;

                    float distance = Vector3.Distance(handPosition, tool.transform.position);
                    if (distance < bestDistance && distance < 2f) // Within 2 units
                    {
                        bestDistance = distance;
                        matchedTool = tool;
                    }
                }
            }

            if (matchedTool != null)
            {
                matches[frame] = matchedTool;
            }
        }

        return matches;
    }

    /// <summary>
    /// Check if hand is in a grasping pose.
    /// </summary>
    private static bool IsHandInGraspPose(AnimationFrame frame, string handBoneName)
    {
        if (frame == null || frame.boneTransforms == null)
            return false;

        if (!frame.boneTransforms.TryGetValue(handBoneName, out TransformData handTransform))
            return false;

        // Simplified: check if hand rotation suggests grasping
        // Actual implementation would analyze finger positions and hand orientation
        Vector3 forward = handTransform.rotation * Vector3.forward;
        float angle = Vector3.Angle(forward, Vector3.forward);

        // If hand is rotated significantly, might be grasping
        return angle > 30f && angle < 150f;
    }

    /// <summary>
    /// Get hand position from frame.
    /// </summary>
    private static Vector3 GetHandPosition(AnimationFrame frame, string handBoneName)
    {
        if (frame == null || frame.boneTransforms == null)
            return Vector3.zero;

        if (frame.boneTransforms.TryGetValue(handBoneName, out TransformData handTransform))
        {
            return handTransform.position;
        }

        return Vector3.zero;
    }
}
