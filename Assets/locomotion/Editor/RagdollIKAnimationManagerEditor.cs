#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for RagdollIKAnimationManager. Adds discovery from prefab directory
/// and from a selected animations directory (developers often pack animations in one folder).
/// </summary>
[CustomEditor(typeof(RagdollIKAnimationManager))]
public class RagdollIKAnimationManagerEditor : Editor
{
    private const string DiscoveredSubfolder = "Discovered";
    private const string TreeSuffix = "_tree";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var manager = (RagdollIKAnimationManager)target;
        if (manager == null) return;

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Animation Discovery", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Discover from prefab: scans the prefab's folder for AnimationClips.\n" +
            "Discover from animations directory: scans the selected folder for clips.\n" +
            "Adds new RagdollAnimationSet entries with created AnimationBehaviorTree prefabs.",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = manager.sourcePrefabForDiscovery != null;
        if (GUILayout.Button("Discover from prefab directory"))
        {
            DiscoverFromPrefabDirectory(manager);
        }
        GUI.enabled = manager.animationsDirectory != null;
        if (GUILayout.Button("Discover from animations directory"))
        {
            DiscoverFromAnimationsDirectory(manager);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    private static string GetFolderPath(Object folderAsset)
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

    /// <summary>Discover animations from a prefab's directory. Called from manager or IK Training window.</summary>
    public static void DiscoverFromPrefab(RagdollIKAnimationManager manager, GameObject prefab)
    {
        if (manager == null || prefab == null) return;
        var prefabPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning("[RagdollIKAnimationManager] Prefab is a scene instance; use a project prefab.");
            return;
        }
        var dir = Path.GetDirectoryName(prefabPath)?.Replace("\\", "/");
        if (string.IsNullOrEmpty(dir)) return;
        DiscoverClipsInFolder(manager, dir, "prefab directory");
    }

    private static void DiscoverFromPrefabDirectory(RagdollIKAnimationManager manager)
    {
        var prefab = manager.sourcePrefabForDiscovery;
        if (prefab == null)
        {
            Debug.LogWarning("[RagdollIKAnimationManager] Assign Source Prefab For Discovery first.");
            return;
        }
        DiscoverFromPrefab(manager, prefab);
    }

    private static void DiscoverFromAnimationsDirectory(RagdollIKAnimationManager manager)
    {
        var folderPath = GetFolderPath(manager.animationsDirectory);
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogWarning("[RagdollIKAnimationManager] Assign Animations Directory (folder from Project) first.");
            return;
        }
        DiscoverClipsInFolder(manager, folderPath, "animations directory");
    }

    private static void DiscoverClipsInFolder(RagdollIKAnimationManager manager, string folderPath, string sourceLabel)
    {
        var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });
        if (guids == null || guids.Length == 0)
        {
            Debug.Log($"[RagdollIKAnimationManager] No AnimationClips found in {sourceLabel}: {folderPath}");
            return;
        }

        var existingClips = new HashSet<string>();
        if (manager.availableAnimations != null)
        {
            foreach (var set in manager.availableAnimations)
            {
                if (set?.animationTree?.animationClip != null)
                    existingClips.Add(AssetDatabase.GetAssetPath(set.animationTree.animationClip));
            }
        }

        var toAdd = new List<RagdollAnimationSet>();
        var outputDir = EnsureDiscoveredFolder(folderPath);

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (existingClips.Contains(path)) continue;

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) continue;

            var abtPrefab = FindOrCreateAnimationBehaviorTreePrefab(clip, outputDir);
            if (abtPrefab == null) continue;

            var set = new RagdollAnimationSet
            {
                displayName = clip.name,
                animationTree = abtPrefab
            };
            toAdd.Add(set);
            existingClips.Add(path);
        }

        Undo.RecordObject(manager, "Discover animations");
        if (manager.availableAnimations == null)
            manager.availableAnimations = new List<RagdollAnimationSet>();
        foreach (var set in toAdd)
            manager.availableAnimations.Add(set);
        EditorUtility.SetDirty(manager);

        Debug.Log($"[RagdollIKAnimationManager] Discovered {toAdd.Count} animations from {sourceLabel} ({folderPath}). Total: {manager.availableAnimations.Count}");
    }

    private static string EnsureDiscoveredFolder(string parentFolder)
    {
        var discoveredPath = parentFolder + "/" + DiscoveredSubfolder;
        var parts = discoveredPath.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
        return current;
    }

    private static AnimationBehaviorTree FindOrCreateAnimationBehaviorTreePrefab(AnimationClip clip, string outputDir)
    {
        var clipPath = AssetDatabase.GetAssetPath(clip);
        if (string.IsNullOrEmpty(clipPath)) return null;

        var clipName = clip.name;
        var prefabName = clipName + TreeSuffix + ".prefab";
        var prefabPath = outputDir + "/" + prefabName;

        var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null)
        {
            var abt = existingPrefab.GetComponent<AnimationBehaviorTree>();
            if (abt != null && abt.animationClip == clip) return abt;
        }

        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
        var newGo = new GameObject(clipName + TreeSuffix);
        var abtNew = newGo.AddComponent<AnimationBehaviorTree>();
        abtNew.animationClip = clip;

        var created = PrefabUtility.SaveAsPrefabAsset(newGo, prefabPath);
        Object.DestroyImmediate(newGo);

        if (created == null) return null;
        var result = created.GetComponent<AnimationBehaviorTree>();
        if (result != null)
            EditorUtility.SetDirty(created);
        return result;
    }
}
#endif
