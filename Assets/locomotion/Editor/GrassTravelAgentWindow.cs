#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class GrassTravelAgentWindow : EditorWindow
{
    GrassTravelAgent _agent;
    Vector2 _scroll;

    [MenuItem("Locomotion/Grass Travel Agent")]
    public static void Open()
    {
        var w = GetWindow<GrassTravelAgentWindow>("Grass TA");
        w.minSize = new Vector2(440, 520);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _agent = (GrassTravelAgent)EditorGUILayout.ObjectField("Agent", _agent, typeof(GrassTravelAgent), true);
        if (_agent == null)
        {
            EditorGUILayout.HelpBox("Assign a GrassTravelAgent.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }
        if (_agent.steps == null || _agent.steps.Count == 0)
            _agent.steps = GrassDevelopment.DefaultPipeline();

        _agent.speciesId = EditorGUILayout.TextField("Species", _agent.speciesId);
        _agent.grassType = (GrassTypeDef)EditorGUILayout.ObjectField("Type", _agent.grassType, typeof(GrassTypeDef), false);
        _agent.composition = (GroundCompositionSet)EditorGUILayout.ObjectField(
            "Composition", _agent.composition, typeof(GroundCompositionSet), false);
        _agent.organism = (PlantOrganismDef)EditorGUILayout.ObjectField(
            "Organism", _agent.organism, typeof(PlantOrganismDef), false);
        _agent.blockProceduralMotion = EditorGUILayout.Toggle("Block procedural motion", _agent.blockProceduralMotion);
        if (_agent.blockProceduralMotion)
        {
            _agent.blockAlong0 = EditorGUILayout.Slider("Block from", _agent.blockAlong0, 0f, 1f);
            _agent.blockAlong1 = EditorGUILayout.Slider("Block to", _agent.blockAlong1, 0f, 1f);
        }
        _agent.selectedStepIndex = EditorGUILayout.IntSlider(
            "Step", _agent.selectedStepIndex, 0, Mathf.Max(0, _agent.steps.Count - 1));
        GrassDevStep step = _agent.SelectedStep;
        if (step != null)
        {
            EditorGUILayout.LabelField("Kind", step.kind.ToString());
            step.water01 = EditorGUILayout.Slider("Water", step.water01, 0f, 1f);
            step.heat01 = EditorGUILayout.Slider("Heat", step.heat01, 0f, 1f);
            step.ground01 = EditorGUILayout.Slider("Ground", step.ground01, 0f, 1f);
            step.light01 = EditorGUILayout.Slider("Light", step.light01, 0f, 1f);
            step.nodeSpacing = EditorGUILayout.FloatField("Node spacing", step.nodeSpacing);
        }
        if (GUILayout.Button("Apply grass type"))
            _agent.ApplyType();

        Rect diamond = GUILayoutUtility.GetRect(280, 280);
        PowerDiamondDrawer.DrawOverlay(
            diamond,
            GrassTravelAgent.DiamondAxes,
            _agent.BlueOptimal01(),
            _agent.RedLimit01(),
            null,
            0f,
            _agent.DashedGreenCurrent01(),
            0f,
            true);
        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_agent);
    }
}
#endif
