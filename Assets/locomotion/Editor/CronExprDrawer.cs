using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CronExprAttribute))]
public sealed class CronExprDrawer : PropertyDrawer
{
    const float Pad = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var line = EditorGUIUtility.singleLineHeight;
        var help = HelpHeight(property.stringValue);
        return line + Pad + help;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var line = EditorGUIUtility.singleLineHeight;
        var fieldRect = new Rect(position.x, position.y, position.width, line);
        EditorGUI.PropertyField(fieldRect, property, label);

        var narrative = CronHumanize.Describe(property.stringValue);
        var helpRect = new Rect(position.x, position.y + line + Pad, position.width, HelpHeight(property.stringValue));
        EditorGUI.HelpBox(helpRect, narrative, MessageType.None);
        EditorGUI.EndProperty();
    }

    static float HelpHeight(string cron)
    {
        var text = CronHumanize.Describe(cron);
        var width = EditorGUIUtility.currentViewWidth - 40f;
        if (width < 80f) width = 200f;
        var style = EditorStyles.helpBox;
        return Mathf.Max(EditorGUIUtility.singleLineHeight * 1.5f, style.CalcHeight(new GUIContent(text), width));
    }
}
