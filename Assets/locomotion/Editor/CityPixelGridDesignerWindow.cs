using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEditor;
using UnityEngine;

public sealed class CityPixelGridDesignerWindow : EditorWindow
{
    CityPixelGrid _grid;
    NarrativeCalendarAsset _exportCalendar;
    int _paintLayer;
    int _paintFrame;
    bool _brushMode;
    bool _paintOn = true;
    CityPixelBrushKind _brushKind = CityPixelBrushKind.Sign;
    CityPixelBrushStamp _brushTemplate = new CityPixelBrushStamp();
    Vector2 _scroll;
    Vector2 _gridScroll;
    float _cellDraw = 12f;
    bool _dragging;
    readonly CityPixelGridPaintStroke _paintStroke = new CityPixelGridPaintStroke();
    readonly CityPixelGridCellSelection _selection = new CityPixelGridCellSelection();

    [MenuItem("Locomotion/City Pixel Grid Designer")]
    public static void Open()
    {
        var w = GetWindow<CityPixelGridDesignerWindow>("City Pixel Grid");
        w.minSize = new Vector2(640, 480);
    }

    public static void Open(CityPixelGrid grid)
    {
        Open();
        var w = GetWindow<CityPixelGridDesignerWindow>("City Pixel Grid");
        w._grid = grid;
        w.Repaint();
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
        EditorGUILayout.LabelField("City Pixel Grid Designer", EditorStyles.boldLabel);
        CityPixelGridDesignerUndo.DrawToolbar();

        _grid = (CityPixelGrid)EditorGUILayout.ObjectField("Grid", _grid, typeof(CityPixelGrid), false);
        if (GUILayout.Button("Create New CityPixelGrid Asset"))
        {
            var g = CreateInstance<CityPixelGrid>();
            g.EnsureLayersAndFrames();
            var path = EditorUtility.SaveFilePanelInProject("Save City Pixel Grid", "CityPixelGrid", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(g, path);
                Undo.RegisterCreatedObjectUndo(g, "Create City Pixel Grid");
                _grid = g;
            }
        }

        if (_grid == null)
        {
            EditorGUILayout.HelpBox("Assign or create a CityPixelGrid asset.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        _grid.EnsureLayersAndFrames();
        DrawHeader();
        DrawModeAndBrush();
        DrawFrameBar();
        CityPixelGridDesignerUndo.DrawSelectionBar(
            _selection, _grid, _paintFrame,
            _brushMode && _brushKind != CityPixelBrushKind.Select,
            EraseSelectedCells, PaintSelectedCells);
        DrawGrid();
        DrawActions();

        if (GUILayout.Button("Mark Dirty"))
            EditorUtility.SetDirty(_grid);

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

    void DrawHeader()
    {
        _grid.worldOrigin = CityPixelGridDesignerUndo.Draw(
            _grid, "Move City Pixel Grid Origin", _grid.worldOrigin,
            v => EditorGUILayout.Vector3Field("World Origin", v));
        EditorGUILayout.BeginHorizontal();
        _grid.width = CityPixelGridDesignerUndo.Draw(
            _grid, "Resize City Pixel Grid", _grid.width,
            v => EditorGUILayout.IntField("Width", v));
        _grid.height = CityPixelGridDesignerUndo.Draw(
            _grid, "Resize City Pixel Grid", _grid.height,
            v => EditorGUILayout.IntField("Height", v));
        EditorGUILayout.EndHorizontal();
        _grid.cellWorldSize = CityPixelGridDesignerUndo.Draw(
            _grid, "City Pixel Grid Cell Size", _grid.cellWorldSize,
            v => EditorGUILayout.FloatField("Cell World Size", v));
        _grid.frameGranularitySec = CityPixelGridDesignerUndo.Draw(
            _grid, "City Pixel Grid Granularity", _grid.frameGranularitySec,
            v => EditorGUILayout.FloatField("Granularity (sec/frame)", v));
        int frames = CityPixelGridDesignerUndo.Draw(
            _grid, "City Pixel Grid Frame Count", _grid.frameCount,
            v => EditorGUILayout.IntSlider("Frame Count", Mathf.Max(1, v), 1, 64));
        if (frames != _grid.frameCount)
            _grid.frameCount = frames;
        _grid.catalog = CityPixelGridDesignerUndo.Draw(
            _grid, "City Pixel Grid Catalog", _grid.catalog,
            v => (CityPlaceableCatalog)EditorGUILayout.ObjectField(
                "Placeable Catalog", v, typeof(CityPlaceableCatalog), false));
        _cellDraw = EditorGUILayout.Slider("Cell Draw Size", _cellDraw, 4f, 24f);

        EditorGUILayout.LabelField(
            $"World size: {_grid.width * _grid.cellWorldSize:F1} x {_grid.height * _grid.cellWorldSize:F1} m");

        SerializedObject so = new SerializedObject(_grid);
        SerializedProperty actors = so.FindProperty("actorsForSizing");
        if (actors != null)
        {
            EditorGUILayout.PropertyField(actors, true);
            so.ApplyModifiedProperties();
        }

        if (GUILayout.Button("Resize Cell From Actors"))
        {
            CityPixelGridDesignerUndo.Record(_grid, "Resize City Pixel Grid Cell");
            float c = _grid.RecalculateCellSize();
            Debug.Log($"[CityPixel] cellWorldSize = {c}");
        }

        if (_grid.layers.Count > 0)
        {
            string[] names = new string[_grid.layers.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = _grid.layers[i].layerId + " (" + _grid.layers[i].kind + ")";
            _paintLayer = Mathf.Clamp(_paintLayer, 0, _grid.layers.Count - 1);
            _paintLayer = EditorGUILayout.Popup("Layer", _paintLayer, names);
        }

        if (GUILayout.Button("Add Layer Preset: PowerLinesDown"))
        {
            CityPixelGridDesignerUndo.RecordComplete(_grid, "Add PowerLinesDown Layer");
            _grid.layers.Add(new CityPixelLayer
            {
                layerId = "power_lines_down",
                kind = CityPixelLayerKind.PowerLinesDown,
                color = new Color(0.9f, 0.7f, 0.1f)
            });
            _grid.EnsureLayersAndFrames();
        }
        if (GUILayout.Button("Add Prison Cell Layers"))
        {
            CityPixelGridDesignerUndo.RecordComplete(_grid, "Add Prison Cell Layers");
            _grid.EnsurePrisonLayers();
        }
        if (GUILayout.Button("Add House Street / Yard / Side Layers"))
        {
            CityPixelGridDesignerUndo.RecordComplete(_grid, "Add House Layers");
            _grid.EnsureHouseLayers();
        }
        if (GUILayout.Button("Add Campus Quad / Path / Building Layers"))
        {
            CityPixelGridDesignerUndo.RecordComplete(_grid, "Add Campus Layers");
            _grid.EnsureCampusLayers();
        }
        if (GUILayout.Button("Add Courtroom Bench / Well / Jury / Gallery / Bar Layers"))
        {
            CityPixelGridDesignerUndo.RecordComplete(_grid, "Add Courtroom Layers");
            _grid.EnsureCourtroomLayers();
        }
    }

    void DrawModeAndBrush()
    {
        _brushMode = EditorGUILayout.Toggle("Brush Mode (cards)", _brushMode);
        if (!_brushMode)
        {
            _paintOn = EditorGUILayout.Toggle("Paint On", _paintOn);
            return;
        }

        _brushKind = (CityPixelBrushKind)EditorGUILayout.EnumPopup("Brush", _brushKind);
        if (_brushKind == CityPixelBrushKind.Eraser)
        {
            EditorGUILayout.HelpBox("Eraser clears brush stamps at clicked cells.", MessageType.None);
            return;
        }
        if (_brushKind == CityPixelBrushKind.Select)
        {
            CityPixelBrushEditors.DrawBrushOptions(_brushKind, ref _brushTemplate);
            return;
        }

        EditorGUILayout.BeginVertical("box");
        CityPixelBrushEditors.DrawBrushOptions(_brushKind, ref _brushTemplate);
        EditorGUILayout.EndVertical();
    }

    void DrawFrameBar()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Prev Frame"))
            _paintFrame = Mathf.Max(0, _paintFrame - 1);
        _paintFrame = EditorGUILayout.IntField("Frame", _paintFrame);
        if (GUILayout.Button("Next Frame"))
        {
            if (_paintFrame >= _grid.frameCount - 1)
            {
                CityPixelGridDesignerUndo.RecordComplete(_grid, "Add City Pixel Grid Frame");
                _grid.frameCount++;
                _grid.EnsureLayersAndFrames();
            }
            _paintFrame++;
        }
        if (GUILayout.Button("Add Frame"))
        {
            CityPixelGridDesignerUndo.RecordComplete(_grid, "Add City Pixel Grid Frame");
            _grid.frameCount++;
            _grid.EnsureLayersAndFrames();
            _paintFrame = _grid.frameCount - 1;
        }
        EditorGUILayout.EndHorizontal();
        _paintFrame = Mathf.Clamp(_paintFrame, 0, Mathf.Max(0, _grid.frameCount - 1));
        _paintFrame = EditorGUILayout.IntSlider(_paintFrame, 0, Mathf.Max(0, _grid.frameCount - 1));
    }

    void DrawGrid()
    {
        float w = _grid.width * _cellDraw;
        float h = _grid.height * _cellDraw;
        _gridScroll = EditorGUILayout.BeginScrollView(_gridScroll, GUILayout.Height(Mathf.Min(420, h + 20)));
        var rect = GUILayoutUtility.GetRect(w, h);

        var layer = _grid.layers[Mathf.Clamp(_paintLayer, 0, _grid.layers.Count - 1)];
        var frame = layer.frames[Mathf.Clamp(_paintFrame, 0, layer.frames.Count - 1)];
        frame.EnsureSize(_grid.width, _grid.height);

        for (int y = 0; y < _grid.height; y++)
        for (int x = 0; x < _grid.width; x++)
        {
            var r = new Rect(rect.x + x * _cellDraw, rect.y + y * _cellDraw, _cellDraw - 1, _cellDraw - 1);
            Color c = layer.color * 0.35f;
            if (frame.Get(x, y, _grid.width) != 0)
                c = layer.color;

            // Stamp overlay
            var stamp = FindStamp(_paintFrame, x, y);
            if (stamp != null)
                c = Color.Lerp(c, CityPixelGrid.BrushColor(stamp.kind), 0.75f);
            c = CityPixelGridDesignerUndo.OverlaySelection(c, _selection, x, y);

            EditorGUI.DrawRect(r, c);
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

        if (Event.current != null && Event.current.type == EventType.MouseUp)
        {
            _dragging = false;
            _paintStroke.End();
            _selection.EndDrag();
        }

        EditorGUILayout.EndScrollView();
    }

    CityPixelBrushStamp FindStamp(int frame, int x, int y)
    {
        if (_grid.brushStamps == null) return null;
        for (int i = 0; i < _grid.brushStamps.Count; i++)
        {
            var s = _grid.brushStamps[i];
            if (s.frameIndex == frame && s.cellX == x && s.cellY == y)
                return s;
        }
        return null;
    }

    void HitCell(int x, int y, bool mouseDown)
    {
        if (_brushMode && _brushKind == CityPixelBrushKind.Select)
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
        _paintStroke.Begin(_grid, _brushMode && _brushKind == CityPixelBrushKind.Eraser ? "Erase City Pixel Grid" : "Paint City Pixel Grid");
        ApplyPaint(x, y);
    }

    void ApplyPaint(int x, int y)
    {
        if (_brushMode)
        {
            if (_brushKind == CityPixelBrushKind.Select)
                return;
            if (_brushKind == CityPixelBrushKind.Eraser)
                _grid.ClearBrushStamp(_paintFrame, x, y);
            else
            {
                var stamp = CityPixelBrushEditors.CloneStampTemplate(_brushTemplate, _paintFrame, x, y);
                stamp.kind = _brushKind;
                _grid.SetBrushStamp(stamp);
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
        _paintStroke.Begin(_grid, "Erase Selected City Pixel Cells");
        foreach (var c in _selection.Cells)
            _grid.ClearBrushStamp(_paintFrame, c.x, c.y);
        _paintStroke.End();
        Repaint();
    }

    void PaintSelectedCells()
    {
        if (_grid == null || _selection.Count == 0 || _brushKind == CityPixelBrushKind.Select) return;
        var cells = new List<Vector2Int>(_selection.Cells);
        _paintStroke.Begin(_grid, "Paint Selected City Pixel Cells");
        for (int i = 0; i < cells.Count; i++)
            ApplyPaint(cells[i].x, cells[i].y);
        _paintStroke.End();
        Repaint();
    }

    void DrawActions()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Bake MST Cache (This Frame)"))
        {
            CityPixelGridDesignerUndo.RecordComplete(_grid, "Bake City Pixel Grid MST");
            CityPixelGridBaker.BakeFrame(_grid, _paintFrame);
            Debug.Log($"[CityPixel] Baked frame {_paintFrame}");
        }
        if (GUILayout.Button("Bake MST Cache (All Frames)"))
        {
            CityPixelGridDesignerUndo.RecordComplete(_grid, "Bake City Pixel Grid MST");
            CityPixelGridBaker.BakeAllFrames(_grid);
            Debug.Log("[CityPixel] Baked all frames");
        }
        EditorGUILayout.EndHorizontal();

        _exportCalendar = (NarrativeCalendarAsset)EditorGUILayout.ObjectField(
            "Export Calendar", _exportCalendar, typeof(NarrativeCalendarAsset), true);
        if (GUILayout.Button("Export Narrative Events") && _exportCalendar != null)
        {
            int n = CityPixelGridRuntime.ExportNarrativeEvents(_grid, _exportCalendar);
            EditorUtility.SetDirty(_exportCalendar);
            Debug.Log($"[CityPixel] Exported/updated {n} narrative events");
        }
        if (GUILayout.Button("Export Prison Cell/Door/Wall Bounds4"))
        {
            var vols = _grid.ExportPrisonClustersToBounds4(_paintFrame);
            Debug.Log($"[CityPixel] Prison Bounds4 clusters: {vols.Count}");
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Open Pixel Light Designer"))
            PixelLightTimedDesignerWindow.Open();
        if (GUILayout.Button("Open LadderLogic Editor"))
            LadderLogicDesignerWindow.OpenWith(_brushTemplate != null ? _brushTemplate.ladderAsset : null);
        EditorGUILayout.EndHorizontal();
    }
}
