#if UNITY_EDITOR
using Locomotion.Narrative.Serialization;
using UnityEditor;
using UnityEngine;

namespace Locomotion.Narrative.EditorTools
{
    public static class NarrativeSerializationMenu
    {
        [MenuItem("Assets/Locomotion/Narrative/Export Calendar (JSON)", true)]
        private static bool ValidateExportCalendarJson() => Selection.activeGameObject?.GetComponent<NarrativeCalendarAsset>() != null || Selection.activeObject is NarrativeCalendarAsset;

        [MenuItem("Assets/Locomotion/Narrative/Export Calendar (JSON)")]
        private static void ExportCalendarJson()
        {
            NarrativeCalendarAsset cal = null;
            if (Selection.activeGameObject != null)
            {
                cal = Selection.activeGameObject.GetComponent<NarrativeCalendarAsset>();
            }
            if (cal == null && Selection.activeObject is NarrativeCalendarAsset)
            {
                cal = (NarrativeCalendarAsset)Selection.activeObject;
            }
            if (cal == null) return;
            string path = EditorUtility.SaveFilePanel("Export Calendar (JSON)", Application.dataPath, cal.name + ".json", "json");
            if (string.IsNullOrEmpty(path)) return;
            NarrativeExportUtility.ExportCalendarToJsonFile(cal, path);
        }

        [MenuItem("Assets/Locomotion/Narrative/Export Calendar (YAML)", true)]
        private static bool ValidateExportCalendarYaml() => Selection.activeGameObject?.GetComponent<NarrativeCalendarAsset>() != null || Selection.activeObject is NarrativeCalendarAsset;

        [MenuItem("Assets/Locomotion/Narrative/Export Calendar (YAML)")]
        private static void ExportCalendarYaml()
        {
            NarrativeCalendarAsset cal = null;
            if (Selection.activeGameObject != null)
            {
                cal = Selection.activeGameObject.GetComponent<NarrativeCalendarAsset>();
            }
            if (cal == null && Selection.activeObject is NarrativeCalendarAsset)
            {
                cal = (NarrativeCalendarAsset)Selection.activeObject;
            }
            if (cal == null) return;
            string path = EditorUtility.SaveFilePanel("Export Calendar (YAML)", Application.dataPath, cal.name + ".yaml", "yaml");
            if (string.IsNullOrEmpty(path)) return;
            NarrativeExportUtility.ExportCalendarToYamlFile(cal, path);
        }

        [MenuItem("Assets/Locomotion/Narrative/Export Tree (JSON)", true)]
        private static bool ValidateExportTreeJson() => Selection.activeGameObject?.GetComponent<NarrativeTreeAsset>() != null || Selection.activeObject is NarrativeTreeAsset;

        [MenuItem("Assets/Locomotion/Narrative/Export Tree (JSON)")]
        private static void ExportTreeJson()
        {
            NarrativeTreeAsset tree = null;
            if (Selection.activeGameObject != null)
            {
                tree = Selection.activeGameObject.GetComponent<NarrativeTreeAsset>();
            }
            if (tree == null && Selection.activeObject is NarrativeTreeAsset)
            {
                tree = (NarrativeTreeAsset)Selection.activeObject;
            }
            if (tree == null) return;
            string path = EditorUtility.SaveFilePanel("Export Tree (JSON)", Application.dataPath, tree.name + ".json", "json");
            if (string.IsNullOrEmpty(path)) return;
            NarrativeExportUtility.ExportTreeToJsonFile(tree, path);
        }

        [MenuItem("Assets/Locomotion/Narrative/Export Tree (YAML)", true)]
        private static bool ValidateExportTreeYaml() => Selection.activeGameObject?.GetComponent<NarrativeTreeAsset>() != null || Selection.activeObject is NarrativeTreeAsset;

        [MenuItem("Assets/Locomotion/Narrative/Export Tree (YAML)")]
        private static void ExportTreeYaml()
        {
            NarrativeTreeAsset tree = null;
            if (Selection.activeGameObject != null)
            {
                tree = Selection.activeGameObject.GetComponent<NarrativeTreeAsset>();
            }
            if (tree == null && Selection.activeObject is NarrativeTreeAsset)
            {
                tree = (NarrativeTreeAsset)Selection.activeObject;
            }
            if (tree == null) return;
            string path = EditorUtility.SaveFilePanel("Export Tree (YAML)", Application.dataPath, tree.name + ".yaml", "yaml");
            if (string.IsNullOrEmpty(path)) return;
            NarrativeExportUtility.ExportTreeToYamlFile(tree, path);
        }
    }
}
#endif

