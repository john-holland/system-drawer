using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Lives on the ragdoll and owns the IK animation list and "selected for training" state.
/// Syncs selection to RagdollAnimationSetManager and ensures one child per selected animation
/// named {displayName}_animation_tree under the ragdoll actor.
/// </summary>
[AddComponentMenu("Locomotion/Ragdoll IK Animation Manager")]
public class RagdollIKAnimationManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ragdoll system (auto-found on this GameObject or children if null)")]
    public RagdollSystem ragdollSystem;

    [Tooltip("Animation set manager to sync selected sets to (auto-found if null)")]
    public RagdollAnimationSetManager animationSetManager;

    [Header("Available Animations")]
    [Tooltip("List of animation sets available for IK training (same shape as RagdollAnimationSetManager)")]
    public List<RagdollAnimationSet> availableAnimations = new List<RagdollAnimationSet>();

    [Header("Discovery (Editor)")]
    [Tooltip("Prefab whose directory to scan for AnimationClips. Use Discover from prefab directory.")]
    public GameObject sourcePrefabForDiscovery;
    [Tooltip("Animations directory to scan (assign folder from Project). Developers often pack animations in one dir.")]
    public Object animationsDirectory;

    [Header("Selection (for training)")]
    [Tooltip("Indices into availableAnimations that are selected for training in the IK Training window")]
    public List<int> selectedIndicesForTraining = new List<int>();

    private void Awake()
    {
        if (ragdollSystem == null)
            ragdollSystem = GetComponent<RagdollSystem>();
        if (ragdollSystem == null)
            ragdollSystem = GetComponentInChildren<RagdollSystem>();

        if (animationSetManager == null)
            animationSetManager = GetComponent<RagdollAnimationSetManager>();
        if (animationSetManager == null)
            animationSetManager = GetComponentInChildren<RagdollAnimationSetManager>();
    }

    /// <summary>Ragdoll actor transform = RagdollSystem transform (parent for name_animation_tree children).</summary>
    public Transform GetRagdollActorTransform()
    {
        if (ragdollSystem != null)
            return ragdollSystem.transform;
        return transform;
    }

    /// <summary>Read-only list of available animation sets.</summary>
    public IReadOnlyList<RagdollAnimationSet> GetAvailableAnimations()
    {
        return availableAnimations ?? new List<RagdollAnimationSet>();
    }

    /// <summary>Current selected indices for training (copy).</summary>
    public List<int> GetSelectedIndices()
    {
        if (selectedIndicesForTraining == null)
            selectedIndicesForTraining = new List<int>();
        return new List<int>(selectedIndicesForTraining);
    }

    /// <summary>Set which indices are selected for training; call SyncSelectionToSetManagerAndHierarchy after.</summary>
    public void SetSelectedIndices(List<int> indices)
    {
        selectedIndicesForTraining = indices != null ? new List<int>(indices) : new List<int>();
    }

    /// <summary>Set selected indices from a set of indices; call SyncSelectionToSetManagerAndHierarchy after.</summary>
    public void SetSelectedIndices(IEnumerable<int> indices)
    {
        selectedIndicesForTraining = indices != null ? new List<int>(indices) : new List<int>();
    }

    /// <summary>Update RagdollAnimationSetManager.animationSets to the selected sets and ensure children named {displayName}_animation_tree under the ragdoll actor.</summary>
    public void SyncSelectionToSetManagerAndHierarchy()
    {
        var available = availableAnimations;
        if (available == null || selectedIndicesForTraining == null)
            return;

        var selectedSets = new List<RagdollAnimationSet>();
        foreach (int i in selectedIndicesForTraining)
        {
            if (i >= 0 && i < available.Count && available[i] != null)
                selectedSets.Add(available[i]);
        }

        if (animationSetManager != null)
            animationSetManager.animationSets = selectedSets;

        Transform parent = GetRagdollActorTransform();
        if (parent == null)
            return;

        for (int i = 0; i < selectedSets.Count; i++)
        {
            RagdollAnimationSet set = selectedSets[i];
            if (set == null || string.IsNullOrEmpty(set.displayName))
                continue;

            string childName = set.displayName.Trim() + "_animation_tree";
            Transform existing = parent.Find(childName);
            if (existing == null)
            {
                var go = new GameObject(childName);
                go.transform.SetParent(parent, false);
                existing = go.transform;
            }

            var abt = existing.GetComponent<AnimationBehaviorTree>();
            if (abt != null && set.animationTree != null)
                abt.animationClip = set.animationTree.animationClip;
#if UNITY_EDITOR
            if (!Application.isPlaying && existing != null)
                EditorUtility.SetDirty(existing.gameObject);
#endif
        }
#if UNITY_EDITOR
        if (!Application.isPlaying && parent != null)
            EditorUtility.SetDirty(parent.gameObject);
#endif
    }

    /// <summary>Get the list of selected animation sets (for training window).</summary>
    public List<RagdollAnimationSet> GetSelectedAnimationSets()
    {
        var list = new List<RagdollAnimationSet>();
        if (availableAnimations == null || selectedIndicesForTraining == null)
            return list;
        foreach (int i in selectedIndicesForTraining)
        {
            if (i >= 0 && i < availableAnimations.Count && availableAnimations[i] != null)
                list.Add(availableAnimations[i]);
        }
        return list;
    }
}
