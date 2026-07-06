using System.Collections.Generic;
using Planetary.Composition;
using Planetary.Rendering;

namespace Planetary
{
    /// <summary>Exposes Composition UI ratio readings to Feature Budget without asmdef cycle.</summary>
    public sealed class PlanetRatioSource : IPlanetRatioSource
    {
        readonly PlanetBody _body;
        readonly PlanetaryCompositionRatioModel _model;

        public PlanetRatioSource(PlanetBody body)
        {
            _body = body;
            _model = body != null && body.ratioModel != null
                ? body.ratioModel
                : PlanetaryCompositionRatioModel.CreateLittlePrinceDefaults();
        }

        public float AnchorRadius => _body != null ? _body.planetRadius : _model.anchorRadius;

        public void CaptureRatioFields(List<RatioFieldSnapshot> output)
        {
            output.Clear();
            if (_body == null)
            {
                AppendModelFields(output, _model);
                return;
            }

            AtmosphereRegressionProfile atmos = null;
            var horizon = _body.horizonLodSettings;
            var sdf = _body.sdfLodProfile ?? (_body.sdfLodRenderer != null ? _body.sdfLodRenderer.profile : null);
            PlanetaryCompositionRatioSolver.CaptureRatiosFromProfile(
                _model, _body, _body.compositionProfile, atmos, horizon, sdf);
            _model.anchorRadius = _body.planetRadius;
            AppendModelFields(output, _model);
        }

        static void AppendModelFields(List<RatioFieldSnapshot> output, PlanetaryCompositionRatioModel model)
        {
            if (model?.fields == null)
                return;
            for (int i = 0; i < model.fields.Count; i++)
            {
                var f = model.fields[i];
                output.Add(new RatioFieldSnapshot
                {
                    id = f.id,
                    ratio = f.ratio,
                    ratioLocked = f.ratioLocked,
                    manualOverride = f.manualOverride
                });
            }
        }
    }
}
