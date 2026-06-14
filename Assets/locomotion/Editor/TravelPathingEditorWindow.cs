using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Authoring and preview for multi-modal travel (typed spatial generator requires BedogaGenerator assembly reference).
/// </summary>
public class TravelPathingEditorWindow : EditorWindow
{
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
        GetWindow<TravelPathingEditorWindow>("Travel Pathing");
    }

    public static void Open(TravelAgent agent)
    {
        TravelPathingEditorWindow w = GetWindow<TravelPathingEditorWindow>("Travel Pathing");
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

        EditorGUILayout.LabelField("Traveling actors", EditorStyles.boldLabel);
        float actorListHeight = Mathf.Min(130f, 24f + Mathf.Max(agents.Length, 1) * 22f);
        scrollActors = EditorGUILayout.BeginScrollView(scrollActors, GUILayout.Height(actorListHeight));
        foreach (TravelAgent ta in agents)
        {
            if (ta == null)
                continue;
            EditorGUILayout.BeginHorizontal();
            GUIStyle style = focusedAgent == ta ? EditorStyles.boldLabel : EditorStyles.label;
            if (GUILayout.Button(ta.gameObject.name, style))
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

        EditorGUILayout.LabelField("Authoring — " + focusedAgent.name, EditorStyles.boldLabel);

        SerializedProperty spatialProp = serializedAgent.FindProperty("spatialGeneratorSlot");
        EditorGUI.BeginChangeCheck();
        Object spatialObj = EditorGUILayout.ObjectField(
            "Spatial Generator",
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

        if (staticSeed.boolValue && spatialProp.objectReferenceValue != null)
            EditorGUILayout.HelpBox("Static generator seed: preview coordinates enabled for seed authoring.", MessageType.None);

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

        EditorGUILayout.LabelField("Travel script (authoring rows)", EditorStyles.boldLabel);
        SerializedProperty rows = serializedAgent.FindProperty("authoringRows");
        EditorGUILayout.PropertyField(rows, true);

        serializedAgent.ApplyModifiedProperties();
        serializedAgent.Update();

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(!AuthoringRowsDirty))
        {
            if (GUILayout.Button("Save row changes", GUILayout.Height(22)))
                SaveAuthoringRows();
            if (GUILayout.Button("Revert row changes", GUILayout.Height(22)))
                RevertAuthoringRows();
        }

        if (AuthoringRowsDirty)
            GUILayout.Label("(unsaved row edits)", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Hint"))
        {
            Undo.RecordObject(focusedAgent, "Add travel authoring row");
            focusedAgent.authoringRows.Add(new TravelAuthoringRow { kind = TravelAuthoringRowKind.Hint });
            EditorUtility.SetDirty(focusedAgent);
            serializedAgent = new SerializedObject(focusedAgent);
            serializedAgent.Update();
        }

        if (GUILayout.Button("Add Node"))
        {
            Undo.RecordObject(focusedAgent, "Add travel authoring row");
            focusedAgent.authoringRows.Add(new TravelAuthoringRow { kind = TravelAuthoringRowKind.Node });
            EditorUtility.SetDirty(focusedAgent);
            serializedAgent = new SerializedObject(focusedAgent);
            serializedAgent.Update();
        }

        if (GUILayout.Button("Add Spatial Node"))
        {
            Undo.RecordObject(focusedAgent, "Add travel authoring row");
            focusedAgent.authoringRows.Add(new TravelAuthoringRow { kind = TravelAuthoringRowKind.SpatialNode });
            EditorUtility.SetDirty(focusedAgent);
            serializedAgent = new SerializedObject(focusedAgent);
            serializedAgent.Update();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Path kinematics", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        float revLimit = EditorGUILayout.Slider("Reverse leg limit", focusedAgent.reverseLegLimit01, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(focusedAgent, "Reverse leg limit");
            focusedAgent.reverseLegLimit01 = revLimit;
            focusedAgent.UpdatePathLengthMetricsPublic();
            EditorUtility.SetDirty(focusedAgent);
        }

        EditorGUILayout.LabelField(
            TravelPathReverseLimits.FormatDistanceLabel(focusedAgent.ReverseBudgetMeters, focusedAgent.TotalPathLengthMeters),
            EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset reverse to default"))
        {
            Undo.RecordObject(focusedAgent, "Reset reverse limit");
            focusedAgent.ResetReverseLegLimitToDefault();
            EditorUtility.SetDirty(focusedAgent);
        }
        EditorGUILayout.EndHorizontal();

        focusedAgent.showVelocityTrack = EditorGUILayout.Toggle("Show velocity track", focusedAgent.showVelocityTrack);
        focusedAgent.showIkSamples = EditorGUILayout.Toggle("Show IK samples", focusedAgent.showIkSamples);
        focusedAgent.showReverseBudget = EditorGUILayout.Toggle("Show reverse budget", focusedAgent.showReverseBudget);
        focusedAgent.velocityTrackSpacingMeters = EditorGUILayout.Slider(
            "Track spacing (m)", focusedAgent.velocityTrackSpacingMeters, 0.5f, 10f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        serializedAgent.Update();
        EditorGUILayout.PropertyField(serializedAgent.FindProperty("previewFitMode"));
        EditorGUILayout.PropertyField(serializedAgent.FindProperty("previewSegmentIndex"));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Rebuild preview"))
        {
            focusedAgent.RebuildCachedPlan();
            EditorUtility.SetDirty(focusedAgent);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Zoom to fit"))
            ZoomToFit(focusedAgent);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Previous segment"))
            StepSegment(focusedAgent, -1);
        if (GUILayout.Button("Next segment"))
            StepSegment(focusedAgent, 1);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Refresh discovery list"))
        {
            focusedAgent.RefreshDiscoveredNodes();
            EditorUtility.SetDirty(focusedAgent);
        }

        EditorGUILayout.LabelField("Discovery", EditorStyles.miniBoldLabel);
        IReadOnlyList<TravelDiscoveredNodeInfo> nodes = focusedAgent.DiscoveredNodes;
        if (nodes == null || nodes.Count == 0)
            EditorGUILayout.HelpBox("No cached discovery. Click Refresh discovery list.", MessageType.None);
        else
        {
            foreach (TravelDiscoveredNodeInfo info in nodes)
                EditorGUILayout.LabelField($"{info.nodeTypeName}: {info.hierarchyPath}", EditorStyles.wordWrappedMiniLabel);
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
