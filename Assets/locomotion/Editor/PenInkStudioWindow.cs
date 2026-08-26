using Locomotion.Open;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Scaffolds ink profile, quill nib, canvas, drying, drawing target, cap open/close, and IK catalog.
/// </summary>
public sealed class PenInkStudioWindow : EditorWindow
{
    string _outputFolder = "Assets/locomotion/painting/Baked/Ink";
    InkMaterialProfile _ink;
    QuillNibDefinition _nib;
    PaintCanvasLayerStack _stack;
    PaintingIkTrainingCatalog _ik;
    OpenCloseTopologyAsset _capTopology;
    GameObject _canvasHost;
    GameObject _instrumentHost;
    PenInkDrawingTarget _drawing;
    bool _curvedDecal;
    string _drawingText = "ink";
    bool _understandingConfirmed;
    PhysicsIKTrainingRunAsset _ikRun;
    PhysicsCardSolver _ikSolver;
    bool _feedRidgeForceToNib;

    [MenuItem("Window/System Drawer/Pen and Ink Studio")]
    public static void Open() => GetWindow<PenInkStudioWindow>("Pen and Ink Studio");

    void OnGUI()
    {
        _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
        _ink = (InkMaterialProfile)EditorGUILayout.ObjectField("Ink Profile", _ink, typeof(InkMaterialProfile), false);
        _nib = (QuillNibDefinition)EditorGUILayout.ObjectField("Quill Nib", _nib, typeof(QuillNibDefinition), false);
        _stack = (PaintCanvasLayerStack)EditorGUILayout.ObjectField("Canvas Stack", _stack, typeof(PaintCanvasLayerStack), false);
        _ik = (PaintingIkTrainingCatalog)EditorGUILayout.ObjectField("IK Catalog", _ik, typeof(PaintingIkTrainingCatalog), false);
        _capTopology = (OpenCloseTopologyAsset)EditorGUILayout.ObjectField("Cap Topology", _capTopology, typeof(OpenCloseTopologyAsset), false);
        _canvasHost = (GameObject)EditorGUILayout.ObjectField("Canvas Host", _canvasHost, typeof(GameObject), true);
        _instrumentHost = (GameObject)EditorGUILayout.ObjectField("Instrument Host", _instrumentHost, typeof(GameObject), true);
        _curvedDecal = EditorGUILayout.Toggle("Curved Decal Canvas", _curvedDecal);
        _feedRidgeForceToNib = EditorGUILayout.Toggle("Hydro ridge force → nib", _feedRidgeForceToNib);
        if (_canvasHost != null)
        {
            var liveHydro = _canvasHost.GetComponent<PaintCanvasHydroSolver>();
            if (liveHydro != null)
            {
                liveHydro.feedRidgeForceToNib = _feedRidgeForceToNib;
                if (_instrumentHost != null)
                {
                    var livePen = _instrumentHost.GetComponent<PenInkInstrument>();
                    if (livePen != null)
                        liveHydro.nibFeedbackTarget = livePen;
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Drawing target", EditorStyles.boldLabel);
        _drawing = (PenInkDrawingTarget)EditorGUILayout.ObjectField("Drawing Target", _drawing, typeof(PenInkDrawingTarget), true);
        if (_drawing != null)
            _drawing.sourceKind = (PenInkDrawingTarget.SourceKind)EditorGUILayout.EnumPopup("Source", _drawing.sourceKind);
        if (_drawing != null && _drawing.sourceKind == PenInkDrawingTarget.SourceKind.Image)
        {
            _drawing.sourceImage = (Texture2D)EditorGUILayout.ObjectField(
                "Image", _drawing.sourceImage, typeof(Texture2D), false);
            _drawing.enableOcrImage = EditorGUILayout.Toggle("OCR image", _drawing.enableOcrImage);
        }
        else
            _drawingText = EditorGUILayout.TextField("Text", _drawingText);
        _understandingConfirmed = EditorGUILayout.Toggle("Understanding confirmed", _understandingConfirmed);

        EditorGUILayout.Space();
        if (GUILayout.Button("Create / Assign Ink Defaults"))
            BakeAll();
        if (GUILayout.Button("Bake Cap Open/Close Topology"))
            BakeCapTopology();
        if (GUILayout.Button("Compile Drawing Target"))
            CompileDrawing();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("IK train", EditorStyles.boldLabel);
        _ikRun = (PhysicsIKTrainingRunAsset)EditorGUILayout.ObjectField("IK Run", _ikRun, typeof(PhysicsIKTrainingRunAsset), false);
        _ikSolver = (PhysicsCardSolver)EditorGUILayout.ObjectField("Solver", _ikSolver, typeof(PhysicsCardSolver), true);
        using (new EditorGUI.DisabledScope(_drawing != null && !_drawing.CanTrain && !_understandingConfirmed))
        {
            if (GUILayout.Button("Open IK Train From Current Pose"))
                TrainIk();
        }
        if (_drawing != null && !_drawing.CanTrain)
            EditorGUILayout.HelpBox("Confirm understanding and compile code points before IK train.", MessageType.Info);
    }

    void BakeAll()
    {
        EnsureFolder(_outputFolder);

        if (_ink == null)
        {
            _ink = InkMaterialProfile.CreateInkDefaults();
            AssetDatabase.CreateAsset(_ink, $"{_outputFolder}/InkMaterialProfile.asset");
        }

        if (_nib == null)
        {
            _nib = QuillNibDefinition.CreateDefaults();
            AssetDatabase.CreateAsset(_nib, $"{_outputFolder}/QuillNib.asset");
        }

        if (_stack == null)
        {
            _stack = CreateInstance<PaintCanvasLayerStack>();
            AssetDatabase.CreateAsset(_stack, $"{_outputFolder}/InkCanvasLayerStack.asset");
        }
        _stack.EnsureBaseLayer();
        _stack.ApplyInkProfile(_ink);
        if (_stack.layers.Count > 0 && _stack.layers[0].composition != null && !AssetDatabase.Contains(_stack.layers[0].composition))
            AssetDatabase.AddObjectToAsset(_stack.layers[0].composition, _stack);
        EditorUtility.SetDirty(_stack);

        if (_ik == null)
        {
            _ik = CreateInstance<PaintingIkTrainingCatalog>();
            AssetDatabase.CreateAsset(_ik, $"{_outputFolder}/PaintingIkTrainingCatalog.asset");
        }
        _ik.EnsureDefaults();
        EditorUtility.SetDirty(_ik);

        if (_canvasHost == null)
        {
            _canvasHost = new GameObject(_curvedDecal ? "InkCanvasCurved" : "InkCanvas");
            Undo.RegisterCreatedObjectUndo(_canvasHost, "Create ink canvas");
        }
        var canvas = _canvasHost.GetComponent<PaintCanvas>() ?? _canvasHost.AddComponent<PaintCanvas>();
        canvas.inkProfile = _ink;
        canvas.layerStack = _stack;
        canvas.surfaceKind = _curvedDecal ? PaintCanvas.SurfaceKind.CurvedDecal : PaintCanvas.SurfaceKind.Plane;
        canvas.ApplyInkProfile();
        canvas.EnsureHydro();
        var hydro = canvas.Hydro;
        hydro.feedRidgeForceToNib = _feedRidgeForceToNib;
        if (_curvedDecal)
        {
            var curved = _canvasHost.GetComponent<PaintCanvasCurvedDecal>() ?? _canvasHost.AddComponent<PaintCanvasCurvedDecal>();
            curved.RebuildMesh();
        }
        var dryer = _canvasHost.GetComponent<InkDryingLayerDriver>() ?? _canvasHost.AddComponent<InkDryingLayerDriver>();
        dryer.ink = _ink;
        dryer.canvas = canvas;
        var narrative = _canvasHost.GetComponent<InkDryingNarrativeBridge>() ?? _canvasHost.AddComponent<InkDryingNarrativeBridge>();
        dryer.narrative = narrative;

        if (_instrumentHost == null)
        {
            _instrumentHost = new GameObject("PenInkInstrument");
            Undo.RegisterCreatedObjectUndo(_instrumentHost, "Create pen ink instrument");
        }
        var pen = _instrumentHost.GetComponent<PenInkInstrument>() ?? _instrumentHost.AddComponent<PenInkInstrument>();
        pen.ink = _ink;
        pen.nib = _nib;
        if (pen.tip == null)
            pen.tip = _instrumentHost.transform;
        canvas.Hydro.nibFeedbackTarget = pen;
        canvas.Hydro.feedRidgeForceToNib = _feedRidgeForceToNib;
        var lemmas = _instrumentHost.GetComponent<PenInkLemmaResolver>() ?? _instrumentHost.AddComponent<PenInkLemmaResolver>();
        lemmas.instrument = pen;
        lemmas.canvas = canvas;
        var goals = _instrumentHost.GetComponent<PenInkIkGoals>() ?? _instrumentHost.AddComponent<PenInkIkGoals>();
        goals.instrument = pen;
        goals.canvas = canvas;

        if (_drawing == null)
            _drawing = _canvasHost.GetComponent<PenInkDrawingTarget>() ?? _canvasHost.AddComponent<PenInkDrawingTarget>();
        _drawing.canvas = canvas;
        _drawing.nibTip = pen.tip;
        _drawing.sourceText = _drawingText;
        _drawing.understandingConfirmed = _understandingConfirmed;

        var quill = PaintBrushCatalog.CreateBuiltin(PaintBrushDefinition.BrushKind.Quill);
        AssetDatabase.CreateAsset(quill, AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/Brush_Quill.asset"));
        var nibBrush = PaintBrushCatalog.CreateBuiltin(PaintBrushDefinition.BrushKind.Nib);
        AssetDatabase.CreateAsset(nibBrush, AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/Brush_Nib.asset"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Pen and Ink Studio", $"Assets ready under {_outputFolder}", "OK");
    }

    void BakeCapTopology()
    {
        EnsureFolder(_outputFolder);
        if (_capTopology == null)
        {
            _capTopology = CreateInstance<OpenCloseTopologyAsset>();
            AssetDatabase.CreateAsset(_capTopology, $"{_outputFolder}/PenCapOpenClose.asset");
        }
        var root = _capTopology.Root;
        root.nodeId = "pen-cap";
        root.enabledInGameplay = true;
        root.autoCloseBt = AutoCloseBtMode.OnStopExit;
        if (_instrumentHost != null)
            root.target = _instrumentHost;
        EditorUtility.SetDirty(_capTopology);
        if (_instrumentHost != null)
        {
            OpenCloseTopologyCompiler.BakeToScene(_capTopology, _instrumentHost);
            OpenCloseTopologyCompiler.CompilePreview(_capTopology);
        }
        AssetDatabase.SaveAssets();
    }

    void CompileDrawing()
    {
        if (_drawing == null && _canvasHost != null)
            _drawing = _canvasHost.GetComponent<PenInkDrawingTarget>() ?? _canvasHost.AddComponent<PenInkDrawingTarget>();
        if (_drawing == null)
        {
            EditorUtility.DisplayDialog("Pen and Ink Studio", "Assign a canvas host or drawing target first.", "OK");
            return;
        }
        if (_drawing.sourceKind != PenInkDrawingTarget.SourceKind.Image)
            _drawing.sourceText = _drawingText;
        _drawing.understandingConfirmed = _understandingConfirmed;
        _drawing.Compile();
        if (_drawing.strokeSdf != null && !AssetDatabase.Contains(_drawing.strokeSdf))
        {
            EnsureFolder(_outputFolder);
            AssetDatabase.CreateAsset(_drawing.strokeSdf, AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/DrawingStrokeSdf.asset"));
        }
        EditorUtility.SetDirty(_drawing);
        AssetDatabase.SaveAssets();
    }

    void TrainIk()
    {
        if (_drawing != null)
        {
            _drawing.understandingConfirmed = _understandingConfirmed;
            if (!_drawing.CanTrain)
            {
                EditorUtility.DisplayDialog("Pen and Ink Studio", "Understanding must be confirmed and code points compiled before IK train.", "OK");
                return;
            }
        }
        PhysicsIKTrainingWindow.OpenAndTrainFromCurrentPose(_ikRun, _ikSolver);
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string[] parts = folder.Replace("\\", "/").Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
