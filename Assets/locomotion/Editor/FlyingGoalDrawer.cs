#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Property drawer for FlyingGoal: groups duration and three axis curves (X, Y, Z) with a 3D curve header.
/// </summary>
[CustomPropertyDrawer(typeof(FlyingGoal))]
public class FlyingGoalDrawer : PropertyDrawer
{
    private const float LineHeight = 18f;
    private const float Spacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight + Spacing;

        SerializedProperty curveX = property.FindPropertyRelative("curveX");
        SerializedProperty curveY = property.FindPropertyRelative("curveY");
        SerializedProperty curveZ = property.FindPropertyRelative("curveZ");
        float curveH = EditorGUIUtility.singleLineHeight;
        if (curveX != null) curveH = Mathf.Max(curveH, EditorGUI.GetPropertyHeight(curveX, GUIContent.none, true));
        if (curveY != null) curveH = Mathf.Max(curveH, EditorGUI.GetPropertyHeight(curveY, GUIContent.none, true));
        if (curveZ != null) curveH = Mathf.Max(curveH, EditorGUI.GetPropertyHeight(curveZ, GUIContent.none, true));

        float h = EditorGUIUtility.singleLineHeight + Spacing; // duration
        h += EditorGUIUtility.singleLineHeight + Spacing; // "3D Curve" label
        h += EditorGUIUtility.singleLineHeight + Spacing; // X label
        h += curveH + Spacing; // curve X
        h += EditorGUIUtility.singleLineHeight + Spacing; // Y label
        h += curveH + Spacing; // curve Y
        h += EditorGUIUtility.singleLineHeight + Spacing; // Z label
        h += curveH + Spacing; // curve Z
        return h;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty duration = property.FindPropertyRelative("duration");
        SerializedProperty curveX = property.FindPropertyRelative("curveX");
        SerializedProperty curveY = property.FindPropertyRelative("curveY");
        SerializedProperty curveZ = property.FindPropertyRelative("curveZ");

        if (duration == null || curveX == null || curveY == null || curveZ == null)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded, label, true);

        if (!property.isExpanded)
            return;

        float y = position.y + EditorGUIUtility.singleLineHeight + Spacing;
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel++;

        // Duration
        Rect rDuration = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(rDuration, duration, new GUIContent("Duration (s)"));
        y += EditorGUIUtility.singleLineHeight + Spacing;

        // 3D Curve header + Set defaults button
        Rect rHeader = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(rHeader, "3D Curve (normalized time 0→1 → world position)", EditorStyles.miniLabel);
        Rect rBtn = new Rect(position.xMax - 100f, y, 98f, EditorGUIUtility.singleLineHeight);
        if (GUI.Button(rBtn, "Set default arc"))
        {
            curveX.animationCurveValue = FlyingGoal.DefaultCurveX();
            curveY.animationCurveValue = FlyingGoal.DefaultCurveY();
            curveZ.animationCurveValue = FlyingGoal.DefaultCurveZ();
        }
        y += EditorGUIUtility.singleLineHeight + Spacing;

        float curveH = Mathf.Max(EditorGUIUtility.singleLineHeight,
            EditorGUI.GetPropertyHeight(curveX, GUIContent.none, true));

        // X
        EditorGUI.LabelField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), "X", EditorStyles.miniLabel);
        y += EditorGUIUtility.singleLineHeight;
        Rect rX = new Rect(position.x, y, position.width, curveH);
        EditorGUI.PropertyField(rX, curveX, GUIContent.none);
        y += curveH + Spacing;

        // Y
        EditorGUI.LabelField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), "Y", EditorStyles.miniLabel);
        y += EditorGUIUtility.singleLineHeight;
        Rect rY = new Rect(position.x, y, position.width, curveH);
        EditorGUI.PropertyField(rY, curveY, GUIContent.none);
        y += curveH + Spacing;

        // Z
        EditorGUI.LabelField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), "Z", EditorStyles.miniLabel);
        y += EditorGUIUtility.singleLineHeight;
        Rect rZ = new Rect(position.x, y, position.width, curveH);
        EditorGUI.PropertyField(rZ, curveZ, GUIContent.none);

        EditorGUI.indentLevel = indent;
    }
}
#endif
