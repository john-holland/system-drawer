using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coordinates multiple <see cref="AnimationBehaviorTree"/> layers on a system-drawer ragdoll actor:
/// ordered evaluation, blend weights, snapshots, and play-order validation.
/// Place on the ragdoll root (or same hierarchy as <see cref="RagdollSystem"/>).
/// </summary>
[AddComponentMenu("Locomotion/System Drawer Animator")]
public class SystemDrawerAnimator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ragdoll driven by animation layers; defaults to self or children.")]
    public RagdollSystem ragdollSystem;

    [Header("Layers")]
    [Tooltip("Registered animation trees and per-layer weights. Use Refresh Animation Trees to populate from children.")]
    public List<AnimationLayerSlot> layers = new List<AnimationLayerSlot>();

    [Header("Evaluation order")]
    [Tooltip("Ordered layer indices (slot.layerIndex values). First entry ticks first. Empty = sort by layer index ascending.")]
    public List<int> playOrder = new List<int>();

    [Tooltip("When true, violations in AssertPlayOrder throw in editor and development builds.")]
    public bool strictPlayOrder;

    [Tooltip("When true, Brain skips BehaviorTree.Execute for trees managed by this animator.")]
    public bool ownsBehaviorTreeExecution = true;

    [Tooltip("Minimum weight to run Execute on a layer.")]
    [SerializeField] private float weightEpsilon = 0.0001f;

    [Header("Debug")]
    [Tooltip("Draw IMGUI overlay with layer state and last assert result.")]
    public bool showRuntimeOverlay;

    [Tooltip("Optional: register this animator on SystemDrawerService under this key for lookup.")]
    public string systemDrawerRegisterKey = "";

    [Header("Animation set manager")]
    [Tooltip("When true, RagdollAnimationSetManager.Play does not switch trees (animator drives layers).")]
    public bool deferAnimationSetManagerPlayback;

    private readonly List<AnimationPlaybackSnapshot> _snapshots = new List<AnimationPlaybackSnapshot>();
    private readonly Dictionary<int, int> _tickPhaseByLayerIndex = new Dictionary<int, int>();
    private readonly HashSet<AnimationBehaviorTree> _registeredTrees = new HashSet<AnimationBehaviorTree>();
    private readonly Dictionary<AnimationBehaviorTree, int> _instanceIds = new Dictionary<AnimationBehaviorTree, int>();
    private int _nextInstanceId = 1;

    private int _reportSequence;
    private string _lastAssertMessage = "OK";
    private bool _lastAssertOk = true;

    /// <summary>Last ActiveSnapshots from the previous TickLayers.</summary>
    public IReadOnlyList<AnimationPlaybackSnapshot> ActiveSnapshots => _snapshots;

    /// <summary>False when the last AssertPlayOrder detected a violation.</summary>
    public bool LastAssertPassed => _lastAssertOk;

    /// <summary>Human-readable status from the last assert.</summary>
    public string LastAssertMessage => _lastAssertMessage;

    private void Awake()
    {
        if (ragdollSystem == null)
            ragdollSystem = GetComponent<RagdollSystem>();
        if (ragdollSystem == null)
            ragdollSystem = GetComponentInChildren<RagdollSystem>();
    }

    private void OnEnable()
    {
        if (!string.IsNullOrWhiteSpace(systemDrawerRegisterKey) && SystemDrawerService.Instance != null)
            SystemDrawerService.Instance.Register(systemDrawerRegisterKey.Trim(), this);
    }

    private void OnDisable()
    {
        if (!string.IsNullOrWhiteSpace(systemDrawerRegisterKey) && SystemDrawerService.Instance != null)
            SystemDrawerService.Instance.Unregister(systemDrawerRegisterKey.Trim());
    }

    private void LateUpdate()
    {
        TickLayers();
    }

    /// <summary>
    /// True if this animator should run <see cref="BehaviorTree.Execute"/> for <paramref name="tree"/>
    /// (owned layer with positive weight).
    /// </summary>
    public bool ManagesBehaviorTree(BehaviorTree tree)
    {
        if (!ownsBehaviorTreeExecution || tree == null || layers == null)
            return false;
        foreach (var slot in layers)
        {
            if (slot == null || slot.animationBehaviorTree == null)
                continue;
            if (slot.animationBehaviorTree.generatedTree == tree && slot.weight > weightEpsilon)
                return true;
        }
        return false;
    }

    /// <summary>When true, <see cref="RagdollAnimationSetManager.Play"/> returns early.</summary>
    public bool ShouldDeferSetManagerPlayback() =>
        deferAnimationSetManagerPlayback && ownsBehaviorTreeExecution && layers != null && layers.Count > 0;

    /// <summary>Assign layer weight by <see cref="AnimationLayerSlot.layerIndex"/>.</summary>
    public void SetLayerWeight(int layerIndex, float weight)
    {
        if (layers == null) return;
        foreach (var slot in layers)
        {
            if (slot != null && slot.layerIndex == layerIndex)
            {
                slot.weight = Mathf.Clamp01(weight);
                return;
            }
        }
    }

    /// <summary>Read layer weight; returns -1 if layer index not found.</summary>
    public float GetLayerWeight(int layerIndex)
    {
        if (layers == null) return -1f;
        foreach (var slot in layers)
        {
            if (slot != null && slot.layerIndex == layerIndex)
                return slot.weight;
        }
        return -1f;
    }

    /// <summary>Set ordered layer indices (copied into <see cref="playOrder"/>).</summary>
    public void SetPlayOrder(IReadOnlyList<int> layerIndicesOrdered)
    {
        playOrder = layerIndicesOrdered != null ? new List<int>(layerIndicesOrdered) : new List<int>();
    }

    /// <summary>Scan under this transform for all <see cref="AnimationBehaviorTree"/> components.</summary>
    public void RefreshAnimationTrees()
    {
        var found = GetComponentsInChildren<AnimationBehaviorTree>(true);
        foreach (var abt in found)
        {
            if (abt == null || _registeredTrees.Contains(abt))
                continue;
            RegisterAnimationBehaviorTree(abt);
        }
    }

    /// <summary>
    /// Auto-registration from <see cref="AnimationBehaviorTree"/> OnEnable.
    /// Adds a new layer slot if none references this tree.
    /// </summary>
    public void RegisterAnimationBehaviorTree(AnimationBehaviorTree abt)
    {
        if (abt == null)
            return;
        _registeredTrees.Add(abt);
        if (!_instanceIds.TryGetValue(abt, out _))
            _instanceIds[abt] = _nextInstanceId++;

        if (layers == null)
            layers = new List<AnimationLayerSlot>();

        foreach (var slot in layers)
        {
            if (slot != null && slot.animationBehaviorTree == abt)
                return;
        }

        int maxIdx = 0;
        foreach (var slot in layers)
        {
            if (slot != null && slot.layerIndex > maxIdx)
                maxIdx = slot.layerIndex;
        }

        layers.Add(new AnimationLayerSlot
        {
            animationBehaviorTree = abt,
            layerIndex = maxIdx + 1,
            weight = 1f,
            displayName = abt.gameObject.name
        });
    }

    /// <summary>Optional: nested child tree notifies parent animator after local procedural steps.</summary>
    public void NotifyChildTreeState(AnimationBehaviorTree childTree, BehaviorTreeNode activeNode, float normalizedTime)
    {
        NotifyReporterPlayback(childTree, activeNode, normalizedTime, -1);
    }

    /// <summary>Called from <see cref="IAnimationLayerReporter.ReportPlaying"/> for nested/procedural reporting.</summary>
    public void NotifyReporterPlayback(AnimationBehaviorTree tree, BehaviorTreeNode activeNode, float normalizedTime, int layerId)
    {
        _reportSequence++;
    }

    private void TickLayers()
    {
        _snapshots.Clear();
        _tickPhaseByLayerIndex.Clear();
        _reportSequence = 0;

        if (layers == null || layers.Count == 0)
        {
            AssertPlayOrder();
            return;
        }

        List<int> orderedSlotIndices = GetOrderedSlotIndices();
        int phase = 0;

        foreach (int slotIndex in orderedSlotIndices)
        {
            if (slotIndex < 0 || slotIndex >= layers.Count)
                continue;
            AnimationLayerSlot slot = layers[slotIndex];
            if (slot == null || slot.animationBehaviorTree == null)
            {
                _snapshots.Add(AnimationPlaybackSnapshot.Empty);
                continue;
            }

            if (slot.weight <= weightEpsilon)
            {
                _snapshots.Add(BuildSnapshot(slot, null, 0f));
                continue;
            }

            BehaviorTree gen = slot.animationBehaviorTree.generatedTree;
            if (gen == null)
            {
                _snapshots.Add(BuildSnapshot(slot, null, slot.weight));
                continue;
            }

            _tickPhaseByLayerIndex[slot.layerIndex] = phase++;

            try
            {
                gen.Execute();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SystemDrawerAnimator] Execute failed on layer {slot.layerIndex}: {e.Message}", this);
            }

            ApplyAdditiveMuscleGroups(slot);
            BehaviorTreeNode node = gen.currentNode;
            _snapshots.Add(BuildSnapshot(slot, node, slot.weight));
        }

        AssertPlayOrder();
    }

    private void ApplyAdditiveMuscleGroups(AnimationLayerSlot slot)
    {
        if (ragdollSystem == null || slot.additiveMuscleGroups == null || slot.additiveMuscleGroups.Count == 0)
            return;
        float w = Mathf.Clamp01(slot.weight);
        foreach (string groupName in slot.additiveMuscleGroups)
        {
            if (string.IsNullOrEmpty(groupName))
                continue;
            ragdollSystem.ActivateMuscleGroup(groupName, w);
        }
    }

    private AnimationPlaybackSnapshot BuildSnapshot(AnimationLayerSlot slot, BehaviorTreeNode node, float weight)
    {
        var abt = slot.animationBehaviorTree;
        int id = abt != null && _instanceIds.TryGetValue(abt, out int iid) ? iid : -1;
        string treeLabel = !string.IsNullOrEmpty(slot.displayName)
            ? slot.displayName
            : (abt != null ? abt.gameObject.name : "?");

        return new AnimationPlaybackSnapshot
        {
            treeName = treeLabel,
            activeNodeName = node != null ? node.gameObject.name : "(none)",
            weight = weight,
            layerIndex = slot.layerIndex,
            normalizedTime = 0f,
            registeredInstanceId = id
        };
    }

    private List<int> GetOrderedSlotIndices()
    {
        var result = new List<int>();
        var seenSlot = new HashSet<int>();

        if (playOrder != null && playOrder.Count > 0)
        {
            foreach (int layerIdx in playOrder)
            {
                int si = FindSlotIndexForLayerIndex(layerIdx);
                if (si >= 0 && seenSlot.Add(si))
                    result.Add(si);
            }
        }

        var remaining = new List<int>();
        for (int i = 0; i < layers.Count; i++)
        {
            if (!seenSlot.Contains(i))
                remaining.Add(i);
        }

        remaining.Sort((a, b) => layers[a].layerIndex.CompareTo(layers[b].layerIndex));
        result.AddRange(remaining);
        return result;
    }

    private int FindSlotIndexForLayerIndex(int layerIndex)
    {
        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i] != null && layers[i].layerIndex == layerIndex)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Validates tick ordering: play-order monotonicity for ticked layers and parent-before-child for nested ABT hierarchies.
    /// </summary>
    public void AssertPlayOrder()
    {
        _lastAssertOk = true;
        _lastAssertMessage = "OK";

        if (layers == null || layers.Count == 0)
            return;

        // Monotonicity along configured playOrder: later entries must have executed after earlier ones (when both ticked)
        if (playOrder != null && playOrder.Count >= 2)
        {
            int? prevPhase = null;
            foreach (int layerIdx in playOrder)
            {
                if (!_tickPhaseByLayerIndex.TryGetValue(layerIdx, out int phase))
                    continue;
                if (prevPhase.HasValue && phase <= prevPhase.Value)
                {
                    FailAssert($"Play order violation: layer {layerIdx} phase {phase} <= previous phase {prevPhase.Value}.");
                    return;
                }
                prevPhase = phase;
            }
        }

        // Nested trees: parent AnimationBehaviorTree transform ancestor of child's — parent phase < child phase when both ticked
        for (int i = 0; i < layers.Count; i++)
        {
            for (int j = 0; j < layers.Count; j++)
            {
                if (i == j) continue;
                AnimationLayerSlot a = layers[i];
                AnimationLayerSlot b = layers[j];
                if (a?.animationBehaviorTree == null || b?.animationBehaviorTree == null)
                    continue;
                if (!IsNestedUnder(b.animationBehaviorTree, a.animationBehaviorTree))
                    continue;
                if (!_tickPhaseByLayerIndex.TryGetValue(a.layerIndex, out int pa))
                    continue;
                if (!_tickPhaseByLayerIndex.TryGetValue(b.layerIndex, out int pb))
                    continue;
                if (pa >= pb)
                {
                    FailAssert($"Hierarchy order violation: nested tree '{b.animationBehaviorTree.name}' (layer {b.layerIndex}) must tick after parent '{a.animationBehaviorTree.name}' (layer {a.layerIndex}).");
                    return;
                }
            }
        }
    }

    private static bool IsNestedUnder(AnimationBehaviorTree child, AnimationBehaviorTree parent)
    {
        return child != null && parent != null && child.transform != parent.transform && child.transform.IsChildOf(parent.transform);
    }

    private void FailAssert(string message)
    {
        _lastAssertOk = false;
        _lastAssertMessage = message;
        Debug.LogError("[SystemDrawerAnimator] " + message, this);
        if (strictPlayOrder)
        {
#if UNITY_EDITOR
            throw new InvalidOperationException("[SystemDrawerAnimator] " + message);
#endif
        }
    }

    private void OnGUI()
    {
        if (!showRuntimeOverlay || !Application.isPlaying)
            return;

        const float w = 420f;
        float x = 10f;
        float y = 10f;
        GUILayout.BeginArea(new Rect(x, y, w, 260f));
        GUILayout.Label($"SystemDrawerAnimator ({name})");
        GUILayout.Label($"Assert: {(_lastAssertOk ? "OK" : "FAIL")} — {_lastAssertMessage}");

        foreach (var s in _snapshots)
        {
            GUILayout.Label($"L{s.layerIndex} [{s.treeName}] {s.activeNodeName} w={s.weight:F2}");
        }

        GUILayout.EndArea();
    }
}
