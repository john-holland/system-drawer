using UnityEditor;
using UnityEngine;

public sealed class WindowPixelLightGridDesignerWindow : EditorWindow
{
    WindowAssemblySpec _spec;
    HouseConstructionPlan _plan;
    PixelLightGridMountGameObject _mount;
    string _floorText = "first";
    Vector2 _scroll;

    [MenuItem("Locomotion/Window PixelLight Grid Designer")]
    public static void Open()
    {
        var w = GetWindow<WindowPixelLightGridDesignerWindow>("Window PixelLight");
        w.minSize = new Vector2(420, 480);
    }

    public static void OpenWith(WindowAssemblySpec spec)
    {
        Open();
        var w = GetWindow<WindowPixelLightGridDesignerWindow>();
        w._spec = spec;
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _spec = (WindowAssemblySpec)EditorGUILayout.ObjectField("Window Assembly", _spec, typeof(WindowAssemblySpec), false);
        _plan = (HouseConstructionPlan)EditorGUILayout.ObjectField("Construction Plan", _plan, typeof(HouseConstructionPlan), false);
        _mount = (PixelLightGridMountGameObject)EditorGUILayout.ObjectField("Mount", _mount, typeof(PixelLightGridMountGameObject), true);
        if (_mount != null)
            PixelLightRadialBrushDrawer.DrawOnMount(_mount);
        _floorText = EditorGUILayout.TextField("Floor", _floorText);

        if (_spec == null)
        {
            if (GUILayout.Button("Create WindowAssemblySpec"))
            {
                var path = EditorUtility.SaveFilePanelInProject("Save Window Assembly", "WindowAssembly", "asset", "");
                if (!string.IsNullOrEmpty(path))
                {
                    var s = CreateInstance<WindowAssemblySpec>();
                    s.ApplyAutoFit();
                    AssetDatabase.CreateAsset(s, path);
                    _spec = s;
                }
            }
            EditorGUILayout.EndScrollView();
            return;
        }

        _spec.paneCountX = EditorGUILayout.IntSlider("Pane count X", _spec.paneCountX, 1, 8);
        _spec.paneCountY = EditorGUILayout.IntSlider("Pane count Y", _spec.paneCountY, 1, 8);
        _spec.openingSize = EditorGUILayout.Vector2Field("Opening size (m)", _spec.openingSize);
        _spec.muntinWidth = EditorGUILayout.FloatField("Muntin width", _spec.muntinWidth);
        _spec.glazing = (WindowGlazingKind)EditorGUILayout.EnumPopup("Glazing", _spec.glazing);
        _spec.slidingSashCount = EditorGUILayout.IntField("Sliding sashes", _spec.slidingSashCount);
        _spec.sideTrim = EditorGUILayout.Toggle("Side trim", _spec.sideTrim);
        _spec.underSillTrim = EditorGUILayout.Toggle("Under-sill trim", _spec.underSillTrim);
        _spec.sillOccupiesRow = EditorGUILayout.Toggle("Sill occupies row", _spec.sillOccupiesRow);
        _spec.shutters = (WindowShutterKind)EditorGUILayout.EnumPopup("Shutters", _spec.shutters);
        _spec.shade = (WindowShadeKind)EditorGUILayout.EnumPopup("Shade", _spec.shade);

        EditorGUILayout.Space();
        _spec.autoFitPixelLightGrid = EditorGUILayout.Toggle("Auto-fit PixelLight grid", _spec.autoFitPixelLightGrid);
        if (_spec.autoFitPixelLightGrid)
        {
            _spec.ApplyAutoFit();
            EditorGUILayout.HelpBox(
                $"3×3 panes → at least 7×7. Fitted now {_spec.pixelLightGridW}×{_spec.pixelLightGridH}. " +
                "Pane/muntin world size is independent of grid cells.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Arbitrary sizing: auto-fit mins (7×7, side/sill rows) are off.", MessageType.None);
            _spec.pixelLightGridW = EditorGUILayout.IntField("Grid W", Mathf.Max(1, _spec.pixelLightGridW));
            _spec.pixelLightGridH = EditorGUILayout.IntField("Grid H", Mathf.Max(1, _spec.pixelLightGridH));
        }

        if (_plan != null && HouseFloorIndex.TryParse(_floorText, out int floor))
        {
            var fp = _plan.GetOrCreateFloor(floor);
            _spec.pixelLightCellSize = fp.pixelLightCellSize;
        }
        _spec.pixelLightCellSize = EditorGUILayout.FloatField("Cell size", _spec.pixelLightCellSize);

        _spec.PaneMuntinWorldSizes(out var pane, out float bar);
        EditorGUILayout.LabelField("Pane world", $"{pane.x:0.000} × {pane.y:0.000}  muntin {bar:0.000}");
        EditorGUILayout.LabelField("Trim runs (no elbows)",
            $"N/S {_spec.TrimRunLengthAlongX()}   E/W {_spec.TrimRunLengthAlongY()}   elbows {MuntinGridLayout.ElbowCount}");

        DrawGridPreview();

        if (GUILayout.Button("Apply to mount") && _mount != null)
        {
            _mount.gridWidth = _spec.pixelLightGridW;
            _mount.gridHeight = _spec.pixelLightGridH;
            _mount.cellSize = _spec.pixelLightCellSize;
            EditorUtility.SetDirty(_mount);
        }
        if (GUILayout.Button("Open Pixel Light Timed Designer"))
            EditorApplication.ExecuteMenuItem("Locomotion/Pixel Light Timed Designer");
        if (GUILayout.Button("Open House Envelope Designer"))
            HouseEnvelopeDesignerWindow.Open();

        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_spec);
    }

    void DrawGridPreview()
    {
        int w = Mathf.Max(1, _spec.pixelLightGridW);
        int h = Mathf.Max(1, _spec.pixelLightGridH);
        float cell = 12f;
        var rect = GUILayoutUtility.GetRect(w * cell, h * cell);
        if (Event.current.type != EventType.Repaint) return;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            bool elbow = (x == 0 || x == w - 1) && (y == 0 || y == h - 1);
            bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
            Color c = elbow ? new Color(0.85f, 0.45f, 0.2f)
                : edge ? new Color(0.55f, 0.5f, 0.35f)
                : new Color(0.35f, 0.45f, 0.6f);
            EditorGUI.DrawRect(new Rect(rect.x + x * cell, rect.y + y * cell, cell - 1f, cell - 1f), c);
        }
    }
}
