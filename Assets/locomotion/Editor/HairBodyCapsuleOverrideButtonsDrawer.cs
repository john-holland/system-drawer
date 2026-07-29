using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HairBodyCapsuleOverrideButtonsAttribute))]
public sealed class HairBodyCapsuleOverrideButtonsDrawer : PropertyDrawer
{
    const float ButtonHeight = 28f;
    const float Spacing = 4f;
    const float HelpHeight = 36f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return ButtonHeight + Spacing + HelpHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Rect row = new Rect(position.x, position.y, position.width, ButtonHeight);
        float half = (row.width - Spacing) * 0.5f;
        Rect autoRect = new Rect(row.x, row.y, half, ButtonHeight);
        Rect clearRect = new Rect(row.x + half + Spacing, row.y, half, ButtonHeight);

        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.55f, 0.9f, 0.55f);
        if (GUI.Button(autoRect, "Auto Set Optional Overrides"))
            RunOnTargets(property, autoSet: true);
        GUI.backgroundColor = prev;

        if (GUI.Button(clearRect, "Clear Overrides"))
            RunOnTargets(property, autoSet: false);

        Rect help = new Rect(position.x, position.y + ButtonHeight + Spacing, position.width, HelpHeight);
        EditorGUI.HelpBox(help,
            "Fills Head / Chest / Shoulders / Arms / Hands / Knees from RagdollSystem or Humanoid Animator.",
            MessageType.Info);
    }

    static void RunOnTargets(SerializedProperty property, bool autoSet)
    {
        var so = property.serializedObject;
        so.ApplyModifiedProperties();
        foreach (Object obj in so.targetObjects)
        {
            var binder = obj as HairBodyCapsuleBinder;
            if (binder == null) continue;
            Undo.RecordObject(binder, autoSet ? "Auto Set Body Capsule Overrides" : "Clear Body Capsule Overrides");
            if (autoSet)
            {
                int n = binder.AutoSetOptionalOverrides();
                Debug.Log($"[HairBodyCapsuleBinder] Auto-set {n} bone override(s) on '{binder.name}'.", binder);
            }
            else
            {
                binder.ClearOptionalOverrides();
            }
            EditorUtility.SetDirty(binder);
        }
        so.Update();
    }
}
