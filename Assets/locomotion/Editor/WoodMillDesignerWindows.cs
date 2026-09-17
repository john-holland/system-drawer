#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class WoodTravelAgentWindow : EditorWindow
{
    WoodTravelAgent _agent;
    Vector2 _scroll;

    [MenuItem("Locomotion/Wood Travel Agent")]
    public static void Open()
    {
        var w = GetWindow<WoodTravelAgentWindow>("Wood TA");
        w.minSize = new Vector2(420, 460);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _agent = (WoodTravelAgent)EditorGUILayout.ObjectField("Agent", _agent, typeof(WoodTravelAgent), true);
        if (_agent == null)
        {
            EditorGUILayout.HelpBox("Assign a WoodTravelAgent.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }
        if (_agent.steps == null || _agent.steps.Count == 0)
            _agent.steps = WoodTravelAgent.DefaultPipeline();
        else
            _agent.EnsurePipeline();
        EditorGUILayout.HelpBox(
            "receive → debark / lathe → plank cut → section → convey → grade → convey → pile → bind → ship",
            MessageType.None);
        _agent.selectedStepIndex = EditorGUILayout.IntSlider(
            "Step", _agent.selectedStepIndex, 0, Mathf.Max(0, _agent.steps.Count - 1));
        var step = _agent.SelectedStep;
        if (step != null)
            EditorGUILayout.LabelField("Kind", step.kind.ToString());
        _agent.deliveryMechanism = EditorGUILayout.TextField("Delivery", _agent.deliveryMechanism);
        _agent.lathe = (LatheSpec)EditorGUILayout.ObjectField("Lathe", _agent.lathe, typeof(LatheSpec), false);
        _agent.pile = (WoodPileSpec)EditorGUILayout.ObjectField("Pile", _agent.pile, typeof(WoodPileSpec), false);
        if (step != null && (step.kind == WoodMillStepKind.PlankCut || step.kind == WoodMillStepKind.Section))
        {
            EditorGUILayout.HelpBox(
                "Log plank cut uses the Lathe PixelLight mill-kerf grid (odd kerfs + center line).",
                MessageType.Info);
            if (_agent.lathe != null)
                EditorGUILayout.LabelField("Lathe kerfs / planks",
                    MillKerfCuts.OddCutCount(_agent.lathe.millCutCount) + " / " +
                    MillKerfCuts.PlankCount(_agent.lathe.millCutCount));
            if (GUILayout.Button("Apply lathe PixelLight kerfs → plank/section"))
                _agent.ApplyLatheKerfsToPlankSteps();
            if (GUILayout.Button("Open Lathe Designer (kerf grid)"))
                LatheDesignerWindow.Open();
            if (step.kerfCount > 0)
                EditorGUILayout.LabelField("Step kerfs / planks / sections",
                    step.kerfCount + " / " + step.plankCount + " / " + step.sectionCount);
        }
        Rect diamond = GUILayoutUtility.GetRect(280, 280);
        PowerDiamondDrawer.DrawOverlay(
            diamond,
            WoodTravelAgent.DiamondAxes,
            _agent.BlueOptimal01(),
            _agent.RedLimit01(),
            _agent.DashedWhiteActive01(),
            _agent.ThreatHalo01());
        if (_agent.OverLimit())
            EditorGUILayout.LabelField("RED — over limit / threat");
        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_agent);
    }
}

public sealed class StationConveyerTravelAgentWindow : EditorWindow
{
    StationConveyerTravelAgent _agent;
    Vector2 _scroll;

    [MenuItem("Locomotion/Station Conveyer Travel Agent")]
    public static void Open()
    {
        var w = GetWindow<StationConveyerTravelAgentWindow>("Conveyer TA");
        w.minSize = new Vector2(420, 400);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _agent = (StationConveyerTravelAgent)EditorGUILayout.ObjectField(
            "Agent", _agent, typeof(StationConveyerTravelAgent), true);
        if (_agent == null)
        {
            EditorGUILayout.HelpBox("Assign a StationConveyerTravelAgent.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }
        if (_agent.steps == null || _agent.steps.Count == 0)
            _agent.steps = StationConveyerTravelAgent.DefaultPipeline();
        _agent.selectedStepIndex = EditorGUILayout.IntSlider(
            "Step", _agent.selectedStepIndex, 0, Mathf.Max(0, _agent.steps.Count - 1));
        _agent.conveyorAnchor = (Transform)EditorGUILayout.ObjectField(
            "Conveyor anchor", _agent.conveyorAnchor, typeof(Transform), true);
        _agent.beltRope = (RopeSystem)EditorGUILayout.ObjectField(
            "Belt rope", _agent.beltRope, typeof(RopeSystem), true);
        if (GUILayout.Button("Clamp size/weight"))
            _agent.ClampSizeWeight();
        var size = _agent.SelectedStep != null
            ? new[] { _agent.SelectedStep.size01, _agent.SelectedStep.weight01, 0.5f, 0.5f }
            : new[] { 0.4f, 0.4f, 0.5f, 0.5f };
        var opt = _agent.SelectedStep != null
            ? new[] { _agent.SelectedStep.optimalSize01, _agent.SelectedStep.optimalWeight01, 0.5f, 0.5f }
            : new[] { 0.35f, 0.35f, 0.5f, 0.5f };
        var lim = _agent.SelectedStep != null
            ? new[] { _agent.SelectedStep.limitSize01, _agent.SelectedStep.limitWeight01, 1f, 1f }
            : new[] { 0.85f, 0.85f, 1f, 1f };
        Rect diamond = GUILayoutUtility.GetRect(280, 280);
        PowerDiamondDrawer.DrawOverlay(
            diamond,
            new[] { "Size", "Weight", "Rate", "Load" },
            opt, lim, size, 0f);
        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_agent);
    }
}

public sealed class LatheDesignerWindow : EditorWindow
{
    LatheSpec _spec;
    PixelLightDesignerView _view = PixelLightDesignerView.Front;
    Bounds4SdfInclusionKind _inclusion = Bounds4SdfInclusionKind.Frame;
    Vector2 _scroll;
    Vector2 _slotScroll;

    [MenuItem("Locomotion/Lathe Designer")]
    public static void Open()
    {
        var w = GetWindow<LatheDesignerWindow>("Lathe");
        w.minSize = new Vector2(480, 640);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _spec = (LatheSpec)EditorGUILayout.ObjectField("Lathe", _spec, typeof(LatheSpec), false);
        if (_spec == null)
        {
            if (GUILayout.Button("Create LatheSpec"))
            {
                var path = EditorUtility.SaveFilePanelInProject("Save Lathe", "Lathe", "asset", "");
                if (!string.IsNullOrEmpty(path))
                {
                    var s = CreateInstance<LatheSpec>();
                    AssetDatabase.CreateAsset(s, path);
                    _spec = s;
                }
            }
            EditorGUILayout.EndScrollView();
            return;
        }
        _spec.bedwaysLengthM = EditorGUILayout.FloatField("Bedways", _spec.bedwaysLengthM);
        _spec.carriageM = EditorGUILayout.Slider("Carriage", _spec.carriageM, 0f, Mathf.Max(0.01f, _spec.bedwaysLengthM));
        _spec.saddleM = EditorGUILayout.FloatField("Saddle", _spec.saddleM);
        _spec.crossSlideM = EditorGUILayout.FloatField("Cross-slide", _spec.crossSlideM);
        _spec.compoundRestDeg = EditorGUILayout.FloatField("Compound rest deg", _spec.compoundRestDeg);
        _spec.toolPostM = EditorGUILayout.FloatField("Tool post", _spec.toolPostM);
        _spec.apronLemma01 = EditorGUILayout.Slider("Apron lemma", _spec.apronLemma01, 0f, 1f);
        _spec.halfNutEngaged = EditorGUILayout.Toggle("Half-nut", _spec.halfNutEngaged);
        _spec.leadScrewPitchMm = EditorGUILayout.FloatField("Lead screw pitch mm", _spec.leadScrewPitchMm);
        _spec.headstockRpm = EditorGUILayout.FloatField("Headstock RPM", _spec.headstockRpm);
        _spec.tailstockM = EditorGUILayout.FloatField("Tailstock", _spec.tailstockM);
        _spec.knurl01 = EditorGUILayout.Slider("Knurl", _spec.knurl01, 0f, 1f);
        _spec.headstockGearbox = (GearboxSpec)EditorGUILayout.ObjectField(
            "Headstock gearbox", _spec.headstockGearbox, typeof(GearboxSpec), false);
        _spec.quickChangeGearbox = (GearboxSpec)EditorGUILayout.ObjectField(
            "Quick-change gearbox", _spec.quickChangeGearbox, typeof(GearboxSpec), false);

        EditorGUILayout.Space();
        _spec.pixelLightCatalog = GearboxLathePixelLightDrawer.DrawCatalogField(
            _spec.pixelLightCatalog, "LathePixelLight");
        if (_spec.headstockGearbox != null && _spec.pixelLightCatalog == null)
            _spec.pixelLightCatalog = _spec.headstockGearbox.catalog;
        _spec.frameMount = (Bounds4SdfInclusionPixelLightMount)EditorGUILayout.ObjectField(
            "Frame mount (bed/ways)", _spec.frameMount, typeof(Bounds4SdfInclusionPixelLightMount), true);
        _spec.shellMount = (Bounds4SdfInclusionPixelLightMount)EditorGUILayout.ObjectField(
            "Shell mount (cover)", _spec.shellMount, typeof(Bounds4SdfInclusionPixelLightMount), true);
        var activeMount = _inclusion == Bounds4SdfInclusionKind.Frame ? _spec.frameMount : _spec.shellMount;
        GearboxLathePixelLightDrawer.DrawFrameShell(
            _spec.pixelLightCatalog, ref _view, ref _inclusion, ref activeMount);
        if (_inclusion == Bounds4SdfInclusionKind.Frame)
            _spec.frameMount = activeMount;
        else
            _spec.shellMount = activeMount;

        if (_spec.pixelLightCatalog != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Frame / shell hollows and doors", EditorStyles.boldLabel);
            if (GUILayout.Button("Ensure lathe hollows/doors"))
            {
                _spec.pixelLightCatalog.EnsureLatheSlots();
                EditorUtility.SetDirty(_spec.pixelLightCatalog);
            }
            PixelLightGridSlotAccordionDrawer.Draw(_spec.pixelLightCatalog, ref _slotScroll, null, null, 220f);
        }

        EditorGUILayout.Space();
        _spec.millKerfMount = (PixelLightGridMountGameObject)EditorGUILayout.ObjectField(
            "Mill kerf mount", _spec.millKerfMount, typeof(PixelLightGridMountGameObject), true);
        EditorGUILayout.HelpBox(
            "Mill-kerf PixelLight is the log plank-cut grid (odd cuts, center line, dual-crescent). Wood TA step PlankCut / Section reads millCutCount.",
            MessageType.None);
        GearboxLathePixelLightDrawer.DrawMillKerf(
            _spec.pixelLightCatalog, ref _spec.millKerfMount, ref _spec.millCutCount);
        EditorGUILayout.LabelField("Planks from kerfs", MillKerfCuts.PlankCount(_spec.millCutCount).ToString());

        if (GUILayout.Button("Open Gearbox Designer"))
            GearboxDesignerWindow.Open();
        EditorGUILayout.EndScrollView();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(_spec);
            if (_spec.pixelLightCatalog != null)
                EditorUtility.SetDirty(_spec.pixelLightCatalog);
        }
    }
}

public sealed class WoodPileDesignerWindow : EditorWindow
{
    WoodPileSpec _spec;
    Vector2 _scroll;
    int _depth;

    [MenuItem("Locomotion/Wood Pile Designer")]
    public static void Open()
    {
        var w = GetWindow<WoodPileDesignerWindow>("Wood Pile");
        w.minSize = new Vector2(440, 480);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _spec = (WoodPileSpec)EditorGUILayout.ObjectField("Pile", _spec, typeof(WoodPileSpec), false);
        if (_spec == null)
        {
            if (GUILayout.Button("Create WoodPileSpec"))
            {
                var path = EditorUtility.SaveFilePanelInProject("Save Wood Pile", "WoodPile", "asset", "");
                if (!string.IsNullOrEmpty(path))
                {
                    var s = CreateInstance<WoodPileSpec>();
                    AssetDatabase.CreateAsset(s, path);
                    _spec = s;
                }
            }
            EditorGUILayout.EndScrollView();
            return;
        }
        _spec.depthLayers = EditorGUILayout.IntField("Depth layers", _spec.depthLayers);
        _spec.moveLogIkModeId = EditorGUILayout.TextField("Move Log IK", _spec.moveLogIkModeId);
        _spec.brushMount = (PixelLightGridMountGameObject)EditorGUILayout.ObjectField(
            "PixelLight brush", _spec.brushMount, typeof(PixelLightGridMountGameObject), true);
        if (_spec.brushMount != null)
        {
            PixelLightRadialBrushDrawer.DrawOnMount(_spec.brushMount);
            GearboxLathePixelLightDrawer.DrawMountPatternGrid(_spec.brushMount);
        }
        _depth = EditorGUILayout.IntField("Depth index", _depth);
        if (GUILayout.Button("Add quadtree bucket"))
            _spec.AddQuadtreeBucket(_depth, Vector3.zero);
        EditorGUILayout.LabelField("Slots", _spec.slots.Count.ToString());
        EditorGUILayout.LabelField("At depth", _spec.SlotCountAtDepth(_depth).ToString());
        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_spec);
    }
}

public sealed class SpaceCannalTravelAgentWindow : EditorWindow
{
    SpaceCannalTravelAgent _agent;
    Vector2 _scroll;

    [MenuItem("Locomotion/Space Cannal Travel Agent")]
    public static void Open()
    {
        var w = GetWindow<SpaceCannalTravelAgentWindow>("Space Cannal TA");
        w.minSize = new Vector2(420, 440);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _agent = (SpaceCannalTravelAgent)EditorGUILayout.ObjectField(
            "Agent", _agent, typeof(SpaceCannalTravelAgent), true);
        if (_agent == null)
        {
            EditorGUILayout.HelpBox("Assign a SpaceCannalTravelAgent.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }
        if (_agent.steps == null || _agent.steps.Count == 0)
            _agent.steps = SpaceCannalTravelAgent.DefaultPipeline();
        _agent.selectedStepIndex = EditorGUILayout.IntSlider(
            "Step", _agent.selectedStepIndex, 0, Mathf.Max(0, _agent.steps.Count - 1));
        EditorGUILayout.LabelField("GER gate", _agent.GateDenied() ? "DENIED (dark bus)" : "live");
        Rect diamond = GUILayoutUtility.GetRect(280, 280);
        PowerDiamondDrawer.DrawOverlay(
            diamond,
            SpaceCannalTravelAgent.DiamondAxes,
            _agent.BlueOptimal01(),
            _agent.RedLimit01(),
            _agent.DashedWhiteActive01(),
            _agent.ThreatHalo01());
        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_agent);
    }
}
#endif
