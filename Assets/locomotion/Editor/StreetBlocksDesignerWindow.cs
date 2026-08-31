using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Locomotion → Street Blocks Designer — depth layers, brushes, MST auto-link, zoom warnings.</summary>
public sealed class StreetBlocksDesignerWindow : EditorWindow
{
    StreetBlocksPlanAsset _plan;
    ScriptableObject _phonePoleConfig;
    int _layer;
    StreetBlocksBrushKind _brush = StreetBlocksBrushKind.TwoWayStreet;
    int _laneCount = 3;
    float _oneWayYaw;
    float _cellDraw = 14f;
    float _zoom = 1f;
    Vector2 _scroll;
    Vector2 _gridScroll;
    bool _dragging;
    Vector2Int? _selected;

    [MenuItem("Locomotion/Street Blocks Designer")]
    public static void Open()
    {
        var w = GetWindow<StreetBlocksDesignerWindow>("Street Blocks");
        w.minSize = new Vector2(720, 520);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Street Blocks Designer", EditorStyles.boldLabel);
        _plan = (StreetBlocksPlanAsset)EditorGUILayout.ObjectField("Plan", _plan, typeof(StreetBlocksPlanAsset), false);
        if (GUILayout.Button("Create New StreetBlocksPlan Asset"))
        {
            var a = CreateInstance<StreetBlocksPlanAsset>();
            a.EnsureDefaultLayers();
            var path = EditorUtility.SaveFilePanelInProject("Save Street Blocks Plan", "StreetBlocksPlan", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(a, path);
                _plan = a;
            }
        }
        if (_plan == null)
        {
            EditorGUILayout.HelpBox("Assign or create a StreetBlocksPlanAsset.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        _plan.EnsureDefaultLayers();
        DrawHeader();
        DrawLayers();
        DrawBrushes();
        DrawGrid();
        DrawActions();
        EditorGUILayout.EndScrollView();
    }

    void DrawHeader()
    {
        _plan.worldOrigin = EditorGUILayout.Vector3Field("World Origin", _plan.worldOrigin);
        EditorGUILayout.BeginHorizontal();
        _plan.width = Mathf.Max(1, EditorGUILayout.IntField("Width", _plan.width));
        _plan.height = Mathf.Max(1, EditorGUILayout.IntField("Height", _plan.height));
        EditorGUILayout.EndHorizontal();
        _plan.cellWorldSize = EditorGUILayout.FloatField("Cell World Size", _plan.cellWorldSize);
        _zoom = EditorGUILayout.Slider("Zoom Granularity", _zoom, 0.2f, 3f);
        _cellDraw = 10f * _zoom;
        EditorGUILayout.LabelField("Overlap: size desc; zoom to show without overlap; layer show/hide.");
    }

    void DrawLayers()
    {
        string[] names = new string[_plan.layers.Count];
        for (int i = 0; i < names.Length; i++)
        {
            var L = _plan.layers[i];
            names[i] = L.layerId + " [" + L.kind + "] " + L.depthMinM + ".." + L.depthMaxM + "m"
                       + (L.visible ? "" : " (hidden)");
        }
        _layer = EditorGUILayout.Popup("Layer", Mathf.Clamp(_layer, 0, names.Length - 1), names);
        var layer = _plan.GetLayer(_layer);
        layer.visible = EditorGUILayout.Toggle("Show Layer", layer.visible);
    }

    void DrawBrushes()
    {
        EditorGUILayout.BeginVertical("box");
        _brush = (StreetBlocksBrushKind)EditorGUILayout.EnumPopup("Brush", _brush);
        if (_brush == StreetBlocksBrushKind.Multilane)
            _laneCount = Mathf.Max(3, EditorGUILayout.IntField("Lanes (3+)", _laneCount));
        if (_brush == StreetBlocksBrushKind.OneWay)
            _oneWayYaw = EditorGUILayout.Slider("One-Way Yaw", _oneWayYaw, 0f, 360f);
        if (_brush == StreetBlocksBrushKind.PhonePole)
            _phonePoleConfig = (ScriptableObject)EditorGUILayout.ObjectField(
                "Phone Pole Config SO", _phonePoleConfig, typeof(ScriptableObject), false);
        if (_brush == StreetBlocksBrushKind.Trash && _zoom < _plan.trashMinZoom)
            EditorGUILayout.HelpBox("Zoom warning: trash brush needs more zoom (too zoomed out).", MessageType.Warning);
        if (_brush == StreetBlocksBrushKind.PhonePole && _zoom < _plan.phonePoleMinZoom)
            EditorGUILayout.HelpBox("Zoom warning: phone poles need more zoom.", MessageType.Warning);
        EditorGUILayout.EndVertical();
    }

    void DrawGrid()
    {
        if (!_plan.GetLayer(_layer).visible)
        {
            EditorGUILayout.HelpBox("Layer hidden.", MessageType.None);
            return;
        }
        float w = _plan.width * _cellDraw;
        float h = _plan.height * _cellDraw;
        _gridScroll = EditorGUILayout.BeginScrollView(_gridScroll, GUILayout.Height(Mathf.Min(440, h + 20)));
        var rect = GUILayoutUtility.GetRect(w, h);
        var visible = _plan.RasterizeVisibleNoOverlap(_layer);
        var visibleSet = new HashSet<long>();
        for (int i = 0; i < visible.Count; i++)
            visibleSet.Add(((long)visible[i].x << 32) ^ (uint)visible[i].y);

        for (int y = 0; y < _plan.height; y++)
        for (int x = 0; x < _plan.width; x++)
        {
            var r = new Rect(rect.x + x * _cellDraw, rect.y + y * _cellDraw, _cellDraw - 1, _cellDraw - 1);
            var cell = _plan.GetCell(_layer, x, y);
            Color c = new Color(0.12f, 0.12f, 0.12f);
            if (cell != null)
            {
                long key = ((long)x << 32) ^ (uint)y;
                if (!visibleSet.Contains(key) && _zoom < 1.2f)
                    c = new Color(0.2f, 0.2f, 0.2f);
                else
                    c = BrushColor(cell.brush);
            }
            if (_selected.HasValue && _selected.Value.x == x && _selected.Value.y == y)
                c = Color.Lerp(c, Color.yellow, 0.45f);
            EditorGUI.DrawRect(r, c);

            // One-way arrow hint
            if (cell != null && cell.brush == StreetBlocksBrushKind.OneWay && _cellDraw > 10f)
            {
                Handles.BeginGUI();
                Handles.color = Color.white;
                Vector2 center = r.center;
                Vector2 dir = Quaternion.Euler(0f, 0f, -cell.oneWayYawDegrees) * Vector2.right * (_cellDraw * 0.35f);
                Handles.DrawLine(center - dir, center + dir);
                Handles.EndGUI();
            }

            Event e = Event.current;
            if (e == null || !r.Contains(e.mousePosition)) continue;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                _dragging = true;
                PaintOrSelect(x, y);
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseDrag && _dragging && e.button == 0 && _brush != StreetBlocksBrushKind.Select)
            {
                PaintOrSelect(x, y);
                e.Use();
                Repaint();
            }
        }

        // Draw MST links
        Handles.BeginGUI();
        Handles.color = new Color(0.4f, 0.8f, 1f, 0.7f);
        for (int i = 0; i < _plan.streetLinks.Count; i++)
        {
            var link = _plan.streetLinks[i];
            Vector2 a = new Vector2(rect.x + (link.a.x + 0.5f) * _cellDraw, rect.y + (link.a.y + 0.5f) * _cellDraw);
            Vector2 b = new Vector2(rect.x + (link.b.x + 0.5f) * _cellDraw, rect.y + (link.b.y + 0.5f) * _cellDraw);
            Handles.DrawLine(a, b);
        }
        Handles.EndGUI();

        if (Event.current != null && Event.current.type == EventType.MouseUp)
            _dragging = false;
        EditorGUILayout.EndScrollView();
    }

    void PaintOrSelect(int x, int y)
    {
        if (_brush == StreetBlocksBrushKind.Select)
        {
            _selected = new Vector2Int(x, y);
            return;
        }
        if (_brush == StreetBlocksBrushKind.Eraser)
        {
            _plan.ClearCell(_layer, x, y);
            EditorUtility.SetDirty(_plan);
            return;
        }
        if (_brush == StreetBlocksBrushKind.Trash && _zoom < _plan.trashMinZoom) return;
        if (_brush == StreetBlocksBrushKind.PhonePole && _zoom < _plan.phonePoleMinZoom) return;

        var cell = new StreetBlocksCell
        {
            x = x,
            y = y,
            brush = _brush,
            laneCount = _laneCount,
            oneWayYawDegrees = _oneWayYaw,
            phonePoleConfig = _phonePoleConfig,
            structureSizeM = _brush == StreetBlocksBrushKind.Building ? 40f
                : _brush == StreetBlocksBrushKind.Multilane ? 12f : 6f,
            show = true
        };
        _plan.SetCell(_layer, x, y, cell);
        EditorUtility.SetDirty(_plan);
    }

    void DrawActions()
    {
        EditorGUILayout.Space();
        if (GUILayout.Button("Auto-Connect Streets (MST)"))
        {
            int n = _plan.AutoConnectStreets();
            Debug.Log("[StreetBlocks] MST links added: " + n);
            EditorUtility.SetDirty(_plan);
        }
        if (GUILayout.Button("Seed Sewer Graph From Buildings"))
        {
            var graph = Object.FindFirstObjectByType<SewerGraph>();
            if (graph == null)
            {
                var go = new GameObject("SewerGraph");
                graph = go.AddComponent<SewerGraph>();
            }
            var water = Object.FindFirstObjectByType<WaterGraph>();
            if (water == null)
            {
                var wgo = new GameObject("WaterGraph");
                water = wgo.AddComponent<WaterGraph>();
            }
            _plan.SeedWaterAndSewerFromBuildings(graph, water);
            Debug.Log("[StreetBlocks] Sewer nodes: " + graph.nodes.Count + " water nodes: " + water.nodes.Count);
        }
        if (GUILayout.Button("Mark Dirty"))
            EditorUtility.SetDirty(_plan);
    }

    static Color BrushColor(StreetBlocksBrushKind k) => k switch
    {
        StreetBlocksBrushKind.TwoWayStreet => new Color(0.35f, 0.35f, 0.4f),
        StreetBlocksBrushKind.Multilane => new Color(0.45f, 0.45f, 0.5f),
        StreetBlocksBrushKind.OneWay => new Color(0.55f, 0.4f, 0.2f),
        StreetBlocksBrushKind.Trash => new Color(0.3f, 0.55f, 0.25f),
        StreetBlocksBrushKind.PhonePole => new Color(0.7f, 0.7f, 0.2f),
        StreetBlocksBrushKind.Building => new Color(0.5f, 0.35f, 0.55f),
        StreetBlocksBrushKind.Sewer => new Color(0.25f, 0.45f, 0.55f),
        StreetBlocksBrushKind.DryWell => new Color(0.4f, 0.55f, 0.7f),
        StreetBlocksBrushKind.Bioswale => new Color(0.2f, 0.6f, 0.35f),
        _ => Color.gray
    };
}
