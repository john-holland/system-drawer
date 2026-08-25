using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public sealed class RoadLanesDesignerWindow : EditorWindow
{
    MonoBehaviour _spline;
    RoadLaneConfigAsset _config;
    CityPixelGrid _grid;
    CityPixelBrushKind _brush = CityPixelBrushKind.RoadLanes;
    CityPixelBrushStamp _stamp = new CityPixelBrushStamp();
    Vector2 _scroll;
    Vector2 _gridScroll;
    int _paintFrame;
    int _paintLayer;
    bool _stacked = true;
    bool _brushMode = true;
    bool _paintOn = true;
    float _cellDraw = 16f;
    bool _dragging;
    readonly CityPixelGridPaintStroke _paintStroke = new CityPixelGridPaintStroke();
    readonly CityPixelGridCellSelection _selection = new CityPixelGridCellSelection();

    static readonly CityPixelBrushKind[] HighwayBrushes =
    {
        CityPixelBrushKind.Select,
        CityPixelBrushKind.RoadLanes,
        CityPixelBrushKind.Overpass,
        CityPixelBrushKind.Bridge,
        CityPixelBrushKind.BridgeAndUnderpass,
        CityPixelBrushKind.StreetLight,
        CityPixelBrushKind.TrafficSignal,
        CityPixelBrushKind.PhonePole,
        CityPixelBrushKind.PedCallButton,
        CityPixelBrushKind.Crosswalk,
        CityPixelBrushKind.Sidewalk,
        CityPixelBrushKind.GrassStrip,
        CityPixelBrushKind.JerseyBarrier,
        CityPixelBrushKind.GuardRail,
        CityPixelBrushKind.WireEnd,
        CityPixelBrushKind.Debris,
        CityPixelBrushKind.Intersection,
        CityPixelBrushKind.StopSign,
        CityPixelBrushKind.Sign,
        CityPixelBrushKind.Eraser
    };

    [MenuItem("Locomotion/Road Lanes Designer")]
    public static void Open()
    {
        var w = GetWindow<RoadLanesDesignerWindow>("Road Lanes");
        w.minSize = new Vector2(640, 560);
    }

    void OnEnable() => CityPixelGridDesignerUndo.BindRepaint(this, OnUndoRedo);

    void OnDisable()
    {
        _paintStroke.End();
        CityPixelGridDesignerUndo.UnbindRepaint(OnUndoRedo);
    }

    void OnUndoRedo() => Repaint();

    void OnGUI()
    {
        if (CityPixelGridDesignerUndo.HandleSelectionHotkeys(_selection))
            Repaint();
        EndPaintStrokeIfMouseUp();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Road Lanes Designer", EditorStyles.boldLabel);
        CityPixelGridDesignerUndo.DrawToolbar();
        DrawBindings();
        DrawConfig();
        DrawPixelGridDesigner();
        DrawSceneActions();
        EditorGUILayout.EndScrollView();
        EndPaintStrokeIfMouseUp();
    }

    void EndPaintStrokeIfMouseUp()
    {
        Event e = Event.current;
        if (e == null) return;
        if (e.type != EventType.MouseUp && e.rawType != EventType.MouseUp) return;
        _dragging = false;
        _paintStroke.End();
        _selection.EndDrag();
    }

    void DrawBindings()
    {
        _spline = (MonoBehaviour)EditorGUILayout.ObjectField("Spline", _spline, typeof(MonoBehaviour), true);
        _config = (RoadLaneConfigAsset)EditorGUILayout.ObjectField("Lane Config", _config, typeof(RoadLaneConfigAsset), false);
        _grid = (CityPixelGrid)EditorGUILayout.ObjectField("City Pixel Grid", _grid, typeof(CityPixelGrid), false);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create Config"))
        {
            var a = CreateInstance<RoadLaneConfigAsset>();
            a.layout = new RoadLaneLayout();
            a.grid = new RoadLaneGridSettings();
            var path = EditorUtility.SaveFilePanelInProject("Save Road Lane Config", "RoadLaneConfig", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(a, path);
                Undo.RegisterCreatedObjectUndo(a, "Create Road Lane Config");
                _config = a;
            }
        }
        if (GUILayout.Button("Save JSON Export") && _config != null)
        {
            string path = EditorUtility.SaveFilePanel("Export Road Lane Config JSON", Application.dataPath, _config.name, "json");
            if (!string.IsNullOrEmpty(path))
                File.WriteAllText(path, _config.ToExportJson());
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawConfig()
    {
        if (_config == null) return;
        EditorGUILayout.LabelField("Lane / shoulder", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        int laneCount = EditorGUILayout.IntField("Lane Count", _config.layout.laneCount);
        float laneWidthM = EditorGUILayout.FloatField("Lane Width", _config.layout.laneWidthM);
        float followTimeSec = EditorGUILayout.FloatField("Follow Time", _config.grid.followTimeSec);
        float gridCarLengths = EditorGUILayout.FloatField("Grid Car Lengths", _config.grid.gridCarLengths);
        float occupancy01 = EditorGUILayout.Slider("Occupancy", _config.grid.occupancy01, 0f, 1f);
        float sidewalkWidthM = EditorGUILayout.FloatField("Sidewalk Width", _config.sidewalkWidthM);
        float sidewalkPaddingM = EditorGUILayout.FloatField("Sidewalk Padding", _config.sidewalkPaddingM);
        float mattingWidth01 = EditorGUILayout.Slider("Matting", _config.mattingWidth01, 0f, 1f);
        float curbHeightM = EditorGUILayout.FloatField("Curb Height", _config.curbHeightM);
        float curbWidthM = EditorGUILayout.FloatField("Curb Width", _config.curbWidthM);
        float dappleBevel01 = EditorGUILayout.Slider("Dapple Bevel", _config.dappleBevel01, 0f, 1f);
        float grassStripWidthM = EditorGUILayout.FloatField("Grass Strip Width", _config.grassStripWidthM);
        if (EditorGUI.EndChangeCheck())
        {
            CityPixelGridDesignerUndo.Record(_config, "Edit Lane Config");
            _config.layout.laneCount = laneCount;
            _config.layout.laneWidthM = laneWidthM;
            _config.grid.followTimeSec = followTimeSec;
            _config.grid.gridCarLengths = gridCarLengths;
            _config.grid.occupancy01 = occupancy01;
            _config.sidewalkWidthM = sidewalkWidthM;
            _config.sidewalkPaddingM = sidewalkPaddingM;
            _config.mattingWidth01 = mattingWidth01;
            _config.curbHeightM = curbHeightM;
            _config.curbWidthM = curbWidthM;
            _config.dappleBevel01 = dappleBevel01;
            _config.grassStripWidthM = grassStripWidthM;
        }
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Recipe: Bridge")) ApplyRecipe(RoadLaneConfigAsset.CreateBridgeRecipe, "Apply Bridge Recipe");
        if (GUILayout.Button("Recipe: Overpass")) ApplyRecipe(RoadLaneConfigAsset.CreateOverpassRecipe, "Apply Overpass Recipe");
        if (GUILayout.Button("Recipe: Bridge+Under")) ApplyRecipe(RoadLaneConfigAsset.CreateBridgeAndUnderpassRecipe, "Apply Bridge+Underpass Recipe");
        EditorGUILayout.EndHorizontal();
    }

    void ApplyRecipe(System.Func<RoadLaneConfigAsset> create, string undoName)
    {
        var tmp = create();
        CityPixelGridDesignerUndo.Record(_config, undoName);
        _config.recipe = new System.Collections.Generic.List<RoadLaneBrushLayerOp>(tmp.recipe);
        DestroyImmediate(tmp);
    }

    void DrawPixelGridDesigner()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Pixel grid (highway section)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Same click-to-paint grid as City Pixel Grid Designer. X = across lanes / shoulder, Y = along the ribbon.",
            MessageType.None);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create Highway Grid Asset"))
            CreateHighwayGrid();
        if (GUILayout.Button("Align Origin / Size From Spline") && _grid != null)
            AlignGridFromSpline();
        if (GUILayout.Button("Open City Pixel Grid Designer") && _grid != null)
            CityPixelGridDesignerWindow.Open(_grid);
        EditorGUILayout.EndHorizontal();

        if (_grid == null)
        {
            EditorGUILayout.HelpBox("Assign or create a CityPixelGrid to paint lanes, lights, sidewalks, and crosswalks.", MessageType.Info);
            return;
        }

        _grid.EnsureHighwayLayers();
        _grid.worldOrigin = CityPixelGridDesignerUndo.Draw(
            _grid, "Move Lane Grid Origin", _grid.worldOrigin,
            v => EditorGUILayout.Vector3Field("World Origin", v));
        EditorGUILayout.BeginHorizontal();
        _grid.width = Mathf.Max(1, CityPixelGridDesignerUndo.Draw(
            _grid, "Resize Lane Grid", _grid.width,
            v => EditorGUILayout.IntField("Width (across)", v)));
        _grid.height = Mathf.Max(1, CityPixelGridDesignerUndo.Draw(
            _grid, "Resize Lane Grid", _grid.height,
            v => EditorGUILayout.IntField("Height (along)", v)));
        EditorGUILayout.EndHorizontal();
        _grid.cellWorldSize = CityPixelGridDesignerUndo.Draw(
            _grid, "Lane Grid Cell Size", _grid.cellWorldSize,
            v => EditorGUILayout.FloatField("Cell World Size", v));
        int frames = CityPixelGridDesignerUndo.Draw(
            _grid, "Lane Grid Frame Count", _grid.frameCount,
            v => EditorGUILayout.IntSlider("Frame Count", Mathf.Max(1, v), 1, 64));
        if (frames != _grid.frameCount)
            _grid.frameCount = frames;
        _cellDraw = EditorGUILayout.Slider("Cell Draw Size", _cellDraw, 6f, 28f);
        _grid.EnsureLayersAndFrames();

        if (_grid.layers.Count > 0)
        {
            string[] names = new string[_grid.layers.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = _grid.layers[i].layerId + " (" + _grid.layers[i].kind + ")";
            _paintLayer = Mathf.Clamp(_paintLayer, 0, _grid.layers.Count - 1);
            _paintLayer = EditorGUILayout.Popup("Layer", _paintLayer, names);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Prev Frame"))
            _paintFrame = Mathf.Max(0, _paintFrame - 1);
        _paintFrame = EditorGUILayout.IntField("Frame", _paintFrame);
        if (GUILayout.Button("Next Frame"))
        {
            if (_paintFrame >= _grid.frameCount - 1)
            {
                CityPixelGridDesignerUndo.RecordComplete(_grid, "Add Lane Grid Frame");
                _grid.frameCount++;
                _grid.EnsureLayersAndFrames();
            }
            _paintFrame++;
        }
        EditorGUILayout.EndHorizontal();
        _paintFrame = Mathf.Clamp(_paintFrame, 0, Mathf.Max(0, _grid.frameCount - 1));

        _brushMode = EditorGUILayout.Toggle("Brush Mode (stamps)", _brushMode);
        _stacked = EditorGUILayout.Toggle("Stacked stamps (overlap → higher floor)", _stacked);
        if (!_brushMode)
            _paintOn = EditorGUILayout.Toggle("Paint On", _paintOn);
        else
        {
            int bi = System.Array.IndexOf(HighwayBrushes, _brush);
            int shown = bi < 0 ? 0 : bi;
            string[] labels = new string[HighwayBrushes.Length];
            for (int i = 0; i < HighwayBrushes.Length; i++)
                labels[i] = HighwayBrushes[i].ToString();
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUILayout.Popup("Highway Brush", shown, labels);
            if (EditorGUI.EndChangeCheck())
                _brush = HighwayBrushes[picked];
            _brush = (CityPixelBrushKind)EditorGUILayout.EnumPopup("All Brushes", _brush);
            if (_brush != CityPixelBrushKind.Eraser && _brush != CityPixelBrushKind.Select)
            {
                EditorGUILayout.BeginVertical("box");
                CityPixelBrushEditors.DrawBrushOptions(_brush, ref _stamp);
                EditorGUILayout.EndVertical();
            }
            else if (_brush == CityPixelBrushKind.Select)
                CityPixelBrushEditors.DrawBrushOptions(_brush, ref _stamp);
            if (_brush != CityPixelBrushKind.Select)
            {
                _stamp.kind = _brush;
                _stamp.laneConfig = _config;
            }
        }

        DrawLaneStripLegend();
        CityPixelGridDesignerUndo.DrawSelectionBar(
            _selection, _grid, _paintFrame,
            _brushMode && _brush != CityPixelBrushKind.Select,
            EraseSelectedCells, PaintSelectedCells);
        DrawGrid();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Bake MST (This Frame)"))
        {
            CityPixelGridDesignerUndo.RecordComplete(_grid, "Bake Lane Grid MST");
            CityPixelGridBaker.BakeFrame(_grid, _paintFrame);
        }
        if (GUILayout.Button("Mark Dirty"))
            EditorUtility.SetDirty(_grid);
        EditorGUILayout.EndHorizontal();
    }

    void DrawLaneStripLegend()
    {
        int lanes = _config != null ? Mathf.Max(1, _config.layout.laneCount) : 0;
        if (lanes <= 0) return;
        EditorGUILayout.LabelField($"Strip: {_grid.width} across × {_grid.height} along  ·  {lanes} lane(s)  ·  cell {_grid.cellWorldSize:0.##} m");
    }

    void DrawGrid()
    {
        if (_grid.layers == null || _grid.layers.Count == 0)
        {
            EditorGUILayout.HelpBox("Grid has no layers. Click Create Highway Grid Asset.", MessageType.Warning);
            return;
        }

        float w = _grid.width * _cellDraw;
        float h = _grid.height * _cellDraw;
        _gridScroll = EditorGUILayout.BeginScrollView(_gridScroll, GUILayout.Height(Mathf.Min(440, h + 24)));
        var rect = GUILayoutUtility.GetRect(w, h);

        var layer = _grid.layers[Mathf.Clamp(_paintLayer, 0, _grid.layers.Count - 1)];
        if (layer.frames == null || layer.frames.Count == 0)
        {
            EditorGUILayout.EndScrollView();
            return;
        }
        var frame = layer.frames[Mathf.Clamp(_paintFrame, 0, layer.frames.Count - 1)];
        frame.EnsureSize(_grid.width, _grid.height);

        int lanes = _config != null ? Mathf.Max(1, _config.layout.laneCount) : 0;
        int lane0 = lanes > 0 ? Mathf.Max(0, (_grid.width - lanes) / 2) : -1;

        for (int y = 0; y < _grid.height; y++)
        for (int x = 0; x < _grid.width; x++)
        {
            var r = new Rect(rect.x + x * _cellDraw, rect.y + y * _cellDraw, _cellDraw - 1, _cellDraw - 1);
            Color c = layer.color * 0.35f;
            if (frame.Get(x, y, _grid.width) != 0)
                c = layer.color;
            if (lanes > 0 && x >= lane0 && x < lane0 + lanes)
                c = Color.Lerp(c, new Color(0.28f, 0.28f, 0.32f), 0.35f);

            var stamp = FindStamp(_paintFrame, x, y);
            if (stamp != null)
                c = Color.Lerp(c, CityPixelGrid.BrushColor(stamp.kind), 0.75f);
            c = CityPixelGridDesignerUndo.OverlaySelection(c, _selection, x, y);

            EditorGUI.DrawRect(r, c);
            if (lanes > 0 && (x == lane0 || x == lane0 + lanes))
                EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), new Color(1f, 1f, 1f, 0.35f));
            if (_selection.Contains(x, y))
                EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), CityPixelGridDesignerUndo.SelectionTint);

            Event e = Event.current;
            if (e != null && r.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    _dragging = true;
                    HitCell(x, y, true);
                    e.Use();
                    Repaint();
                }
                else if (e.type == EventType.MouseDrag && _dragging && e.button == 0)
                {
                    HitCell(x, y, false);
                    e.Use();
                    Repaint();
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    CityPixelBrushStamp FindStamp(int frame, int x, int y)
    {
        if (_grid.brushStamps == null) return null;
        CityPixelBrushStamp best = null;
        int bestFloor = int.MinValue;
        for (int i = 0; i < _grid.brushStamps.Count; i++)
        {
            var s = _grid.brushStamps[i];
            if (s == null || s.frameIndex != frame || s.cellX != x || s.cellY != y) continue;
            if (s.floorIndex >= bestFloor)
            {
                bestFloor = s.floorIndex;
                best = s;
            }
        }
        return best;
    }

    void HitCell(int x, int y, bool mouseDown)
    {
        if (_brushMode && _brush == CityPixelBrushKind.Select)
        {
            Event e = Event.current;
            bool add = e != null && e.shift;
            bool toggle = e != null && (e.control || e.command);
            if (mouseDown) _selection.Begin(x, y, add, toggle);
            else _selection.DragTo(x, y);
            return;
        }
        PaintAt(x, y);
    }

    void PaintAt(int x, int y)
    {
        _paintStroke.Begin(_grid, _brushMode && _brush == CityPixelBrushKind.Eraser ? "Erase Lane Grid" : "Paint Lane Grid");
        ApplyPaint(x, y);
    }

    void ApplyPaint(int x, int y)
    {
        if (_brushMode)
        {
            if (_brush == CityPixelBrushKind.Select)
                return;
            if (_brush == CityPixelBrushKind.Eraser)
                _grid.ClearBrushStamp(_paintFrame, x, y);
            else if (_config != null &&
                     (_brush == CityPixelBrushKind.Bridge ||
                      _brush == CityPixelBrushKind.BridgeAndUnderpass ||
                      _brush == CityPixelBrushKind.Overpass ||
                      _brush == CityPixelBrushKind.RoadLanes))
            {
                CityPixelRecipeApplier.Apply(_grid, _config, _paintFrame, x, y);
            }
            else
            {
                var stamp = CityPixelBrushEditors.CloneStampTemplate(_stamp, _paintFrame, x, y);
                stamp.kind = _brush;
                stamp.laneConfig = _config;
                if (_stacked) _grid.SetBrushStampStacked(stamp);
                else _grid.SetBrushStamp(stamp);
                PaintLayerForBrush(_brush, x, y);
            }
            EditorUtility.SetDirty(_grid);
            return;
        }

        var layer = _grid.layers[Mathf.Clamp(_paintLayer, 0, _grid.layers.Count - 1)];
        var frame = layer.frames[Mathf.Clamp(_paintFrame, 0, layer.frames.Count - 1)];
        frame.EnsureSize(_grid.width, _grid.height);
        frame.Set(x, y, _grid.width, _paintOn ? (byte)1 : (byte)0);
        EditorUtility.SetDirty(_grid);
    }

    void EraseSelectedCells()
    {
        if (_grid == null || _selection.Count == 0) return;
        _paintStroke.Begin(_grid, "Erase Selected Lane Cells");
        foreach (var c in _selection.Cells)
            _grid.ClearBrushStamp(_paintFrame, c.x, c.y);
        _paintStroke.End();
        Repaint();
    }

    void PaintSelectedCells()
    {
        if (_grid == null || _selection.Count == 0 || _brush == CityPixelBrushKind.Select) return;
        var cells = new List<Vector2Int>(_selection.Cells);
        _paintStroke.Begin(_grid, "Paint Selected Lane Cells");
        for (int i = 0; i < cells.Count; i++)
            ApplyPaint(cells[i].x, cells[i].y);
        _paintStroke.End();
        Repaint();
    }

    void PaintLayerForBrush(CityPixelBrushKind kind, int x, int y)
    {
        switch (kind)
        {
            case CityPixelBrushKind.StreetLight:
            case CityPixelBrushKind.TrafficSignal:
                _grid.PaintLayerCell(CityPixelLayerKind.StreetLight, _paintFrame, x, y);
                break;
            case CityPixelBrushKind.Crosswalk:
            case CityPixelBrushKind.RoadLanes:
                _grid.PaintLayerCell(CityPixelLayerKind.Highway, _paintFrame, x, y);
                break;
            case CityPixelBrushKind.Sidewalk:
                _grid.PaintLayerCell(CityPixelLayerKind.Sidewalk, _paintFrame, x, y);
                break;
            case CityPixelBrushKind.GrassStrip:
                _grid.PaintLayerCell(CityPixelLayerKind.GrassStrip, _paintFrame, x, y);
                break;
            case CityPixelBrushKind.Debris:
                _grid.PaintLayerCell(CityPixelLayerKind.Debris, _paintFrame, x, y);
                break;
            case CityPixelBrushKind.Overpass:
            case CityPixelBrushKind.Bridge:
                _grid.PaintLayerCell(CityPixelLayerKind.Overpass, _paintFrame, x, y);
                break;
        }
    }

    void CreateHighwayGrid()
    {
        var g = CreateInstance<CityPixelGrid>();
        g.EnsureHighwayLayers();
        int lanes = _config != null ? Mathf.Max(2, _config.layout.laneCount) : 4;
        g.width = lanes + 4;
        g.height = 24;
        g.cellWorldSize = _config != null ? _config.layout.laneWidthM : 3.5f;
        AlignGridFromSpline(g);
        var path = EditorUtility.SaveFilePanelInProject("Save Highway Pixel Grid", "RoadLanesHighwayGrid", "asset", "");
        if (string.IsNullOrEmpty(path))
        {
            DestroyImmediate(g);
            return;
        }
        AssetDatabase.CreateAsset(g, path);
        Undo.RegisterCreatedObjectUndo(g, "Create Highway Grid");
        _grid = g;
    }

    void AlignGridFromSpline() => AlignGridFromSpline(_grid);

    void AlignGridFromSpline(CityPixelGrid grid)
    {
        if (grid == null || _spline == null) return;
        if (EditorUtility.IsPersistent(grid))
            CityPixelGridDesignerUndo.RecordComplete(grid, "Align Lane Grid From Spline");
        Vector3 origin = _spline.transform.position;
        float length = ReadSplineLength(_spline);
        float cell = _config != null ? Mathf.Max(0.5f, _config.layout.laneWidthM) : grid.cellWorldSize;
        int lanes = _config != null ? Mathf.Max(1, _config.layout.laneCount) : 2;
        grid.cellWorldSize = cell;
        grid.width = Mathf.Max(grid.width, lanes + 4);
        if (length > 0.5f)
            grid.height = Mathf.Max(8, Mathf.CeilToInt(length / cell));
        grid.worldOrigin = origin - new Vector3(grid.width * cell * 0.5f, 0f, 0f);
        grid.EnsureHighwayLayers();
        EditorUtility.SetDirty(grid);
    }

    static float ReadSplineLength(MonoBehaviour spline)
    {
        if (spline == null) return 0f;
        var getLen = spline.GetType().GetMethod("GetTotalLength", BindingFlags.Instance | BindingFlags.Public);
        if (getLen != null)
            return (float)getLen.Invoke(spline, null);
        var field = spline.GetType().GetField("controlPoints");
        if (field?.GetValue(spline) is IList<Vector3> pts && pts.Count >= 2)
        {
            float len = 0f;
            for (int i = 1; i < pts.Count; i++)
                len += Vector3.Distance(pts[i - 1], pts[i]);
            return len;
        }
        return 0f;
    }

    void DrawSceneActions()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
        if (GUILayout.Button("Scan debris along spline") && _spline != null && _config != null)
            ScanDebris();
        if (GUILayout.Button("Create Intersection Lot from spline"))
            CreateIntersectionLot();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Pull phone-wire associations"))
            PhoneWireContinuuuumClient.Pull();
        if (GUILayout.Button("Push scene indexes"))
            PhoneWireContinuuuumClient.PushScene();
        EditorGUILayout.EndHorizontal();
        if (_spline != null && GUILayout.Button("Bind RoadLaneSplineBinding"))
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Bind Road Lanes");
            var bind = _spline.GetComponent<RoadLaneSplineBinding>();
            if (bind == null)
                bind = Undo.AddComponent<RoadLaneSplineBinding>(_spline.gameObject);
            else
                Undo.RecordObject(bind, "Bind Road Lanes");
            bind.config = _config;
            if (_config != null)
            {
                bind.layout = _config.layout;
                bind.grid = _config.grid;
            }
            EditorUtility.SetDirty(bind);
            Undo.CollapseUndoOperations(group);
        }
    }

    void ScanDebris()
    {
        var hits = RoadDebrisSizeSolver.ScanRibbon(_spline.transform.position, new Vector3(8f, 4f, 40f), _spline.transform.rotation, ~0);
        var defs = RoadDebrisSizeSolver.FromScanHits(hits);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Scan Lane Debris");
        CityPixelGridDesignerUndo.Record(_config, "Scan Lane Debris");
        if (_grid != null)
            CityPixelGridDesignerUndo.RecordComplete(_grid, "Scan Lane Debris");
        _config.debris = defs;
        var checkpoint = new InteractedObjectCheckpoint();
        if (_grid != null)
        {
            _grid.EnsureHighwayLayers();
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null) continue;
                checkpoint.RememberFirstSeen(hits[i].gameObject);
                if (_grid.WorldToCell(hits[i].bounds.center, out int x, out int y))
                {
                    _grid.SetBrushStampStacked(new CityPixelBrushStamp
                    {
                        frameIndex = _paintFrame,
                        cellX = x,
                        cellY = y,
                        kind = CityPixelBrushKind.Debris,
                        signPrefab = hits[i].gameObject
                    });
                    _grid.PaintLayerCell(CityPixelLayerKind.Debris, _paintFrame, x, y);
                }
            }
            EditorUtility.SetDirty(_grid);
        }
        EditorUtility.SetDirty(_config);
        Undo.CollapseUndoOperations(group);
    }

    void CreateIntersectionLot()
    {
        var go = new GameObject("IntersectionLot");
        if (_spline != null)
            go.transform.position = _spline.transform.position;
        var pad = go.AddComponent<RoadLot>();
        pad.lotKind = RoadLotKind.Intersection;
        var lot = go.AddComponent<IntersectionLot>();
        lot.pad = pad;
        lot.EnsureFourLegs(new[] { "n", "e", "s", "w" });
        lot.intersectionCard = TAIntersectionCard.Generate(go.transform.position);
        lot.intersectionCard.BindLot(lot);
        PhoneWireContinuuuumClient.AutoFill(lot);
        Undo.RegisterCreatedObjectUndo(go, "Create Intersection Lot");
        Selection.activeGameObject = go;
    }
}
