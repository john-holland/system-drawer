#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// After large pulls, many .cs files exist on disk but never enter AssetDatabase
/// (especially stub GUID-only .meta files). Force-import any script that is not a MonoScript.
/// </summary>
[InitializeOnLoad]
internal static class ForceImportOrphanLocomotionScripts
{
    const string PrefKey = "ForceImportOrphanLocomotionScripts.Done.";

    static ForceImportOrphanLocomotionScripts()
    {
        EditorApplication.delayCall += RunOnce;
    }

    static void RunOnce()
    {
        string key = PrefKey + Application.dataPath;
        if (SessionState.GetBool(key, false))
            return;

        int imported = 0;
        var roots = new[]
        {
            "Assets/locomotion",
            "Assets/Continuuuum",
            "Assets/BedogaGenerator",
            "Assets/Environment",
            "Assets/SystemDrawer",
        };
        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
                continue;
            foreach (var path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string assetPath = path.Replace('\\', '/');
                if (assetPath.Contains("/_PendingAssetDbImport/"))
                    continue;
                var type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (type == typeof(MonoScript))
                    continue;
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                imported++;
            }
        }

        SessionState.SetBool(key, true);
        if (imported > 0)
        {
            Debug.Log($"[ForceImportOrphanLocomotionScripts] Imported {imported} orphan scripts. Restoring from _PendingAssetDbImport may be needed after a clean reimport.");
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Locomotion/Force Import Orphan Scripts")]
    static void MenuForce()
    {
        SessionState.SetBool(PrefKey + Application.dataPath, false);
        RunOnce();
    }
}
#endif
