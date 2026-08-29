using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class RelationshipTravelAgentWindow : EditorWindow
{
    RelationshipTravelAgent _agent;
    NarrativeCalendarAsset _calendar;
    RelationshipDialogGraphView _graph;
    IMGUIContainer _form;
    Vector2 _scroll;
    int _subjectCount;

    [MenuItem("Locomotion/Relationship Travel Agent")]
    public static void Open()
    {
        var w = GetWindow<RelationshipTravelAgentWindow>("Relationship Travel Agent");
        w.minSize = new Vector2(520, 640);
    }

    public static void OpenWith(RelationshipTravelAgent agent)
    {
        Open();
        GetWindow<RelationshipTravelAgentWindow>()._agent = agent;
    }

    void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.flexDirection = FlexDirection.Column;
        _form = new IMGUIContainer(DrawForm);
        _form.style.minHeight = 420;
        _form.style.flexGrow = 0;
        root.Add(_form);
        _graph = new RelationshipDialogGraphView();
        _graph.style.minHeight = 220;
        _graph.style.flexGrow = 1;
        root.Add(_graph);
        RefreshGraph();
    }

    void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    void OnInspectorUpdate() => Repaint();

    void OnEditorUpdate()
    {
        if (focusedWindow == this)
            Repaint();
    }

    void DrawForm()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(400));
        _agent = (RelationshipTravelAgent)EditorGUILayout.ObjectField(
            "Agent", _agent, typeof(RelationshipTravelAgent), true);
        if (_agent == null)
        {
            EditorGUILayout.HelpBox("Assign a RelationshipTravelAgent.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        _agent.route = (RelationshipRoute)EditorGUILayout.ObjectField(
            "Route", _agent.route, typeof(RelationshipRoute), false);
        _agent.dialogTree = (RelationshipDialogTree)EditorGUILayout.ObjectField(
            "Dialog tree", _agent.dialogTree, typeof(RelationshipDialogTree), false);
        _agent.loveWarden = (LoveWarden)EditorGUILayout.ObjectField(
            "Love Warden", _agent.loveWarden, typeof(LoveWarden), true);
        _agent.romanceWarden = (RomanceWarden)EditorGUILayout.ObjectField(
            "Romance Warden", _agent.romanceWarden, typeof(RomanceWarden), true);
        _agent.consentWarden = (ConsentWarden)EditorGUILayout.ObjectField(
            "Consent Warden", _agent.consentWarden, typeof(ConsentWarden), true);
        _agent.theocraticWarden = (TheocraticWarden)EditorGUILayout.ObjectField(
            "Theocratic Warden", _agent.theocraticWarden, typeof(TheocraticWarden), true);
        _agent.justiceWarden = (JusticeWarden)EditorGUILayout.ObjectField(
            "Justice Warden", _agent.justiceWarden, typeof(JusticeWarden), true);
        _agent.rightsWarden = (RightsWarden)EditorGUILayout.ObjectField(
            "Rights Warden", _agent.rightsWarden, typeof(RightsWarden), true);
        _agent.threatWarden = (ThreatWarden)EditorGUILayout.ObjectField(
            "Threat Warden", _agent.threatWarden, typeof(ThreatWarden), true);
        _agent.courtWarden = (CourtWarden)EditorGUILayout.ObjectField(
            "Court Warden", _agent.courtWarden, typeof(CourtWarden), true);
        _agent.corruptionWarden = (CorruptionWarden)EditorGUILayout.ObjectField(
            "Corruption Warden", _agent.corruptionWarden, typeof(CorruptionWarden), true);
        _agent.governmentWarden = (GovernmentWarden)EditorGUILayout.ObjectField(
            "Government Warden", _agent.governmentWarden, typeof(GovernmentWarden), true);
        _agent.lawWarden = (LawWarden)EditorGUILayout.ObjectField(
            "Law Warden", _agent.lawWarden, typeof(LawWarden), true);
        _agent.bioRhythm = (RelationshipBioRhythm)EditorGUILayout.ObjectField(
            "Bio Rhythm", _agent.bioRhythm, typeof(RelationshipBioRhythm), true);
        _agent.ragdoll = (RelationshipRagdoll)EditorGUILayout.ObjectField(
            "Ragdoll", _agent.ragdoll, typeof(RelationshipRagdoll), true);
        _calendar = (NarrativeCalendarAsset)EditorGUILayout.ObjectField(
            "Calendar", _calendar, typeof(NarrativeCalendarAsset), true);

        if (_agent.subjects == null)
            _agent.subjects = new List<GameObject>();
        _subjectCount = EditorGUILayout.IntField("Subjects", _agent.subjects.Count);
        _subjectCount = Mathf.Max(0, _subjectCount);
        while (_agent.subjects.Count < _subjectCount)
            _agent.subjects.Add(null);
        while (_agent.subjects.Count > _subjectCount)
            _agent.subjects.RemoveAt(_agent.subjects.Count - 1);
        for (int i = 0; i < _agent.subjects.Count; i++)
        {
            _agent.subjects[i] = (GameObject)EditorGUILayout.ObjectField(
                $"Subject {i}", _agent.subjects[i], typeof(GameObject), true);
        }

        RomanceSeverity target = _agent.route != null ? _agent.route.targetSeverity : RomanceSeverity.GoingOut;
        if (GUILayout.Button("Resolve Path"))
        {
            _agent.ResolvePath(_agent.subjects, target);
            RefreshGraph();
        }

        if (_agent.steps == null)
            _agent.steps = new List<RelationshipStep>();
        if (_agent.steps.Count > 0)
        {
            _agent.selectedStepIndex = EditorGUILayout.IntSlider(
                "Step", _agent.selectedStepIndex, 0, _agent.steps.Count - 1);
        }

        var step = _agent.SelectedStep;
        if (step != null)
        {
            step.station = (RelationshipStationKind)EditorGUILayout.EnumPopup("Station", step.station);
            step.targetSeverity = (RomanceSeverity)EditorGUILayout.EnumPopup("Stage", step.targetSeverity);
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
            step.hasInpaint = EditorGUILayout.Toggle("Developer in-paint", step.hasInpaint);
            if (step.hasInpaint)
                step.inpaintWorld = EditorGUILayout.Vector3Field("In-paint world", step.inpaintWorld);
            step.predictedWorld = EditorGUILayout.Vector3Field("Predicted world", step.predictedWorld);
            step.dialogColumnIndex = EditorGUILayout.IntField("Dialog column", step.dialogColumnIndex);
        }

        if (GUILayout.Button("Prebake Calendar"))
        {
            int n = _agent.PrebakeCalendar(_calendar);
            EditorGUILayout.HelpBox($"Wrote {n} relationship events.", MessageType.Info);
        }
        if (GUILayout.Button("Apply Selected Plan Effect"))
            _agent.CompleteSelected();

        float shine = 0.5f + 0.5f * Mathf.Sin((float)EditorApplication.timeSinceStartup * 2.2f);
        Rect diamond = GUILayoutUtility.GetRect(280, 280);
        PowerDiamondDrawer.DrawOverlay(
            diamond,
            RelationshipPowerDiamond.Axes,
            null,
            _agent.RedLimit01(),
            _agent.WhiteActual01(),
            0f,
            _agent.GreenExpected01(),
            shine);

        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_agent);
    }

    void RefreshGraph()
    {
        if (_graph == null) return;
        RelationshipDialogTree tree = null;
        if (_agent != null)
            tree = _agent.dialogTree != null
                ? _agent.dialogTree
                : (_agent.route != null ? _agent.route.dialogTree : null);
        _graph.Populate(tree);
    }
}
