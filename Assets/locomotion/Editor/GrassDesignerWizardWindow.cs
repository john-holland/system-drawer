#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class GrassDesignerWizardWindow : EditorWindow
{
    enum Step
    {
        Climate = 0,
        Soil = 1,
        Morphology = 2,
        Save = 3
    }

    GrassTypeDef _type;
    Step _step;
    string _folder = "Assets";
    Vector2 _scroll;

    [MenuItem("Window/System Drawer/Grass Designer")]
    public static void Open()
    {
        var w = GetWindow<GrassDesignerWizardWindow>("Grass Designer");
        w.minSize = new Vector2(420, 480);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _step = (Step)GUILayout.Toolbar((int)_step, new[] { "Climate", "Soil", "Morphology", "Save" });
        _type = (GrassTypeDef)EditorGUILayout.ObjectField("Grass type", _type, typeof(GrassTypeDef), false);
        if (_type == null && GUILayout.Button("New grass type"))
        {
            _type = CreateInstance<GrassTypeDef>();
            _type.ApplyMorphology(GrassMorphology.Bunchgrass);
        }
        if (_type == null)
        {
            EditorGUILayout.HelpBox("Create or assign a GrassTypeDef.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        switch (_step)
        {
            case Step.Climate:
                _type.speciesId = EditorGUILayout.TextField("Species", _type.speciesId);
                _type.thermal = (GrassThermalClass)EditorGUILayout.EnumPopup("Thermal", _type.thermal);
                _type.monthStart = EditorGUILayout.IntSlider("Month start", _type.monthStart, 1, 12);
                _type.monthEnd = EditorGUILayout.IntSlider("Month end", _type.monthEnd, 1, 12);
                _type.biomeTempMinC = EditorGUILayout.FloatField("Biome min C", _type.biomeTempMinC);
                _type.biomeTempMaxC = EditorGUILayout.FloatField("Biome max C", _type.biomeTempMaxC);
                _type.waterRequirement01 = EditorGUILayout.Slider("Water", _type.waterRequirement01, 0f, 1f);
                break;
            case Step.Soil:
                _type.manifoldConfigId = EditorGUILayout.TextField("Soil manifold", _type.manifoldConfigId);
                _type.shedSize = EditorGUILayout.FloatField("Shed size", _type.shedSize);
                _type.leaf = (LeafSpec)EditorGUILayout.ObjectField("Leaf", _type.leaf, typeof(LeafSpec), false);
                _type.windFit = (KarmanVortexWindFit)EditorGUILayout.ObjectField(
                    "Wind fit", _type.windFit, typeof(KarmanVortexWindFit), false);
                break;
            case Step.Morphology:
                GrassMorphology next = (GrassMorphology)EditorGUILayout.EnumPopup("Form", _type.morphology);
                if (next != _type.morphology)
                    _type.ApplyMorphology(next);
                DrawSteps();
                break;
            default:
                _folder = EditorGUILayout.TextField("Folder", _folder);
                if (GUILayout.Button("Save type and organism"))
                    Save();
                break;
        }

        EditorGUILayout.BeginHorizontal();
        if (_step > Step.Climate && GUILayout.Button("Back"))
            _step--;
        if (_step < Step.Save && GUILayout.Button("Next"))
            _step++;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
        if (GUI.changed && _type != null)
            EditorUtility.SetDirty(_type);
    }

    void DrawSteps()
    {
        if (_type.steps == null)
            _type.steps = GrassDevelopment.DefaultPipeline();
        for (int i = 0; i < _type.steps.Count; i++)
        {
            GrassDevStep step = _type.steps[i];
            if (step == null)
                continue;
            EditorGUILayout.LabelField(i + " " + step.kind, EditorStyles.boldLabel);
            step.nodeSpacing = EditorGUILayout.FloatField("Node spacing", step.nodeSpacing);
        }
        EditorGUILayout.HelpBox(
            GrassDevelopment.RhizomeNodePrecedesEmergence(_type.steps)
                ? "Rhizome node is formed before a new tiller emerges."
                : "Rhizome emergence is ordered before its node.",
            GrassDevelopment.RhizomeNodePrecedesEmergence(_type.steps) ? MessageType.Info : MessageType.Warning);
    }

    void Save()
    {
        if (_type == null)
            return;
        string folder = string.IsNullOrEmpty(_folder) ? "Assets" : _folder;
        if (!AssetDatabase.IsValidFolder(folder))
            folder = "Assets";
        string stem = string.IsNullOrEmpty(_type.speciesId) ? "grass" : _type.speciesId;
        string typePath = Path.Combine(folder, stem + "_GrassType.asset").Replace('\\', '/');
        if (AssetDatabase.Contains(_type))
            EditorUtility.SetDirty(_type);
        else
        {
            if (AssetDatabase.LoadAssetAtPath<GrassTypeDef>(typePath) != null)
                AssetDatabase.DeleteAsset(typePath);
            AssetDatabase.CreateAsset(_type, typePath);
        }
        PlantOrganismDef organism = _type.CreateOrganism();
        string orgPath = Path.Combine(folder, stem + "_Organism.asset").Replace('\\', '/');
        if (AssetDatabase.LoadAssetAtPath<PlantOrganismDef>(orgPath) != null)
            AssetDatabase.DeleteAsset(orgPath);
        AssetDatabase.CreateAsset(organism, orgPath);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(_type);
    }
}
#endif
