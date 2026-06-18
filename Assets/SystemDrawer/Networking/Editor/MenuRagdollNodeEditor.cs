#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MenuRagdollNode))]
public sealed class MenuRagdollNodeEditor : Editor
{
    SerializedProperty eventNameProp;
    SerializedProperty serverModeMaskProp;
    SerializedProperty clientRoleMaskProp;
    SerializedProperty managedProp;
    SerializedProperty stackSiblingsProp;
    SerializedProperty fitXProp;
    SerializedProperty fitYProp;
    SerializedProperty stackDirectionProp;
    SerializedProperty wrapDirectionProp;
    SerializedProperty placementModeProp;
    SerializedProperty minSpaceProp;
    SerializedProperty maxSpaceProp;
    SerializedProperty optimalSpaceProp;

    void OnEnable()
    {
        eventNameProp = serializedObject.FindProperty("eventName");
        serverModeMaskProp = serializedObject.FindProperty("serverModeMask");
        clientRoleMaskProp = serializedObject.FindProperty("clientRoleMask");
        managedProp = serializedObject.FindProperty("managedByNetworkRequirements");
        stackSiblingsProp = serializedObject.FindProperty("stackSiblingsHorizontally");
        fitXProp = serializedObject.FindProperty("fitX");
        fitYProp = serializedObject.FindProperty("fitY");
        stackDirectionProp = serializedObject.FindProperty("stackDirection");
        wrapDirectionProp = serializedObject.FindProperty("wrapDirection");
        placementModeProp = serializedObject.FindProperty("placementMode");
        minSpaceProp = serializedObject.FindProperty("minSpace");
        maxSpaceProp = serializedObject.FindProperty("maxSpace");
        optimalSpaceProp = serializedObject.FindProperty("optimalSpace");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var node = (MenuRagdollNode)target;
        bool locked = IsSpecLocked(node);

        EditorGUILayout.LabelField("Menu Ragdoll", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(locked))
        {
            EditorGUILayout.PropertyField(eventNameProp);
            EditorGUILayout.PropertyField(serverModeMaskProp);
            EditorGUILayout.PropertyField(clientRoleMaskProp);
        }
        EditorGUILayout.PropertyField(managedProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spatial 2D", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(locked))
        {
            EditorGUILayout.PropertyField(stackSiblingsProp);
            EditorGUILayout.PropertyField(fitXProp);
            EditorGUILayout.PropertyField(fitYProp);
            EditorGUILayout.PropertyField(stackDirectionProp);
            EditorGUILayout.PropertyField(wrapDirectionProp);
            EditorGUILayout.PropertyField(placementModeProp);
            EditorGUILayout.PropertyField(minSpaceProp);
            EditorGUILayout.PropertyField(maxSpaceProp);
            EditorGUILayout.PropertyField(optimalSpaceProp);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prefabs / Advanced", EditorStyles.boldLabel);
        DrawPropertiesExcluding(serializedObject,
            "m_Script",
            "eventName", "serverModeMask", "clientRoleMask", "managedByNetworkRequirements",
            "stackSiblingsHorizontally", "fitX", "fitY", "stackDirection", "wrapDirection",
            "placementMode", "minSpace", "maxSpace", "optimalSpace");

        serializedObject.ApplyModifiedProperties();
    }

    static bool IsSpecLocked(MenuRagdollNode node)
    {
        if (node == null || !node.managedByNetworkRequirements)
            return false;
        var host = node.GetComponentInParent<MainMenuSpatialGenerator>();
        if (host == null)
            host = Object.FindAnyObjectByType<MainMenuSpatialGenerator>();
        return host != null && host.syncNetworkRequirements;
    }
}
#endif
