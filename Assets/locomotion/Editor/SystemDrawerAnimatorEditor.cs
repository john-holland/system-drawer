using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector for <see cref="SystemDrawerAnimator"/>: refresh discovered trees, layer table, play order hints.
/// </summary>
[CustomEditor(typeof(SystemDrawerAnimator))]
public class SystemDrawerAnimatorEditor : Editor
{
    private SerializedProperty _ragdoll;
    private SerializedProperty _layers;
    private SerializedProperty _playOrder;
    private SerializedProperty _strictPlayOrder;
    private SerializedProperty _ownsExecution;
    private SerializedProperty _weightEpsilon;
    private SerializedProperty _showOverlay;
    private SerializedProperty _registerKey;
    private SerializedProperty _deferSetManager;

    private void OnEnable()
    {
        _ragdoll = serializedObject.FindProperty("ragdollSystem");
        _layers = serializedObject.FindProperty("layers");
        _playOrder = serializedObject.FindProperty("playOrder");
        _strictPlayOrder = serializedObject.FindProperty("strictPlayOrder");
        _ownsExecution = serializedObject.FindProperty("ownsBehaviorTreeExecution");
        _weightEpsilon = serializedObject.FindProperty("weightEpsilon");
        _showOverlay = serializedObject.FindProperty("showRuntimeOverlay");
        _registerKey = serializedObject.FindProperty("systemDrawerRegisterKey");
        _deferSetManager = serializedObject.FindProperty("deferAnimationSetManagerPlayback");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var anim = (SystemDrawerAnimator)target;

        EditorGUILayout.PropertyField(_ragdoll);
        EditorGUILayout.PropertyField(_ownsExecution);
        EditorGUILayout.PropertyField(_weightEpsilon);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_layers, true);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Evaluation order", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Play Order lists layer indices (the Layer Index field on each slot). Empty = ascending layer index. Nested AnimationBehaviorTree children must tick after their parent (AssertPlayOrder).",
            MessageType.None);
        EditorGUILayout.PropertyField(_playOrder, true);
        EditorGUILayout.PropertyField(_strictPlayOrder);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Integration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_deferSetManager);
        EditorGUILayout.PropertyField(_registerKey);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_showOverlay);

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Animation Trees", GUILayout.Height(24)))
        {
            Undo.RecordObject(anim, "Refresh Animation Trees");
            anim.RefreshAnimationTrees();
            EditorUtility.SetDirty(anim);
        }
        if (GUILayout.Button("Clear Layers List", GUILayout.Height(24)))
        {
            if (EditorUtility.DisplayDialog("Clear layers", "Remove all layer slots?", "Clear", "Cancel"))
            {
                Undo.RecordObject(anim, "Clear Layers");
                anim.layers.Clear();
                EditorUtility.SetDirty(anim);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (Application.isPlaying && anim.ActiveSnapshots != null && anim.ActiveSnapshots.Count > 0)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Playback (play mode)", EditorStyles.boldLabel);
            foreach (var s in anim.ActiveSnapshots)
            {
                EditorGUILayout.LabelField(
                    $"L{s.layerIndex}  {s.treeName}  →  {s.activeNodeName}  w={s.weight:F2}");
            }
            EditorGUILayout.LabelField("Assert", anim.LastAssertPassed ? "OK" : "FAIL");
            EditorGUILayout.LabelField(anim.LastAssertMessage, EditorStyles.wordWrappedMiniLabel);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
