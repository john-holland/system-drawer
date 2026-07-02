using NUnit.Framework;
using Planetary.Composition;
using UnityEngine;

namespace Planetary.Tests
{
    public class PlanetaryCompositionRatioSolverTests
    {
        [Test]
        public void AnchorRadius_DoublesLockedShellValues()
        {
            var model = PlanetaryCompositionRatioModel.CreateLittlePrinceDefaults();
            model.ApplyAnchorRadius(500f);
            float coreThickness500 = model.GetValue("core.thickness");
            model.ApplyAnchorRadius(1000f);
            float coreThickness1000 = model.GetValue("core.thickness");
            Assert.AreEqual(coreThickness500 * 2f, coreThickness1000, 0.01f);
        }

        [Test]
        public void UnlockedSmoothRadius_UnchangedWhenRadiusScales()
        {
            var model = PlanetaryCompositionRatioModel.CreateLittlePrinceDefaults();
            model.ApplyAnchorRadius(500f);
            float smooth = model.GetValue("core.smooth");
            model.ApplyAnchorRadius(1000f);
            Assert.AreEqual(smooth, model.GetValue("core.smooth"), 0.01f);
        }

        [Test]
        public void LittlePrincePreset_RoundTripsThroughSolver()
        {
            var lib = PlanetaryCompositionPresetLibrary.CreateWithBuiltInPresets();
            Assert.IsTrue(lib.TryGetPreset("little-prince", out var preset));
            var model = PlanetaryCompositionRatioModel.CreateLittlePrinceDefaults();
            var go = new GameObject("planet");
            var body = go.AddComponent<PlanetBody>();
            body.planetRadius = preset.planetRadius;
            PlanetaryCompositionRatioSolver.CaptureRatiosFromProfile(
                model, body, preset.composition, preset.atmosphere, preset.horizonLod, preset.sdfLod);
            PlanetaryCompositionRatioSolver.WriteToProfile(
                model, body, preset.composition, preset.atmosphere, preset.horizonLod, preset.sdfLod);
            Assert.AreEqual(500f, body.planetRadius, 0.01f);
            Assert.AreEqual(-375f, preset.composition.layers[0].shellOffsetMeters, 0.01f);
            Object.DestroyImmediate(go);
        }
    }
}
