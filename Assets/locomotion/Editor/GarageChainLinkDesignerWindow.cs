#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class GarageChainLinkDesignerWindow : EditorWindow
{
    GarageChainSpec _spec;
    GarageChainLinkKind _kind = GarageChainLinkKind.Chain;
    Vector2 _scroll;
    readonly SdfMaxCompositionPreviewDrawer _preview = new SdfMaxCompositionPreviewDrawer();

    [MenuItem("Locomotion/Garage Chain Link Designer")]
    public static void Open() => Open(null);

    public static void Open(GarageChainSpec spec)
    {
        var w = GetWindow<GarageChainLinkDesignerWindow>("Chain Link");
        w.minSize = new Vector2(440, 560);
        if (spec != null)
            w._spec = spec;
        w.Focus();
    }

    void OnEnable() => SceneView.duringSceneGui += OnSceneGui;

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGui;
        _preview.Dispose();
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _spec = (GarageChainSpec)EditorGUILayout.ObjectField("Chain spec", _spec, typeof(GarageChainSpec), false);
        if (_spec == null)
        {
            if (GUILayout.Button("Create GarageChainSpec"))
            {
                var path = EditorUtility.SaveFilePanelInProject("Save Garage Chain", "GarageChain", "asset", "");
                if (!string.IsNullOrEmpty(path))
                {
                    var s = CreateInstance<GarageChainSpec>();
                    AssetDatabase.CreateAsset(s, path);
                    _spec = s;
                }
            }
            EditorGUILayout.EndScrollView();
            return;
        }

        _spec.linkCurve ??= new GarageChainLinkCurve();
        _kind = (GarageChainLinkKind)EditorGUILayout.EnumPopup("Link kind", _kind);
        _spec.selectedKind = _kind;
        _spec.linkPitchM = EditorGUILayout.FloatField("Link pitch (m)", _spec.linkPitchM);
        _spec.linkCurve.SyncFromPitch(_spec.linkPitchM);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Raccoon-mask plate", EditorStyles.boldLabel);
        var c = _spec.linkCurve;
        c.plateOuterR = EditorGUILayout.FloatField("Plate outer R", c.plateOuterR);
        c.waistScale = EditorGUILayout.Slider("Waist scale", c.waistScale, 0.15f, 1f);
        c.plateThickness = EditorGUILayout.FloatField("Plate thickness", c.plateThickness);
        c.plateGap = EditorGUILayout.FloatField("Plate gap", c.plateGap);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pins + hollow sheaths", EditorStyles.boldLabel);
        c.pinRadius = EditorGUILayout.FloatField("Pin radius", c.pinRadius);
        c.pinLength = EditorGUILayout.FloatField("Pin length", c.pinLength);
        c.rollerRadius = EditorGUILayout.FloatField("Roller radius", c.rollerRadius);
        c.rollerInnerRadius = EditorGUILayout.FloatField("Roller inner R", c.rollerInnerRadius);
        c.rollerLength = EditorGUILayout.FloatField("Roller length", c.rollerLength);
        c.weldK = EditorGUILayout.FloatField("Weld blend", c.weldK);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Master shear clip", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            _kind == GarageChainLinkKind.Master
                ? "Master: left pin is not welded. Shear clip (U + two blades) snaps on that side."
                : "Chain / Broken share the welded raccoon assembly. Steel differs; the curve does not.",
            MessageType.Info);
        c.clipThickness = EditorGUILayout.FloatField("Clip thickness", c.clipThickness);
        c.clipSpan = EditorGUILayout.FloatField("Clip span", c.clipSpan);

        var def = _spec.DefFor(_kind);
        def.linkSdf = (SdfMax.SdfMaxCompositionAsset)EditorGUILayout.ObjectField(
            "Baked link SDF", def.linkSdf, typeof(SdfMax.SdfMaxCompositionAsset), false);
        def.pieceCurve = (CustomRadialSideAsset)EditorGUILayout.ObjectField(
            "Piece curve", def.pieceCurve, typeof(CustomRadialSideAsset), false);

        EditorGUILayout.Space();
        if (GUILayout.Button("Bake SDF"))
            BakeToAsset();
        _preview.Draw(_spec.DefFor(_kind).linkSdf);
        if (GUILayout.Button("Open Garage Chain Designer"))
            GarageChainDesignerWindow.Open();

        if (GUI.changed)
            EditorUtility.SetDirty(_spec);
        EditorGUILayout.EndScrollView();
    }

    void BakeToAsset()
    {
        if (_spec == null)
            return;
        string specPath = AssetDatabase.GetAssetPath(_spec);
        if (string.IsNullOrEmpty(specPath))
        {
            _spec.BakeLinkSdf(_kind);
            EditorUtility.SetDirty(_spec);
            _preview.Invalidate();
            Repaint();
            return;
        }

        string dir = Path.GetDirectoryName(specPath)?.Replace('\\', '/') ?? "Assets";
        string file = $"{_spec.name}_{_kind}LinkSdf.asset";
        string path = $"{dir}/{file}";
        var existing = AssetDatabase.LoadAssetAtPath<SdfMax.SdfMaxCompositionAsset>(path);
        var baked = _spec.BakeLinkSdf(_kind, existing);
        if (existing == null)
            AssetDatabase.CreateAsset(baked, path);
        else
            EditorUtility.SetDirty(existing);
        _spec.DefFor(_kind).linkSdf = AssetDatabase.LoadAssetAtPath<SdfMax.SdfMaxCompositionAsset>(path);
        EditorUtility.SetDirty(_spec);
        AssetDatabase.SaveAssets();
        _preview.Invalidate();
        Repaint();
    }

    void OnSceneGui(SceneView view)
    {
        if (_spec?.linkCurve == null)
            return;
        var curve = _spec.linkCurve;
        curve.SyncFromPitch(_spec.linkPitchM);
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        Handles.color = new Color(0.85f, 0.55f, 0.2f, 1f);
        DrawPath(GarageChainLinkCurves.RaccoonMaskPath(curve));

        Handles.color = new Color(0.4f, 0.75f, 1f, 1f);
        DrawPath(GarageChainLinkCurves.PinAxis(curve, true));
        DrawPath(GarageChainLinkCurves.PinAxis(curve, false));

        Handles.color = new Color(0.3f, 0.9f, 0.55f, 1f);
        DrawWireCapsule(curve.LeftPin, curve.RollerRadius, curve.RollerLength);
        DrawWireCapsule(curve.RightPin, curve.RollerRadius, curve.RollerLength);

        if (_kind == GarageChainLinkKind.Master)
        {
            Handles.color = new Color(1f, 0.35f, 0.35f, 1f);
            DrawPath(GarageChainLinkCurves.ShearClipPath(curve));
            DrawPath(GarageChainLinkCurves.ShearBladePath(curve, true));
            DrawPath(GarageChainLinkCurves.ShearBladePath(curve, false));
        }

        Handles.Label(curve.LeftPin + Vector3.up * (curve.PlateOuterR + 0.01f), "L pin");
        Handles.Label(curve.RightPin + Vector3.up * (curve.PlateOuterR + 0.01f), "R pin");
    }

    static void DrawPath(System.Collections.Generic.IList<Vector3> path)
    {
        if (path == null || path.Count < 2)
            return;
        var pts = new Vector3[path.Count];
        for (int i = 0; i < path.Count; i++)
            pts[i] = path[i];
        Handles.DrawAAPolyLine(3f, pts);
    }

    static void DrawWireCapsule(Vector3 center, float radius, float length)
    {
        float h = length * 0.5f;
        Handles.DrawWireDisc(center + Vector3.forward * h, Vector3.forward, radius);
        Handles.DrawWireDisc(center + Vector3.back * h, Vector3.forward, radius);
        Handles.DrawWireDisc(center, Vector3.forward, radius);
        Handles.DrawLine(center + new Vector3(radius, 0f, -h), center + new Vector3(radius, 0f, h));
        Handles.DrawLine(center + new Vector3(-radius, 0f, -h), center + new Vector3(-radius, 0f, h));
    }
}
#endif
