#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot: scripts that exist on disk but never entered the AssetDatabase
/// (common after pull with stub .meta files) are force-imported so Continuuuum.Runtime can see them.
/// </summary>
[InitializeOnLoad]
internal static class ForceImportMissingContinuuuumScripts
{
    static readonly string[] Paths =
    {
        "Assets/Continuuuum/AdverbIfPostfix.cs",
        "Assets/Continuuuum/IfPredicate.cs",
        "Assets/Continuuuum/WebcamAnimWebPreview.cs",
        "Assets/Continuuuum/Dimensions/DimensionMaterialCrossFader.cs",
        "Assets/Continuuuum/Dimensions/DimensionParticleCleanup.cs",
        "Assets/Continuuuum/Dimensions/DimensionalLemmaBinding.cs",
        "Assets/Continuuuum/Dimensions/DimensionalLemmaPosition.cs",
        "Assets/Continuuuum/Dimensions/DimensionalLemmaVelocityBridge.cs",
        "Assets/Continuuuum/Dimensions/DimensionalOpenCloseBtEntry.cs",
        "Assets/Continuuuum/Dimensions/DimensionalShaderComponent.cs",
        "Assets/Continuuuum/Dimensions/DimensionalTypes.cs",
        "Assets/Continuuuum/Dimensions/IDimensionalOpenCloseRunner.cs",
        "Assets/Continuuuum/Dimensions/SharedDimensionalGenericCache.cs",
        "Assets/Continuuuum/Localization/ChatLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/ChefLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/FrameShellInclusionLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/GameSessionLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/HousingLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/LegalLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/PenInkLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/RelationshipLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/RoadLaneLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/ScribeLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/SewingLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/StreetLightLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/TasteNotesLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/ThreatLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/UniversityLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/UtilityLemmaPropertyKeys.cs",
        "Assets/Continuuuum/Localization/VoteLemmaPropertyKeys.cs",
    };

    static ForceImportMissingContinuuuumScripts()
    {
        EditorApplication.delayCall += RunOnce;
    }

    static void RunOnce()
    {
        int imported = 0;
        foreach (var path in Paths)
        {
            if (!System.IO.File.Exists(path))
                continue;
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (type == typeof(MonoScript))
                continue;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            imported++;
        }

        if (imported > 0)
        {
            Debug.Log($"[ForceImportMissingContinuuuumScripts] Imported {imported} missing Continuuuum scripts.");
            AssetDatabase.Refresh();
        }
    }
}
#endif
