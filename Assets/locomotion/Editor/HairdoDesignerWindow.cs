#if UNITY_EDITOR
using System.IO;
using SdfMax;
using SdfMax.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Hairdo designer: precedence + checkbox + weight blend, power diamond, obscene SDF sexpr.
/// </summary>
public sealed class HairdoDesignerWindow : EditorWindow
{
    GameObject _actorRoot;
    HairPlumeConfig _config;
    HairPlumePhysicsDriver _driver;
    Transform _scalpRoot;
    SdfMaxCompositionAsset _composition;

    HairdoBlend _blend = HairdoBlend.CreateDefault();
    HairdoParams _fineTune = new HairdoParams();
    bool _fineTuneOverride;
    bool _fineTuneFoldout;
    bool _applyBlendOnChange;

    string _sexpr = "";
    string _parseError;
    Vector2 _scroll;
    Vector2 _sexprScroll;
    string _outputFolder = "Assets/locomotion/hair/Baked";

    const float DiamondSize = 200f;

    [MenuItem("Window/System Drawer/Hairdo Designer")]
    public static void Open()
    {
        var w = GetWindow<HairdoDesignerWindow>("Hairdo Designer");
        w.minSize = new Vector2(640, 520);
    }

    void OnEnable()
    {
        _blend ??= HairdoBlend.CreateDefault();
        _blend.EnsureSlots();
        RefreshFineTuneFromBlend();
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawActorSection();
        EditorGUILayout.Space(6);
        DrawConfigSection();
        EditorGUILayout.Space(8);
        DrawPowerDiamondSection();
        EditorGUILayout.Space(6);
        DrawFineTune();
        EditorGUILayout.Space(8);
        DrawObsceneSdfSection();
        EditorGUILayout.Space(8);
        DrawApplySection();

        EditorGUILayout.EndScrollView();
    }

    void DrawActorSection()
    {
        EditorGUILayout.LabelField("Actor", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _actorRoot = (GameObject)EditorGUILayout.ObjectField("Actor Root", _actorRoot, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck() && _actorRoot != null)
            TrySeedFromActorSelection(false);

        if (_actorRoot == null && Selection.activeGameObject != null)
        {
            EditorGUILayout.HelpBox("No actor assigned — selection can seed Default From Actor.", MessageType.Info);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Default Everything From Actor"))
                DefaultEverythingFromActor();
            if (GUILayout.Button("Use Selection As Actor", GUILayout.Width(160)))
            {
                if (Selection.activeGameObject != null)
                {
                    _actorRoot = Selection.activeGameObject;
                    TrySeedFromActorSelection(false);
                }
            }
        }

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField("Physics Driver", _driver, typeof(HairPlumePhysicsDriver), true);
        EditorGUILayout.ObjectField("Scalp Root", _scalpRoot, typeof(Transform), true);
        EditorGUI.EndDisabledGroup();
    }

    void DrawConfigSection()
    {
        EditorGUILayout.LabelField("Config", EditorStyles.boldLabel);
        _config = (HairPlumeConfig)EditorGUILayout.ObjectField("Config", _config, typeof(HairPlumeConfig), false);
        _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Config Asset"))
                CreateConfig();
            using (new EditorGUI.DisabledScope(_config == null))
            {
                if (GUILayout.Button("Read Params From Config"))
                {
                    _fineTune = new HairdoParams();
                    _fineTune.ReadFrom(_config);
                    _fineTuneOverride = true;
                }
            }
        }
    }

    void DrawPowerDiamondSection()
    {
        EditorGUILayout.LabelField("Power diamond", EditorStyles.boldLabel);
        _blend.EnsureSlots();

        bool hasBlend = _blend.TryEvaluate(out var blended, out float front, out float side, out float back, out float length);
        if (hasBlend && !_fineTuneOverride)
            _fineTune = blended;

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                var order = _blend.SortedSlotIndices();
                EditorGUI.BeginChangeCheck();
                for (int oi = 0; oi < order.Count; oi++)
                {
                    int si = order[oi];
                    var slot = _blend.slots[si];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        slot.precedence = EditorGUILayout.IntField(slot.precedence, GUILayout.Width(36));
                        slot.enabled = EditorGUILayout.Toggle(slot.enabled, GUILayout.Width(18));
                        GUILayout.Label(HairdoPresetCatalog.DisplayName(slot.kind), GUILayout.Width(88));
                        using (new EditorGUI.DisabledScope(!slot.enabled))
                        {
                            slot.weight = EditorGUILayout.Slider(slot.weight, 0f, 1f);
                        }
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    _fineTuneOverride = false;
                    if (_applyBlendOnChange)
                        ApplyCurvesOnly();
                }
            }

            Rect diamondRect = GUILayoutUtility.GetRect(DiamondSize, DiamondSize, GUILayout.Width(DiamondSize), GUILayout.Height(DiamondSize));
            if (_fineTuneOverride)
            {
                front = _fineTune.DiamondFront01;
                side = _fineTune.DiamondSide01;
                back = _fineTune.DiamondBack01;
                length = _fineTune.DiamondLength01;
                hasBlend = true;
            }

            HairdoPowerDiamondDrawer.Draw(diamondRect, front, side, back, length, hasBlend);
        }

        _applyBlendOnChange = EditorGUILayout.ToggleLeft("Apply blend on change (curves only)", _applyBlendOnChange);
    }

    void DrawFineTune()
    {
        _fineTuneFoldout = EditorGUILayout.Foldout(_fineTuneFoldout, "Fine tune", true);
        if (!_fineTuneFoldout) return;

        EditorGUI.BeginChangeCheck();
        _fineTune.maxStrandLengthM = EditorGUILayout.Slider("Length (m)", _fineTune.maxStrandLengthM, 0.05f, HairdoParams.CatalogMaxLengthM);
        _fineTune.peakHeightM = EditorGUILayout.Slider("Peak height (m)", _fineTune.peakHeightM, 0.01f, 0.5f);
        _fineTune.gaussianSigma = EditorGUILayout.Slider("Volume / sigma", _fineTune.gaussianSigma, 0.05f, 2f);
        _fineTune.plumeTipHold = EditorGUILayout.Slider("Tip hold", _fineTune.plumeTipHold, 0f, 1f);
        _fineTune.gaussianFluxGain = EditorGUILayout.Slider("Flux gain", _fineTune.gaussianFluxGain, 0f, 2f);
        _fineTune.hairlineFront = EditorGUILayout.Slider("Hairline front", _fineTune.hairlineFront, 0f, 1.5f);
        _fineTune.hairlineSide = EditorGUILayout.Slider("Hairline sides", _fineTune.hairlineSide, 0f, 1.5f);
        _fineTune.hairlineBack = EditorGUILayout.Slider("Hairline back", _fineTune.hairlineBack, 0f, 1.5f);
        _fineTune.hairlineCrown = EditorGUILayout.Slider("Hairline crown", _fineTune.hairlineCrown, 0f, 1.5f);
        _fineTune.fringeHeight = EditorGUILayout.Slider("Fringe height", _fineTune.fringeHeight, 0f, 1f);
        _fineTune.sideTiltDeg = EditorGUILayout.Slider("Side tilt (deg)", _fineTune.sideTiltDeg, -30f, 30f);
        _fineTune.flare = EditorGUILayout.Slider("Flare", _fineTune.flare, 0.5f, 2f);
        _fineTune.partMode = (HairdoPartMode)EditorGUILayout.EnumPopup("Part", _fineTune.partMode);
        _fineTune.partWidthM = EditorGUILayout.Slider("Part width", _fineTune.partWidthM, 0.001f, 0.05f);
        _fineTune.partStrength = EditorGUILayout.Slider("Part strength", _fineTune.partStrength, 0f, 1f);
        _fineTune.curlAmount = EditorGUILayout.Slider("Curl amount", _fineTune.curlAmount, 0f, 1f);
        _fineTune.curlFrequency = EditorGUILayout.Slider("Curl frequency", _fineTune.curlFrequency, 0.5f, 8f);
        _fineTune.curlTightness = EditorGUILayout.Slider("Curl tightness", _fineTune.curlTightness, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            _fineTuneOverride = true;
            if (_applyBlendOnChange)
                ApplyCurvesOnly();
        }
    }

    void DrawObsceneSdfSection()
    {
        EditorGUILayout.LabelField("Obscene SDF Max expression", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Regenerate overwrites this text from the blend. Hand-edit, then Apply Expression (Apply first if you want to keep edits before regenerating).",
            MessageType.None);

        _composition = (SdfMaxCompositionAsset)EditorGUILayout.ObjectField(
            "Composition", _composition, typeof(SdfMaxCompositionAsset), false);

        _sexprScroll = EditorGUILayout.BeginScrollView(_sexprScroll, GUILayout.MinHeight(160), GUILayout.MaxHeight(280));
        _sexpr = EditorGUILayout.TextArea(_sexpr ?? "", GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        if (!string.IsNullOrEmpty(_parseError))
            EditorGUILayout.HelpBox(_parseError, MessageType.Error);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Regenerate Expression From Blend"))
                RegenerateSdfExpression();
            if (GUILayout.Button("Apply Expression To Composition"))
                ApplySexprToComposition();
            if (GUILayout.Button("Open SDF Max Composition Editor"))
                SdfMaxCompositionEditorWindow.ShowWindow(null, _composition);
        }
    }

    void DrawApplySection()
    {
        EditorGUILayout.LabelField("Apply", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(_config == null))
        {
            if (GUILayout.Button("Apply Curves To Config"))
                ApplyCurvesOnly();
            if (GUILayout.Button("Apply Curves + Bake Radial"))
                ApplyCurvesAndBakeRadial();
        }

        if (GUILayout.Button("Open Hair Lattice Bake"))
            HairLatticeBakeWindow.Open();
    }

    void RefreshFineTuneFromBlend()
    {
        _fineTune = _blend.EvaluateOrDefault();
        _fineTuneOverride = false;
    }

    HairdoParams CurrentParams()
    {
        if (_fineTuneOverride)
            return _fineTune;
        return _blend.EvaluateOrDefault();
    }

    void ApplyCurvesOnly()
    {
        if (_config == null)
        {
            EditorUtility.DisplayDialog("Hairdo Designer", "Assign or create a HairPlumeConfig.", "OK");
            return;
        }

        Undo.RecordObject(_config, "Apply hairdo curves");
        CurrentParams().ApplyTo(_config);
        EditorUtility.SetDirty(_config);
        if (_driver != null)
        {
            Undo.RecordObject(_driver, "Wire hairdo config");
            _driver.config = _config;
            if (_scalpRoot != null)
                _driver.scalpRoot = _scalpRoot;
            _driver.EnsurePartGizmo();
            EditorUtility.SetDirty(_driver);
        }
    }

    void ApplyCurvesAndBakeRadial()
    {
        ApplyCurvesOnly();
        if (_config == null) return;

        var bake = HairLatticeWaterfallBaker.Bake(_config, _composition, _scalpRoot);
        EnsureFolder(_outputFolder);
        string texPath = $"{_outputFolder}/HairdoRadial_{_config.name}.png";
        WritePng(texPath, bake.texture);
        if (_driver != null)
        {
            Undo.RecordObject(_driver, "Assign hairdo radial bake");
            _driver.config = _config;
            if (_composition != null)
                _driver.plumeSdfComposition = _composition;
            _driver.LoadBake(bake.pixels);
            EditorUtility.SetDirty(_driver);
        }

        Object.DestroyImmediate(bake.texture);
    }

    void RegenerateSdfExpression()
    {
        if (_config == null)
        {
            EditorUtility.DisplayDialog("Hairdo Designer", "Assign or create a config first.", "OK");
            return;
        }

        var parms = CurrentParams();
        Undo.RecordObject(_config, "Apply hairdo before SDF regen");
        parms.ApplyTo(_config);
        EditorUtility.SetDirty(_config);

        var built = HairdoSdfExpressionBuilder.Build(_config, _blend, parms);
        _sexpr = built.sexpr;
        _parseError = null;

        EnsureFolder(_outputFolder);
        string assetPath = $"{_outputFolder}/HairdoSdf_{SanitizeFile(_config.name)}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<SdfMaxCompositionAsset>(assetPath);
        if (existing != null)
        {
            Undo.RecordObject(existing, "Regenerate hairdo SDF");
            existing.nodes = built.asset.nodes;
            existing.rootNodeIndex = built.asset.rootNodeIndex;
            EditorUtility.SetDirty(existing);
            _composition = existing;
            Object.DestroyImmediate(built.asset);
        }
        else
        {
            AssetDatabase.CreateAsset(built.asset, assetPath);
            _composition = built.asset;
        }

        AssetDatabase.SaveAssets();
        if (_driver != null)
        {
            Undo.RecordObject(_driver, "Assign hairdo SDF");
            _driver.plumeSdfComposition = _composition;
            EditorUtility.SetDirty(_driver);
        }
    }

    void ApplySexprToComposition()
    {
        if (!HairdoSdfSexpr.TryParse(_sexpr, out var parsed, out _parseError))
            return;

        EnsureFolder(_outputFolder);
        string name = _config != null ? _config.name : "Hairdo";
        string assetPath = $"{_outputFolder}/HairdoSdf_{SanitizeFile(name)}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<SdfMaxCompositionAsset>(assetPath);
        if (existing != null)
        {
            Undo.RecordObject(existing, "Apply hairdo SDF expression");
            existing.nodes = parsed.nodes;
            existing.rootNodeIndex = parsed.rootNodeIndex;
            EditorUtility.SetDirty(existing);
            _composition = existing;
            Object.DestroyImmediate(parsed);
        }
        else if (_composition != null && AssetDatabase.Contains(_composition))
        {
            Undo.RecordObject(_composition, "Apply hairdo SDF expression");
            _composition.nodes = parsed.nodes;
            _composition.rootNodeIndex = parsed.rootNodeIndex;
            EditorUtility.SetDirty(_composition);
            Object.DestroyImmediate(parsed);
        }
        else
        {
            AssetDatabase.CreateAsset(parsed, assetPath);
            _composition = parsed;
        }

        _parseError = null;
        AssetDatabase.SaveAssets();
        if (_driver != null)
        {
            Undo.RecordObject(_driver, "Assign hairdo SDF");
            _driver.plumeSdfComposition = _composition;
            EditorUtility.SetDirty(_driver);
        }
    }

    void DefaultEverythingFromActor()
    {
        if (_actorRoot == null)
            _actorRoot = Selection.activeGameObject;
        if (_actorRoot == null)
        {
            EditorUtility.DisplayDialog("Hairdo Designer", "Assign an Actor Root or select one in the hierarchy.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Default Hairdo From Actor");
        int group = Undo.GetCurrentGroup();

        _driver = _actorRoot.GetComponentInChildren<HairPlumePhysicsDriver>(true);
        if (_driver == null)
            _driver = Undo.AddComponent<HairPlumePhysicsDriver>(_actorRoot);

        var ragdoll = _actorRoot.GetComponentInChildren<RagdollSystem>(true)
                      ?? _actorRoot.GetComponentInParent<RagdollSystem>();
        var animator = _actorRoot.GetComponentInChildren<Animator>(true);

        _scalpRoot = _driver.scalpRoot;
        if (_scalpRoot == null && animator != null && animator.isHuman)
            _scalpRoot = animator.GetBoneTransform(HumanBodyBones.Head);
        if (_scalpRoot == null && ragdoll != null)
        {
            // Prefer named head if binder resolve path exists later
            _scalpRoot = _actorRoot.transform;
        }

        if (_scalpRoot == null)
            _scalpRoot = _actorRoot.transform;

        var binder = _driver.GetComponent<HairBodyCapsuleBinder>()
                     ?? _driver.gameObject.GetComponent<HairBodyCapsuleBinder>();
        if (binder == null)
            binder = Undo.AddComponent<HairBodyCapsuleBinder>(_driver.gameObject);

        Undo.RecordObject(binder, "Wire hair body binder");
        binder.ragdoll = ragdoll;
        binder.animator = animator;
        binder.scalpRoot = _scalpRoot;
        binder.AutoSetOptionalOverrides();

        if (binder.head != null)
            _scalpRoot = binder.head;

        bool createdConfig = false;
        if (_config == null)
        {
            EnsureFolder(_outputFolder);
            string actorName = SanitizeFile(_actorRoot.name);
            string path = $"{_outputFolder}/HairPlumeConfig_{actorName}.asset";
            _config = AssetDatabase.LoadAssetAtPath<HairPlumeConfig>(path);
            if (_config == null)
            {
                _config = ScriptableObject.CreateInstance<HairPlumeConfig>();
                _config.name = $"HairPlumeConfig_{actorName}";
                AssetDatabase.CreateAsset(_config, path);
                createdConfig = true;
            }
        }

        Undo.RecordObject(_config, "Hairdo defaults");
        _config.ApplyLatticeBakeDefaults();
        EstimateScalpFromHead(binder);
        EditorUtility.SetDirty(_config);

        Undo.RecordObject(_driver, "Wire hairdo driver");
        _driver.config = _config;
        _driver.scalpRoot = _scalpRoot;
        _driver.bodyBinder = binder;
        binder.config = _config;
        _driver.EnsurePartGizmo();
        EditorUtility.SetDirty(_driver);
        EditorUtility.SetDirty(binder);

        _blend.ResetToCrewDefault();
        RefreshFineTuneFromBlend();
        CurrentParams().ApplyTo(_config);
        EditorUtility.SetDirty(_config);

        if (createdConfig)
            AssetDatabase.SaveAssets();

        Undo.CollapseUndoOperations(group);
        EditorGUIUtility.PingObject(_driver);
    }

    void TrySeedFromActorSelection(bool fullDefault)
    {
        if (_actorRoot == null) return;
        _driver = _actorRoot.GetComponentInChildren<HairPlumePhysicsDriver>(true);
        if (_driver != null)
        {
            if (_driver.config != null) _config = _driver.config;
            if (_driver.scalpRoot != null) _scalpRoot = _driver.scalpRoot;
            if (_driver.plumeSdfComposition != null) _composition = _driver.plumeSdfComposition;
        }

        if (fullDefault)
            DefaultEverythingFromActor();
    }

    void EstimateScalpFromHead(HairBodyCapsuleBinder binder)
    {
        if (_config == null) return;
        Transform head = binder != null && binder.head != null ? binder.head : _scalpRoot;
        if (head == null) return;

        float r = _config.headCapsuleRadius;
        var col = head.GetComponent<CapsuleCollider>();
        if (col != null)
            r = Mathf.Max(col.radius * MaxAbsScale(head), 0.05f);
        else
        {
            var rend = head.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Bounds b = rend.bounds;
                r = Mathf.Max(b.extents.x, b.extents.z, 0.05f);
            }
        }

        _config.scalpRadiusM = Mathf.Clamp(r, 0.05f, 0.25f);
        _config.centerPateLocal = new Vector3(0f, _config.scalpRadiusM * 0.45f, 0f);
        _config.headCapsuleRadius = r;
    }

    static float MaxAbsScale(Transform t)
    {
        Vector3 s = t.lossyScale;
        return Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z), 1e-3f);
    }

    void CreateConfig()
    {
        EnsureFolder(_outputFolder);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/HairPlumeConfig_Hairdo.asset");
        var cfg = ScriptableObject.CreateInstance<HairPlumeConfig>();
        cfg.ApplyLatticeBakeDefaults();
        AssetDatabase.CreateAsset(cfg, path);
        _config = cfg;
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(cfg);
    }

    static string SanitizeFile(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Hairdo";
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Replace(' ', '_');
    }

    static void WritePng(string path, Texture2D tex)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
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
#endif
