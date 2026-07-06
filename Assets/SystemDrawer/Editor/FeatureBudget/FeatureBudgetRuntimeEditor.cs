#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FeatureBudgetRuntime))]
public class FeatureBudgetRuntimeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var runtime = (FeatureBudgetRuntime)target;
        EditorGUILayout.Space();
        WizardStandardAssetsUi.DrawSetupSection(runtime,
            "Creates or links DefaultFeatureBudgetProfile.asset and syncs ratio bindings from scene planet when present.");
    }
}
#endif
