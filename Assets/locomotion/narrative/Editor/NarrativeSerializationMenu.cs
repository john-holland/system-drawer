#if UNITY_EDITOR
using Locomotion.Narrative.Serialization;
using UnityEditor;
using UnityEngine;

namespace Locomotion.Narrative.EditorTools
{
    public static class NarrativeSerializationMenu
    {
        [MenuItem("Assets/Locomotion/Narrative/Export Calendar (JSON)", true)]
        private static bool ValidateExportCalendarJson() => Selection.activeObject is NarrativeCalendarAsset;

        [MenuItem("Assets/Locomotion/Narrative/Export Calendar (JSON)")]
        private static void ExportCalendarJson()
        {
            var cal = (NarrativeCalendarAsset)Selection.activeObject;
            string path = EditorUtility.SaveFilePanel("Export Calendar (JSON)", Application.dataPath, cal.name + ".json", "json");
            if (string.IsNullOrEmpty(path)) return;
            NarrativeExportUtility.ExportCalendarToJsonFile(cal, path);
        }

        [MenuItem("Assets/Locomotion/Narrative/Export Calendar (YAML)", true)]
        private static bool ValidateExportCalendarYaml() => Selection.activeObject is NarrativeCalendarAsset;

        [MenuItem("Assets/Locomotion/Narrative/Export Calendar (YAML)")]
        private static void ExportCalendarYaml()
        {
            var cal = (NarrativeCalendarAsset)Selection.activeObject;
            string path = EditorUtility.SaveFilePanel("Export Calendar (YAML)", Application.dataPath, cal.name + ".yaml", "yaml");
            if (string.IsNullOrEmpty(path)) return;
            NarrativeExportUtility.ExportCalendarToYamlFile(cal, path);
        }

        [MenuItem("Assets/Locomotion/Narrative/Export Tree (JSON)", true)]
        private static bool ValidateExportTreeJson() => Selection.activeObject is NarrativeTreeAsset;

        [MenuItem("Assets/Locomotion/Narrative/Export Tree (JSON)")]
        private static void ExportTreeJson()
        {
            var tree = (NarrativeTreeAsset)Selection.activeObject;
            string path = EditorUtility.SaveFilePanel("Export Tree (JSON)", Application.dataPath, tree.name + ".json", "json");
            if (string.IsNullOrEmpty(path)) return;
            NarrativeExportUtility.ExportTreeToJsonFile(tree, path);
        }

        [MenuItem("Assets/Locomotion/Narrative/Export Tree (YAML)", true)]
        private static bool ValidateExportTreeYaml() => Selection.activeObject is NarrativeTreeAsset;

        [MenuItem("Assets/Locomotion/Narrative/Export Tree (YAML)")]
        private static void ExportTreeYaml()
        {
            var tree = (NarrativeTreeAsset)Selection.activeObject;
            string path = EditorUtility.SaveFilePanel("Export Tree (YAML)", Application.dataPath, tree.name + ".yaml", "yaml");
            if (string.IsNullOrEmpty(path)) return;
            NarrativeExportUtility.ExportTreeToYamlFile(tree, path);
        }
    }
}
#endif

