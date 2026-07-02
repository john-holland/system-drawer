using Planetary.Composition;
using Planetary.Rendering;
using UnityEditor;
using UnityEngine;

namespace Planetary.Editor
{
    public sealed class PlanetaryCompositionEditorWindow : EditorWindow
    {
        PlanetBody _body;
        PlanetaryCompositionProfile _composition;
        AtmosphereRegressionProfile _atmosphere;
        HorizonLodSettings _horizon;
        PlanetarySdfLodProfile _sdfLod;
        PlanetaryCompositionPresetLibrary _presetLibrary;
        PlanetaryCompositionRatioModel _model = PlanetaryCompositionRatioModel.CreateLittlePrinceDefaults();
        int _presetIndex;
        Vector2 _scroll;

        [MenuItem("Window/System Drawer/Planet/Composition UI")]
        public static void Open() => GetWindow<PlanetaryCompositionEditorWindow>("Planetary Composition");

        void OnEnable()
        {
            if (_presetLibrary == null)
                _presetLibrary = PlanetaryCompositionPresetLibrary.CreateWithBuiltInPresets();
            SyncFromSelection();
        }

        void SyncFromSelection()
        {
            _body = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<PlanetBody>()
                : FindFirstObjectByType<PlanetBody>();
            if (_body == null)
                return;
            _composition = _body.compositionProfile;
            if (_body.sdfLodRenderer != null)
                _sdfLod = _body.sdfLodRenderer.profile;
            if (_body.horizonLodSettings != null)
                _horizon = _body.horizonLodSettings;
            PlanetaryCompositionRatioSolver.CaptureRatiosFromProfile(
                _model, _body, _composition, _atmosphere, _horizon, _sdfLod);
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            _body = (PlanetBody)EditorGUILayout.ObjectField("Planet Body", _body, typeof(PlanetBody), true);
            if (_body != null && GUILayout.Button("Sync From PlanetBody"))
                SyncFromSelection();

            EditorGUILayout.Space(8);
            DrawPresetSection();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Anchor Radius", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            float newR = EditorGUILayout.FloatField("Planet Radius R (m)", _model.anchorRadius);
            if (EditorGUI.EndChangeCheck())
            {
                PlanetaryCompositionRatioSolver.ApplyAnchorRadius(_model, newR);
                PushToProfiles();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Lock All Shell Geometry"))
            {
                _model.LockAllShellGeometry();
                PushToProfiles();
            }
            if (GUILayout.Button("Unlock All Artistic"))
            {
                _model.UnlockAllArtistic();
                PushToProfiles();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            DrawFieldSection("Core", "core");
            DrawFieldSection("Mantle", "mantle");
            DrawFieldSection("Lava", "lava");
            DrawFieldSection("Crust", "crust");
            DrawAtmosphereSection();
            DrawFieldSection("Horizon LOD", "horizon", false);
            DrawFieldSection("SDF LOD", "sdf", false);

            EditorGUILayout.Space(12);
            if (GUILayout.Button("Apply To Profiles"))
                PushToProfiles();
            if (GUILayout.Button("Apply & Rebuild Planet"))
            {
                PushToProfiles();
                if (_body != null)
                {
                    _body.RebuildAll();
                    EditorUtility.SetDirty(_body);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawPresetSection()
        {
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            _presetLibrary = (PlanetaryCompositionPresetLibrary)EditorGUILayout.ObjectField(
                "Library", _presetLibrary, typeof(PlanetaryCompositionPresetLibrary), false);
            if (_presetLibrary == null || _presetLibrary.presets == null || _presetLibrary.presets.Length == 0)
                return;
            var names = new string[_presetLibrary.presets.Length];
            for (int i = 0; i < names.Length; i++)
                names[i] = _presetLibrary.presets[i].displayName;
            _presetIndex = EditorGUILayout.Popup("Preset", _presetIndex, names);
            if (GUILayout.Button("Apply Preset"))
                ApplyPreset(_presetLibrary.presets[_presetIndex]);
        }

        void ApplyPreset(PlanetaryCompositionPreset preset)
        {
            if (_body != null)
            {
                _body.planetRadius = preset.planetRadius;
                if (preset.composition != null)
                    _body.compositionProfile = preset.composition;
                if (_body.sdfLodRenderer != null && preset.sdfLod != null)
                    _body.sdfLodRenderer.profile = preset.sdfLod;
                if (preset.horizonLod != null)
                    _body.horizonLodSettings = preset.horizonLod;
            }
            _composition = preset.composition;
            _atmosphere = preset.atmosphere;
            _horizon = preset.horizonLod;
            _sdfLod = preset.sdfLod;
            PlanetaryCompositionRatioSolver.CaptureRatiosFromProfile(
                _model, _body, _composition, _atmosphere, _horizon, _sdfLod);
            _model.anchorRadius = preset.planetRadius;
            PushToProfiles();
        }

        void DrawFieldSection(string title, string prefix, bool includeWeight = true)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            DrawRatioField($"{prefix}.offset", "Offset (m)");
            DrawRatioField($"{prefix}.thickness", "Thickness (m)");
            DrawRatioField($"{prefix}.smooth", "Smooth radius (m)");
            if (includeWeight)
                DrawRatioField($"{prefix}.weight", "Weight");
        }

        void DrawAtmosphereSection()
        {
            EditorGUILayout.LabelField("Atmosphere", EditorStyles.boldLabel);
            DrawRatioField("atmos.cloudBase", "Cloud base (m)");
            DrawRatioField("atmos.cloudTop", "Cloud top (m)");
            DrawRatioField("atmos.troposphereTop", "Troposphere top (m)");
            DrawRatioField("atmos.pressureScaleHeight", "Pressure scale height (m)");
            DrawRatioField("atmos.cloudDensityCoeff", "Cloud density coeff");
        }

        void DrawRatioField(string id, string label)
        {
            if (!_model.TryGetField(id, out var field))
                return;
            EditorGUILayout.BeginHorizontal();
            bool locked = EditorGUILayout.Toggle(field.ratioLocked, GUILayout.Width(18));
            float value = field.ratioLocked
                ? field.ratio * _model.anchorRadius
                : field.manualOverride;
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.FloatField(label, value);
            if (EditorGUI.EndChangeCheck())
            {
                if (locked)
                    field.ratio = _model.anchorRadius > 1e-6f ? value / _model.anchorRadius : value;
                else
                    field.manualOverride = value;
                field.ratioLocked = locked;
                _model.SetField(field);
                PushToProfiles();
            }
            else if (locked != field.ratioLocked)
            {
                field.ratioLocked = locked;
                _model.SetField(field);
            }
            EditorGUILayout.EndHorizontal();
        }

        void PushToProfiles()
        {
            PlanetaryCompositionRatioSolver.WriteToProfile(
                _model, _body, _composition, _atmosphere, _horizon, _sdfLod);
            if (_composition != null)
                EditorUtility.SetDirty(_composition);
            if (_atmosphere != null)
                EditorUtility.SetDirty(_atmosphere);
            if (_horizon != null)
                EditorUtility.SetDirty(_horizon);
            if (_sdfLod != null)
                EditorUtility.SetDirty(_sdfLod);
        }
    }
}
