#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot repair for BedogaGenerator .cs.meta files with duplicate or missing MonoImporter blocks.
/// Run via menu: Bedoga/Repair Script Meta GUIDs (close other file locks first).
/// </summary>
public static class RepairBedogaMetaGuids
{
    static readonly Dictionary<string, string> KnownGuids = new Dictionary<string, string>
    {
        { "BuildAround.cs", "af7963170d09f35458e32584d7d4c28e" },
        { "CausalityPlacementCoordinator.cs", "ac5595103dd0f364c985ff38eda22275" },
        { "IBoundsProvider.cs", "d67771e3651eee04f810391ee8b8c041" },
        { "ISpatialGeneratorLayer.cs", "66f2c49c642d14e4ea85eafd612ec92a" },
        { "ISpatialGeneratorPrefabResolver.cs", "37666a68e300d03418de74df099fa550" },
        { "LayoutPlacementPolicy.cs", "8ca3e8f0c496b2244b0e6ba638c849b1" },
        { "LayoutSlotHintMapper.cs", "2c5af8b51c20a3347bec95a7e9abc9b1" },
        { "Narrative4DPlacer.cs", "d56306697448f4346a14c6c60106cfe4" },
        { "NarrativePathfindingCoverage.cs", "d24ae359a1c8f97488697d71d54df004" },
        { "Placement/PlacementSlotConfig.cs", "72884e6c8c46f4a498926702a7ea854f" },
        { "Placement/SGBehaviorTreeEmptySpace.cs", "69b037f7ea7bfe54ea968d0a16e2d16c" },
        { "PlanetGeometryToolsUI.cs", "47afdca752b9fe14aa91e04bb9fad4ae" },
        { "SceneGraph.cs", "ecd69b9cd622a9948a42e43914a4a686" },
        { "SGBehaviorTreeNode.cs", "cd33de8bf98d01c4eb5fa80c3724ab7a" },
        { "Spatial4DExportUtility.cs", "a60d4d804b6fd4848a2c4e3f5606e979" },
        { "Spatial4DExpressionsDto.cs", "d9999517ec609cd40a14b6e150394535" },
        { "Spatial4DGatewayDtos.cs", "72283db3e43b0a74ab71f293da10f9ab" },
        { "Spatial4DInGameUI.cs", "0ecf7252d0257c144a8bc72af5c2ae9c" },
        { "Spatial4DMirrorNode.cs", "88aee1aec0a05554fa91b814c2564cd8" },
        { "Spatial4DServiceWizard.cs", "43b7a2473a34cc742b01f8609bdcba42" },
        { "SpatialGenerator.cs", "213a28ba1bb78764db5679917eb68298" },
        { "SpatialGenerator4D.cs", "17a3a349417f64e4e9c734c992501398" },
        { "SpatialGenerator4DOrchestrator.cs", "02c5ac856d0921246b3f0738ec542b0b" },
        { "SpatialGeneratorBase.cs", "04fec8839e51a2848bb0e0862c8b616b" },
        { "SpatialGeneratorSkin.cs", "3fb90c9bbea123545ace295013f70d30" },
        { "SpatialGeneratorSkinController.cs", "46451c71e4baf0445b347a8269f1e8a2" },
        { "SpatialGeneratorStylesheet.cs", "a786ca3af1c3d464ebe7f8a68e5864e2" },
        { "SpatialSkinLooseObject.cs", "7b5c6532b5ebbd54e9c877b76844188b" },
        { "StretchObject.cs", "5eeb4097d6981ce4188fa0cf077cde82" },
        { "StylesheetPrefabResolver.cs", "b559aa2b7c1461f468e5918a716319f0" },
        { "TriggerSpatialGenerateAction.cs", "d3579d7f34d20d24abfa8b0cff9c60af" },
    };

    const string MonoBlock = @"MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
";

    [MenuItem("Bedoga/Repair Script Meta GUIDs")]
    public static void Repair()
    {
        string dir = Path.Combine(Application.dataPath, "BedogaGenerator");
        var csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).ToList();
        int fixedCount = 0;
        foreach (var name in csFiles)
        {
            if (!KnownGuids.TryGetValue(name, out string guid))
                continue;
            string metaPath = Path.Combine(dir, name + ".meta");
            string content = "fileFormatVersion: 2\n" + "guid: " + guid + "\n" + MonoBlock;
            File.WriteAllText(metaPath, content);
            fixedCount++;
        }
        AssetDatabase.Refresh();
        Debug.Log($"[RepairBedogaMetaGuids] Rewrote {fixedCount} meta files. Reimport if errors persist.");
    }
}
#endif
