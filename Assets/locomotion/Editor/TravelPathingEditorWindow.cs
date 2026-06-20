using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Authoring and preview for multi-modal travel (typed spatial generator requires BedogaGenerator assembly reference).
/// </summary>
public class TravelPathingEditorWindow : EditorWindow
{
    static class Tips
    {
        public static readonly GUIContent TravelingActors = new GUIContent(
            "Traveling actors",
            "Every TravelAgent in the open scene. Click a name to select it in the hierarchy and edit its path here.");

        public static readonly GUIContent SpatialGenerator = new GUIContent(
            "Spatial Generator",
            "Bedoga SpatialGenerator or 4D orchestrator. When assigned with raw locations disabled, preview coordinates come from the generator workflow.");

        public static readonly GUIContent TravelScriptRows = new GUIContent(
            "Travel script (authoring rows)",
            "Ordered waypoints and bindings: coordinates, planner hints, narrative nodes, and Bedoga spatial nodes.");

        public static readonly GUIContent SaveRowChanges = new GUIContent(
            "Save row changes",
            "Persist authoring-row edits on this TravelAgent to disk.");

        public static readonly GUIContent RevertRowChanges = new GUIContent(
            "Revert row changes",
            "Discard unsaved row edits and restore the last saved snapshot.");

        public static readonly GUIContent UnsavedEdits = new GUIContent(
            "(unsaved row edits)",
            "Authoring rows differ from the last Save. Rebuild preview still uses current values; Save writes them to the asset.");

        public static readonly GUIContent AddHint = new GUIContent(
            "Add Hint",
            "Append a planner hint row — a lightweight landmark that biases routing without binding a full narrative node.");

        public static readonly GUIContent AddNode = new GUIContent(
            "Add Node",
            "Append a narrative or behavior-tree node reference row.");

        public static readonly GUIContent AddSpatialNode = new GUIContent(
            "Add Spatial Node",
            "Append a Bedoga spatial-generator node binding row.");

        public static readonly GUIContent ReverseLegLimit = new GUIContent(
            "Reverse leg limit",
            "Maximum share of total path arc length allowed for reverse or backtracking samples (skier-track kinematics).");

        public static readonly GUIContent ResetReverseDefault = new GUIContent(
            "Reset reverse to default",
            "Restore the limit to 100% for paths under 500 m, otherwise 50%.");

        public static readonly GUIContent ShowVelocityTrack = new GUIContent(
            "Show velocity track",
            "Draw speed-colored tick marks along the cached path in the Scene view.");

        public static readonly GUIContent ShowIkSamples = new GUIContent(
            "Show IK samples",
            "Draw IK solve sample points on path gizmos.");

        public static readonly GUIContent ShowReverseBudget = new GUIContent(
            "Show reverse budget",
            "Highlight reverse-budget consumption along the path in the Scene view.");

        public static readonly GUIContent TrackSpacing = new GUIContent(
            "Track spacing (m)",
            "Distance between velocity-track tick marks along the path.");

        public static readonly GUIContent RebuildPreview = new GUIContent(
            "Rebuild preview",
            "Run traversibility and multibody solvers, refresh cached plan gizmos, and update path metrics.");

        public static readonly GUIContent ZoomToFit = new GUIContent(
            "Zoom to fit",
            "Frame the Scene view to the entire path or the current segment, per Preview fit mode.");

        public static readonly GUIContent PreviousSegment = new GUIContent(
            "Previous segment",
            "Step to the prior plan segment and reframe the Scene view.");

        public static readonly GUIContent NextSegment = new GUIContent(
            "Next segment",
            "Step to the next plan segment and reframe the Scene view.");

        public static readonly GUIContent RefreshDiscovery = new GUIContent(
            "Refresh discovery list",
            "Scan the actor hierarchy for BehaviorTreeNode components (editor snapshot, not per-frame).");

        public static readonly GUIContent Discovery = new GUIContent(
            "Discovery",
            "Behavior-tree nodes found under the actor root on the last refresh.");

        public static readonly GUIContent LockedCoordinatesHelp = new GUIContent(
            "",
            "Preview start, goal, and coordinate mode are driven by the assigned spatial generator workflow.");
    }

    TravelAgent focusedAgent;
    Vector2 scrollActors;
    Vector2 scrollMain;
    SerializedObject serializedAgent;

    /// <summary>Deep copy baseline for <see cref="TravelAgent.authoringRows"/> (save/revert).</summary>
    List<TravelAuthoringRow> _authoringRowsBaseline;

    static TravelAgent PendingRebuild;

    [MenuItem("Window/System Drawer/Travel/Pathing Editor", false, 150)]
    public static void ShowWindow()
    {
        var window = GetWindow<TravelPathingEditorWindow>("Travel Pathing");
        window.titleContent = new GUIContent("Travel Pathing", "Author multi-modal travel paths, preview plans, and manage authoring rows.");
    }

    public static void Open(TravelAgent agent)
    {
        TravelPathingEditorWindow w = GetWindow<TravelPathingEditorWindow>("Travel Pathing");
        w.titleContent = new GUIContent("Travel Pathing", "Author multi-modal travel paths, preview plans, and manage authoring rows.");
        w.FocusTravelAgent(agent);
    }

    void FocusTravelAgent(TravelAgent agent)
    {
        focusedAgent = agent;
        serializedAgent = agent != null ? new SerializedObject(agent) : null;
        CaptureAuthoringBaseline();
        Repaint();
    }

    void CaptureAuthoringBaseline()
    {
        if (focusedAgent == null)
        {
            _authoringRowsBaseline = null;
            return;
        }

        _authoringRowsBaseline = CloneAuthoringRows(focusedAgent.authoringRows);
    }

    static List<TravelAuthoringRow> CloneAuthoringRows(List<TravelAuthoringRow> src)
    {
        var dst = new List<TravelAuthoringRow>();
        if (src == null)
            return dst;
        foreach (TravelAuthoringRow r in src)
        {
            if (r == null)
                continue;
            dst.Add(new TravelAuthoringRow
            {
                kind = r.kind,
                worldPosition = r.worldPosition,
                narrativeTime = r.narrativeTime,
                actorMapKey = r.actorMapKey ?? "",
                actorReference = r.actorReference,
                notes = r.notes ?? ""
            });
        }

        return dst;
    }

    static bool AuthoringRowsMatchBaseline(List<TravelAuthoringRow> current, List<TravelAuthoringRow> baseline)
    {
        if (baseline == null && (current == null || current.Count == 0))
            return true;
        if (baseline == null || current == null)
            return false;
        if (current.Count != baseline.Count)
            return false;
        for (int i = 0; i < current.Count; i++)
        {
            TravelAuthoringRow a = current[i];
            TravelAuthoringRow b = baseline[i];
            if (a == null && b == null)
                continue;
            if (a == null || b == null)
                return false;
            if (a.kind != b.kind ||
                a.worldPosition != b.worldPosition ||
                Mathf.Abs(a.narrativeTime - b.narrativeTime) > 1e-6f ||
                (a.actorMapKey ?? "") != (b.actorMapKey ?? "") ||
                a.actorReference != b.actorReference ||
                (a.notes ?? "") != (b.notes ?? ""))
                return false;
        }

        return true;
    }

    bool AuthoringRowsDirty =>
        focusedAgent != null &&
        !AuthoringRowsMatchBaseline(focusedAgent.authoringRows, _authoringRowsBaseline);

    void SaveAuthoringRows()
    {
        if (focusedAgent == null)
            return;
        EditorUtility.SetDirty(focusedAgent);
        AssetDatabase.SaveAssets();
        CaptureAuthoringBaseline();
        serializedAgent?.Update();
    }

    void RevertAuthoringRows()
    {
        if (focusedAgent == null || _authoringRowsBaseline == null)
            return;
        Undo.RecordObject(focusedAgent, "Revert travel authoring rows");
        focusedAgent.authoringRows = CloneAuthoringRows(_authoringRowsBaseline);
        EditorUtility.SetDirty(focusedAgent);
        serializedAgent = new SerializedObject(focusedAgent);
        serializedAgent.Update();
    }

    void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChanged;
    }

    void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }

    void OnSelectionChanged()
    {
        if (Selection.activeGameObject != null)
        {
            TravelAgent ta = Selection.activeGameObject.GetComponent<TravelAgent>();
            if (ta != null)
                FocusTravelAgent(ta);
        }
    }

    static void ScheduleRebuild(TravelAgent ta)
    {
        PendingRebuild = ta;
        EditorApplication.delayCall -= RunPendingRebuild;
        EditorApplication.delayCall += RunPendingRebuild;
    }

    static void RunPendingRebuild()
    {
        EditorApplication.delayCall -= RunPendingRebuild;
        if (PendingRebuild != null)
        {
            PendingRebuild.RebuildCachedPlan();
            EditorUtility.SetDirty(PendingRebuild);
            SceneView.RepaintAll();
        }

        PendingRebuild = null;
    }

    void OnGUI()
    {
        TravelAgent[] agents = FindObjectsByType<TravelAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        EditorGUILayout.LabelField(Tips.TravelingActors, EditorStyles.boldLabel);
        float actorListHeight = Mathf.Min(130f, 24f + Mathf.Max(agents.Length, 1) * 22f);
        scrollActors = EditorGUILayout.BeginScrollView(scrollActors, GUILayout.Height(actorListHeight));
        foreach (TravelAgent ta in agents)
        {
            if (ta == null)
                continue;
            EditorGUILayout.BeginHorizontal();
            GUIStyle style = focusedAgent == ta ? EditorStyles.boldLabel : EditorStyles.label;
            if (GUILayout.Button(new GUIContent(ta.gameObject.name, "Focus this TravelAgent for path authoring and preview."), style))
            {
                Selection.activeGameObject = ta.gameObject;
                EditorGUIUtility.PingObject(ta.gameObject);
                FocusTravelAgent(ta);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        if (focusedAgent == null)
        {
            EditorGUILayout.HelpBox("Select a Travel Agent from the list or scene.", MessageType.Info);
            return;
        }

        if (serializedAgent == null || serializedAgent.targetObject != focusedAgent)
            serializedAgent = new SerializedObject(focusedAgent);

        serializedAgent.Update();

        const float chromeAboveMainScroll = 56f;
        float scrollHeight = Mathf.Max(120f, position.height - actorListHeight - chromeAboveMainScroll);
        scrollMain = EditorGUILayout.BeginScrollView(scrollMain, GUILayout.Height(scrollHeight));

        EditorGUILayout.LabelField(
            new GUIContent("Authoring — " + focusedAgent.name, "Preview inputs, travel script, path kinematics, and Scene-view tools for the focused TravelAgent."),
            EditorStyles.boldLabel);

        SerializedProperty spatialProp = serializedAgent.FindProperty("spatialGeneratorSlot");
        EditorGUI.BeginChangeCheck();
        Object spatialObj = EditorGUILayout.ObjectField(
            Tips.SpatialGenerator,
            spatialProp.objectReferenceValue,
            typeof(SpatialGeneratorBase),
            true);
        if (EditorGUI.EndChangeCheck())
            spatialProp.objectReferenceValue = spatialObj;

        SerializedProperty disableRaw = serializedAgent.FindProperty("disableRawLocationWhenSpatialGeneratorAssigned");
        SerializedProperty staticSeed = serializedAgent.FindProperty("staticGeneratorSeedMode");
        EditorGUILayout.PropertyField(disableRaw);
        EditorGUILayout.PropertyField(staticSeed);

        bool lockCoords = spatialProp.objectReferenceValue != null &&
                          disableRaw.boolValue &&
                          !staticSeed.boolValue;

        EditorGUI.BeginDisabledGroup(lockCoords);
        EditorGUILayout.PropertyField(serializedAgent.FindProperty("previewStartWorld"));
        EditorGUILayout.PropertyField(serializedAgent.FindProperty("previewGoalWorld"));
        EditorGUILayout.PropertyField(serializedAgent.FindProperty("coordinateMode"));
        EditorGUI.EndDisabledGroup();

        if (lockCoords)
            EditorGUILayout.HelpBox(Tips.LockedCoordinatesHelp.tooltip, MessageType.None);

        if (staticSeed.boolValue && spatialProp.objectReferenceValue != null)
            EditorGUILayout.HelpBox(
                "Static generator seed: preview coordinates enabled for seed authoring.",
                MessageType.None);

        EditorGUILayout.PropertyField(serializedAgent.FindProperty("pathingSolverForPreview"));
        EditorGUILayout.PropertyField(serializedAgent.FindProperty("ragdollAnimationSetManager"));
        EditorGUILayout.PropertyField(serializedAgent.FindProperty("hintVehicle"));

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedAgent.FindProperty("requireAsset01"));
        EditorGUILayout.PropertyField(serializedAgent.FindProperty("requireType01"));
        if (EditorGUI.EndChangeCheck())
            ScheduleRebuild(focusedAgent);

        SerializedProperty toolList = serializedAgent.FindProperty("toolSectionsForPreview");
        SerializedProperty acroList = serializedAgent.FindProperty("acrobaticsSectionsForPreview");
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(toolList, true);
        EditorGUILayout.PropertyField(acroList, true);
        if (EditorGUI.EndChangeCheck())
            ScheduleRebuild(focusedAgent);

        EditorGUILayout.LabelField(Tips.TravelScriptRows, EditorStyles.boldLabel);
        SerializedProperty rows = serializedAgent.FindProperty("authoringRows");
        EditorGUILayout.PropertyField(rows, true);

        serializedAgent.ApplyModifiedProperties();
        serializedAgent.Update();

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(!AuthoringRowsDirty))
        {
            if (GUILayout.Button(Tips.SaveRowChanges, GUILayout.Height(22)))
                SaveAuthoringRows();
            if (GUILayout.Button(Tips.RevertRowChanges, GUILayout.Height(22)))
                RevertAuthoringRows();
        }

        if (AuthoringRowsDirty)
            GUILayout.Label(Tips.UnsavedEdits, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(Tips.AddHint))
        {
            Undo.RecordObject(focusedAgent, "Add travel authoring row");
            focusedAgent.authoringRows.Add(new TravelAuthoringRow { kind = TravelAuthoringRowKind.Hint });
            EditorUtility.SetDirty(focusedAgent);
            serializedAgent = new SerializedObject(focusedAgent);
            serializedAgent.Update();
        }

        if (GUILayout.Button(Tips.AddNode))
        {
            Undo.RecordObject(focusedAgent, "Add travel authoring row");
            focusedAgent.authoringRows.Add(new TravelAuthoringRow { kind = TravelAuthoringRowKind.Node });
            EditorUtility.SetDirty(focusedAgent);
            serializedAgent = new SerializedObject(focusedAgent);
            serializedAgent.Update();
        }

        if (GUILayout.Button(Tips.AddSpatialNode))
        {
            Undo.RecordObject(focusedAgent, "Add travel authoring row");
            focusedAgent.authoringRows.Add(new TravelAuthoringRow { kind = TravelAuthoringRowKind.SpatialNode });
            EditorUtility.SetDirty(focusedAgent);
            serializedAgent = new SerializedObject(focusedAgent);
            serializedAgent.Update();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            new GUIContent("Path kinematics", "Skier-track limits and Scene-view overlays for the cached path."),
            EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        float revLimit = EditorGUILayout.Slider(Tips.ReverseLegLimit, focusedAgent.reverseLegLimit01, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(focusedAgent, "Reverse leg limit");
            focusedAgent.reverseLegLimit01 = revLimit;
            focusedAgent.UpdatePathLengthMetricsPublic();
            EditorUtility.SetDirty(focusedAgent);
        }

        EditorGUILayout.LabelField(
            new GUIContent(
                TravelPathReverseLimits.FormatDistanceLabel(focusedAgent.ReverseBudgetMeters, focusedAgent.TotalPathLengthMeters),
                "Reverse budget meters allowed versus total cached path length."),
            EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(Tips.ResetReverseDefault))
        {
            Undo.RecordObject(focusedAgent, "Reset reverse limit");
            focusedAgent.ResetReverseLegLimitToDefault();
            EditorUtility.SetDirty(focusedAgent);
        }
        EditorGUILayout.EndHorizontal();

        focusedAgent.showVelocityTrack = EditorGUILayout.Toggle(Tips.ShowVelocityTrack, focusedAgent.showVelocityTrack);
        focusedAgent.showIkSamples = EditorGUILayout.Toggle(Tips.ShowIkSamples, focusedAgent.showIkSamples);
        focusedAgent.showReverseBudget = EditorGUILayout.Toggle(Tips.ShowReverseBudget, focusedAgent.showReverseBudget);
        focusedAgent.velocityTrackSpacingMeters = EditorGUILayout.Slider(
            Tips.TrackSpacing, focusedAgent.velocityTrackSpacingMeters, 0.5f, 10f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            new GUIContent("Preview", "Rebuild, frame, and step through the cached multi-modal plan in the Scene view."),
            EditorStyles.boldLabel);
        serializedAgent.Update();
        EditorGUILayout.PropertyField(serializedAgent.FindProperty("previewFitMode"));
        EditorGUILayout.PropertyField(serializedAgent.FindProperty("previewSegmentIndex"));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(Tips.RebuildPreview))
        {
            focusedAgent.RebuildCachedPlan();
            EditorUtility.SetDirty(focusedAgent);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button(Tips.ZoomToFit))
            ZoomToFit(focusedAgent);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(Tips.PreviousSegment))
            StepSegment(focusedAgent, -1);
        if (GUILayout.Button(Tips.NextSegment))
            StepSegment(focusedAgent, 1);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button(Tips.RefreshDiscovery))
        {
            focusedAgent.RefreshDiscoveredNodes();
            EditorUtility.SetDirty(focusedAgent);
        }

        EditorGUILayout.LabelField(Tips.Discovery, EditorStyles.miniBoldLabel);
        IReadOnlyList<TravelDiscoveredNodeInfo> nodes = focusedAgent.DiscoveredNodes;
        if (nodes == null || nodes.Count == 0)
            EditorGUILayout.HelpBox("No cached discovery. Click Refresh discovery list.", MessageType.None);
        else
        {
            foreach (TravelDiscoveredNodeInfo info in nodes)
            {
                EditorGUILayout.LabelField(
                    new GUIContent($"{info.nodeTypeName}: {info.hierarchyPath}", info.serializedSummary),
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        serializedAgent.ApplyModifiedProperties();

        EditorGUILayout.EndScrollView();
    }

    static void StepSegment(TravelAgent ta, int delta)
    {
        ta.RebuildCachedPlan();
        GenericMultiModalPathPlan plan = ta.CachedPlan;
        if (plan?.segments == null || plan.segments.Count == 0)
            return;

        int n = plan.segments.Count;
        ta.previewSegmentIndex = ((ta.previewSegmentIndex + delta) % n + n) % n;
        EditorUtility.SetDirty(ta);
        if (ta.previewFitMode == TravelPreviewFitMode.CurrentSegment)
            ZoomSegment(ta);
        else
            ZoomToFit(ta);
    }

    static void ZoomToFit(TravelAgent ta)
    {
        GenericMultiModalPathPlan plan = ta.CachedPlan;
        if (plan == null || plan.IsEmpty)
            return;

        Bounds b;
        if (ta.previewFitMode == TravelPreviewFitMode.CurrentSegment &&
            plan.segments != null &&
            ta.previewSegmentIndex >= 0 &&
            ta.previewSegmentIndex < plan.segments.Count)
        {
            MultiModalSegment seg = plan.segments[ta.previewSegmentIndex];
            b = BoundsFromWaypoints(seg?.waypoints);
        }
        else
            b = BoundsFromWaypoints(plan.FlattenWaypointsForGizmos());

        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null && b.size.sqrMagnitude > 1e-6f)
            sv.Frame(b, false);
    }

    static void ZoomSegment(TravelAgent ta)
    {
        GenericMultiModalPathPlan plan = ta.CachedPlan;
        if (plan?.segments == null || ta.previewSegmentIndex < 0 ||
            ta.previewSegmentIndex >= plan.segments.Count)
            return;
        Bounds b = BoundsFromWaypoints(plan.segments[ta.previewSegmentIndex]?.waypoints);
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null && b.size.sqrMagnitude > 1e-6f)
            sv.Frame(b, false);
    }

    static Bounds BoundsFromWaypoints(List<Vector3> pts)
    {
        if (pts == null || pts.Count == 0)
            return new Bounds(Vector3.zero, Vector3.zero);
        Bounds b = new Bounds(pts[0], Vector3.zero);
        for (int i = 1; i < pts.Count; i++)
            b.Encapsulate(pts[i]);
        b.Expand(1f);
        return b;
    }
}
