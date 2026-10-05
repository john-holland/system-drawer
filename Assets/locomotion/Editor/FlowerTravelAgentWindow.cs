#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class FlowerTravelAgentWindow : EditorWindow
{
    FlowerTravelAgent _agent;
    Vector2 _scroll;

    [MenuItem("Locomotion/Flower Travel Agent")]
    public static void Open()
    {
        var w = GetWindow<FlowerTravelAgentWindow>("Flower TA");
        w.minSize = new Vector2(440, 520);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _agent = (FlowerTravelAgent)EditorGUILayout.ObjectField(
            "Agent", _agent, typeof(FlowerTravelAgent), true);
        if (_agent == null)
        {
            EditorGUILayout.HelpBox("Assign a FlowerTravelAgent.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }
        if (_agent.steps == null || _agent.steps.Count == 0)
            _agent.steps = FlowerDevelopment.DefaultPipeline();

        _agent.speciesId = EditorGUILayout.TextField("Species", _agent.speciesId);
        _agent.organism = (PlantOrganismDef)EditorGUILayout.ObjectField(
            "Organism", _agent.organism, typeof(PlantOrganismDef), false);
        _agent.fruitMapping = (FlowerFruitMapping)EditorGUILayout.ObjectField(
            "Fruit map", _agent.fruitMapping, typeof(FlowerFruitMapping), false);
        _agent.fertilized = EditorGUILayout.Toggle("Fertilized", _agent.fertilized);
        _agent.selectedStepIndex = EditorGUILayout.IntSlider(
            "Step", _agent.selectedStepIndex, 0, Mathf.Max(0, _agent.steps.Count - 1));

        var step = _agent.SelectedStep;
        if (step != null)
        {
            EditorGUILayout.LabelField("Kind", step.kind.ToString());
            step.attractsPollinators = EditorGUILayout.Toggle("Attracts pollinators", step.attractsPollinators);
            step.releaseImpulse = EditorGUILayout.FloatField("Release impulse", step.releaseImpulse);
            step.intakeAngleDeg = EditorGUILayout.FloatField("Intake angle", step.intakeAngleDeg);
            step.intakeRadius = EditorGUILayout.FloatField("Intake radius", step.intakeRadius);
            DrawPieces(step);
            DrawMaps(step);
        }

        if (GUILayout.Button("Advance phenology"))
            _agent.TryAdvancePhenology();
        if (GUILayout.Button("Bake Open/Close BT (bud through anthesis)"))
        {
            var parent = _agent.transform.Find("FlowerOpenClose")
                         ?? new GameObject("FlowerOpenClose").transform;
            parent.SetParent(_agent.transform, false);
            Locomotion.Open.FlowerOpenCloseBt.Bake(_agent, parent, _agent.transform);
        }

        Rect diamond = GUILayoutUtility.GetRect(280, 280);
        PowerDiamondDrawer.DrawOverlay(
            diamond,
            FlowerTravelAgent.DiamondAxes,
            _agent.BlueOptimal01(),
            _agent.RedLimit01(),
            _agent.DashedWhiteActive01(),
            0f);
        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_agent);
    }

    static void DrawPieces(FlowerDevStep step)
    {
        if (step.pieces == null) return;
        EditorGUILayout.LabelField("Centroids", EditorStyles.boldLabel);
        for (int i = 0; i < step.pieces.Count; i++)
        {
            var piece = step.pieces[i];
            if (piece == null) continue;
            piece.centroidLocal = EditorGUILayout.Vector3Field(piece.pieceId, piece.centroidLocal);
        }
    }

    static void DrawMaps(FlowerDevStep step)
    {
        if (step.maps == null || step.maps.Count == 0) return;
        EditorGUILayout.LabelField("Piece maps", EditorStyles.boldLabel);
        for (int i = 0; i < step.maps.Count; i++)
        {
            var map = step.maps[i];
            if (map == null) continue;
            if (map.tween == null)
                map.tween = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            map.tween = EditorGUILayout.CurveField(map.pieceId, map.tween);
        }
    }
}
#endif
