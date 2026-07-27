#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Weather;

/// <summary>Mouth / toilet / sink / shower hygiene authoring with dental RT preview.</summary>
public sealed class HygieneEditorWindow : EditorWindow
{
    enum Section { Mouth, Toilet, Sink, Shower }

    Section _section = Section.Mouth;
    GameObject _actor;
    MouthInteriorRuntime _mouth;
    ToiletStation _toilet;
    Transform _sink;
    Transform _shower;
    MonoBehaviour _sinkPlan;
    ScriptableObject _sinkTopology;
    MonoBehaviour _showerPlan;
    ScriptableObject _showerTopology;
    WeatherPhysicsManifold _manifold;
    Vector2 _scroll;
    bool _showSkin = true;
    int _selectedTooth;
    RenderTexture _mouthRt;
    Camera _previewCam;
    GameObject _previewRoot;
    readonly List<GameObject> _previewTeeth = new List<GameObject>();
    const int PreviewSize = 256;

    [MenuItem("Window/System Drawer/Hygiene/Hygiene Editor", false, 500)]
    public static void ShowWindow()
    {
        var w = GetWindow<HygieneEditorWindow>("Hygiene");
        w.minSize = new Vector2(720, 520);
    }

    void OnDisable() => CleanupPreview();

    void OnGUI()
    {
        _section = (Section)GUILayout.Toolbar((int)_section, new[] { "Mouth", "Toilet", "Sink", "Shower" });
        _actor = (GameObject)EditorGUILayout.ObjectField("Actor", _actor, typeof(GameObject), true);
        if (_actor != null && _mouth == null)
            _mouth = _actor.GetComponent<MouthInteriorRuntime>() ?? _actor.GetComponentInChildren<MouthInteriorRuntime>();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        switch (_section)
        {
            case Section.Mouth: DrawMouth(); break;
            case Section.Toilet: DrawToilet(); break;
            case Section.Sink: DrawSink(); break;
            case Section.Shower: DrawShower(); break;
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawMouth()
    {
        EditorGUILayout.LabelField("Dental", EditorStyles.boldLabel);
        _mouth = (MouthInteriorRuntime)EditorGUILayout.ObjectField("Mouth Runtime", _mouth, typeof(MouthInteriorRuntime), true);
        if (_mouth == null)
        {
            if (GUILayout.Button("Add MouthInteriorRuntime to Actor") && _actor != null)
            {
                _mouth = _actor.AddComponent<MouthInteriorRuntime>();
                _mouth.EnsureDefaultTeeth();
                _mouth.RebuildToothVisuals();
            }
            return;
        }

        _mouth.EnsureDefaultTeeth();
        var seed = _mouth.seed != null ? _mouth.seed : DeveloperRespectsSeed.FindOrCreate(_mouth.gameObject);
        seed.seed = EditorGUILayout.IntField("Developer Respects Seed", seed.seed);
        EditorGUILayout.LabelField("Preferred Chew Side", $"{seed.PreferredChewSide01:F3} ({(seed.PreferRightSide ? "Right" : "Left")})");
        if (GUILayout.Button("Reseed Preferred Side"))
            seed.Reseed(seed.seed + 1);

        EditorGUI.BeginChangeCheck();
        _mouth.jawOpen01 = EditorGUILayout.Slider("Jaw Open", _mouth.jawOpen01, 0f, 1f);
        _mouth.jawRollDeg = EditorGUILayout.Slider("Jaw Roll Deg", _mouth.jawRollDeg, -12f, 12f);
        _mouth.gumHeightScale = EditorGUILayout.Slider("Gum Height Scale", _mouth.gumHeightScale, 0.1f, 2f);
        _mouth.ditherMouthSkin = EditorGUILayout.Toggle("Dither Mouth Skin", _mouth.ditherMouthSkin);
        _showSkin = EditorGUILayout.Toggle("View With Skin", _showSkin);
        _mouth.upperGumRenderer = (Renderer)EditorGUILayout.ObjectField("Upper Gum Renderer", _mouth.upperGumRenderer, typeof(Renderer), true);
        _mouth.lowerGumRenderer = (Renderer)EditorGUILayout.ObjectField("Lower Gum Renderer", _mouth.lowerGumRenderer, typeof(Renderer), true);
        _mouth.mouthSkinRenderer = (Renderer)EditorGUILayout.ObjectField("Mouth Skin Renderer", _mouth.mouthSkinRenderer, typeof(Renderer), true);
        if (EditorGUI.EndChangeCheck())
        {
            _mouth.BindGumMaterials();
            _mouth.ApplyJawPose();
            EditorUtility.SetDirty(_mouth);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gum Height Maps", EditorStyles.boldLabel);
        _mouth.upperGumHeightMap = (Texture2D)EditorGUILayout.ObjectField("Upper Gum", _mouth.upperGumHeightMap, typeof(Texture2D), false);
        _mouth.lowerGumHeightMap = (Texture2D)EditorGUILayout.ObjectField("Lower Gum", _mouth.lowerGumHeightMap, typeof(Texture2D), false);
        _mouth.upperGumFissures = (Texture2D)EditorGUILayout.ObjectField("Upper Fissures", _mouth.upperGumFissures, typeof(Texture2D), false);
        _mouth.lowerGumFissures = (Texture2D)EditorGUILayout.ObjectField("Lower Fissures", _mouth.lowerGumFissures, typeof(Texture2D), false);
        if (GUILayout.Button("Generate Procedural Gum Height Maps"))
        {
            var teeth = _mouth.teeth.ToArray();
            _mouth.upperGumHeightMap = GumHeightMapGenerator.Generate(128, teeth, ToothArch.Upper);
            _mouth.lowerGumHeightMap = GumHeightMapGenerator.Generate(128, teeth, ToothArch.Lower);
            _mouth.BindGumMaterials();
            EditorUtility.SetDirty(_mouth);
        }
        if (GUILayout.Button("Rebuild Tooth Visuals"))
        {
            _mouth.RebuildToothVisuals();
            RebuildPreviewTeeth();
            EditorUtility.SetDirty(_mouth);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Teeth", EditorStyles.boldLabel);
        if (_mouth.teeth.Count > 0)
        {
            _selectedTooth = Mathf.Clamp(_selectedTooth, 0, _mouth.teeth.Count - 1);
            _selectedTooth = EditorGUILayout.IntSlider("Selected Tooth", _selectedTooth, 0, _mouth.teeth.Count - 1);
            var t = _mouth.teeth[_selectedTooth];
            EditorGUI.BeginChangeCheck();
            t.kind = (ToothKind)EditorGUILayout.EnumPopup("Kind", t.kind);
            t.zone = ToothSlot.ZoneFor(t.kind);
            EditorGUILayout.EnumPopup("Zone", t.zone);
            t.arch = (ToothArch)EditorGUILayout.EnumPopup("Arch", t.arch);
            t.side = (ToothSide)EditorGUILayout.EnumPopup("Side", t.side);
            t.stop01 = EditorGUILayout.Slider("Stop on Spline", t.stop01, 0f, 1f);
            t.biteOffset = EditorGUILayout.Vector3Field("Bite Offset", t.biteOffset);
            t.present = EditorGUILayout.Toggle("Present", t.present);
            t.staticMesh = (Mesh)EditorGUILayout.ObjectField("Static Mesh", t.staticMesh, typeof(Mesh), false);
            if (EditorGUI.EndChangeCheck())
            {
                RebuildPreviewTeeth();
                EditorUtility.SetDirty(_mouth);
            }
        }

        DrawMouthPreview();
        if (_actor != null)
        {
            var sheet = _actor.GetComponent<LifeSystemsSheet>();
            if (sheet != null)
            {
                sheet.EnsureDefaults();
                EditorGUILayout.LabelField("Ablution", sheet.Get01(LifeSystemsChannelCatalog.Ablution).ToString("F2"));
            }
        }
    }

    void DrawMouthPreview()
    {
        EnsurePreview();
        if (_mouthRt == null) return;
        EditorGUILayout.LabelField("Mouth Preview", EditorStyles.boldLabel);
        var rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.ExpandWidth(false));
        EditorGUI.DrawPreviewTexture(rect, _mouthRt);
        if (_previewRoot != null)
            _previewRoot.SetActive(_showSkin);
        SyncPreviewTeeth();
        if (_previewCam != null && _mouth != null)
        {
            _previewCam.transform.position = _mouth.transform.position + _mouth.transform.forward * -0.25f + Vector3.up * 0.05f;
            _previewCam.transform.LookAt(_mouth.transform.position);
            _previewCam.Render();
            Repaint();
        }
    }

    void EnsurePreview()
    {
        if (_mouthRt != null) return;
        _mouthRt = new RenderTexture(PreviewSize, PreviewSize, 16);
        var camGo = new GameObject("HygieneMouthPreviewCam");
        camGo.hideFlags = HideFlags.HideAndDontSave;
        _previewCam = camGo.AddComponent<Camera>();
        _previewCam.targetTexture = _mouthRt;
        _previewCam.clearFlags = CameraClearFlags.SolidColor;
        _previewCam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
        _previewCam.nearClipPlane = 0.01f;
        _previewCam.farClipPlane = 5f;
        _previewRoot = new GameObject("HygieneMouthPreviewSkin");
        _previewRoot.hideFlags = HideFlags.HideAndDontSave;
        RebuildPreviewTeeth();
    }

    void RebuildPreviewTeeth()
    {
        for (int i = 0; i < _previewTeeth.Count; i++)
        {
            if (_previewTeeth[i] != null)
                DestroyImmediate(_previewTeeth[i]);
        }
        _previewTeeth.Clear();
        if (_mouth == null || _previewRoot == null) return;
        _mouth.EnsureDefaultTeeth();
        foreach (var slot in _mouth.EnumeratePresent())
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"Preview_{slot.kind}";
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(_previewRoot.transform, false);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            _previewTeeth.Add(go);
        }
        SyncPreviewTeeth();
    }

    void SyncPreviewTeeth()
    {
        if (_mouth == null || _previewRoot == null) return;
        int i = 0;
        foreach (var slot in _mouth.EnumeratePresent())
        {
            if (i >= _previewTeeth.Count) break;
            var go = _previewTeeth[i++];
            if (go == null) continue;
            go.transform.position = _mouth.ResolveToothWorld(slot);
            go.transform.localScale = Vector3.one * 0.008f;
            go.SetActive(slot.present && (_showSkin || true));
            // Highlight selected
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                bool sel = _mouth.teeth.IndexOf(slot) == _selectedTooth;
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", sel ? new Color(1f, 0.85f, 0.4f) : new Color(0.95f, 0.95f, 0.9f));
                block.SetColor("_Color", sel ? new Color(1f, 0.85f, 0.4f) : new Color(0.95f, 0.95f, 0.9f));
                r.SetPropertyBlock(block);
            }
        }
        // Skin proxy sphere
        Transform skin = _previewRoot.transform.Find("SkinProxy");
        if (skin == null)
        {
            var skinGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skinGo.name = "SkinProxy";
            skinGo.hideFlags = HideFlags.HideAndDontSave;
            skinGo.transform.SetParent(_previewRoot.transform, false);
            Object.DestroyImmediate(skinGo.GetComponent<Collider>());
            skin = skinGo.transform;
        }
        skin.gameObject.SetActive(_showSkin);
        if (_mouth != null)
        {
            skin.position = _mouth.transform.position;
            skin.localScale = Vector3.one * 0.12f;
        }
    }

    void CleanupPreview()
    {
        for (int i = 0; i < _previewTeeth.Count; i++)
        {
            if (_previewTeeth[i] != null)
                DestroyImmediate(_previewTeeth[i]);
        }
        _previewTeeth.Clear();
        if (_previewCam != null) DestroyImmediate(_previewCam.gameObject);
        if (_previewRoot != null) DestroyImmediate(_previewRoot);
        if (_mouthRt != null)
        {
            _mouthRt.Release();
            DestroyImmediate(_mouthRt);
        }
        _previewCam = null;
        _previewRoot = null;
        _mouthRt = null;
    }

    void DrawToilet()
    {
        _toilet = (ToiletStation)EditorGUILayout.ObjectField("Toilet", _toilet, typeof(ToiletStation), true);
        if (_toilet == null) return;
        EditorGUI.BeginChangeCheck();
        _toilet.includesBidet = EditorGUILayout.Toggle("Includes Bidet", _toilet.includesBidet);
        _toilet.useToiletPaperBt = EditorGUILayout.Toggle("Use TP BT", _toilet.useToiletPaperBt);
        _toilet.paperScroll = (PaperScrollSystem)EditorGUILayout.ObjectField("Paper Scroll", _toilet.paperScroll, typeof(PaperScrollSystem), true);
        _toilet.lidTopology = (ScriptableObject)EditorGUILayout.ObjectField("Lid Topology", _toilet.lidTopology, typeof(ScriptableObject), false);
        _toilet.lidPlan = (MonoBehaviour)EditorGUILayout.ObjectField("Lid Plan", _toilet.lidPlan, typeof(MonoBehaviour), true);
        _toilet.seatAnchor = (Transform)EditorGUILayout.ObjectField("Seat Anchor", _toilet.seatAnchor, typeof(Transform), true);
        _toilet.bowlAnchor = (Transform)EditorGUILayout.ObjectField("Bowl Anchor", _toilet.bowlAnchor, typeof(Transform), true);
        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(_toilet);
        if (_toilet.paperScroll != null)
        {
            EditorGUILayout.IntField("Sheets Remaining", _toilet.paperScroll.sheetsRemaining);
            EditorGUILayout.FloatField("Wound Radius", _toilet.paperScroll.WoundRadiusM);
        }
        EditorGUILayout.HelpBox("GoalType.Toilet → ToiletVisitNode (before → excrete → after). Bidet clears groin smells; else TP scrunch.", MessageType.Info);
    }

    void DrawSink()
    {
        _sink = (Transform)EditorGUILayout.ObjectField("Sink Root", _sink, typeof(Transform), true);
        _sinkPlan = (MonoBehaviour)EditorGUILayout.ObjectField("Open/Close Plan", _sinkPlan, typeof(MonoBehaviour), true);
        _sinkTopology = (ScriptableObject)EditorGUILayout.ObjectField("Topology Asset", _sinkTopology, typeof(ScriptableObject), false);
        _manifold = (WeatherPhysicsManifold)EditorGUILayout.ObjectField("Physics Manifold", _manifold, typeof(WeatherPhysicsManifold), true);
        EditorGUILayout.HelpBox(
            "WashHandsNode whitelist: water, odor. Blacklist: skin. Duck-typed BeginOpen on sink.",
            MessageType.Info);
        if (_sink != null && GUILayout.Button("Ping Sink"))
            EditorGUIUtility.PingObject(_sink);
        if (GUILayout.Button("Preview Bake + BeginOpen") && _sink != null)
        {
            WashHandsNode.TryBakeOpenClose(_sinkPlan, _sinkTopology);
            WashHandsNode.TryBeginOpen(_sink);
        }
        if (GUILayout.Button("Clear Hand Smells on Actor") && _actor != null)
            HygieneSmellClearService.ClearHands(_actor);
        if (_manifold != null && _sink != null && GUILayout.Button("Clear Manifold Sphere at Sink"))
        {
            HygieneManifoldClearService.ClearSphere(
                _manifold, _sink.position, 0.35f,
                new List<string> { HygieneManifoldClearService.ChannelWater, HygieneManifoldClearService.ChannelOdor },
                new List<string> { HygieneManifoldClearService.ChannelSkin });
        }
    }

    void DrawShower()
    {
        _shower = (Transform)EditorGUILayout.ObjectField("Shower Head", _shower, typeof(Transform), true);
        _showerPlan = (MonoBehaviour)EditorGUILayout.ObjectField("Open/Close Plan", _showerPlan, typeof(MonoBehaviour), true);
        _showerTopology = (ScriptableObject)EditorGUILayout.ObjectField("Topology Asset", _showerTopology, typeof(ScriptableObject), false);
        _manifold = (WeatherPhysicsManifold)EditorGUILayout.ObjectField("Physics Manifold", _manifold, typeof(WeatherPhysicsManifold), true);
        EditorGUILayout.HelpBox(
            "ShowerNode whitelist: water, humidity, odor. Blacklist: skin.",
            MessageType.Info);
        if (_shower != null && GUILayout.Button("Ping Shower"))
            EditorGUIUtility.PingObject(_shower);
        if (GUILayout.Button("Preview Bake Shower Topology"))
            WashHandsNode.TryBakeOpenClose(_showerPlan, _showerTopology);
        if (GUILayout.Button("Clear All Smells on Actor") && _actor != null)
            HygieneSmellClearService.ClearAllOn(_actor);
        if (_manifold != null && _shower != null && GUILayout.Button("Clear Manifold Sphere at Shower"))
        {
            HygieneManifoldClearService.ClearSphere(
                _manifold, _shower.position, 1.2f,
                new List<string>
                {
                    HygieneManifoldClearService.ChannelWater,
                    HygieneManifoldClearService.ChannelHumidity,
                    HygieneManifoldClearService.ChannelOdor
                },
                new List<string> { HygieneManifoldClearService.ChannelSkin });
        }
    }
}
#endif
