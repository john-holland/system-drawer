using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Locomotion.Rig;

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
        AnimationClip clip = animationClip;
        if (clip == null)
        {
            // Try to get from animator controller if provided
            clip = GetClipFromAnimator();
        }
        
        if (clip == null)
        {
            Debug.LogError("[AnimationBehaviorTree.ConvertAnimationToFrames] No animation clip found! " +
                "Please assign an AnimationClip to the 'Animation Clip' field in the inspector. " +
                "If you're using an AnimatorController, assign it to 'Animator Controller' field instead.");
            yield break;
        }

        Debug.Log($"[AnimationBehaviorTree.ConvertAnimationToFrames] Processing clip: {clip.name}, length: {clip.length}s, frameRate: {clip.frameRate}, samplingRate: {frameSamplingRate}");

        // Check if animation has any bindings/curves
        #if UNITY_EDITOR
        var bindings = UnityEditor.AnimationUtility.GetCurveBindings(clip);
        var objectBindings = UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(clip);
        bool hasData = (bindings != null && bindings.Length > 0) || (objectBindings != null && objectBindings.Length > 0);
        Debug.Log($"[AnimationBehaviorTree.ConvertAnimationToFrames] Animation has {bindings?.Length ?? 0} curve bindings and {objectBindings?.Length ?? 0} object bindings. Has data: {hasData}");
        #else
        bool hasData = true; // Assume has data in runtime
        #endif

        float frameTime = 1f / clip.frameRate;
        int totalFrames = Mathf.RoundToInt(clip.length * clip.frameRate);
        
        // Handle zero-length or very short animations (e.g., face animations)
        // Sample at least one frame at time 0 if the animation has data
        int minFrames = hasData ? 1 : 0;
        if (totalFrames < minFrames && hasData)
        {
            Debug.LogWarning($"[AnimationBehaviorTree.ConvertAnimationToFrames] Animation clip '{clip.name}' has zero or very short length ({clip.length}s) but contains animation data. " +
                $"Will sample at least {minFrames} frame(s) at time 0.");
            totalFrames = minFrames;
        }
        else if (totalFrames < minFrames && !hasData)
        {
            Debug.LogError($"[AnimationBehaviorTree.ConvertAnimationToFrames] Animation clip '{clip.name}' has zero length ({clip.length}s) and no animation data. " +
                $"This clip cannot be converted to frames. Please use a different animation clip.");
            yield break;
        }
        
        Debug.Log($"[AnimationBehaviorTree.ConvertAnimationToFrames] Total frames: {totalFrames}, frameTime: {frameTime}s");

        int frameCount = 0;
        for (int i = 0; i < totalFrames; i += frameSamplingRate)
        {
            float time = i * frameTime;
            AnimationFrame frame = ExtractFrame(clip, time, i);
            if (frame != null)
            {
                frameCount++;
                yield return frame;
            }
            else
            {
                Debug.LogWarning($"[AnimationBehaviorTree.ConvertAnimationToFrames] ExtractFrame returned null for frame {i} at time {time}s");
            }
        }

        Debug.Log($"[AnimationBehaviorTree.ConvertAnimationToFrames] Extracted {frameCount} frames from animation");
    }

    /// <summary>
    /// Safely gets the name of a Unity object, handling unassigned references.
    /// </summary>
    private static string SafeGetObjectName(UnityEngine.Object obj, string defaultValue = "null")
    {
        try
        {
            if (obj == null)
                return defaultValue;
            
            return obj.name;
        }
        catch (UnityEngine.UnassignedReferenceException)
        {
            return "unassigned";
        }
        catch (System.Exception)
        {
            return defaultValue;
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

        // Safely get animator controller name (handle unassigned references)
        // Note: Accessing the field itself can throw UnassignedReferenceException
        // We use reflection to safely access the field value
        string animatorControllerName = "null";
        string animationClipName = "null";
        
        // Use reflection to safely get field values without triggering UnassignedReferenceException
        try
        {
            var animatorControllerField = typeof(AnimationBehaviorTree).GetField("animatorController", BindingFlags.Public | BindingFlags.Instance);
            var animatorControllerValue = animatorControllerField?.GetValue(this) as RuntimeAnimatorController;
            animatorControllerName = SafeGetObjectName(animatorControllerValue, "null");
        }
        catch (UnityEngine.UnassignedReferenceException)
        {
            animatorControllerName = "unassigned";
        }
        catch (System.Exception)
        {
            // Fallback: try direct access
            try
            {
                animatorControllerName = SafeGetObjectName(animatorController, "null");
            }
            catch
            {
                animatorControllerName = "unassigned";
            }
        }
        
        try
        {
            var animationClipField = typeof(AnimationBehaviorTree).GetField("animationClip", BindingFlags.Public | BindingFlags.Instance);
            var animationClipValue = animationClipField?.GetValue(this) as AnimationClip;
            animationClipName = SafeGetObjectName(animationClipValue, "null");
        }
        catch (UnityEngine.UnassignedReferenceException)
        {
            animationClipName = "unassigned";
        }
        catch (System.Exception)
        {
            // Fallback: try direct access
            try
            {
                animationClipName = SafeGetObjectName(animationClip, "null");
            }
            catch
            {
                animationClipName = "unassigned";
            }
        }
        
        Debug.Log($"[AnimationBehaviorTree] Starting generation. AnimationClip: {animationClipName}, AnimatorController: {animatorControllerName}");

        isGenerating = true;

        try
        {
            // Convert animation to frames
            Debug.Log("[AnimationBehaviorTree] Converting animation to frames...");
            allFrames = new List<AnimationFrame>(ConvertAnimationToFrames());
            Debug.Log($"[AnimationBehaviorTree] Converted {allFrames.Count} frames from animation.");

            if (allFrames.Count == 0)
            {
                Debug.LogWarning("[AnimationBehaviorTree] No frames generated! Check animation clip and frame sampling settings.");
            }

            // Apply breakout curves
            if (breakoutCurves != null && breakoutCurves.Count > 0)
            {
                Debug.Log($"[AnimationBehaviorTree] Applying {breakoutCurves.Count} breakout curves...");
                ApplyBreakoutCurves(allFrames);
            }

            // Remove dropped frames from active list
            int droppedCount = allFrames.Count(f => f != null && f.isDropped);
            allFrames.RemoveAll(f => f != null && f.isDropped);
            if (droppedCount > 0)
            {
                Debug.Log($"[AnimationBehaviorTree] Removed {droppedCount} dropped frames. Remaining: {allFrames.Count}");
            }

            // Detect tool usage if enabled
            if (autoDetectToolUsage)
            {
                Debug.Log("[AnimationBehaviorTree] Detecting tool usage requirements...");
                DetectToolUsageRequirements();
            }

            // Create behavior tree structure
            Debug.Log("[AnimationBehaviorTree] Creating behavior tree structure...");
            CreateBehaviorTreeStructure();

            // Estimate durations
            Debug.Log("[AnimationBehaviorTree] Estimating durations...");
            EstimateDurations();

            Debug.Log($"[AnimationBehaviorTree] Generation complete! Root node: {rootNode?.name ?? "null"}, Generated tree: {generatedTree?.name ?? "null"}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AnimationBehaviorTree] Error during generation: {e.Message}\n{e.StackTrace}");
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
        {
            Debug.LogWarning($"[AnimationBehaviorTree.ExtractFrame] Clip is null for frame {frameIndex}");
            return null;
        }

        if (time < 0f || time > clip.length)
        {
            Debug.LogWarning($"[AnimationBehaviorTree.ExtractFrame] Time {time}s out of range [0, {clip.length}] for frame {frameIndex}");
            return null;
        }

        AnimationFrame frame = new AnimationFrame
        {
            frameIndex = frameIndex,
            time = time
        };

        // Sample animation at this time
        frame.boneTransforms = new Dictionary<string, TransformData>();
        
        // Try to sample the animation clip using AnimationClip.SampleAnimation
        // This requires a GameObject with the same hierarchy as the animation
        GameObject sampleTarget = GetComponent<RagdollSystem>()?.ragdollRoot?.gameObject ?? gameObject;
        
        if (sampleTarget != null)
        {
            try
            {
                // Sample the animation clip at this time
                // This will apply the animation to the GameObject hierarchy
                // Note: SampleAnimation is an instance method that takes (GameObject, time)
                clip.SampleAnimation(sampleTarget, time);
                
                // Extract bone transforms from the sampled hierarchy
                ExtractBoneTransforms(sampleTarget, frame.boneTransforms);
                
                // Extract root motion if available
                // Note: rootMotion is a delta, not absolute position
                // For now, we'll store the current position/rotation
                // A more sophisticated implementation would calculate the delta from the previous frame
                frame.rootMotion = sampleTarget.transform.position;
                frame.rootRotation = sampleTarget.transform.rotation;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AnimationBehaviorTree.ExtractFrame] Error sampling animation at time {time}s: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[AnimationBehaviorTree.ExtractFrame] No target GameObject found for sampling animation. " +
                "Make sure the AnimationBehaviorTree component is on a GameObject with a RagdollSystem or use a sample target.");
        }

        Debug.Log($"[AnimationBehaviorTree.ExtractFrame] Extracted frame {frameIndex} at time {time}s (boneTransforms count: {frame.boneTransforms.Count})");

        return frame;
    }

    /// <summary>
    /// Extract bone transforms from a GameObject hierarchy.
    /// </summary>
    private void ExtractBoneTransforms(GameObject root, Dictionary<string, TransformData> boneTransforms)
    {
        if (root == null || boneTransforms == null)
            return;

        // Get RagdollSystem to find bone mapping
        RagdollSystem ragdoll = GetComponent<RagdollSystem>();
        if (ragdoll == null)
            ragdoll = root.GetComponent<RagdollSystem>();
        
        // If we have a ragdoll system, use its bone map
        if (ragdoll != null)
        {
            BoneMap boneMap = ragdoll.GetComponent<BoneMap>();
            if (boneMap != null)
            {
                // Extract transforms for all mapped bones
                // This is a simplified implementation - would need to traverse bone map
                ExtractTransformsRecursive(root.transform, boneTransforms);
            }
            else
            {
                // Fallback: extract all transforms
                ExtractTransformsRecursive(root.transform, boneTransforms);
            }
        }
        else
        {
            // No ragdoll system - extract all transforms
            ExtractTransformsRecursive(root.transform, boneTransforms);
        }
    }

    /// <summary>
    /// Recursively extract transforms from a hierarchy.
    /// </summary>
    private void ExtractTransformsRecursive(Transform transform, Dictionary<string, TransformData> boneTransforms)
    {
        if (transform == null || boneTransforms == null)
            return;

        // Store this transform
        string boneName = transform.name;
        if (!boneTransforms.ContainsKey(boneName))
        {
            boneTransforms[boneName] = new TransformData
            {
                position = transform.localPosition,
                rotation = transform.localRotation,
                scale = transform.localScale
            };
        }

        // Recursively process children
        for (int i = 0; i < transform.childCount; i++)
        {
            ExtractTransformsRecursive(transform.GetChild(i), boneTransforms);
        }
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
        Debug.Log($"[AnimationBehaviorTree.CreateBehaviorTreeStructure] Starting. allFrames: {(allFrames == null ? "null" : allFrames.Count.ToString())}, rootNode: {(rootNode == null ? "null" : rootNode.name)}");

        // Clear existing tree
        if (rootNode != null)
        {
            Debug.Log($"[AnimationBehaviorTree.CreateBehaviorTreeStructure] Destroying existing root node: {rootNode.name}");
            DestroyImmediate(rootNode.gameObject);
            rootNode = null;
        }

        if (generatedTree != null)
        {
            Debug.Log($"[AnimationBehaviorTree.CreateBehaviorTreeStructure] Clearing generatedTree.rootNode");
            generatedTree.rootNode = null;
        }

        if (allFrames == null || allFrames.Count == 0)
        {
            Debug.LogWarning($"[AnimationBehaviorTree.CreateBehaviorTreeStructure] No frames to process! allFrames is {(allFrames == null ? "null" : "empty")}. Returning without creating tree structure.");
            return;
        }

        Debug.Log($"[AnimationBehaviorTree.CreateBehaviorTreeStructure] Creating root node with {allFrames.Count} frames...");

        // Create root node
        GameObject rootGO = new GameObject("AnimationRoot");
        rootGO.transform.SetParent(transform);
        rootNode = rootGO.AddComponent<AnimationBehaviorTreeNode>();
        rootNode.nodeType = NodeType.Sequence;
        rootNode.rootBehaviorTree = this;
        rootNode.animationClip = animationClip;
        Debug.Log($"[AnimationBehaviorTree.CreateBehaviorTreeStructure] Created root node: {rootNode.name}");

        // Create child nodes for each frame
        int frameNodeCount = 0;
        foreach (var frame in allFrames)
        {
            if (frame == null || frame.isDropped)
            {
                if (frame != null && frame.isDropped)
                {
                    Debug.Log($"[AnimationBehaviorTree.CreateBehaviorTreeStructure] Skipping dropped frame {frame.frameIndex}");
                }
                continue;
            }

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
                if (frameNode.physicsCard != null)
                {
                    Debug.Log($"[AnimationBehaviorTree.CreateBehaviorTreeStructure] Generated physics card for frame {frame.frameIndex}: {frameNode.physicsCard.sectionName}");
                }
                else
                {
                    Debug.LogWarning($"[AnimationBehaviorTree.CreateBehaviorTreeStructure] Failed to generate physics card for frame {frame.frameIndex}");
                }
            }
            else
            {
                Debug.LogWarning($"[AnimationBehaviorTree.CreateBehaviorTreeStructure] No RagdollSystem found for frame {frame.frameIndex}");
            }

            rootNode.children.Add(frameNode);
            frameNodeCount++;
        }

        Debug.Log($"[AnimationBehaviorTree.CreateBehaviorTreeStructure] Created {frameNodeCount} frame nodes. Root node children count: {rootNode.children.Count}");

        // Set root node on behavior tree
        if (generatedTree == null)
        {
            generatedTree = GetComponent<BehaviorTree>();
            if (generatedTree == null)
            {
                Debug.Log("[AnimationBehaviorTree.CreateBehaviorTreeStructure] Creating new BehaviorTree component");
                generatedTree = gameObject.AddComponent<BehaviorTree>();
            }
            else
            {
                Debug.Log("[AnimationBehaviorTree.CreateBehaviorTreeStructure] Found existing BehaviorTree component");
            }
        }

        generatedTree.rootNode = rootNode;
        Debug.Log($"[AnimationBehaviorTree.CreateBehaviorTreeStructure] Complete! Root node: {rootNode.name}, Generated tree root: {generatedTree.rootNode?.name ?? "null"}");
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
