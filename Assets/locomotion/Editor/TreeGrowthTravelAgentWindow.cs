#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class TreeGrowthTravelAgentWindow : EditorWindow
{
    TreeGrowthTravelAgent _agent;
    Vector2 _scroll;

    [MenuItem("Locomotion/Tree Growth Travel Agent")]
    public static void Open()
    {
        var w = GetWindow<TreeGrowthTravelAgentWindow>("Tree Growth TA");
        w.minSize = new Vector2(420, 440);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _agent = (TreeGrowthTravelAgent)EditorGUILayout.ObjectField(
            "Agent", _agent, typeof(TreeGrowthTravelAgent), true);
        if (_agent == null)
        {
            EditorGUILayout.HelpBox("Assign a TreeGrowthTravelAgent.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }
        if (_agent.steps == null || _agent.steps.Count == 0)
            _agent.steps = TreeGrowthTravelAgent.DefaultPipeline();
        _agent.plantDef = (LotGrassPlantDef)EditorGUILayout.ObjectField(
            "Plant def", _agent.plantDef, typeof(LotGrassPlantDef), false);
        _agent.selectedStepIndex = EditorGUILayout.IntSlider(
            "Step", _agent.selectedStepIndex, 0, Mathf.Max(0, _agent.steps.Count - 1));
        _agent.enforceNaturalGrowthFromPhysicsManifolds = EditorGUILayout.Toggle(
            "Enforce natural growth", _agent.enforceNaturalGrowthFromPhysicsManifolds);
        if (GUILayout.Button("Complete selected (success)"))
            _agent.CompleteSelected(true);
        if (GUILayout.Button("Fail selected (stop if no failure event)"))
            _agent.CompleteSelected(false);
        _agent.groundEvent.openCloseComplete = EditorGUILayout.Toggle(
            "Open/close complete", _agent.groundEvent.openCloseComplete);
        if (GUILayout.Button("Apply ground composition"))
            _agent.ApplyGroundCompositionAfterOpenClose();
        if (GUILayout.Button("Bake Open/Close BT (growth order)"))
        {
            var parent = _agent.transform.Find("TreeGrowthOpenClose")
                         ?? new GameObject("TreeGrowthOpenClose").transform;
            parent.SetParent(_agent.transform, false);
            Locomotion.Open.TreeGrowthOpenCloseBt.Bake(_agent, parent, _agent.transform);
        }

        Rect diamond = GUILayoutUtility.GetRect(280, 280);
        PowerDiamondDrawer.DrawOverlay(
            diamond,
            TreeGrowthTravelAgent.DiamondAxes,
            _agent.BlueOptimal01(),
            _agent.RedLimit01(),
            _agent.DashedWhiteActive01(),
            0f);
        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_agent);
    }
}

public sealed class ChainsawDesignerWindow : EditorWindow
{
    ChainsawSpec _spec;
    Vector2 _scroll;

    [MenuItem("Locomotion/Chainsaw Designer")]
    public static void Open()
    {
        var w = GetWindow<ChainsawDesignerWindow>("Chainsaw");
        w.minSize = new Vector2(420, 360);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _spec = (ChainsawSpec)EditorGUILayout.ObjectField("Chainsaw", _spec, typeof(ChainsawSpec), false);
        if (_spec == null)
        {
            if (GUILayout.Button("Create ChainsawSpec"))
            {
                var path = EditorUtility.SaveFilePanelInProject("Save Chainsaw", "Chainsaw", "asset", "");
                if (!string.IsNullOrEmpty(path))
                {
                    var s = CreateInstance<ChainsawSpec>();
                    AssetDatabase.CreateAsset(s, path);
                    _spec = s;
                }
            }
            EditorGUILayout.EndScrollView();
            return;
        }
        _spec.chain = (GarageChainSpec)EditorGUILayout.ObjectField("Cutting chain", _spec.chain, typeof(GarageChainSpec), false);
        _spec.motorGearbox = (GearboxSpec)EditorGUILayout.ObjectField("Motor gearbox", _spec.motorGearbox, typeof(GearboxSpec), false);
        _spec.sharpness01 = EditorGUILayout.Slider("Sharpness", _spec.sharpness01, 0f, 1f);
        _spec.hardness01 = EditorGUILayout.Slider("Hardness", _spec.hardness01, 0f, 1f);
        _spec.barLengthM = EditorGUILayout.FloatField("Bar length", _spec.barLengthM);
        if (GUILayout.Button("Open Gearbox Designer"))
            GearboxDesignerWindow.Open();
        if (GUILayout.Button("Open Garage Chain Designer"))
            GarageChainDesignerWindow.Open();
        if (GUI.changed)
            EditorUtility.SetDirty(_spec);
        EditorGUILayout.EndScrollView();
    }
}
#endif
