using Locomotion.Open;
using UnityEditor;
using UnityEngine;

public sealed class HouseConstructionTravelAgentWindow : EditorWindow
{
    HouseConstructionTravelAgent _agent;
    Vector2 _scroll;

    [MenuItem("Locomotion/House Construction Travel Agent")]
    public static void Open()
    {
        var w = GetWindow<HouseConstructionTravelAgentWindow>("House Construction TA");
        w.minSize = new Vector2(420, 420);
    }

    public static void OpenWith(HouseConstructionTravelAgent agent)
    {
        Open();
        var w = GetWindow<HouseConstructionTravelAgentWindow>();
        w._agent = agent;
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _agent = (HouseConstructionTravelAgent)EditorGUILayout.ObjectField(
            "Agent", _agent, typeof(HouseConstructionTravelAgent), true);
        if (_agent == null)
        {
            EditorGUILayout.HelpBox("Assign a HouseConstructionTravelAgent.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        if (_agent.steps == null || _agent.steps.Count == 0)
            _agent.steps = HouseConstructionTravelAgent.DefaultPipeline();

        _agent.selectedStepIndex = EditorGUILayout.IntSlider(
            "Step", _agent.selectedStepIndex, 0, Mathf.Max(0, _agent.steps.Count - 1));
        _agent.siteOpen = EditorGUILayout.Toggle("Site Open", _agent.siteOpen);
        if (GUILayout.Button("Harden Selected Step"))
            _agent.HardenSelected();
        if (GUILayout.Button("Bake Open/Close BT (placement order)"))
        {
            var parent = _agent.transform.Find("ConstructionOpenClose")
                         ?? new GameObject("ConstructionOpenClose").transform;
            parent.SetParent(_agent.transform, false);
            HouseConstructionOpenCloseBt.Bake(_agent, parent, _agent.transform);
        }
        Rect diamond = GUILayoutUtility.GetRect(280, 280);
        PowerDiamondDrawer.DrawOverlay(
            diamond,
            PowerDiamondDrawer.ConstructionAxes,
            _agent.BlueOptimal01(),
            _agent.RedLimit01(),
            _agent.DashedWhiteActive01(),
            _agent.ThreatHalo01());

        var step = _agent.SelectedStep;
        if (step != null)
        {
            EditorGUILayout.LabelField("Kind", step.kind.ToString());
            EditorGUILayout.LabelField("Progress", step.progress01.ToString("0.00"));
            EditorGUILayout.LabelField(_agent.OverLimit() ? "RED — over limit / threat" : "Within limits");
        }

        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_agent);
    }
}
