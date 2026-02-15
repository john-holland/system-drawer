#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ensures Barracuda's ONNX assembly references Google.Protobuf so ONNX import compiles.
/// Run once on load; add Google.Protobuf.dll to Assets/Plugins/Google.Protobuf (see README there).
/// </summary>
[InitializeOnLoad]
public static class BarracudaProtobufPatcher
{
    private const string ProtobufRef = "Google.Protobuf";
    private const string AsmdefRelativePath = "Barracuda/Runtime/ONNX/Unity.Barracuda.ONNX.asmdef";

    static BarracudaProtobufPatcher()
    {
        EditorApplication.delayCall += () =>
        {
            try { EnsureBarracudaOnnxReferencesProtobuf(); }
            catch (System.Exception ex) { Debug.LogWarning("[BarracudaProtobufPatcher] " + ex.Message); }
        };
    }

    private static void EnsureBarracudaOnnxReferencesProtobuf()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string packageCache = Path.Combine(projectRoot, "Library", "PackageCache");
        if (!Directory.Exists(packageCache)) return;

        string asmdefPath = null;
        foreach (var dir in Directory.GetDirectories(packageCache))
        {
            if (!Path.GetFileName(dir).StartsWith("com.unity.barracuda")) continue;
            string candidate = Path.Combine(dir, AsmdefRelativePath);
            if (File.Exists(candidate))
            {
                asmdefPath = candidate;
                break;
            }
        }

        if (string.IsNullOrEmpty(asmdefPath)) return;

        string json = File.ReadAllText(asmdefPath);
        if (json.Contains(ProtobufRef)) return;

        // Add Google.Protobuf to precompiledReferences
        const string emptyRefs = "\"precompiledReferences\": []";
        string newRefs = "\"precompiledReferences\": [ \"" + ProtobufRef + "\" ]";
        if (json.IndexOf(emptyRefs, System.StringComparison.Ordinal) >= 0)
        {
            json = json.Replace(emptyRefs, newRefs);
            File.WriteAllText(asmdefPath, json);
            AssetDatabase.Refresh();
        }
    }
}
#endif
