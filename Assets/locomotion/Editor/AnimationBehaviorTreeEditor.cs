using System.Collections.Generic;
using System.IO;
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
        serializedObject.Update();

        // Clip configurations list with active index
        var clipConfigurationsProp = serializedObject.FindProperty("clipConfigurations");
        var activeClipIndexProp = serializedObject.FindProperty("activeClipIndex");
        if (clipConfigurationsProp != null)
        {
            EditorGUILayout.LabelField("Clip Configurations", EditorStyles.boldLabel);
            for (int i = 0; i < clipConfigurationsProp.arraySize; i++)
            {
                var elemProp = clipConfigurationsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.PropertyField(elemProp, true);
                EditorGUILayout.EndVertical();
                var clipProp = elemProp.FindPropertyRelative("clip");
                var displayNameProp = elemProp.FindPropertyRelative("displayName");
                var clip = clipProp?.objectReferenceValue as AnimationClip;
                string targetName = clip != null ? clip.name : (displayNameProp?.stringValue ?? "");
                GUI.enabled = targetComponent.animationsDirectory != null && !string.IsNullOrEmpty(targetName);
                if (GUILayout.Button("Update", GUILayout.Width(60)))
                {
                    if (UpdateConfigFromDirectory(targetComponent, i))
                        serializedObject.Update();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
            if (activeClipIndexProp != null && clipConfigurationsProp.arraySize > 0)
            {
                activeClipIndexProp.intValue = EditorGUILayout.IntSlider("Active Clip Index", activeClipIndexProp.intValue, 0, clipConfigurationsProp.arraySize - 1);
            }
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Clip"))
            {
                clipConfigurationsProp.arraySize++;
                var newElem = clipConfigurationsProp.GetArrayElementAtIndex(clipConfigurationsProp.arraySize - 1);
                if (newElem != null)
                {
                    var displayNameProp = newElem.FindPropertyRelative("displayName");
                    if (displayNameProp != null)
                        displayNameProp.stringValue = "New Clip";
                }
            }
            if (GUILayout.Button("Remove Last Clip") && clipConfigurationsProp.arraySize > 0)
            {
                clipConfigurationsProp.arraySize--;
            }
            GUI.enabled = clipConfigurationsProp.arraySize == 0;
            if (GUILayout.Button("Auto Fill Clips"))
            {
                Undo.RecordObject(targetComponent, "Auto Fill Clips");
                targetComponent.AutoFillClipConfigurations();
                EditorUtility.SetDirty(targetComponent);
                serializedObject.Update();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        DrawPropertiesExcluding(serializedObject, "clipConfigurations", "activeClipIndex", "m_Script");

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Behavior Tree"))
        {
            targetComponent.GenerateBehaviorTree();
        }

        if (GUILayout.Button("Open Timeline Window"))
        {
            AnimationBehaviorTreeTimelineWindow.OpenWindow(targetComponent);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tool Usage Goals", EditorStyles.boldLabel);

        if (GUILayout.Button("Auto-Detect Tool Usage"))
        {
            targetComponent.autoDetectToolUsage = true;
            targetComponent.DetectToolUsageRequirements();
        }

        var config = targetComponent.GetActiveConfiguration();
        var droppedFrames = config?.droppedFrames;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dropped Frames", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Dropped Frames Count: {droppedFrames?.Count ?? 0}");

        if (droppedFrames != null && droppedFrames.Count > 0)
        {
            if (GUILayout.Button("Restore All Dropped Frames"))
            {
                var framesToRestore = new List<AnimationFrame>(droppedFrames);
                foreach (var frame in framesToRestore)
                {
                    targetComponent.RestoreDroppedFrame(frame);
                }
            }

            if (GUILayout.Button("Clear Dropped Frames"))
            {
                droppedFrames.Clear();
            }
        }
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

    private static bool UpdateConfigFromDirectory(AnimationBehaviorTree abt, int configIndex)
    {
        if (abt?.animationsDirectory == null || abt.clipConfigurations == null || configIndex < 0 || configIndex >= abt.clipConfigurations.Count)
            return false;
        var config = abt.clipConfigurations[configIndex];
        string targetName = config.clip != null ? config.clip.name : (config.displayName ?? "");
        if (string.IsNullOrEmpty(targetName)) return false;
        string folderPath = GetFolderPath(abt.animationsDirectory);
        if (string.IsNullOrEmpty(folderPath)) return false;
        var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null && clip.name == targetName)
            {
                Undo.RecordObject(abt, "Update clip from directory");
                config.clip = clip;
                EditorUtility.SetDirty(abt);
                if (configIndex == abt.activeClipIndex)
                    abt.GenerateBehaviorTree();
                return true;
            }
        }
        return false;
    }
}
