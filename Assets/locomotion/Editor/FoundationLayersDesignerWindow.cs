using UnityEditor;
using UnityEngine;
using SdfMax;

public sealed class FoundationLayersDesignerWindow : EditorWindow
{
    HouseConstructionPlan _plan;
    int _paintLayer;
    int _paintFrame;
    HouseFoundationBrushKind _brush = HouseFoundationBrushKind.Paint;
    HouseFoundationEditorMode _mode = HouseFoundationEditorMode.Construction;
    WallBrushSpec _wallBrush;
    string _floorText = "first";
    Vector2 _scroll;
    float _cellDraw = 12f;
    Vector2Int? _selected;
    bool _dragging;

    [MenuItem("Locomotion/House Foundation Layers")]
    public static void Open()
    {
        var w = GetWindow<FoundationLayersDesignerWindow>("House Foundation");
        w.minSize = new Vector2(640, 480);
    }

    public static void OpenWith(HouseConstructionPlan plan, WallBrushCatalog catalog)
    {
        Open();
        var w = GetWindow<FoundationLayersDesignerWindow>();
        w._plan = plan;
        if (plan != null && catalog != null)
        {
            plan.wallBrushes = catalog;
            catalog.EnsureBuiltins();
        }
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _plan = (HouseConstructionPlan)EditorGUILayout.ObjectField("Plan", _plan, typeof(HouseConstructionPlan), false);
        if (GUILayout.Button("Create HouseConstructionPlan"))
        {
            var p = CreateInstance<HouseConstructionPlan>();
            p.EnsureDefaultLayers();
            var path = EditorUtility.SaveFilePanelInProject("Save House Construction Plan", "HouseConstructionPlan", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(p, path);
                _plan = p;
            }
        }

        if (_plan == null)
        {
            EditorGUILayout.EndScrollView();
            return;
        }

        _plan.EnsureDefaultLayers();
        _plan.wallBrushes = (WallBrushCatalog)EditorGUILayout.ObjectField(
            "Wall Brushes", _plan.wallBrushes, typeof(WallBrushCatalog), false);
        if (_plan.wallBrushes != null)
            _plan.wallBrushes.EnsureBuiltins();

        _floorText = EditorGUILayout.TextField("Floor", string.IsNullOrEmpty(_floorText) ? _plan.floorText : _floorText);
        if (GUILayout.Button("Apply Floor Text"))
        {
            _plan.floorText = _floorText;
            _plan.ApplyFloorText();
            _floorText = _plan.floorText;
        }

        var floor = _plan.GetOrCreateFloor(_plan.activeFloorIndex);
        floor.storyHeightM = EditorGUILayout.FloatField("Story Height", floor.storyHeightM);
        floor.finishFloorKind = (HouseFinishFloorKind)EditorGUILayout.EnumPopup("Finish Floor", floor.finishFloorKind);
        floor.pixelLightGridW = EditorGUILayout.IntField("PixelLight W", floor.pixelLightGridW);
        floor.pixelLightGridH = EditorGUILayout.IntField("PixelLight H", floor.pixelLightGridH);
        floor.pixelLightCellSize = EditorGUILayout.FloatField("PixelLight Cell", floor.pixelLightCellSize);

        DrawModeBar();
        DrawBrushBar();
        DrawWallBrushStamps();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Open House Construction Travel Agent"))
            HouseConstructionTravelAgentWindow.Open();
        if (GUILayout.Button("Open Wall Brush Designer"))
            WallBrushDesignerWindow.OpenWith(_plan.wallBrushes);
        EditorGUILayout.EndHorizontal();

        if (_plan.layers.Count > 0)
        {
            string[] names = new string[_plan.layers.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = _plan.layers[i].layerId;
            _paintLayer = EditorGUILayout.Popup("Layer", Mathf.Clamp(_paintLayer, 0, names.Length - 1), names);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("< Frame"))
            _paintFrame = Mathf.Max(0, _paintFrame - 1);
        EditorGUILayout.LabelField("Frame " + _paintFrame, GUILayout.Width(80));
        if (GUILayout.Button("Frame >"))
            _paintFrame = Mathf.Min(_plan.frameCount - 1, _paintFrame + 1);
        EditorGUILayout.EndHorizontal();

        DrawGrid();
        DrawSelectionInfo();

        if (GUILayout.Button("Stamp painted cells"))
            StampPaintedCells();

        if (GUILayout.Button("Bake Soft → Hard SDF"))
        {
            _plan.BakeSoftToHard();
            EditorUtility.SetDirty(_plan);
        }
        if (GUILayout.Button("SPH Foundation Pour (max slab)"))
        {
            var sph = new DigScoopSph();
            sph.BuildSubtractNode(Vector3.zero, 0.01f);
            _plan.hardSdf = SdfMaxSoftToHardBaker.BakeBoxUnionWithOpenings(
                new Vector3(_plan.width * 0.5f, 0.2f, _plan.height * 0.5f), null, 0.1f);
            EditorUtility.SetDirty(_plan);
        }

        EditorGUILayout.EndScrollView();
        if (GUI.changed)
            EditorUtility.SetDirty(_plan);
    }

    void DrawModeBar()
    {
        EditorGUILayout.LabelField("Mode (primaries + complements)", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        ModeButton(HouseFoundationEditorMode.Construction, "Construction");
        ModeButton(HouseFoundationEditorMode.Electrical, "Electrical");
        ModeButton(HouseFoundationEditorMode.Hvac, "HVAC");
        ModeButton(HouseFoundationEditorMode.Lighting, "Lighting");
        ModeButton(HouseFoundationEditorMode.Insulation, "Insulation");
        ModeButton(HouseFoundationEditorMode.Water, "Water");
        ModeButton(HouseFoundationEditorMode.Yard, "Yard");
        EditorGUILayout.EndHorizontal();
        if (_mode == HouseFoundationEditorMode.Lighting && GUILayout.Button("Open Window PixelLight Designer"))
        {
            var win = Selection.activeObject as WindowAssemblySpec;
            if (win != null)
                WindowPixelLightGridDesignerWindow.OpenWith(win);
            else
                WindowPixelLightGridDesignerWindow.Open();
        }
    }

    void ModeButton(HouseFoundationEditorMode mode, string label)
    {
        Color c = HouseFoundationPalette.ModeColor(mode);
        bool active = _mode == mode && _wallBrush == null;
        if (TintedButton(label, c, active))
        {
            bool again = _mode == mode;
            _mode = mode;
            _wallBrush = null;
            string layerId = HouseFoundationPalette.ModeLayerId(mode);
            SelectLayerId(layerId);
            if (again && mode == HouseFoundationEditorMode.Lighting)
            {
                var win = Selection.activeObject as WindowAssemblySpec;
                if (win != null)
                    WindowPixelLightGridDesignerWindow.OpenWith(win);
                else
                    WindowPixelLightGridDesignerWindow.Open();
            }
        }
    }

    void DrawBrushBar()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Brush", GUILayout.Width(44));
        BrushButton(HouseFoundationBrushKind.Select, "Select");
        BrushButton(HouseFoundationBrushKind.Paint, "Paint");
        BrushButton(HouseFoundationBrushKind.Erase, "Erase");
        if (GUILayout.Button("Add brush+!", GUILayout.Width(110)))
            AddBrushPlus();
        EditorGUILayout.EndHorizontal();

        Color brushColor = _wallBrush != null ? _wallBrush.color : HouseFoundationPalette.BrushColor(_brush);
        EditorGUILayout.BeginHorizontal();
        var swatch = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f));
        EditorGUI.DrawRect(swatch, brushColor);
        string wall = _wallBrush != null ? "  ·  " + _wallBrush.displayName : "";
        EditorGUILayout.LabelField(
            $"Active brush: {_brush}  ·  {_mode} ({HouseFoundationGridInfo.ColorName(HouseFoundationPalette.ModeColor(_mode))}){wall}",
            EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        if (_brush == HouseFoundationBrushKind.Select)
            EditorGUILayout.HelpBox("Select a grid square to inspect occupancy, floor, and PixelLight mapping.", MessageType.None);
        EditorGUILayout.EndVertical();
    }

    void DrawWallBrushStamps()
    {
        var catalog = _plan.wallBrushes;
        if (catalog == null || catalog.brushes == null || catalog.brushes.Count == 0)
            return;
        EditorGUILayout.LabelField("Wall brushes (discrete pieces)", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        int shown = 0;
        for (int i = 0; i < catalog.brushes.Count; i++)
        {
            var spec = catalog.brushes[i];
            if (spec == null) continue;
            if (shown > 0 && shown % 4 == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
            bool active = _wallBrush == spec;
            if (TintedButton(spec.displayName, spec.color, active))
                SelectWallBrush(spec);
            shown++;
        }
        EditorGUILayout.EndHorizontal();
    }

    void SelectWallBrush(WallBrushSpec spec)
    {
        _wallBrush = spec;
        _brush = HouseFoundationBrushKind.Paint;
        if (spec != null)
            SelectLayerId(spec.targetLayerId);
    }

    void SelectLayerId(string layerId)
    {
        if (string.IsNullOrEmpty(layerId) || _plan.layers == null) return;
        for (int i = 0; i < _plan.layers.Count; i++)
        {
            if (_plan.layers[i] != null && _plan.layers[i].layerId == layerId)
            {
                _paintLayer = i;
                break;
            }
        }
    }

    void AddBrushPlus()
    {
        var catalog = EnsureCatalogAsset();
        if (catalog == null) return;
        string layerId = "sheathing";
        if (_plan.layers != null && _plan.layers.Count > 0)
        {
            int i = Mathf.Clamp(_paintLayer, 0, _plan.layers.Count - 1);
            if (_plan.layers[i] != null && !string.IsNullOrEmpty(_plan.layers[i].layerId))
                layerId = _plan.layers[i].layerId;
        }
        var spec = catalog.AddBrush(HouseWallBrushKind.Custom, layerId);
        var path = EditorUtility.SaveFilePanelInProject("Save Wall Brush", spec.brushId, "asset", "Add brush+!");
        if (string.IsNullOrEmpty(path))
        {
            catalog.brushes.Remove(spec);
            DestroyImmediate(spec);
            return;
        }
        AssetDatabase.CreateAsset(spec, path);
        EditorUtility.SetDirty(catalog);
        EditorUtility.SetDirty(_plan);
        AssetDatabase.SaveAssets();
        SelectWallBrush(spec);
        WallBrushDesignerWindow.OpenWith(catalog);
    }

    WallBrushCatalog EnsureCatalogAsset()
    {
        if (_plan.wallBrushes != null)
        {
            _plan.wallBrushes.EnsureBuiltins();
            return _plan.wallBrushes;
        }
        var path = EditorUtility.SaveFilePanelInProject("Save Wall Brush Catalog", "WallBrushCatalog", "asset", "");
        if (string.IsNullOrEmpty(path)) return null;
        var cat = CreateInstance<WallBrushCatalog>();
        cat.EnsureBuiltins();
        AssetDatabase.CreateAsset(cat, path);
        for (int i = 0; i < cat.brushes.Count; i++)
        {
            var b = cat.brushes[i];
            if (b == null) continue;
            b.name = b.brushId;
            AssetDatabase.AddObjectToAsset(b, cat);
        }
        AssetDatabase.SaveAssets();
        _plan.wallBrushes = cat;
        EditorUtility.SetDirty(_plan);
        return cat;
    }

    void BrushButton(HouseFoundationBrushKind kind, string label)
    {
        bool paintActive = kind == HouseFoundationBrushKind.Paint && _brush == kind && _wallBrush == null;
        bool otherActive = kind != HouseFoundationBrushKind.Paint && _brush == kind;
        if (TintedButton(label, HouseFoundationPalette.BrushColor(kind), paintActive || otherActive))
        {
            _brush = kind;
            if (kind != HouseFoundationBrushKind.Paint)
                _wallBrush = null;
        }
    }

    static bool TintedButton(string label, Color color, bool active)
    {
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = active ? color : Color.Lerp(color, Color.black, 0.4f);
        bool hit = GUILayout.Button(active ? "● " + label : label);
        GUI.backgroundColor = old;
        return hit;
    }

    void DrawGrid()
    {
        if (_plan.layers.Count == 0) return;
        var layer = _plan.layers[Mathf.Clamp(_paintLayer, 0, _plan.layers.Count - 1)];
        int f = Mathf.Clamp(_paintFrame, 0, layer.frames.Count - 1);
        var frame = layer.frames[f];
        frame.EnsureSize(_plan.width, _plan.height);
        var rect = GUILayoutUtility.GetRect(_plan.width * _cellDraw, _plan.height * _cellDraw);
        Event e = Event.current;
        var catalog = _plan.wallBrushes;

        if (e.type == EventType.Repaint)
        {
            for (int y = 0; y < _plan.height; y++)
            for (int x = 0; x < _plan.width; x++)
            {
                var cell = new Rect(rect.x + x * _cellDraw, rect.y + y * _cellDraw, _cellDraw - 1f, _cellDraw - 1f);
                byte v = frame.Get(x, y, _plan.width);
                Color c = HouseFoundationPalette.ColorForCell(v, catalog);
                if (_selected.HasValue && _selected.Value.x == x && _selected.Value.y == y)
                    c = Color.Lerp(c, HouseFoundationPalette.SelectWhite, 0.45f);
                EditorGUI.DrawRect(cell, c);
            }

            if (_selected.HasValue)
            {
                int sx = _selected.Value.x;
                int sy = _selected.Value.y;
                var outline = new Rect(rect.x + sx * _cellDraw, rect.y + sy * _cellDraw, _cellDraw - 1f, _cellDraw - 1f);
                Handles.BeginGUI();
                Handles.color = HouseFoundationPalette.BrushColor(_brush);
                Handles.DrawSolidRectangleWithOutline(outline, Color.clear, HouseFoundationPalette.BrushColor(_brush));
                Handles.EndGUI();
            }
        }

        if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
        {
            _dragging = true;
            ApplyBrushAt(rect, e.mousePosition);
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.MouseDrag && _dragging && e.button == 0 && _brush != HouseFoundationBrushKind.Select)
        {
            if (rect.Contains(e.mousePosition))
            {
                ApplyBrushAt(rect, e.mousePosition);
                e.Use();
                Repaint();
            }
        }
        else if (e.type == EventType.MouseUp)
            _dragging = false;
    }

    void ApplyBrushAt(Rect grid, Vector2 mouse)
    {
        int x = Mathf.FloorToInt((mouse.x - grid.x) / _cellDraw);
        int y = Mathf.FloorToInt((mouse.y - grid.y) / _cellDraw);
        if (x < 0 || y < 0 || x >= _plan.width || y >= _plan.height)
            return;

        if (_brush == HouseFoundationBrushKind.Select)
        {
            _selected = new Vector2Int(x, y);
            return;
        }

        var layer = _plan.layers[Mathf.Clamp(_paintLayer, 0, _plan.layers.Count - 1)];
        int f = Mathf.Clamp(_paintFrame, 0, layer.frames.Count - 1);
        var frame = layer.frames[f];
        frame.EnsureSize(_plan.width, _plan.height);
        byte paint = 0;
        if (_brush == HouseFoundationBrushKind.Paint)
            paint = _wallBrush != null ? _wallBrush.paintByte : HouseFoundationPalette.PaintValue(_mode);
        frame.Set(x, y, _plan.width, paint);
        _selected = new Vector2Int(x, y);
        EditorUtility.SetDirty(_plan);
    }

    void StampPaintedCells()
    {
        var house = Object.FindFirstObjectByType<HousingBuildingRagdoll>();
        Transform fallback = null;
        if (house != null)
            fallback = house.transform;
        else
        {
            var root = GameObject.Find("wall_brushes");
            if (root == null)
                root = new GameObject("wall_brushes");
            fallback = root.transform;
        }
        int n = WallBrushCellStamp.StampOccupiedCells(_plan, _paintLayer, _paintFrame, fallback, house);
        Debug.Log("[House Foundation] Stamped " + n + " wall-brush pieces.");
    }

    void DrawSelectionInfo()
    {
        EditorGUILayout.Space(4f);
        string info;
        if (_selected.HasValue)
            info = HouseFoundationGridInfo.Describe(
                _plan, _selected.Value.x, _selected.Value.y, _paintLayer, _paintFrame, _brush, _mode);
        else
            info = HouseFoundationGridInfo.Describe(_plan, -1, -1, _paintLayer, _paintFrame, _brush, _mode);

        var style = new GUIStyle(EditorStyles.helpBox)
        {
            richText = false,
            wordWrap = true,
            fontSize = 11,
            padding = new RectOffset(8, 8, 6, 6)
        };
        EditorGUILayout.LabelField(info, style);
    }
}
