using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class CourtroomPixelGridDesignerWindow : EditorWindow
{
    LegalBuilding _building;
    CityPixelGrid _grid;
    PixelLightMultiSlotCatalog _catalog;
    Vector2 _scroll;
    Vector2 _slotsScroll;
    Vector2 _gridScroll;
    int _paintFrame;
    int _paintLayer;
    bool _paintOn = true;
    float _cellDraw = 16f;
    readonly CityPixelGridPaintStroke _paintStroke = new CityPixelGridPaintStroke();
    readonly CityPixelGridCellSelection _selection = new CityPixelGridCellSelection();

    [MenuItem("Locomotion/Courtroom PixelLight Designer")]
    public static void Open()
    {
        var w = GetWindow<CourtroomPixelGridDesignerWindow>("Courtroom PixelLight");
        w.minSize = new Vector2(640, 560);
    }

    public static void Open(LegalBuilding building)
    {
        Open();
        var w = GetWindow<CourtroomPixelGridDesignerWindow>();
        w._building = building;
        if (building != null)
        {
            w._grid = building.courtroomGrid;
            w._catalog = building.pixelLightCatalog;
        }
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
        EditorGUILayout.LabelField("Courtroom PixelLight Designer", EditorStyles.boldLabel);
        CityPixelGridDesignerUndo.DrawToolbar();
        _building = (LegalBuilding)EditorGUILayout.ObjectField(
            "Legal Building", _building, typeof(LegalBuilding), true);
        if (_building != null && _grid == null)
            _grid = _building.courtroomGrid;
        _grid = (CityPixelGrid)EditorGUILayout.ObjectField("Pixel Grid", _grid, typeof(CityPixelGrid), false);
        _catalog = (PixelLightMultiSlotCatalog)EditorGUILayout.ObjectField(
            "PixelLight Catalog", _catalog, typeof(PixelLightMultiSlotCatalog), false);
        if (_building != null)
        {
            _building.courtroomGrid = _grid;
            _building.pixelLightCatalog = _catalog;
        }

        if (_grid != null && GUILayout.Button("Add Courtroom Bench / Well / Jury / Gallery / Bar Layers"))
        {
            CityPixelGridDesignerUndo.RecordComplete(_grid, "Add Courtroom Layers");
            _grid.EnsureCourtroomLayers();
        }
        if (_catalog != null && GUILayout.Button("Ensure Courtroom PixelLight Slots"))
        {
            Undo.RecordObject(_catalog, "Ensure Courtroom PixelLight Slots");
            _catalog.EnsureCourtroomSlots();
            EditorUtility.SetDirty(_catalog);
        }

        DrawRooms();
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("PixelLight slots", EditorStyles.boldLabel);
        PixelLightGridSlotAccordionDrawer.Draw(_catalog, ref _slotsScroll, null, null);

        if (_grid != null)
        {
            _grid.EnsureLayersAndFrames();
            DrawPaint();
        }
        else
            EditorGUILayout.HelpBox("Assign a CityPixelGrid.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Open City Pixel Grid Designer") && _grid != null)
            CityPixelGridDesignerWindow.Open(_grid);
        if (GUILayout.Button("Open Pixel Light Designer"))
            PixelLightTimedDesignerWindow.Open();
        if (GUILayout.Button("Export Courtroom Clusters To Bounds4") && _grid != null)
        {
            var vols = _grid.ExportCourtroomClustersToBounds4(_paintFrame);
            Debug.Log("[Courtroom] exported " + (vols != null ? vols.Count : 0) + " Bounds4 clusters.");
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
        if (Event.current != null && Event.current.type == EventType.MouseUp)
        {
            _paintStroke.End();
            _selection.EndDrag();
        }
    }

    void DrawRooms()
    {
        if (_building == null) return;
        _building.EnsureDefaultRooms();
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Legal rooms (PixelLight slot or SG4D in-paint)", EditorStyles.boldLabel);
        for (int i = 0; i < _building.rooms.Count; i++)
        {
            var r = _building.rooms[i];
            if (r == null) continue;
            EditorGUILayout.BeginVertical("box");
            EditorGUI.BeginChangeCheck();
            r.roomId = EditorGUILayout.TextField("Room Id", r.roomId ?? "");
            r.displayName = EditorGUILayout.TextField("Display", r.displayName ?? "");
            r.kind = (LegalRoomKind)EditorGUILayout.EnumPopup("Kind", r.kind);
            r.floorIndex = EditorGUILayout.IntField("Floor", r.floorIndex);
            r.pixelLightSlots = (PixelLightMultiSlotCatalog)EditorGUILayout.ObjectField(
                "PixelLight Catalog", r.pixelLightSlots, typeof(PixelLightMultiSlotCatalog), false);
            r.pixelLightSlotId = EditorGUILayout.TextField("PixelLight Slot", r.pixelLightSlotId ?? "");
            r.sg4dPrompt = EditorGUILayout.TextField("SG4D Prompt", r.sg4dPrompt ?? "");
            r.inpaintPrompt = EditorGUILayout.TextField("In-paint Prompt", r.inpaintPrompt ?? "");
            r.worldPosition = EditorGUILayout.Vector3Field("World", r.worldPosition);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_building, "Edit Legal Room");
                EditorUtility.SetDirty(_building);
            }
            EditorGUILayout.EndVertical();
        }
        if (GUILayout.Button("Add Room"))
        {
            Undo.RecordObject(_building, "Add Legal Room");
            _building.rooms.Add(new LegalRoomSpec());
            EditorUtility.SetDirty(_building);
        }
    }

    void DrawPaint()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Paint", EditorStyles.boldLabel);
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
        if (layer.frames == null || layer.frames.Count == 0)
        {
            EditorGUILayout.EndScrollView();
            return;
        }
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
            if (e != null && r.Contains(e.mousePosition) && e.button == 0 && _paintOn
                && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
            {
                CityPixelGridDesignerUndo.Record(_grid, "Paint Courtroom");
                frame.Set(x, y, _grid.width, (byte)(e.shift ? 0 : 1));
                e.Use();
                Repaint();
            }
        }
        EditorGUILayout.EndScrollView();
    }
}
