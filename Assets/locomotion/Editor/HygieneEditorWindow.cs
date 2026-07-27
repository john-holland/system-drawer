#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
    Vector2 _scroll;
    bool _showSkin = true;
    int _selectedTooth;
    RenderTexture _mouthRt;
    Camera _previewCam;
    GameObject _previewRoot;
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
            }
            return;
        }

        _mouth.EnsureDefaultTeeth();
        var seed = _mouth.seed != null ? _mouth.seed : DeveloperRespectsSeed.FindOrCreate(_mouth.gameObject);
        seed.seed = EditorGUILayout.IntField("Developer Respects Seed", seed.seed);
        EditorGUILayout.LabelField("Preferred Chew Side", $"{seed.PreferredChewSide01:F3} ({(seed.PreferRightSide ? "Right" : "Left")})");
        if (GUILayout.Button("Reseed Preferred Side"))
            seed.Reseed(seed.seed + 1);

        _mouth.jawOpen01 = EditorGUILayout.Slider("Jaw Open", _mouth.jawOpen01, 0f, 1f);
        _mouth.gumHeightScale = EditorGUILayout.Slider("Gum Height Scale", _mouth.gumHeightScale, 0.1f, 2f);
        _mouth.ditherMouthSkin = EditorGUILayout.Toggle("Dither Mouth Skin", _mouth.ditherMouthSkin);
        _showSkin = EditorGUILayout.Toggle("View With Skin", _showSkin);

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
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_mouth);
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
    }

    void CleanupPreview()
    {
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
        _toilet.includesBidet = EditorGUILayout.Toggle("Includes Bidet", _toilet.includesBidet);
        _toilet.useToiletPaperBt = EditorGUILayout.Toggle("Use TP BT", _toilet.useToiletPaperBt);
        _toilet.paperScroll = (PaperScrollSystem)EditorGUILayout.ObjectField("Paper Scroll", _toilet.paperScroll, typeof(PaperScrollSystem), true);
        _toilet.lidTopology = (ScriptableObject)EditorGUILayout.ObjectField("Lid Topology", _toilet.lidTopology, typeof(ScriptableObject), false);
        _toilet.seatAnchor = (Transform)EditorGUILayout.ObjectField("Seat Anchor", _toilet.seatAnchor, typeof(Transform), true);
        _toilet.bowlAnchor = (Transform)EditorGUILayout.ObjectField("Bowl Anchor", _toilet.bowlAnchor, typeof(Transform), true);
        if (_toilet.paperScroll != null)
        {
            EditorGUILayout.IntField("Sheets Remaining", _toilet.paperScroll.sheetsRemaining);
            EditorGUILayout.FloatField("Wound Radius", _toilet.paperScroll.WoundRadiusM);
        }
    }

    void DrawSink()
    {
        _sink = (Transform)EditorGUILayout.ObjectField("Sink Root", _sink, typeof(Transform), true);
        EditorGUILayout.HelpBox("WashHandsNode uses open/close topology on sink + clears hand smells; manifold whitelist excludes skin.", MessageType.Info);
        if (_sink != null && GUILayout.Button("Ping Sink"))
            EditorGUIUtility.PingObject(_sink);
    }

    void DrawShower()
    {
        _shower = (Transform)EditorGUILayout.ObjectField("Shower Head", _shower, typeof(Transform), true);
        EditorGUILayout.HelpBox("ShowerNode clears whole-body smells and manifold water/humidity/odor; skin blacklisted.", MessageType.Info);
        if (_shower != null && GUILayout.Button("Ping Shower"))
            EditorGUIUtility.PingObject(_shower);
    }
}
#endif
