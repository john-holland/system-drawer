#if UNITY_EDITOR
using Locomotion.Open;
using UnityEditor;
using UnityEngine;

public sealed class CannalTravelAgentWindow : EditorWindow
{
    CannalTravelAgent _agent;
    CannalStepKind _insertKind = CannalStepKind.Transit;
    Vector2 _scroll;

    [MenuItem("Locomotion/Cannal Travel Agent")]
    public static void Open()
    {
        var w = GetWindow<CannalTravelAgentWindow>("Cannal TA");
        w.minSize = new Vector2(420, 460);
    }

    public static void OpenWith(CannalTravelAgent agent)
    {
        Open();
        GetWindow<CannalTravelAgentWindow>()._agent = agent;
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _agent = (CannalTravelAgent)EditorGUILayout.ObjectField(
            "Agent", _agent, typeof(CannalTravelAgent), true);
        if (_agent == null)
        {
            EditorGUILayout.HelpBox("Assign a CannalTravelAgent.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        if (_agent.steps == null || _agent.steps.Count == 0)
            _agent.steps = CannalTravelAgent.DefaultPipeline();

        _agent.ribbon = (CanalRibbonSpec)EditorGUILayout.ObjectField(
            "Ribbon", _agent.ribbon, typeof(CanalRibbonSpec), false);
        _agent.lockSpec = (CanalLockSpec)EditorGUILayout.ObjectField(
            "Lock", _agent.lockSpec, typeof(CanalLockSpec), false);
        _agent.selectedStepIndex = EditorGUILayout.IntSlider(
            "Step", _agent.selectedStepIndex, 0, Mathf.Max(0, _agent.steps.Count - 1));
        EditorGUILayout.BeginHorizontal();
        _insertKind = (CannalStepKind)EditorGUILayout.EnumPopup("Insert step", _insertKind);
        if (GUILayout.Button("Insert", GUILayout.Width(72)))
        {
            Undo.RecordObject(_agent, "Insert cannal step");
            _agent.InsertStep(_insertKind, _agent.selectedStepIndex);
            EditorUtility.SetDirty(_agent);
        }
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Bake water velocity"))
            _agent.BakeWater();
        if (_agent.waterBake != null)
            EditorGUILayout.LabelField("Water bins", _agent.waterBake.BinCount.ToString());
        if (GUILayout.Button("Bake Open/Close BT (lock order)"))
        {
            var parent = _agent.transform.Find("CanalLockOpenClose")
                         ?? new GameObject("CanalLockOpenClose").transform;
            parent.SetParent(_agent.transform, false);
            CanalLockOpenCloseBt.Bake(_agent, parent, _agent.transform);
        }

        Rect diamond = GUILayoutUtility.GetRect(280, 280);
        PowerDiamondDrawer.DrawOverlay(
            diamond,
            CannalTravelAgent.DiamondAxes,
            _agent.BlueOptimal01(),
            _agent.RedLimit01(),
            _agent.DashedWhiteActive01(),
            _agent.ThreatHalo01());

        var step = _agent.SelectedStep;
        if (step != null)
        {
            EditorGUILayout.LabelField("Kind", step.kind.ToString());
            EditorGUILayout.LabelField(_agent.OverLimit() ? "RED — over limit / threat" : "Within limits");
        }

        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_agent);
    }
}
#endif
