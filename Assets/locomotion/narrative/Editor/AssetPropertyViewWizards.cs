#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Locomotion.Narrative;

namespace Locomotion.Narrative.EditorTools
{
    /// <summary>
    /// Reusable UI for asset property view wizards: progression steps and buttons.
    /// Used by Prompt Tree Inspector for rounded prefab and fill selection progression.
    /// </summary>
    public static class AssetPropertyViewWizards
    {
        /// <summary>Draw rounded prefab progression (steps 1-5) with status and fix buttons.</summary>
        public static void DrawRoundedPrefabProgression(
            SceneObjectRegistry registry,
            SceneObjectEntry entry,
            string keyOrPhrase,
            System.Action<int> onFixStep = null)
        {
            var comp = AssetCompletenessUtility.GetCompleteness(registry, entry, keyOrPhrase);
            EditorGUILayout.LabelField($"Rounded prefab: {comp.CompletedCount}/{AssetCompletenessUtility.StepCount} steps", EditorStyles.miniBoldLabel);

            DrawStepRow(1, "ORM registered", comp.ormRegistered, "Register in SceneObjectRegistry", onFixStep);
            DrawStepRow(2, "Has reference/prefab", comp.hasReferencePrefab, "Assign prefab or reference", onFixStep);
            DrawStepRow(3, "Has mesh", comp.hasMesh, "Add mesh/renderer", onFixStep);
            DrawStepRow(4, "Has materials", comp.hasMaterials, "Assign material", onFixStep);
            DrawStepRow(5, "Has animations", comp.hasAnimations, "Add animator/clips", onFixStep);
        }

        /// <summary>Draw generator/asset fill selection progression (steps 1-4).</summary>
        public static void DrawFillSelectionProgression(
            InterpretedEventBinding binding,
            SceneObjectRegistry registry,
            SceneObjectEntry entry,
            SpatialGeneratorStylesheet stylesheet,
            string nodeKey,
            System.Action<int> onFixStep = null)
        {
            bool step1 = binding.status == BindingStatus.OrmMatched && !string.IsNullOrEmpty(binding.resolvedOrmKey);
            bool step2 = entry != null && (entry.prefabForClone != null || entry.reference != null);
            bool step3 = false;
            if (stylesheet != null && !string.IsNullOrEmpty(nodeKey))
            {
                var overrides = stylesheet.GetPrefabOverrides(nodeKey);
                step3 = overrides != null && overrides.Count > 0;
            }
            bool step4 = step1 && step2 && step3;

            EditorGUILayout.LabelField("Fill selection", EditorStyles.miniBoldLabel);
            DrawStepRow(1, "Phrase resolved to ORM key", step1 ? AssetCompletenessUtility.StepStatus.Ok : AssetCompletenessUtility.StepStatus.Missing, "Resolve in ORM (Fill missing links)", onFixStep);
            DrawStepRow(2, "ORM entry has prefab", step2 ? AssetCompletenessUtility.StepStatus.Ok : AssetCompletenessUtility.StepStatus.Missing, "Assign prefab", onFixStep);
            DrawStepRow(3, "Stylesheet/node maps key", step3 ? AssetCompletenessUtility.StepStatus.Ok : AssetCompletenessUtility.StepStatus.Missing, "Add stylesheet entry", onFixStep);
            DrawStepRow(4, "Ready for placement", step4 ? AssetCompletenessUtility.StepStatus.Ok : AssetCompletenessUtility.StepStatus.Missing, "—", null);
        }

        private static void DrawStepRow(int stepIndex, string label, AssetCompletenessUtility.StepStatus status, string fixLabel, System.Action<int> onFixStep)
        {
            EditorGUILayout.BeginHorizontal();
            bool ok = status == AssetCompletenessUtility.StepStatus.Ok;
            EditorGUILayout.LabelField(ok ? "✓" : "○", GUILayout.Width(16));
            EditorGUILayout.LabelField(label, GUILayout.ExpandWidth(true));
            if (!ok && onFixStep != null && !string.IsNullOrEmpty(fixLabel) && fixLabel != "—")
            {
                if (GUILayout.Button(fixLabel, GUILayout.Height(18)))
                    onFixStep(stepIndex);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
