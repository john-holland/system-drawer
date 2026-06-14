using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;
using Locomotion.Rig;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Root component that manages animation-to-behavior-tree conversion.
/// Converts Unity animations (AnimationClip/AnimatorController) into behavior trees with physics cards.
/// </summary>
[AddComponentMenu("Locomotion/Animation Behavior Tree")]
public class AnimationBehaviorTree : MonoBehaviour, IAnimationLayerReporter
{
    [Header("Clip Configurations")]
    [Tooltip("List of clip configurations (each holds one clip + its settings)")]
    public List<ABTClipConfig> clipConfigurations = new List<ABTClipConfig>();

    [Tooltip("Which config is used for generation and as primary. Default 0.")]
    public int activeClipIndex = 0;

    [Header("Animation Source (Legacy - migrated to clipConfigurations)")]
    [FormerlySerializedAs("animationClip")]
    [SerializeField]
    [Tooltip("Source animation clip. Kept for backward compatibility; migrated to clipConfigurations on first use.")]
    private AnimationClip _legacyAnimationClip;

    [Tooltip("Alternative source from animator")]
    public RuntimeAnimatorController animatorController;

    [Header("Discovery (Editor)")]
    [Tooltip("Animations directory to scan for clips (assign folder from Project). Used by Auto Fill Clips.")]
    public Object animationsDirectory;

    [Header("Defaults for new clip configs")]
    [Tooltip("Sample every Nth frame (default: 1 = every frame)")]
    public int frameSamplingRate = 1;

    [Tooltip("Use only keyframes if true")]
    public bool useKeyframesOnly = false;

    [Tooltip("Interpolation mode")]
    public InterpolationMode interpolationMode = InterpolationMode.Linear;

    [Tooltip("Automatically detect tool usage requirements from animation")]
    public bool autoDetectToolUsage = false;

    [Header("Generated Tree")]
    [Tooltip("Reference to generated behavior tree")]
    public BehaviorTree generatedTree;

    [Tooltip("Primary root animation node (first in rootNodes; used by BehaviorTree execution)")]
    public AnimationBehaviorTreeNode rootNode;

    [Tooltip("Multiple animation root nodes (one per clip or mode). Primary root is first; others used by IK trainer and ragdoll.")]
    public List<AnimationBehaviorTreeNode> rootNodes = new List<AnimationBehaviorTreeNode>();

    [Header("Playback")]
    [Tooltip("1 = forward frame order, -1 = reverse frame order for generated sequence roots.")]
    public int playbackDirection = 1;

    // Internal state
    private List<AnimationFrame> allFrames = new List<AnimationFrame>();
    private bool isGenerating = false;

    /// <summary>
    /// Get the active clip configuration, or null if none.
    /// </summary>
    public ABTClipConfig GetActiveConfiguration()
    {
        if (clipConfigurations == null || clipConfigurations.Count == 0)
            return null;
        int idx = Mathf.Clamp(activeClipIndex, 0, clipConfigurations.Count - 1);
        return clipConfigurations[idx];
    }

    /// <summary>
    /// Ensure at least one clip config exists; migrate from legacy animationClip if needed.
    /// Returns the active config.
    /// </summary>
    public ABTClipConfig GetOrCreateDefaultConfiguration()
    {
        if (clipConfigurations == null)
            clipConfigurations = new List<ABTClipConfig>();

        if (clipConfigurations.Count == 0)
        {
            // Migrate from legacy animationClip
            AnimationClip clip = _legacyAnimationClip;
            if (clip == null)
                clip = GetClipFromAnimator();
            if (clip != null)
            {
                var config = ABTClipConfig.FromClip(clip);
                config.frameSamplingRate = frameSamplingRate;
                config.useKeyframesOnly = useKeyframesOnly;
                config.interpolationMode = interpolationMode;
                clipConfigurations.Add(config);
            }
        }

        return GetActiveConfiguration();
    }

    /// <summary>
    /// Populate clipConfigurations from animatorController or _legacyAnimationClip when empty.
    /// Returns true if any configs were added.
    /// </summary>
    public bool AutoFillClipConfigurations()
    {
        if (clipConfigurations != null && clipConfigurations.Count > 0)
            return false;

        if (clipConfigurations == null)
            clipConfigurations = new List<ABTClipConfig>();

        var clipsToAdd = new List<AnimationClip>();
        var seen = new HashSet<AnimationClip>();

        // 1. animatorController
        if (animatorController != null && animatorController.animationClips != null)
        {
            foreach (var clip in animatorController.animationClips)
            {
                if (clip != null && !seen.Contains(clip))
                {
                    seen.Add(clip);
                    clipsToAdd.Add(clip);
                }
            }
        }

        // 2. _legacyAnimationClip (fallback if no clips from animator)
        if (clipsToAdd.Count == 0 && _legacyAnimationClip != null && !seen.Contains(_legacyAnimationClip))
        {
            seen.Add(_legacyAnimationClip);
            clipsToAdd.Add(_legacyAnimationClip);
        }

#if UNITY_EDITOR
        // 3. animationsDirectory (when animator and legacy yield nothing)
        if (clipsToAdd.Count == 0 && animationsDirectory != null)
        {
            var folderPath = GetFolderPathForDiscovery(animationsDirectory);
            if (!string.IsNullOrEmpty(folderPath))
            {
                var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });
                if (guids != null)
                {
                    foreach (var guid in guids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                        if (clip != null && !seen.Contains(clip))
                        {
                            seen.Add(clip);
                            clipsToAdd.Add(clip);
                        }
                    }
                }
            }
        }
#endif

        if (clipsToAdd.Count == 0)
            return false;

        foreach (var clip in clipsToAdd)
        {
            var config = ABTClipConfig.FromClip(clip);
            config.frameSamplingRate = frameSamplingRate;
            config.useKeyframesOnly = useKeyframesOnly;
            config.interpolationMode = interpolationMode;
            clipConfigurations.Add(config);
        }

        activeClipIndex = 0;
        return true;
    }

#if UNITY_EDITOR
    private static string GetFolderPathForDiscovery(Object folderAsset)
    {
        if (folderAsset == null) return null;
        var path = AssetDatabase.GetAssetPath(folderAsset);
        if (string.IsNullOrEmpty(path)) return null;
        if (!AssetDatabase.IsValidFolder(path) && !Directory.Exists(path))
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) path = dir.Replace("\\", "/");
        }
        return path;
    }
#endif

    /// <summary>
    /// Primary animation clip (from active config). Backward compatibility.
    /// </summary>
    public AnimationClip GetAnimationClip()
    {
        var config = GetOrCreateDefaultConfiguration();
        return config?.clip ?? _legacyAnimationClip ?? GetClipFromAnimator();
    }

    /// <summary>
    /// Primary animation clip (from active config). Backward compatibility for code that reads/writes animationTree.animationClip.
    /// </summary>
    public AnimationClip animationClip
    {
        get => GetAnimationClip();
        set
        {
            _legacyAnimationClip = value;
            var config = GetOrCreateDefaultConfiguration();
            if (config != null)
                config.clip = value;
        }
    }

    /// <summary>
    /// Convert animation to frames using IEnumerable.
    /// </summary>
    public IEnumerable<AnimationFrame> ConvertAnimationToFrames()
    {
        var config = GetOrCreateDefaultConfiguration();
        AnimationClip clip = config?.clip ?? _legacyAnimationClip ?? GetClipFromAnimator();
        if (clip == null)
        {
            Debug.LogError("[AnimationBehaviorTree.ConvertAnimationToFrames] No animation clip found! " +
                "Please assign an AnimationClip or add a clip configuration.");
            yield break;
        }

        int samplingRate = config?.frameSamplingRate ?? frameSamplingRate;
        Debug.Log($"[AnimationBehaviorTree.ConvertAnimationToFrames] Processing clip: {clip.name}, length: {clip.length}s, frameRate: {clip.frameRate}, samplingRate: {samplingRate}");

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
        for (int i = 0; i < totalFrames; i += samplingRate)
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
            var clipVal = GetAnimationClip();
            animationClipName = SafeGetObjectName(clipVal, "null");
        }
        catch (System.Exception)
        {
            animationClipName = "unassigned";
        }

        Debug.Log($"[AnimationBehaviorTree] Starting generation. AnimationClip: {animationClipName}, AnimatorController: {animatorControllerName}");

        // Auto-fill clip configs from animator or legacy clip if empty
        AutoFillClipConfigurations();

        isGenerating = true;

        var config = GetOrCreateDefaultConfiguration();
        if (config == null)
        {
            Debug.LogError("[AnimationBehaviorTree] No clip configuration. Add a clip to clipConfigurations or assign animationClip.");
            isGenerating = false;
            return;
        }

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
            var curves = config.breakoutCurves;
            if (curves != null && curves.Count > 0)
            {
                Debug.Log($"[AnimationBehaviorTree] Applying {curves.Count} breakout curves...");
                ApplyBreakoutCurves(allFrames, curves);
            }

            // Remove dropped frames from active list and add to config.droppedFrames
            int droppedCount = 0;
            foreach (var f in allFrames.Where(f => f != null && f.isDropped).ToList())
            {
                if (!config.droppedFrames.Contains(f))
                    config.droppedFrames.Add(f);
                droppedCount++;
            }
            allFrames.RemoveAll(f => f != null && f.isDropped);
            if (droppedCount > 0)
            {
                Debug.Log($"[AnimationBehaviorTree] Removed {droppedCount} dropped frames. Remaining: {allFrames.Count}");
            }

            // Detect tool usage if enabled
            if (autoDetectToolUsage)
            {
                Debug.Log("[AnimationBehaviorTree] Detecting tool usage requirements...");
                DetectToolUsageRequirements(config);
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
    public void ApplyBreakoutCurves(List<AnimationFrame> frames, List<BreakoutCurve> curves = null)
    {
        if (frames == null)
            return;
        if (curves == null)
            curves = GetActiveConfiguration()?.breakoutCurves;
        if (curves == null || curves.Count == 0)
            return;

        AnimationFrameInterpolator.ApplyBreakoutCurves(frames, curves);
    }

    /// <summary>
    /// Detect tool usage requirements from animation.
    /// </summary>
    public void DetectToolUsageRequirements(ABTClipConfig config = null)
    {
        if (allFrames == null || allFrames.Count == 0)
            return;

        config ??= GetActiveConfiguration();
        if (config?.toolUsageGoals == null)
            return;

        RagdollSystem ragdoll = GetComponent<RagdollSystem>();
        if (ragdoll == null)
            ragdoll = FindAnyObjectByType<RagdollSystem>();

        if (ragdoll == null)
            return;

        AnimationClip clip = config.clip ?? GetAnimationClip();
        List<BehaviorTreeGoal> detectedGoals = AnimationToolUsageDetector.DetectToolUsage(clip, allFrames, ragdoll);

        foreach (var goal in detectedGoals)
        {
            if (goal != null && !config.toolUsageGoals.Contains(goal))
                config.toolUsageGoals.Add(goal);
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

        var config = GetActiveConfiguration();
        if (config?.toolUsageGoals != null && !config.toolUsageGoals.Contains(goal))
            config.toolUsageGoals.Add(goal);
    }

    /// <summary>
    /// Drop a frame (adds to active config's droppedFrames list).
    /// </summary>
    public void DropFrame(AnimationFrame frame)
    {
        if (frame == null || frame.isDropped)
            return;

        var config = GetOrCreateDefaultConfiguration();
        if (config == null)
            return;

        frame.isDropped = true;
        if (config.droppedFrames == null)
            config.droppedFrames = new List<AnimationFrame>();
        if (!config.droppedFrames.Contains(frame))
            config.droppedFrames.Add(frame);

        allFrames.Remove(frame);
        CreateBehaviorTreeStructure();
    }

    /// <summary>
    /// Restore a dropped frame.
    /// </summary>
    public void RestoreDroppedFrame(AnimationFrame frame)
    {
        if (frame == null || !frame.isDropped)
            return;

        var config = GetActiveConfiguration();
        if (config?.droppedFrames == null || !config.droppedFrames.Contains(frame))
            return;

        frame.isDropped = false;
        config.droppedFrames.Remove(frame);

        if (!allFrames.Contains(frame))
        {
            allFrames.Add(frame);
            allFrames = allFrames.OrderBy(f => f.frameIndex).ToList();
        }

        CreateBehaviorTreeStructure();
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

        // Clear existing tree (all roots)
        if (rootNodes != null)
        {
            for (int i = rootNodes.Count - 1; i >= 0; i--)
            {
                if (rootNodes[i] != null && rootNodes[i].gameObject != null)
                {
                    DestroyImmediate(rootNodes[i].gameObject);
                }
            }
            rootNodes.Clear();
        }
        rootNode = null;

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
        rootNode.animationClip = GetAnimationClip();
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
            frameNode.animationClip = GetAnimationClip();
            frameNode.rootBehaviorTree = this;
            frameNode.boneTransforms = new Dictionary<string, TransformData>(frame.boneTransforms);

            // Generate physics card for this frame
            RagdollSystem ragdoll = GetComponent<RagdollSystem>();
            if (ragdoll == null)
                ragdoll = FindAnyObjectByType<RagdollSystem>();

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

        if (rootNodes == null)
            rootNodes = new List<AnimationBehaviorTreeNode>();
        rootNodes.Add(rootNode);
        generatedTree.rootNode = rootNode;
        Debug.Log($"[AnimationBehaviorTree.CreateBehaviorTreeStructure] Complete! Root node: {rootNode.name}, Generated tree root: {generatedTree.rootNode?.name ?? "null"}");
    }

    /// <summary>
    /// Create a new animation root (e.g. for another clip or mode). Does not assign as primary root.
    /// </summary>
    public AnimationBehaviorTreeNode CreateRoot(string name)
    {
        if (rootNodes == null)
            rootNodes = new List<AnimationBehaviorTreeNode>();
        GameObject rootGO = new GameObject(name ?? "AnimationRoot");
        rootGO.transform.SetParent(transform);
        var node = rootGO.AddComponent<AnimationBehaviorTreeNode>();
        node.nodeType = NodeType.Sequence;
        node.rootBehaviorTree = this;
        node.animationClip = GetAnimationClip();
        rootNodes.Add(node);
        if (rootNode == null)
            rootNode = node;
        return node;
    }

    /// <summary>
    /// Get all animation root nodes (for IK trainer and ragdoll storage).
    /// </summary>
    public IReadOnlyList<AnimationBehaviorTreeNode> GetRootNodes()
    {
        if (rootNodes == null)
            rootNodes = new List<AnimationBehaviorTreeNode>();
        return rootNodes;
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
            cardSolver = FindAnyObjectByType<PhysicsCardSolver>();

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

    private void OnEnable()
    {
        var host = GetComponentInParent<ISystemDrawerAnimationRegistration>();
        if (host != null)
            host.RegisterAnimationBehaviorTree(this);
    }

    void IAnimationLayerReporter.RegisterWithHost(ISystemDrawerAnimationRegistration host)
    {
        host?.RegisterAnimationBehaviorTree(this);
    }

    void IAnimationLayerReporter.ReportPlaying(BehaviorTreeNode activeNode, float normalizedTime, int layerId)
    {
        var host = GetComponentInParent<ISystemDrawerAnimationRegistration>();
        host?.NotifyReporterPlayback(this, activeNode, normalizedTime, layerId);
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
