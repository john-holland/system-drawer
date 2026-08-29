using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEditor;
using UnityEngine;

public sealed class ConversationBusTravelAgentWindow : EditorWindow
{
    ConversationBusTravelAgent _agent;
    NarrativeCalendarAsset _calendar;
    Vector2 _scroll;
    ConversationSectionType _newSection = ConversationSectionType.Dialog;
    static readonly string[] DiamondAxes = { "Court", "Constitution", "Rights", "Theocratic" };

    [MenuItem("Locomotion/Conversation Bus")]
    public static void Open()
    {
        var w = GetWindow<ConversationBusTravelAgentWindow>("Conversation Bus");
        w.minSize = new Vector2(520, 640);
    }

    public static void OpenWith(ConversationBusTravelAgent agent)
    {
        Open();
        GetWindow<ConversationBusTravelAgentWindow>()._agent = agent;
    }

    void OnEnable() => Undo.undoRedoPerformed += OnUndoRedo;

    void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

    void OnUndoRedo() => Repaint();

    void OnGUI()
    {
        CityPixelGridDesignerUndo.DrawToolbar();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _agent = (ConversationBusTravelAgent)EditorGUILayout.ObjectField(
            "Agent", _agent, typeof(ConversationBusTravelAgent), true);
        if (_agent == null)
        {
            EditorGUILayout.HelpBox("Assign a ConversationBusTravelAgent.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        DrawWardens();
        _calendar = (NarrativeCalendarAsset)EditorGUILayout.ObjectField(
            "Calendar", _calendar != null ? _calendar : _agent.calendar, typeof(NarrativeCalendarAsset), true);
        _agent.calendar = _calendar;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Power diamond (escape dialog)", EditorStyles.boldLabel);
        var diamond = GUILayoutUtility.GetRect(220, 160);
        PowerDiamondDrawer.DrawOverlay(
            diamond, DiamondAxes, _agent.DiamondActual01(), _agent.DiamondLimit01(),
            _agent.DiamondActual01(), 0f, _agent.DiamondGreen01(), 0.4f);
        EditorGUILayout.LabelField("Green: Love / Romance / Consent / Justice (0.5 if missing)", EditorStyles.miniLabel);

        DrawKv("Agent limits", _agent.limits, () =>
        {
            Undo.RecordObject(_agent, "new+! limit");
            _agent.AddDefaultLimit();
            EditorUtility.SetDirty(_agent);
        });

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Accordion legs", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        _newSection = (ConversationSectionType)EditorGUILayout.EnumPopup(_newSection);
        if (GUILayout.Button("new++!", GUILayout.Width(72)))
        {
            Undo.RecordObject(_agent, "new++! section");
            _agent.AddSection(_newSection);
            EditorUtility.SetDirty(_agent);
        }
        EditorGUILayout.EndHorizontal();

        if (_agent.steps == null)
            _agent.steps = new List<ConversationBusStep>();
        for (int i = 0; i < _agent.steps.Count; i++)
            DrawStep(i);

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(_agent.ComposeDialoguePrompt(), MessageType.None);
        if (GUILayout.Button("Prebake Calendar"))
        {
            Undo.RecordObject(_agent, "Prebake conversation");
            if (_calendar != null) Undo.RecordObject(_calendar, "Prebake conversation");
            _agent.PrebakeCalendar(_calendar);
            EditorUtility.SetDirty(_agent);
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawWardens()
    {
        _agent.courtWarden = (CourtWarden)EditorGUILayout.ObjectField(
            "Court", _agent.courtWarden, typeof(CourtWarden), true);
        _agent.corruptionWarden = (CorruptionWarden)EditorGUILayout.ObjectField(
            "Corruption", _agent.corruptionWarden, typeof(CorruptionWarden), true);
        _agent.constitutionWarden = (ConstitutionWarden)EditorGUILayout.ObjectField(
            "Constitution", _agent.constitutionWarden, typeof(ConstitutionWarden), true);
        _agent.rightsWarden = (RightsWarden)EditorGUILayout.ObjectField(
            "Rights", _agent.rightsWarden, typeof(RightsWarden), true);
        _agent.justiceWarden = (JusticeWarden)EditorGUILayout.ObjectField(
            "Justice", _agent.justiceWarden, typeof(JusticeWarden), true);
        _agent.theocraticWarden = (TheocraticWarden)EditorGUILayout.ObjectField(
            "Theocratic", _agent.theocraticWarden, typeof(TheocraticWarden), true);
        _agent.loveWarden = (LoveWarden)EditorGUILayout.ObjectField(
            "Love", _agent.loveWarden, typeof(LoveWarden), true);
        _agent.romanceWarden = (RomanceWarden)EditorGUILayout.ObjectField(
            "Romance", _agent.romanceWarden, typeof(RomanceWarden), true);
        _agent.consentWarden = (ConsentWarden)EditorGUILayout.ObjectField(
            "Consent", _agent.consentWarden, typeof(ConsentWarden), true);
    }

    void DrawStep(int i)
    {
        var step = _agent.steps[i];
        if (step == null) return;
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        step.accordionOpen = EditorGUILayout.Foldout(step.accordionOpen, step.displayName + " (" + step.sectionType + ")", true);
        if (GUILayout.Button("...", GUILayout.Width(28)))
        {
            var menu = new GenericMenu();
            int idx = i;
            menu.AddItem(new GUIContent("Remove"), false, () =>
            {
                Undo.RecordObject(_agent, "Remove conversation step");
                _agent.steps.RemoveAt(idx);
                EditorUtility.SetDirty(_agent);
            });
            menu.ShowAsContext();
        }
        EditorGUILayout.EndHorizontal();
        if (!step.accordionOpen)
        {
            EditorGUILayout.EndVertical();
            return;
        }
        Undo.RecordObject(_agent, "Edit conversation step");
        step.displayName = EditorGUILayout.TextField("Name", step.displayName ?? "");
        step.sectionType = (ConversationSectionType)EditorGUILayout.EnumPopup("Section", step.sectionType);
        step.dialogNodeId = EditorGUILayout.TextField("Dialog node", step.dialogNodeId ?? "");
        step.dialogTreeSetId = EditorGUILayout.TextField("Dialog tree set", step.dialogTreeSetId ?? "");
        step.conversationCard = (ConversationCard)EditorGUILayout.ObjectField(
            "Conversation Card", step.conversationCard, typeof(ConversationCard), false);
        step.lawConversationCard = (LawConversationCard)EditorGUILayout.ObjectField(
            "Law Conversation Card", step.lawConversationCard, typeof(LawConversationCard), false);
        step.religiousLawCard = (ReligiousLawCard)EditorGUILayout.ObjectField(
            "Religious Law Card", step.religiousLawCard, typeof(ReligiousLawCard), false);
        step.lawCard = (LawCard)EditorGUILayout.ObjectField(
            "Law Card", step.lawCard, typeof(LawCard), false);
        if (step.sectionType == ConversationSectionType.Councilor || step.councilorCard != null)
            DrawCouncilor(step);
        if (step.sectionType == ConversationSectionType.Chancellor || step.chancellorCard != null)
            DrawChancellor(step);
        DrawKv("Step limits", step.limits, () =>
        {
            Undo.RecordObject(_agent, "new+! step limit");
            step.AddDefaultLimit();
            EditorUtility.SetDirty(_agent);
        });
        EditorGUILayout.EndVertical();
    }

    static void DrawCouncilor(ConversationBusStep step)
    {
        if (step.councilorCard == null)
            step.councilorCard = new CouncilorCard();
        EditorGUILayout.LabelField("Councilor", EditorStyles.boldLabel);
        step.councilorCard.developerInpaint = EditorGUILayout.Toggle(
            "Developer in-paint", step.councilorCard.developerInpaint);
        step.councilorCard.inpaintPrompt = EditorGUILayout.TextField(
            "In-paint prompt", step.councilorCard.inpaintPrompt ?? "");
    }

    static void DrawChancellor(ConversationBusStep step)
    {
        if (step.chancellorCard == null)
            step.chancellorCard = new ChancellorCard();
        EditorGUILayout.LabelField("Chancellor", EditorStyles.boldLabel);
        step.chancellorCard.isHeadOfUniversity = EditorGUILayout.Toggle(
            "Head of university", step.chancellorCard.isHeadOfUniversity);
        step.chancellorCard.universityReference = (UniversityCampusAsset)EditorGUILayout.ObjectField(
            "University", step.chancellorCard.universityReference, typeof(UniversityCampusAsset), false);
        step.chancellorCard.isScribe = EditorGUILayout.Toggle("Scribe", step.chancellorCard.isScribe);
        step.chancellorCard.penInk = (PenInkInstrument)EditorGUILayout.ObjectField(
            "Pen + ink", step.chancellorCard.penInk, typeof(PenInkInstrument), true);
        step.chancellorCard.sharedCanvas = (PaintCanvas)EditorGUILayout.ObjectField(
            "Shared canvas", step.chancellorCard.sharedCanvas, typeof(PaintCanvas), true);
        step.chancellorCard.inpaintPrompt = EditorGUILayout.TextField(
            "In-paint prompt", step.chancellorCard.inpaintPrompt ?? "");
        if (GUILayout.Button("Wire shared canvas"))
            step.chancellorCard.WireSharedCanvas();
    }

    static void DrawKv(string label, List<WardenLimitKv> list, System.Action add)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        if (GUILayout.Button("new+!", GUILayout.Width(56)))
            add();
        EditorGUILayout.EndHorizontal();
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            var row = list[i];
            if (row == null) continue;
            EditorGUILayout.BeginHorizontal();
            row.key = EditorGUILayout.TextField(row.key ?? "");
            row.value01 = EditorGUILayout.Slider(row.value01, 0f, 1f);
            EditorGUILayout.EndHorizontal();
        }
    }
}
