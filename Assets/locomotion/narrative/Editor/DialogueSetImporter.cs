#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Locomotion.Narrative.Editor
{
    /// <summary>
    /// Exports legacy Unity dialogue-set text to compiled JSON via DialogueSpanParser.
    /// </summary>
    public static class DialogueSetImporter
    {
        [MenuItem("Window/Locomotion/Narrative/Import Dialogue Set From Text")]
        public static void ImportFromSelection()
        {
            var textAsset = Selection.activeObject as TextAsset;
            if (textAsset == null)
            {
                EditorUtility.DisplayDialog("Dialogue Import", "Select a TextAsset containing lemma dialogue spans.", "OK");
                return;
            }

            var compiled = DialogueSpanParser.Compile(textAsset.text, textAsset.name);
            string json = JsonUtility.ToJson(new DialogueCompileWrapper
            {
                setId = compiled.setId,
                nodeCount = compiled.nodes.Count,
                issueCount = compiled.issues.Count
            }, true);

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Compiled Dialogue JSON",
                compiled.setId + ".json",
                "json",
                "Export compiled dialogue set");
            if (string.IsNullOrEmpty(path))
                return;

            System.IO.File.WriteAllText(path, DialogueCompileJson.Write(compiled));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Dialogue Import", $"Exported {compiled.nodes.Count} root nodes to {path}", "OK");
        }

        [System.Serializable]
        class DialogueCompileWrapper
        {
            public string setId;
            public int nodeCount;
            public int issueCount;
        }
    }

    static class DialogueCompileJson
    {
        public static string Write(DialogueSpanParser.CompileResult result)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{\n  \"setId\": \"").Append(Escape(result.setId)).Append("\",\n  \"nodes\": ");
            sb.Append(NodesToJson(result.nodes, 2));
            sb.Append("\n}");
            return sb.ToString();
        }

        static string NodesToJson(System.Collections.Generic.List<DialogueNodeDto> nodes, int indent)
        {
            var pad = new string(' ', indent);
            var sb = new System.Text.StringBuilder();
            sb.Append("[\n");
            for (int i = 0; i < nodes.Count; i++)
            {
                sb.Append(pad).Append(NodeToJson(nodes[i], indent + 2));
                if (i < nodes.Count - 1) sb.Append(",");
                sb.Append("\n");
            }
            sb.Append(new string(' ', indent - 2)).Append("]");
            return sb.ToString();
        }

        static string NodeToJson(DialogueNodeDto n, int indent)
        {
            var pad = new string(' ', indent);
            var sb = new System.Text.StringBuilder();
            sb.Append("{\n");
            sb.Append(pad).Append("\"id\": \"").Append(Escape(n.id)).Append("\",\n");
            sb.Append(pad).Append("\"text\": \"").Append(Escape(n.text)).Append("\",\n");
            sb.Append(pad).Append("\"presentation\": \"").Append(Escape(n.presentation)).Append("\",\n");
            if (!string.IsNullOrEmpty(n.answerId))
                sb.Append(pad).Append("\"answerId\": \"").Append(Escape(n.answerId)).Append("\",\n");
            if (!string.IsNullOrEmpty(n.speakerKey))
                sb.Append(pad).Append("\"speakerKey\": \"").Append(Escape(n.speakerKey)).Append("\",\n");
            if (!string.IsNullOrEmpty(n.visMode))
                sb.Append(pad).Append("\"visMode\": \"").Append(Escape(n.visMode)).Append("\",\n");
            sb.Append(pad).Append("\"children\": ").Append(NodesToJson(n.children, indent + 2)).Append("\n");
            sb.Append(new string(' ', indent - 2)).Append("}");
            return sb.ToString();
        }

        static string Escape(string s) =>
            (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
#endif
