#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RagdollLimbCapsuleFit))]
public sealed class RagdollLimbCapsuleFitEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var fit = (RagdollLimbCapsuleFit)target;
        EditorGUILayout.HelpBox(
            "Adjust translation/rotation here — no Scene gizmo required. " +
            "Use +90° when a hand capsule points like a wristwatch on one rig but not another.",
            MessageType.Info);

        DrawDefaultInspector();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Quick rotate", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+90° X")) { Undo.RecordObject(fit, "Rotate capsule +90 X"); fit.RotateAxisDegrees(0, 90f); EditorUtility.SetDirty(fit); }
        if (GUILayout.Button("+90° Y")) { Undo.RecordObject(fit, "Rotate capsule +90 Y"); fit.RotateAxisDegrees(1, 90f); EditorUtility.SetDirty(fit); }
        if (GUILayout.Button("+90° Z")) { Undo.RecordObject(fit, "Rotate capsule +90 Z"); fit.RotateAxisDegrees(2, 90f); EditorUtility.SetDirty(fit); }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("-90° X")) { Undo.RecordObject(fit, "Rotate capsule -90 X"); fit.RotateAxisDegrees(0, -90f); EditorUtility.SetDirty(fit); }
        if (GUILayout.Button("-90° Y")) { Undo.RecordObject(fit, "Rotate capsule -90 Y"); fit.RotateAxisDegrees(1, -90f); EditorUtility.SetDirty(fit); }
        if (GUILayout.Button("-90° Z")) { Undo.RecordObject(fit, "Rotate capsule -90 Z"); fit.RotateAxisDegrees(2, -90f); EditorUtility.SetDirty(fit); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Nudge center (local cm)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("← X")) { Undo.RecordObject(fit, "Nudge capsule"); fit.NudgeLocal(new Vector3(-0.01f, 0, 0)); EditorUtility.SetDirty(fit); }
        if (GUILayout.Button("→ X")) { Undo.RecordObject(fit, "Nudge capsule"); fit.NudgeLocal(new Vector3(0.01f, 0, 0)); EditorUtility.SetDirty(fit); }
        if (GUILayout.Button("↑ Y")) { Undo.RecordObject(fit, "Nudge capsule"); fit.NudgeLocal(new Vector3(0, 0.01f, 0)); EditorUtility.SetDirty(fit); }
        if (GUILayout.Button("↓ Y")) { Undo.RecordObject(fit, "Nudge capsule"); fit.NudgeLocal(new Vector3(0, -0.01f, 0)); EditorUtility.SetDirty(fit); }
        if (GUILayout.Button("⊕ Z")) { Undo.RecordObject(fit, "Nudge capsule"); fit.NudgeLocal(new Vector3(0, 0, 0.01f)); EditorUtility.SetDirty(fit); }
        if (GUILayout.Button("⊖ Z")) { Undo.RecordObject(fit, "Nudge capsule"); fit.NudgeLocal(new Vector3(0, 0, -0.01f)); EditorUtility.SetDirty(fit); }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Apply", GUILayout.Height(24)))
        {
            Undo.RecordObject(fit, "Apply limb capsule fit");
            fit.Apply();
            EditorUtility.SetDirty(fit);
        }
    }
}
#endif
