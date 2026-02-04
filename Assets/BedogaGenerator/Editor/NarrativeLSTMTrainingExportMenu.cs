#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor menu to export 4D spatial expressions to the LSTM training folder (expressions/*.json).
/// </summary>
public static class NarrativeLSTMTrainingExportMenu
{
    [MenuItem("Locomotion/Narrative/Export 4D expressions for LSTM training...")]
    private static void Export4DForLSTMTraining()
    {
        string basePath = EditorUtility.SaveFolderPanel("LSTM training output (4D expressions)", Application.dataPath, "NarrativeLSTM_Training");
        if (string.IsNullOrEmpty(basePath)) return;
        string expressionsDir = Path.Combine(basePath, "expressions");
        Directory.CreateDirectory(expressionsDir);
        string sourcePath = EditorUtility.OpenFilePanel("Select 4D expressions JSON (optional)", Application.dataPath, "json");
        if (!string.IsNullOrEmpty(sourcePath) && File.Exists(sourcePath))
        {
            string destPath = Path.Combine(expressionsDir, Path.GetFileName(sourcePath));
            if (string.IsNullOrEmpty(Path.GetFileName(sourcePath)))
                destPath = Path.Combine(expressionsDir, "expressions.json");
            File.Copy(sourcePath, destPath, true);
            EditorUtility.DisplayDialog("LSTM training export", $"Copied 4D expressions to {destPath}", "OK");
        }
        else
            EditorUtility.DisplayDialog("LSTM training export", $"Created {expressionsDir}. Add 4D expressions JSON files there manually, or run again and select a file.", "OK");
        AssetDatabase.Refresh();
    }
}
#endif
