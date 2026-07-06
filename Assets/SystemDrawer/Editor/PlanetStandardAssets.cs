using Planetary;
using Planetary.Composition;
using Planetary.Rendering;
using Planetary.Tectonics;
using SpatialVolumes;
using UnityEditor;
using UnityEngine;

internal static class PlanetStandardAssets
{
    internal static WizardSetupReport Setup(PlanetServiceWizardComponent wizard)
    {
        var report = new WizardSetupReport();
        if (wizard == null)
            return report;

        var library = EnsurePresetLibrary(report);
        ApplyLittlePrincePreset(library, report);

        GameObject planetGo = wizard.planetSystemObject;
        PlanetBody body = null;
        if (planetGo != null)
            body = planetGo.GetComponentInChildren<PlanetBody>(true);
        if (body == null)
            body = WizardStandardAssetsCore.FindFirstInScene<PlanetBody>();

        if (body == null)
        {
            var hub = WizardStandardAssetsCore.ResolveHubRoot(wizard);
            var sceneRoot = WizardStandardAssetsCore.EnsureStandardSceneRoot(hub, report);
            var parent = sceneRoot != null ? sceneRoot : hub;
            planetGo = WizardStandardAssetsCore.FindOrCreateChild(parent, "PlanetSystem", report);
            body = WizardStandardAssetsCore.FindOrAddComponent<PlanetBody>(planetGo, report);
            WizardStandardAssetsCore.FindOrAddComponent<PlanetMeshStreamingService>(planetGo, report);
            WizardStandardAssetsCore.FindOrAddComponent<SpatialVolumeProvider>(planetGo, report);
            WizardStandardAssetsCore.FindOrAddComponent<PlanetInteriorPhysicsUpdater>(planetGo, report);

            var renderChild = WizardStandardAssetsCore.FindOrCreateChild(planetGo.transform, "PlanetRenderer", report);
            WizardStandardAssetsCore.FindOrAddComponent<PlanetRenderer>(renderChild, report);
            var sdfChild = WizardStandardAssetsCore.FindOrCreateChild(planetGo.transform, "PlanetSdfLod", report);
            WizardStandardAssetsCore.FindOrAddComponent<PlanetarySdfLodRenderer>(sdfChild, report);
        }
        else
        {
            planetGo = body.gameObject;
            report.Skipped.Add("PlanetBody in scene");
        }

        if (library != null && library.TryGetPreset("little-prince", out var preset))
            ApplyPresetToBody(body, preset, report);

        if (wizard.planetSystemObject != planetGo)
        {
            Undo.RecordObject(wizard, "Assign planet system");
            wizard.planetSystemObject = planetGo;
            EditorUtility.SetDirty(wizard);
            report.Linked.Add("PlanetServiceWizardComponent.planetSystemObject");
        }

        WizardStandardAssetsCore.MarkSceneDirty(wizard);
        return report;
    }

    static PlanetaryCompositionPresetLibrary EnsurePresetLibrary(WizardSetupReport report)
    {
        var existing = WizardStandardAssetsCore.LoadAssetAtPath<PlanetaryCompositionPresetLibrary>(
            WizardStandardAssetsPaths.Planetary.LittlePrincePresetLibrary);
        if (existing != null)
        {
            report.Skipped.Add("LittlePrincePresetLibrary asset");
            return existing;
        }

        var lib = PlanetaryCompositionPresetLibrary.CreateWithBuiltInPresets();
        WizardStandardAssetsCore.EnsureFolder(WizardStandardAssetsPaths.Root + "/Planetary");
        AssetDatabase.CreateAsset(lib, WizardStandardAssetsPaths.Planetary.LittlePrincePresetLibrary);
        if (lib.presets != null)
        {
            for (int i = 0; i < lib.presets.Length; i++)
            {
                var p = lib.presets[i];
                if (p.composition != null)
                    AssetDatabase.AddObjectToAsset(p.composition, lib);
                if (p.atmosphere != null)
                    AssetDatabase.AddObjectToAsset(p.atmosphere, lib);
                if (p.horizonLod != null)
                    AssetDatabase.AddObjectToAsset(p.horizonLod, lib);
                if (p.sdfLod != null)
                    AssetDatabase.AddObjectToAsset(p.sdfLod, lib);
            }
        }

        AssetDatabase.SaveAssets();
        report.Created.Add("LittlePrincePresetLibrary");
        return lib;
    }

    static void ApplyLittlePrincePreset(PlanetaryCompositionPresetLibrary library, WizardSetupReport report)
    {
        if (library == null || !library.TryGetPreset("little-prince", out _))
            report.Warnings.Add("Little Prince preset not found in library.");
    }

    static void ApplyPresetToBody(PlanetBody body, PlanetaryCompositionPreset preset, WizardSetupReport report)
    {
        if (body == null)
            return;

        Undo.RecordObject(body, "Apply Little Prince preset");
        body.planetRadius = preset.planetRadius;
        if (preset.composition != null)
            body.compositionProfile = preset.composition;
        if (preset.horizonLod != null)
            body.horizonLodSettings = preset.horizonLod;
        if (body.sdfLodRenderer != null && preset.sdfLod != null)
            body.sdfLodRenderer.profile = preset.sdfLod;
        else if (preset.sdfLod != null)
            body.sdfLodProfile = preset.sdfLod;

        body.ratioModel = PlanetaryCompositionRatioModel.CreateLittlePrinceDefaults();
        body.ratioModel.anchorRadius = preset.planetRadius;
        PlanetaryCompositionRatioSolver.ApplyAnchorRadius(body.ratioModel, preset.planetRadius);
        PlanetaryCompositionRatioSolver.CaptureRatiosFromProfile(
            body.ratioModel, body, body.compositionProfile, preset.atmosphere,
            body.horizonLodSettings,
            body.sdfLodProfile ?? (body.sdfLodRenderer != null ? body.sdfLodRenderer.profile : null));

        EditorUtility.SetDirty(body);
        report.Linked.Add("PlanetBody Little Prince preset");
    }
}
