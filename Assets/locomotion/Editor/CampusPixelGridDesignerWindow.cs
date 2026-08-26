using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class CampusPixelGridDesignerWindow : EditorWindow
{
    UniversityCampusAsset _campus;
    CityPixelGrid _grid;
    CityPixelBrushStamp _stamp = new CityPixelBrushStamp();
    Vector2 _scroll;
    Vector2 _gridScroll;
    int _paintFrame;
    int _paintLayer;
    bool _brushMode = true;
    bool _paintOn = true;
    float _cellDraw = 16f;
    bool _dragging;
    CityPixelBrushKind _brush = CityPixelBrushKind.Building;
    readonly CityPixelGridPaintStroke _paintStroke = new CityPixelGridPaintStroke();
    readonly CityPixelGridCellSelection _selection = new CityPixelGridCellSelection();

    [MenuItem("Locomotion/Campus Pixel Grid Designer")]
    public static void Open()
    {
        var w = GetWindow<CampusPixelGridDesignerWindow>("Campus Pixel Grid");
        w.minSize = new Vector2(640, 560);
    }

    public static void Open(UniversityCampusAsset campus)
    {
        Open();
        var w = GetWindow<CampusPixelGridDesignerWindow>("Campus Pixel Grid");
        w._campus = campus;
        if (campus != null)
            w._grid = campus.campusGrid;
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
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Campus Pixel Grid Designer", EditorStyles.boldLabel);
        CityPixelGridDesignerUndo.DrawToolbar();
        _campus = (UniversityCampusAsset)EditorGUILayout.ObjectField(
            "Campus", _campus, typeof(UniversityCampusAsset), false);
        if (_campus != null && _grid == null)
            _grid = _campus.campusGrid;
        _grid = (CityPixelGrid)EditorGUILayout.ObjectField("Pixel Grid", _grid, typeof(CityPixelGrid), false);
        if (_campus != null)
            _campus.campusGrid = _grid;

        if (_campus != null && GUILayout.Button("Ensure Default Elevation Bands"))
        {
            Undo.RecordObject(_campus, "Campus Elevation");
            _campus.EnsureDefaultElevationBands();
            EditorUtility.SetDirty(_campus);
        }

        if (_grid != null && GUILayout.Button("Add Campus Quad / Path / Building Layers"))
        {
            CityPixelGridDesignerUndo.RecordComplete(_grid, "Add Campus Layers");
            _grid.EnsureCampusLayers();
        }

        DrawRooms();
        DrawElevation();
        if (_grid != null)
        {
            _grid.EnsureLayersAndFrames();
            DrawPaint();
        }
        else
            EditorGUILayout.HelpBox("Assign a CityPixelGrid (same runtime as the city designer).", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Open City Pixel Grid Designer") && _grid != null)
            CityPixelGridDesignerWindow.Open(_grid);
        if (GUILayout.Button("Open Pixel Light Designer"))
            PixelLightTimedDesignerWindow.Open();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
        if (Event.current != null && Event.current.type == EventType.MouseUp)
        {
            _dragging = false;
            _paintStroke.End();
            _selection.EndDrag();
        }
    }

    void DrawRooms()
    {
        if (_campus == null) return;
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Campus Rooms (PixelLight slot or SG4D in-paint)", EditorStyles.boldLabel);
        if (_campus.rooms == null)
            _campus.rooms = new List<CampusRoomSpec>();
        for (int i = 0; i < _campus.rooms.Count; i++)
        {
            var r = _campus.rooms[i];
            if (r == null) continue;
            EditorGUILayout.BeginVertical("box");
            r.roomId = EditorGUILayout.TextField("Room Id", r.roomId ?? "");
            r.displayName = EditorGUILayout.TextField("Display", r.displayName ?? "");
            r.floorIndex = EditorGUILayout.IntField("Floor", r.floorIndex);
            r.zoneId = EditorGUILayout.TextField("Zone Id", r.zoneId ?? "");
            r.station = (LearningStationKind)EditorGUILayout.EnumPopup("Station", r.station);
            r.pixelLightSlots = (PixelLightMultiSlotCatalog)EditorGUILayout.ObjectField(
                "PixelLight Catalog", r.pixelLightSlots, typeof(PixelLightMultiSlotCatalog), false);
            r.pixelLightSlotId = EditorGUILayout.TextField("PixelLight Slot", r.pixelLightSlotId ?? "");
            r.sg4dPrompt = EditorGUILayout.TextField("SG4D Prompt", r.sg4dPrompt ?? "");
            r.inpaintPrompt = EditorGUILayout.TextField("In-paint Prompt", r.inpaintPrompt ?? "");
            r.worldPosition = EditorGUILayout.Vector3Field("World", r.worldPosition);
            EditorGUILayout.EndVertical();
        }
        if (GUILayout.Button("Add Room"))
        {
            Undo.RecordObject(_campus, "Add Campus Room");
            _campus.rooms.Add(new CampusRoomSpec());
            EditorUtility.SetDirty(_campus);
        }
    }

    void DrawElevation()
    {
        if (_campus == null) return;
        EditorGUILayout.Space(4);
        _campus.elevationPlan = (StreetBlocksPlanAsset)EditorGUILayout.ObjectField(
            "Street Blocks Elevation (optional)", _campus.elevationPlan, typeof(StreetBlocksPlanAsset), false);
        if (_campus.elevationBands == null) return;
        for (int i = 0; i < _campus.elevationBands.Count; i++)
        {
            var b = _campus.elevationBands[i];
            if (b == null) continue;
            EditorGUILayout.BeginHorizontal();
            b.id = EditorGUILayout.TextField(b.id ?? "", GUILayout.Width(90));
            b.depthMinM = EditorGUILayout.FloatField(b.depthMinM);
            b.depthMaxM = EditorGUILayout.FloatField(b.depthMaxM);
            EditorGUILayout.EndHorizontal();
        }
    }

    void DrawPaint()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Paint (same CityPixelGrid as the city designer)", EditorStyles.boldLabel);
        _brushMode = EditorGUILayout.Toggle("Brush Mode", _brushMode);
        if (_brushMode)
        {
            _brush = (CityPixelBrushKind)EditorGUILayout.EnumPopup("Brush", _brush);
            CityPixelBrushEditors.DrawBrushOptions(_brush, ref _stamp);
        }
        else
            _paintOn = EditorGUILayout.Toggle("Paint On", _paintOn);

        if (_grid.layers != null && _grid.layers.Count > 0)
        {
            string[] names = new string[_grid.layers.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = _grid.layers[i] != null ? _grid.layers[i].layerId : i.ToString();
            _paintLayer = EditorGUILayout.Popup("Layer", Mathf.Clamp(_paintLayer, 0, names.Length - 1), names);
        }
        _paintFrame = EditorGUILayout.IntSlider("Frame", _paintFrame, 0, Mathf.Max(0, _grid.frameCount - 1));
        _cellDraw = EditorGUILayout.Slider("Cell Draw", _cellDraw, 8f, 28f);

        float w = _grid.width * _cellDraw;
        float h = _grid.height * _cellDraw;
        _gridScroll = EditorGUILayout.BeginScrollView(_gridScroll, GUILayout.Height(Mathf.Min(360, h + 20)));
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
            c = CityPixelGridDesignerUndo.OverlaySelection(c, _selection, x, y);
            EditorGUI.DrawRect(r, c);
            Event e = Event.current;
            if (e != null && r.Contains(e.mousePosition) && e.button == 0)
            {
                if (e.type == EventType.MouseDown)
                {
                    _dragging = true;
                    Hit(x, y, true);
                    e.Use();
                    Repaint();
                }
                else if (e.type == EventType.MouseDrag && _dragging)
                {
                    Hit(x, y, false);
                    e.Use();
                    Repaint();
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    void Hit(int x, int y, bool mouseDown)
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
        _paintStroke.Begin(_grid, "Paint Campus Pixel Grid");
        if (_brushMode)
        {
            if (_brush == CityPixelBrushKind.Eraser)
                _grid.ClearBrushStamp(_paintFrame, x, y);
            else
            {
                var stamp = CityPixelBrushEditors.CloneStampTemplate(_stamp, _paintFrame, x, y);
                stamp.kind = _brush;
                _grid.SetBrushStamp(stamp);
            }
        }
        else
        {
            var layer = _grid.layers[Mathf.Clamp(_paintLayer, 0, _grid.layers.Count - 1)];
            var frame = layer.frames[Mathf.Clamp(_paintFrame, 0, layer.frames.Count - 1)];
            frame.Set(x, y, _grid.width, _paintOn ? (byte)1 : (byte)0);
        }
        EditorUtility.SetDirty(_grid);
    }
}
