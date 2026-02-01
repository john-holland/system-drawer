using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SGBehaviorTreeNode))]
public class SGBehaviorTreeNodeEditor : Editor
{
    private SerializedProperty fitXProp;
    private SerializedProperty fitYProp;
    private SerializedProperty fitZProp;
    private SerializedProperty stackDirectionProp;
    private SerializedProperty wrapDirectionProp;
    private SerializedProperty placeFlushProp;
    private SerializedProperty placeSearchModeProp;
    private SerializedProperty placementModeProp;
    private SerializedProperty minSpaceProp;
    private SerializedProperty maxSpaceProp;
    private SerializedProperty optimalSpaceProp;
    private SerializedProperty gameObjectPrefabsProp;

    private void OnEnable()
    {
        fitXProp = serializedObject.FindProperty("fitX");
        fitYProp = serializedObject.FindProperty("fitY");
        fitZProp = serializedObject.FindProperty("fitZ");
        stackDirectionProp = serializedObject.FindProperty("stackDirection");
        wrapDirectionProp = serializedObject.FindProperty("wrapDirection");
        placeFlushProp = serializedObject.FindProperty("placeFlush");
        placeSearchModeProp = serializedObject.FindProperty("placeSearchMode");
        placementModeProp = serializedObject.FindProperty("placementMode");
        minSpaceProp = serializedObject.FindProperty("minSpace");
        maxSpaceProp = serializedObject.FindProperty("maxSpace");
        optimalSpaceProp = serializedObject.FindProperty("optimalSpace");
        gameObjectPrefabsProp = serializedObject.FindProperty("gameObjectPrefabs");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SGBehaviorTreeNode node = (SGBehaviorTreeNode)target;

        DrawDefaultInspectorExceptFitStack();

        EditorGUILayout.Space(2f);
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = node.gameObjectPrefabs != null && node.gameObjectPrefabs.Count > 0;
        if (GUILayout.Button("Fit Size to Prefab Bounds"))
        {
            FitSizeToPrefabBounds(node);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Fit / Stack", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Fit X", EditorStyles.miniLabel);
        fitXProp.enumValueIndex = GUILayout.SelectionGrid(fitXProp.enumValueIndex, new[] { "Center", "Left", "Right" }, 3);
        EditorGUILayout.LabelField("Fit Y", EditorStyles.miniLabel);
        fitYProp.enumValueIndex = GUILayout.SelectionGrid(fitYProp.enumValueIndex, new[] { "Center", "Down", "Up" }, 3);
        EditorGUILayout.LabelField("Fit Z", EditorStyles.miniLabel);
        fitZProp.enumValueIndex = GUILayout.SelectionGrid(fitZProp.enumValueIndex, new[] { "Center", "Backward", "Forward" }, 3);
        EditorGUILayout.Space(2f);
        EditorGUILayout.PropertyField(stackDirectionProp, new GUIContent("Stack Direction"));
        EditorGUILayout.PropertyField(wrapDirectionProp, new GUIContent("Wrap Direction"));
        EditorGUILayout.PropertyField(placeFlushProp, new GUIContent("Place Flush"));
        EditorGUILayout.PropertyField(placeSearchModeProp, new GUIContent("Place Search Mode"));
        EditorGUILayout.PropertyField(placementModeProp, new GUIContent("Placement Mode"));

        serializedObject.ApplyModifiedProperties();

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("Placed (runtime)", node.GetPlacementCount());
        EditorGUI.EndDisabledGroup();
    }

    private void DrawDefaultInspectorExceptFitStack()
    {
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.name == "fitX" || iterator.name == "fitY" || iterator.name == "fitZ"
                || iterator.name == "stackDirection" || iterator.name == "wrapDirection" || iterator.name == "placeFlush" || iterator.name == "placeSearchMode" || iterator.name == "placementMode")
                continue;
            EditorGUILayout.PropertyField(iterator, true);
        }
    }

    private void FitSizeToPrefabBounds(SGBehaviorTreeNode node)
    {
        if (node.gameObjectPrefabs == null || node.gameObjectPrefabs.Count == 0)
        {
            Debug.LogWarning("[SGBehaviorTreeNode] No prefabs assigned; cannot fit size.");
            return;
        }

        Vector3 maxSize = Vector3.zero;
        int fittedCount = 0;
        foreach (GameObject prefab in node.gameObjectPrefabs)
        {
            if (prefab == null) continue;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null) continue;
            try
            {
                Bounds b = ComputeBoundsInHierarchy(instance);
                Vector3 size = b.size;
                if (size.x > maxSize.x) maxSize.x = size.x;
                if (size.y > maxSize.y) maxSize.y = size.y;
                if (size.z > maxSize.z) maxSize.z = size.z;
                fittedCount++;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        if (fittedCount == 0)
        {
            Debug.LogWarning("[SGBehaviorTreeNode] Could not compute bounds from any prefab (no Renderers or Colliders).");
            return;
        }

        const float minAxis = 0.01f;
        maxSize.x = Mathf.Max(maxSize.x, minAxis);
        maxSize.y = Mathf.Max(maxSize.y, minAxis);
        maxSize.z = Mathf.Max(maxSize.z, minAxis);

        Undo.RecordObject(node, "Fit Size to Prefab Bounds");
        if (minSpaceProp != null) minSpaceProp.vector3Value = maxSize;
        if (maxSpaceProp != null) maxSpaceProp.vector3Value = maxSize;
        if (optimalSpaceProp != null) optimalSpaceProp.vector3Value = maxSize;
        serializedObject.ApplyModifiedProperties();
        Debug.Log($"[SGBehaviorTreeNode] Fit size to prefab bounds: min/max/optimal = {maxSize} (from {fittedCount} prefab(s)).");
    }

    private static Bounds ComputeBoundsInHierarchy(GameObject root)
    {
        bool hasAny = false;
        Bounds combined = new Bounds(root.transform.position, Vector3.zero);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r.enabled)
            {
                if (!hasAny) { combined = r.bounds; hasAny = true; }
                else combined.Encapsulate(r.bounds);
            }
        }
        Collider[] colliders = root.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            if (c.enabled)
            {
                if (!hasAny) { combined = c.bounds; hasAny = true; }
                else combined.Encapsulate(c.bounds);
            }
        }
        RectTransform rect = root.GetComponentInChildren<RectTransform>();
        if (rect != null)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Bounds rtb = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Length; i++) rtb.Encapsulate(corners[i]);
            if (!hasAny) { combined = rtb; hasAny = true; }
            else combined.Encapsulate(rtb);
        }
        if (!hasAny)
            combined = new Bounds(root.transform.position, Vector3.one);
        return combined;
    }
}
