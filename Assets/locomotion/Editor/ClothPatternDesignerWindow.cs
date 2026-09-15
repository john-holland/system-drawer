#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class ClothPatternDesignerWindow : EditorWindow
{
    ClothBoltSpec _bolt;
    ClothSplineKind _kind = ClothSplineKind.Cut;
    int _pathIndex;
    string _svg = "M 0 0 L 1 0 L 1 1 L 0 1 Z";
    Vector2 _scroll;

    [MenuItem("Locomotion/Cloth Pattern Designer")]
    public static void Open() => Open(null);

    public static void Open(ClothBoltSpec bolt)
    {
        var w = GetWindow<ClothPatternDesignerWindow>("Cloth Pattern");
        w.minSize = new Vector2(460, 560);
        w._bolt = bolt;
    }

    void OnEnable() => SceneView.duringSceneGui += OnSceneGui;

    void OnDisable() => SceneView.duringSceneGui -= OnSceneGui;

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _bolt = (ClothBoltSpec)EditorGUILayout.ObjectField("Bolt", _bolt, typeof(ClothBoltSpec), false);
        if (_bolt == null)
        {
            if (GUILayout.Button("Create ClothBoltSpec"))
            {
                var path = EditorUtility.SaveFilePanelInProject("Save Cloth Bolt", "ClothBolt", "asset", "");
                if (!string.IsNullOrEmpty(path))
                {
                    var s = CreateInstance<ClothBoltSpec>();
                    AssetDatabase.CreateAsset(s, path);
                    _bolt = s;
                }
            }
            EditorGUILayout.EndScrollView();
            return;
        }
        _bolt.widthM = EditorGUILayout.FloatField("Width m", _bolt.widthM);
        _bolt.lengthM = EditorGUILayout.FloatField("Length m", _bolt.lengthM);
        _bolt.thicknessM = EditorGUILayout.FloatField("Thickness m", _bolt.thicknessM);
        _bolt.weaveUv = EditorGUILayout.Vector2Field("Weave UV", _bolt.weaveUv);
        _bolt.commodityKey = EditorGUILayout.TextField("Commodity", _bolt.commodityKey);
        _bolt.loopSection = (SkinnedMeshLoopSectionAsset)EditorGUILayout.ObjectField(
            "Loop section", _bolt.loopSection, typeof(SkinnedMeshLoopSectionAsset), false);
        _bolt.joinLoopIdA = EditorGUILayout.TextField("Join loop A", _bolt.joinLoopIdA);
        _bolt.joinLoopIdB = EditorGUILayout.TextField("Join loop B", _bolt.joinLoopIdB);
        if (GUILayout.Button("Open Skinned Loop Section"))
            EditorApplication.ExecuteMenuItem("Window/System Drawer/Mesh/Skinned Loop Section");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spline ribbons (cut / fold / stitch)", EditorStyles.boldLabel);
        _kind = (ClothSplineKind)EditorGUILayout.EnumPopup("New path kind", _kind);
        if (GUILayout.Button("Add path"))
            _bolt.AddPath(_kind);
        if (_bolt.paths.Count > 0)
        {
            _pathIndex = EditorGUILayout.IntSlider("Path", _pathIndex, 0, _bolt.paths.Count - 1);
            var path = _bolt.paths[_pathIndex];
            path.kind = (ClothSplineKind)EditorGUILayout.EnumPopup("Kind", path.kind);
            path.pathId = EditorGUILayout.TextField("Id", path.pathId);
            path.joinLoopIdA = EditorGUILayout.TextField("Join loop A", path.joinLoopIdA);
            path.joinLoopIdB = EditorGUILayout.TextField("Join loop B", path.joinLoopIdB);
            if (GUILayout.Button("Add grabber (UV center)"))
                path.grabbers.Add(new ClothGrabberPin { uv = new Vector2(0.5f, 0.5f), weight01 = 1f });
            if (GUILayout.Button("Add control point"))
                path.controlPoints.Add(new Vector3(_bolt.widthM * 0.5f, 0f, _bolt.lengthM * 0.5f));
            EditorGUILayout.LabelField("Points / grabbers", path.controlPoints.Count + " / " + path.grabbers.Count);
            EditorGUILayout.LabelField("Pleats", ClothPleatBaker.PleatCount(ClothPleatBaker.PathLength(path), 0.08f).ToString());
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("SVG import", EditorStyles.boldLabel);
        _svg = EditorGUILayout.TextArea(_svg, GUILayout.MinHeight(64));
        if (GUILayout.Button("Import SVG as cut path"))
        {
            var imported = ClothSvgPathParser.ToCutPath(_svg, _bolt.widthM, _bolt.lengthM);
            _bolt.paths.Add(imported);
            _pathIndex = _bolt.paths.Count - 1;
        }
        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_bolt);
    }

    void OnSceneGui(SceneView view)
    {
        if (_bolt == null || _bolt.paths == null || _bolt.paths.Count == 0) return;
        if (_pathIndex < 0 || _pathIndex >= _bolt.paths.Count) return;
        var path = _bolt.paths[_pathIndex];
        if (path.controlPoints == null) return;
        Handles.color = path.kind == ClothSplineKind.Cut
            ? Color.red
            : path.kind == ClothSplineKind.Fold
                ? Color.cyan
                : Color.yellow;
        for (int i = 0; i < path.controlPoints.Count; i++)
        {
            EditorGUI.BeginChangeCheck();
            var p = Handles.PositionHandle(path.controlPoints[i], Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                path.controlPoints[i] = p;
                EditorUtility.SetDirty(_bolt);
            }
            if (i > 0)
                Handles.DrawLine(path.controlPoints[i - 1], path.controlPoints[i]);
        }
        Handles.color = Color.magenta;
        for (int i = 0; i < path.grabbers.Count; i++)
        {
            var g = path.grabbers[i];
            var world = new Vector3(g.uv.x * _bolt.widthM, 0.02f, g.uv.y * _bolt.lengthM);
            Handles.SphereHandleCap(0, world, Quaternion.identity, 0.04f, EventType.Repaint);
        }
    }
}
#endif
