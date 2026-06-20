#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Exports <see cref="VocabularyBuiltInRegistry"/> to continuum_api JSON for the lemma library API.</summary>
public static class VocabularyBuiltInJsonExporter
{
    const string RelativeOutput = "Scripts/continuum_api/data/builtin_vocabulary.json";

    [MenuItem("Continuum/Export Built-in Vocabulary JSON")]
    public static void ExportFromMenu()
    {
        var path = Export();
        EditorUtility.DisplayDialog("Built-in Vocabulary", $"Exported {VocabularyBuiltInRegistry.Count} entries to:\n{path}", "OK");
    }

    /// <summary>CLI: -batchmode -executeMethod VocabularyBuiltInJsonExporter.ExportFromCli</summary>
    public static void ExportFromCli()
    {
        Export();
    }

    public static string Export()
    {
        var sb = new StringBuilder();
        sb.Append("{\"version\":1,\"items\":[");
        bool first = true;
        foreach (var d in VocabularyBuiltInRegistry.All)
        {
            if (!first)
                sb.Append(',');
            first = false;
            sb.Append('{');
            sb.Append("\"id\":").Append(JsonQuote(d.Id)).Append(',');
            sb.Append("\"term\":").Append(JsonQuote(d.Term)).Append(',');
            sb.Append("\"posTag\":").Append(JsonQuote(d.PosTag)).Append(',');
            sb.Append("\"languageCode\":").Append(JsonQuote(d.LanguageCode)).Append(',');
            sb.Append("\"builtInCategory\":").Append(JsonQuote(d.Category.ToString())).Append(',');
            sb.Append("\"tags\":");
            if (d.Tags != null && d.Tags.Count > 0)
            {
                sb.Append('[');
                for (int i = 0; i < d.Tags.Count; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    sb.Append(JsonQuote(d.Tags[i]));
                }
                sb.Append(']');
            }
            else
            {
                sb.Append("[]");
            }
            sb.Append('}');
        }
        sb.Append("]}");
        var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", RelativeOutput));
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    static string JsonQuote(string s) => JsonUtility.ToJson(s ?? "");
}
#endif
