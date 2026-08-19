using Locomotion.Narrative;
using UnityEditor;
using UnityEngine;

public sealed class EducationalTravelAgentWindow : EditorWindow
{
    EducationalTravelAgent _agent;
    NarrativeCalendarAsset _calendar;
    Vector2 _scroll;

    [MenuItem("Locomotion/Educational Travel Agent")]
    public static void Open()
    {
        var w = GetWindow<EducationalTravelAgentWindow>("Educational Travel Agent");
        w.minSize = new Vector2(440, 480);
    }

    public static void OpenWith(EducationalTravelAgent agent)
    {
        Open();
        GetWindow<EducationalTravelAgentWindow>()._agent = agent;
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _agent = (EducationalTravelAgent)EditorGUILayout.ObjectField(
            "Agent", _agent, typeof(EducationalTravelAgent), true);
        if (_agent == null)
        {
            EditorGUILayout.HelpBox("Assign an EducationalTravelAgent.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        _agent.doll = (CivilianPaperDoll)EditorGUILayout.ObjectField(
            "Paper Doll", _agent.doll, typeof(CivilianPaperDoll), false);
        _agent.targetRole = (CareerRoleSpec)EditorGUILayout.ObjectField(
            "Target Role", _agent.targetRole, typeof(CareerRoleSpec), false);
        _agent.warden = (CareerWarden)EditorGUILayout.ObjectField(
            "Career Warden", _agent.warden, typeof(CareerWarden), true);
        _calendar = (NarrativeCalendarAsset)EditorGUILayout.ObjectField(
            "Calendar", _calendar, typeof(NarrativeCalendarAsset), true);

        if (GUILayout.Button("Resolve Path (lane + credentials)"))
            _agent.ResolvePath(_agent.doll, _agent.targetRole);

        if (_agent.steps == null)
            _agent.steps = new System.Collections.Generic.List<EducationalStep>();
        if (_agent.steps.Count > 0)
        {
            _agent.selectedStepIndex = EditorGUILayout.IntSlider(
                "Step", _agent.selectedStepIndex, 0, _agent.steps.Count - 1);
        }

        var step = _agent.SelectedStep;
        if (step != null)
        {
            step.station = (LearningStationKind)EditorGUILayout.EnumPopup("Station", step.station);
            step.timing = (EducationalTimingMode)EditorGUILayout.EnumPopup("Timing", step.timing);
            if (step.timing == EducationalTimingMode.RngRange)
            {
                step.minSeconds = EditorGUILayout.FloatField("Min seconds", step.minSeconds);
                step.maxSeconds = EditorGUILayout.FloatField("Max seconds", step.maxSeconds);
            }
            else if (step.timing == EducationalTimingMode.Specific)
                step.durationSeconds = EditorGUILayout.FloatField("Duration seconds", step.durationSeconds);
            else
                step.enablesEventId = EditorGUILayout.TextField("Enables event", step.enablesEventId);
            step.effect = (CareerPlanEffect)EditorGUILayout.EnumPopup("Plan effect", step.effect);
            step.targetRoleId = EditorGUILayout.TextField("Effect role", step.targetRoleId);
            step.hasInpaint = EditorGUILayout.Toggle("Developer in-paint", step.hasInpaint);
            if (step.hasInpaint)
                step.inpaintWorld = EditorGUILayout.Vector3Field("In-paint world", step.inpaintWorld);
            step.predictedWorld = EditorGUILayout.Vector3Field("Predicted world", step.predictedWorld);
        }

        if (GUILayout.Button("Prebake Calendar"))
        {
            int n = _agent.PrebakeCalendar(_calendar);
            EditorGUILayout.HelpBox($"Wrote {n} educational events.", MessageType.Info);
        }
        if (GUILayout.Button("Apply Selected Plan Effect"))
            _agent.CompleteSelected();

        Rect diamond = GUILayoutUtility.GetRect(280, 280);
        PowerDiamondDrawer.DrawOverlay(
            diamond,
            CivilianPaperDoll.GradeAxes,
            _agent.BlueExpected01(),
            _agent.RedFire01(),
            _agent.WhiteStep01(),
            0f);

        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_agent);
    }
}
