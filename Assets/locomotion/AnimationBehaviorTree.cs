using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Root component that manages animation-to-behavior-tree conversion.
/// Converts Unity animations (AnimationClip/AnimatorController) into behavior trees with physics cards.
/// </summary>
[AddComponentMenu("Locomotion/Animation Behavior Tree")]
public class AnimationBehaviorTree : MonoBehaviour
{
    [Header("Animation Source")]
    [Tooltip("Source animation clip")]
    public AnimationClip animationClip;

    [Tooltip("Alternative source from animator")]
    public RuntimeAnimatorController animatorController;

    [Header("Frame Sampling")]
    [Tooltip("Sample every Nth frame (default: 1 = every frame)")]
    public int frameSamplingRate = 1;

    [Tooltip("Use only keyframes if true")]
    public bool useKeyframesOnly = false;

    [Header("Interpolation")]
    [Tooltip("Interpolation mode")]
    public InterpolationMode interpolationMode = InterpolationMode.Linear;

    [Header("Breakout Curves")]
    [Tooltip("Manual frame mapping overrides")]
    public List<BreakoutCurve> breakoutCurves = new List<BreakoutCurve>();

    [Header("Generated Tree")]
    [Tooltip("Reference to generated behavior tree")]
    public BehaviorTree generatedTree;

    [Tooltip("Root animation node")]
    public AnimationBehaviorTreeNode rootNode;

    [Header("Attenuation")]
    [Tooltip("Animation attenuation settings")]
    public AttenuationProperties attenuationProperties = new AttenuationProperties();

    [Header("Tool Usage")]
    [Tooltip("Goals for tool usage (shortcuts for animations requiring tools)")]
    public List<BehaviorTreeGoal> toolUsageGoals = new List<BehaviorTreeGoal>();

    [Tooltip("Automatically detect tool usage requirements from animation")]
    public bool autoDetectToolUsage = false;

    [Header("Dropped Frames")]
    [Tooltip("Frames that were dropped/trimmed (for recovery)")]
    public List<AnimationFrame> droppedFrames = new List<AnimationFrame>();

    // Internal state
    private List<AnimationFrame> allFrames = new List<AnimationFrame>();
    private bool isGenerating = false;

    /// <summary>
    /// Convert animation to frames using IEnumerable.
    /// </summary>
    public IEnumerable<AnimationFrame> ConvertAnimationToFrames()
    {
        AnimationClip clip = animationClip ?? GetClipFromAnimator();
        if (clip == null)
            yield break;

        float frameTime = 1f / clip.frameRate;
        int totalFrames = Mathf.RoundToInt(clip.length * clip.frameRate);

        for (int i = 0; i < totalFrames; i += frameSamplingRate)
        {
            float time = i * frameTime;
            AnimationFrame frame = ExtractFrame(clip, time, i);
            if (frame != null)
            {
                yield return frame;
            }
        }
    }

    /// <summary>
    /// Generate behavior tree from animation.
    /// </summary>
    public void GenerateBehaviorTree()
    {
        if (isGenerating)
        {
            Debug.LogWarning("AnimationBehaviorTree: Already generating, please wait.");
            return;
        }

        isGenerating = true;

        try
        {
            // Convert animation to frames
            allFrames = new List<AnimationFrame>(ConvertAnimationToFrames());

            // Apply breakout curves
            if (breakoutCurves != null && breakoutCurves.Count > 0)
            {
                ApplyBreakoutCurves(allFrames);
            }

            // Remove dropped frames from active list
            allFrames.RemoveAll(f => f != null && f.isDropped);

            // Detect tool usage if enabled
            if (autoDetectToolUsage)
            {
                DetectToolUsageRequirements();
            }

            // Create behavior tree structure
            CreateBehaviorTreeStructure();

            // Estimate durations
            EstimateDurations();
        }
        finally
        {
            isGenerating = false;
        }
    }

    /// <summary>
    /// Apply breakout curves to frames.
    /// </summary>
    public void ApplyBreakoutCurves(List<AnimationFrame> frames)
    {
        if (frames == null || breakoutCurves == null)
            return;

        AnimationFrameInterpolator.ApplyBreakoutCurves(frames, breakoutCurves);
    }

    /// <summary>
    /// Detect tool usage requirements from animation.
    /// </summary>
    public void DetectToolUsageRequirements()
    {
        if (allFrames == null || allFrames.Count == 0)
            return;

        RagdollSystem ragdoll = GetComponent<RagdollSystem>();
        if (ragdoll == null)
            ragdoll = FindObjectOfType<RagdollSystem>();

        if (ragdoll == null)
            return;

        List<BehaviorTreeGoal> detectedGoals = AnimationToolUsageDetector.DetectToolUsage(
            animationClip, allFrames, ragdoll);

        // Merge with existing goals
        foreach (var goal in detectedGoals)
        {
            if (goal != null && !toolUsageGoals.Contains(goal))
            {
                toolUsageGoals.Add(goal);
            }
        }
    }

    /// <summary>
    /// Set tool usage goal.
    /// </summary>
    public void SetToolUsageGoal(GameObject tool, BehaviorTreeGoal goal)
    {
        if (tool == null || goal == null)
            return;

        goal.target = tool;
        goal.type = GoalType.ToolUsage;

        if (!toolUsageGoals.Contains(goal))
        {
            toolUsageGoals.Add(goal);
        }
    }

    /// <summary>
    /// Drop a frame (adds to droppedFrames list).
    /// </summary>
    public void DropFrame(AnimationFrame frame)
    {
        if (frame == null || frame.isDropped)
            return;

        frame.isDropped = true;
        if (!droppedFrames.Contains(frame))
        {
            droppedFrames.Add(frame);
        }

        // Remove from active frames
        allFrames.Remove(frame);

        // Rebuild behavior tree structure
        CreateBehaviorTreeStructure();
    }

    /// <summary>
    /// Restore a dropped frame.
    /// </summary>
    public void RestoreDroppedFrame(AnimationFrame frame)
    {
        if (frame == null || !frame.isDropped)
            return;

        if (droppedFrames.Contains(frame))
        {
            frame.isDropped = false;
            droppedFrames.Remove(frame);

            // Re-insert into active frames at original position
            // For simplicity, add to end - could be improved to maintain original order
            if (!allFrames.Contains(frame))
            {
                allFrames.Add(frame);
                allFrames = allFrames.OrderBy(f => f.frameIndex).ToList();
            }

            // Rebuild behavior tree structure
            CreateBehaviorTreeStructure();
        }
    }

    /// <summary>
    /// Trim frame range (drops frames to droppedFrames list).
    /// </summary>
    public void TrimFrames(int startFrame, int endFrame)
    {
        List<AnimationFrame> framesToDrop = allFrames.Where(f => 
            f != null && f.frameIndex >= startFrame && f.frameIndex <= endFrame).ToList();

        foreach (var frame in framesToDrop)
        {
            DropFrame(frame);
        }
    }

    /// <summary>
    /// Extract a single frame from animation clip.
    /// </summary>
    private AnimationFrame ExtractFrame(AnimationClip clip, float time, int frameIndex)
    {
        if (clip == null)
            return null;

        AnimationFrame frame = new AnimationFrame
        {
            frameIndex = frameIndex,
            time = time
        };

        // Sample animation at this time
        // This is a simplified implementation - actual implementation would:
        // 1. Sample all bone transforms at this time
        // 2. Extract root motion
        // 3. Store in frame.boneTransforms

        // For now, create empty frame structure
        frame.boneTransforms = new Dictionary<string, TransformData>();

        return frame;
    }

    /// <summary>
    /// Get animation clip from animator controller.
    /// </summary>
    private AnimationClip GetClipFromAnimator()
    {
        if (animatorController == null)
            return null;

        // Get first clip from animator controller
        // This is simplified - actual implementation would handle multiple clips
        Animator animator = GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            var clips = animator.runtimeAnimatorController.animationClips;
            if (clips != null && clips.Length > 0)
            {
                return clips[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Create behavior tree structure from frames.
    /// </summary>
    private void CreateBehaviorTreeStructure()
    {
        // Clear existing tree
        if (rootNode != null)
        {
            DestroyImmediate(rootNode.gameObject);
            rootNode = null;
        }

        if (generatedTree != null)
        {
            generatedTree.rootNode = null;
        }

        if (allFrames == null || allFrames.Count == 0)
            return;

        // Create root node
        GameObject rootGO = new GameObject("AnimationRoot");
        rootGO.transform.SetParent(transform);
        rootNode = rootGO.AddComponent<AnimationBehaviorTreeNode>();
        rootNode.nodeType = NodeType.Sequence;
        rootNode.rootBehaviorTree = this;
        rootNode.animationClip = animationClip;

        // Create child nodes for each frame
        foreach (var frame in allFrames)
        {
            if (frame == null || frame.isDropped)
                continue;

            GameObject frameGO = new GameObject($"Frame_{frame.frameIndex}");
            frameGO.transform.SetParent(rootGO.transform);
            AnimationBehaviorTreeNode frameNode = frameGO.AddComponent<AnimationBehaviorTreeNode>();
            frameNode.frameIndex = frame.frameIndex;
            frameNode.frameTime = frame.time;
            frameNode.animationClip = animationClip;
            frameNode.rootBehaviorTree = this;
            frameNode.boneTransforms = new Dictionary<string, TransformData>(frame.boneTransforms);

            // Generate physics card for this frame
            RagdollSystem ragdoll = GetComponent<RagdollSystem>();
            if (ragdoll == null)
                ragdoll = FindObjectOfType<RagdollSystem>();

            if (ragdoll != null)
            {
                frameNode.physicsCard = AnimationPhysicsCardGenerator.GenerateCardFromFrame(frame, ragdoll);
            }

            rootNode.children.Add(frameNode);
        }

        // Set root node on behavior tree
        if (generatedTree == null)
        {
            generatedTree = GetComponent<BehaviorTree>();
            if (generatedTree == null)
            {
                generatedTree = gameObject.AddComponent<BehaviorTree>();
            }
        }

        generatedTree.rootNode = rootNode;
    }

    /// <summary>
    /// Estimate durations for all nodes.
    /// </summary>
    private void EstimateDurations()
    {
        if (generatedTree == null || rootNode == null)
            return;

        PhysicsCardSolver cardSolver = GetComponent<PhysicsCardSolver>();
        if (cardSolver == null)
            cardSolver = FindObjectOfType<PhysicsCardSolver>();

        List<GoodSection> cards = new List<GoodSection>();
        if (cardSolver != null)
        {
            RagdollState state = GetComponent<RagdollSystem>()?.GetCurrentState() ?? new RagdollState();
            cards = cardSolver.FindApplicableCards(state);
        }

        // Estimate duration for root node
        rootNode.EstimateDurationFromCards(cards);
    }

    private RagdollState GetCurrentState()
    {
        RagdollSystem ragdoll = GetComponent<RagdollSystem>();
        if (ragdoll != null)
        {
            return ragdoll.GetCurrentState();
        }
        return new RagdollState();
    }
}

/// <summary>
/// Interpolation mode for frame mapping.
/// </summary>
public enum InterpolationMode
{
    Linear,
    Bezier,
    Cubic
}
