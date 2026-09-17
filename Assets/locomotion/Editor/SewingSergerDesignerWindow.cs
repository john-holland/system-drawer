#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class SewingSergerDesignerWindow : EditorWindow
{
    SewingMachineSpec _sew;
    SergerSpec _serger;
    PixelLightDesignerView _view = PixelLightDesignerView.Front;
    Bounds4SdfInclusionKind _inclusion = Bounds4SdfInclusionKind.Frame;
    Vector2 _scroll;
    Vector2 _sewSlotScroll;
    Vector2 _sergerSlotScroll;
    PixelLightGridMountGameObject _stitchMount;
    PixelLightGridMountGameObject _overlockMount;

    [MenuItem("Locomotion/Sewing + Serger Designer")]
    public static void Open()
    {
        var w = GetWindow<SewingSergerDesignerWindow>("Sewing / Serger");
        w.minSize = new Vector2(460, 560);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _sew = (SewingMachineSpec)EditorGUILayout.ObjectField(
            "Sewing machine", _sew, typeof(SewingMachineSpec), false);
        if (_sew == null && GUILayout.Button("Create SewingMachineSpec"))
        {
            var path = EditorUtility.SaveFilePanelInProject("Save Sewing Machine", "SewingMachine", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                _sew = CreateInstance<SewingMachineSpec>();
                _sew.stitchProgram = SewingStitchProgram.DefaultLockstitch();
                AssetDatabase.CreateAsset(_sew, path);
            }
        }
        if (_sew != null)
        {
            _sew.needleKind = (CutToolKind)EditorGUILayout.EnumPopup("Needle", _sew.needleKind);
            _sew.needleBarGearbox = (GearboxSpec)EditorGUILayout.ObjectField(
                "Needle-bar gearbox", _sew.needleBarGearbox, typeof(GearboxSpec), false);
            _sew.pixelLightCatalog = GearboxLathePixelLightDrawer.DrawCatalogField(
                _sew.pixelLightCatalog, "SewingPixelLight");
            _sew.frameMount = (Bounds4SdfInclusionPixelLightMount)EditorGUILayout.ObjectField(
                "Frame mount", _sew.frameMount, typeof(Bounds4SdfInclusionPixelLightMount), true);
            _sew.shellMount = (Bounds4SdfInclusionPixelLightMount)EditorGUILayout.ObjectField(
                "Shell mount", _sew.shellMount, typeof(Bounds4SdfInclusionPixelLightMount), true);
            var mount = _inclusion == Bounds4SdfInclusionKind.Frame ? _sew.frameMount : _sew.shellMount;
            GearboxLathePixelLightDrawer.DrawFrameShell(
                _sew.pixelLightCatalog, ref _view, ref _inclusion, ref mount);
            if (_inclusion == Bounds4SdfInclusionKind.Frame)
                _sew.frameMount = mount;
            else
                _sew.shellMount = mount;

            if (_sew.pixelLightCatalog != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Grid slots (light / hollow / door)", EditorStyles.boldLabel);
                if (GUILayout.Button("Ensure sewing hollows/doors"))
                {
                    _sew.pixelLightCatalog.EnsureSewingMachineSlots();
                    EditorUtility.SetDirty(_sew.pixelLightCatalog);
                }
                PixelLightGridSlotAccordionDrawer.Draw(_sew.pixelLightCatalog, ref _sewSlotScroll, null, null, 220f);
            }

            EditorGUILayout.Space();
            if (_sew.stitchProgram == null)
                _sew.stitchProgram = SewingStitchProgram.DefaultLockstitch();
            GearboxLathePixelLightDrawer.DrawSewingStitchProgram(
                _sew.pixelLightCatalog, _sew.stitchProgram, ref _stitchMount);

            EditorGUILayout.Space();
            DrawThreadGuides(_sew);
            if (GUILayout.Button("Bake sewing SDF"))
                BakeMachineSdf(_sew.frameMount, _sew.shellMount, true, _sew.pixelLightCatalog);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "magnetoIndex 0 = Frame/Shell body. Front × Shell × magnetoIndex 1 = stitch program. Hollows cut Frame/Shell; doors pair frameId + doorId + hingeLabel. Show stacks pads/skews overlapping slots by zIndex.",
            MessageType.None);

        EditorGUILayout.Space();
        _serger = (SergerSpec)EditorGUILayout.ObjectField("Serger", _serger, typeof(SergerSpec), false);
        if (_serger == null && GUILayout.Button("Create SergerSpec"))
        {
            var path = EditorUtility.SaveFilePanelInProject("Save Serger", "Serger", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                _serger = CreateInstance<SergerSpec>();
                _serger.stitchProgram = SewingStitchProgram.DefaultOverlock();
                AssetDatabase.CreateAsset(_serger, path);
            }
        }
        if (_serger != null)
        {
            _serger.needlePhase01 = EditorGUILayout.Slider("Needle phase", _serger.needlePhase01, 0f, 1f);
            _serger.looperPhase01 = EditorGUILayout.Slider("Looper phase", _serger.looperPhase01, 0f, 1f);
            _serger.differentialFeed01 = EditorGUILayout.Slider("Differential", _serger.differentialFeed01, 0f, 1f);
            _serger.threadTension01 = EditorGUILayout.Slider("Thread tension", _serger.threadTension01, 0f, 1f);
            EditorGUILayout.LabelField("Differential feed", _serger.DifferentialFeed().ToString("0.00"));
            if (_serger.IsJammed)
                EditorGUILayout.HelpBox("Serger jammed — tension over limit.", MessageType.Warning);
            _serger.ikCatalog = (TayloringIkTrainingCatalog)EditorGUILayout.ObjectField(
                "IK catalog", _serger.ikCatalog, typeof(TayloringIkTrainingCatalog), false);
            _serger.pixelLightCatalog = GearboxLathePixelLightDrawer.DrawCatalogField(
                _serger.pixelLightCatalog, "SergerPixelLight");
            _serger.frameMount = (Bounds4SdfInclusionPixelLightMount)EditorGUILayout.ObjectField(
                "Frame mount", _serger.frameMount, typeof(Bounds4SdfInclusionPixelLightMount), true);
            _serger.shellMount = (Bounds4SdfInclusionPixelLightMount)EditorGUILayout.ObjectField(
                "Shell mount", _serger.shellMount, typeof(Bounds4SdfInclusionPixelLightMount), true);
            var sergeMount = _inclusion == Bounds4SdfInclusionKind.Frame ? _serger.frameMount : _serger.shellMount;
            GearboxLathePixelLightDrawer.DrawFrameShell(
                _serger.pixelLightCatalog, ref _view, ref _inclusion, ref sergeMount);
            if (_inclusion == Bounds4SdfInclusionKind.Frame)
                _serger.frameMount = sergeMount;
            else
                _serger.shellMount = sergeMount;

            if (_serger.pixelLightCatalog != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Grid slots (light / hollow / door)", EditorStyles.boldLabel);
                if (GUILayout.Button("Ensure serger hollows/doors"))
                {
                    _serger.pixelLightCatalog.EnsureSergerSlots();
                    EditorUtility.SetDirty(_serger.pixelLightCatalog);
                }
                PixelLightGridSlotAccordionDrawer.Draw(_serger.pixelLightCatalog, ref _sergerSlotScroll, null, null, 220f);
            }

            EditorGUILayout.Space();
            if (_serger.stitchProgram == null)
                _serger.stitchProgram = SewingStitchProgram.DefaultOverlock();
            GearboxLathePixelLightDrawer.DrawSewingStitchProgram(
                _serger.pixelLightCatalog, _serger.stitchProgram, ref _overlockMount);

            if (GUILayout.Button("Bake serger SDF"))
                BakeMachineSdf(_serger.frameMount, _serger.shellMount, false, _serger.pixelLightCatalog);
        }
        EditorGUILayout.EndScrollView();
        if (GUI.changed)
        {
            if (_sew != null) EditorUtility.SetDirty(_sew);
            if (_serger != null) EditorUtility.SetDirty(_serger);
        }
    }

    static void DrawThreadGuides(SewingMachineSpec sew)
    {
        if (sew.threadGuides == null)
            sew.threadGuides = new System.Collections.Generic.List<string>();
        EditorGUILayout.LabelField("Thread guides", EditorStyles.boldLabel);
        for (int i = 0; i < sew.threadGuides.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            sew.threadGuides[i] = EditorGUILayout.TextField(sew.threadGuides[i]);
            if (GUILayout.Button("-", GUILayout.Width(22)))
            {
                sew.threadGuides.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("Add thread guide"))
            sew.threadGuides.Add("guide");
    }

    static void BakeMachineSdf(
        Bounds4SdfInclusionPixelLightMount frame,
        Bounds4SdfInclusionPixelLightMount shell,
        bool sewing,
        PixelLightMultiSlotCatalog catalog)
    {
        var dest = sewing ? SewingMachineSdfBuiltins.BuildSewing() : SewingMachineSdfBuiltins.BuildSerger();
        var mount = shell != null ? shell : frame;
        if (mount != null)
        {
            mount.composition = dest;
            mount.BakeHardwareSubtract(catalog, dest, false);
            EditorUtility.SetDirty(mount);
        }
    }
}
#endif
