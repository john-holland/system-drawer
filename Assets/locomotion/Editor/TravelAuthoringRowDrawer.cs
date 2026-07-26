#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Property drawer for TravelAuthoringRow: Zoom button beside worldPosition for Scene framing.
/// </summary>
[CustomPropertyDrawer(typeof(TravelAuthoringRow))]
public class TravelAuthoringRowDrawer : PropertyDrawer
{
    const float ZoomWidth = 52f;
    const float Spacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        float h = EditorGUIUtility.singleLineHeight + Spacing;
        SerializedProperty iterator = property.Copy();
        SerializedProperty end = property.GetEndProperty();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
        {
            enterChildren = false;
            h += EditorGUI.GetPropertyHeight(iterator, true) + Spacing;
        }
        return h;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty worldPos = property.FindPropertyRelative("worldPosition");
        if (worldPos == null)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        // Draw full default foldout, then overlay Zoom on the worldPosition line when expanded.
        EditorGUI.BeginProperty(position, label, property);
        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded,
            label,
            true);

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        float y = position.y + EditorGUIUtility.singleLineHeight + Spacing;
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel++;

        SerializedProperty iterator = property.Copy();
        SerializedProperty end = property.GetEndProperty();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
        {
            enterChildren = false;
            float h = EditorGUI.GetPropertyHeight(iterator, true);
            Rect row = new Rect(position.x, y, position.width, h);
            if (iterator.name == "worldPosition")
            {
                Rect field = new Rect(row.x, row.y, row.width - ZoomWidth - 4f, EditorGUIUtility.singleLineHeight);
                Rect btn = new Rect(row.xMax - ZoomWidth, row.y, ZoomWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(field, iterator, true);
                if (GUI.Button(btn, "Zoom"))
                    TravelPathingEditorWindow.FrameWorldPoint(iterator.vector3Value);
            }
            else
            {
                EditorGUI.PropertyField(row, iterator, true);
            }

            y += h + Spacing;
        }

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }
}
#endif
