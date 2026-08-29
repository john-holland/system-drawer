using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class LawTravelAgentWindow : EditorWindow
{
    LawTravelAgent _agent;
    NarrativeCalendarAsset _calendar;
    LawTravelAgentGraphView _graph;
    IMGUIContainer _form;
    Vector2 _scroll;
    LawStageKind _newKind = LawStageKind.Draft;
    static readonly string[] Axes = { "Constitution", "Justice", "Rights", "Law" };

    [MenuItem("Locomotion/Law Travel Agent")]
    public static void Open()
    {
        var w = GetWindow<LawTravelAgentWindow>("Law Travel Agent");
        w.minSize = new Vector2(560, 680);
    }

    public static void OpenWith(LawTravelAgent agent)
    {
        Open();
        GetWindow<LawTravelAgentWindow>()._agent = agent;
    }

    void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.flexDirection = FlexDirection.Column;
        _form = new IMGUIContainer(DrawForm);
        _form.style.minHeight = 420;
        _form.style.flexGrow = 0;
        root.Add(_form);
        _graph = new LawTravelAgentGraphView();
        _graph.style.minHeight = 220;
        _graph.style.flexGrow = 1;
        root.Add(_graph);
        RefreshGraph();
    }

    void OnEnable() => Undo.undoRedoPerformed += OnUndoRedo;

    void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

    void OnUndoRedo()
    {
        RefreshGraph();
        Repaint();
    }

    void RefreshGraph()
    {
        _graph?.Populate(_agent);
    }

    void DrawForm()
    {
        CityPixelGridDesignerUndo.DrawToolbar();
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(400));
        _agent = (LawTravelAgent)EditorGUILayout.ObjectField(
            "Agent", _agent, typeof(LawTravelAgent), true);
        if (_agent == null)
        {
            EditorGUILayout.HelpBox("Assign a LawTravelAgent.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        _agent.conversationBus = (ConversationBusTravelAgent)EditorGUILayout.ObjectField(
            "Conversation Bus", _agent.conversationBus, typeof(ConversationBusTravelAgent), true);
        _agent.constitutionWarden = (ConstitutionWarden)EditorGUILayout.ObjectField(
            "Constitution", _agent.constitutionWarden, typeof(ConstitutionWarden), true);
        _agent.justiceWarden = (JusticeWarden)EditorGUILayout.ObjectField(
            "Justice", _agent.justiceWarden, typeof(JusticeWarden), true);
        _agent.rightsWarden = (RightsWarden)EditorGUILayout.ObjectField(
            "Rights", _agent.rightsWarden, typeof(RightsWarden), true);
        _agent.loveWarden = (LoveWarden)EditorGUILayout.ObjectField(
            "Love", _agent.loveWarden, typeof(LoveWarden), true);
        _agent.romanceWarden = (RomanceWarden)EditorGUILayout.ObjectField(
            "Romance", _agent.romanceWarden, typeof(RomanceWarden), true);
        _agent.consentWarden = (ConsentWarden)EditorGUILayout.ObjectField(
            "Consent", _agent.consentWarden, typeof(ConsentWarden), true);
        _agent.lawWarden = (LawWarden)EditorGUILayout.ObjectField(
            "Law", _agent.lawWarden, typeof(LawWarden), true);
        _calendar = (NarrativeCalendarAsset)EditorGUILayout.ObjectField(
            "Calendar", _calendar != null ? _calendar : _agent.calendar, typeof(NarrativeCalendarAsset), true);
        _agent.calendar = _calendar;

        var diamond = GUILayoutUtility.GetRect(220, 160);
        PowerDiamondDrawer.DrawOverlay(
            diamond, Axes, _agent.DiamondWhite01(), _agent.DiamondRed01(),
            _agent.DiamondWhite01(), 0f, _agent.DiamondGreen01(), 0.4f);

        EditorGUILayout.BeginHorizontal();
        _newKind = (LawStageKind)EditorGUILayout.EnumPopup(_newKind);
        if (GUILayout.Button("Add new stage"))
        {
            Undo.RecordObject(_agent, "Add law stage");
            _agent.AddStage(_newKind);
            EditorUtility.SetDirty(_agent);
            RefreshGraph();
        }
        EditorGUILayout.EndHorizontal();

        if (_agent.stages != null)
        {
            for (int i = 0; i < _agent.stages.Count; i++)
            {
                var s = _agent.stages[i];
                if (s == null) continue;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                s.accordionOpen = EditorGUILayout.Foldout(s.accordionOpen, s.displayName, true);
                if (GUILayout.Button("...", GUILayout.Width(28)))
                {
                    int idx = i;
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Remove"), false, () =>
                    {
                        Undo.RecordObject(_agent, "Remove law stage");
                        _agent.RemoveStageAt(idx);
                        EditorUtility.SetDirty(_agent);
                        RefreshGraph();
                    });
                    menu.ShowAsContext();
                }
                EditorGUILayout.EndHorizontal();
                if (s.accordionOpen)
                {
                    Undo.RecordObject(_agent, "Edit law stage");
                    s.displayName = EditorGUILayout.TextField("Name", s.displayName ?? "");
                    s.kind = (LawStageKind)EditorGUILayout.EnumPopup("Kind", s.kind);
                    s.lawCard = (LawCard)EditorGUILayout.ObjectField("Law Card", s.lawCard, typeof(LawCard), false);
                    s.conversationLeg = (ConversationBusTravelAgent)EditorGUILayout.ObjectField(
                        "Conversation leg", s.conversationLeg, typeof(ConversationBusTravelAgent), true);
                    DrawDollList("Congresspeople", s.congresspeople);
                    DrawDollList("Senators", s.senators);
                    DrawExecList(s.executives);
                    DrawTheoList(s.theocrats);
                    DrawDollList("Monarchs", s.monarchs);
                }
                EditorGUILayout.EndVertical();
            }
        }

        if (GUILayout.Button("Prebake Calendar"))
        {
            Undo.RecordObject(_agent, "Prebake law");
            _agent.PrebakeCalendar(_calendar);
            EditorUtility.SetDirty(_agent);
        }
        EditorGUILayout.EndScrollView();
    }

    static void DrawDollList<T>(string label, List<T> list) where T : UnityEngine.Object
    {
        if (list == null) return;
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
        for (int i = 0; i < list.Count; i++)
            list[i] = (T)EditorGUILayout.ObjectField(list[i], typeof(T), false);
        if (GUILayout.Button("Add " + label, GUILayout.Width(160)))
            list.Add(null);
    }

    static void DrawExecList(List<ExecutiveCard> list)
    {
        if (list == null) return;
        EditorGUILayout.LabelField("Executives", EditorStyles.miniBoldLabel);
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i] ?? new ExecutiveCard();
            e.militaristic = EditorGUILayout.Toggle("Militaristic", e.militaristic);
            list[i] = e;
        }
        if (GUILayout.Button("Add Executive", GUILayout.Width(160)))
            list.Add(new ExecutiveCard());
    }

    static void DrawTheoList(List<ReligiousFigure> list)
    {
        if (list == null) return;
        EditorGUILayout.LabelField("Theocrats", EditorStyles.miniBoldLabel);
        for (int i = 0; i < list.Count; i++)
            list[i] = (ReligiousFigure)EditorGUILayout.ObjectField(list[i], typeof(ReligiousFigure), false);
        if (GUILayout.Button("Add Theocrat", GUILayout.Width(160)))
            list.Add(null);
    }
}
