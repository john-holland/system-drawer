using UnityEditor;
using UnityEngine;

/// <summary>Locomotion → Park Plant Planner — grid, pencil/fill/plant dropdown, placement squares, time layers.</summary>
public sealed class ParkPlantPlannerWindow : EditorWindow
{
    enum ToolMode { Pencil, Fill, Eraser }

    ParkPlantPlanAsset _plan;
    LotGrassPlantDef _plantDef;
    int _timeLayer;
    int _speciesIndex;
    ToolMode _tool = ToolMode.Pencil;
    Color _paintColor = new Color(0.25f, 0.7f, 0.3f);
    float _stage01 = 0.5f;
    float _cellDraw = 14f;
    Vector2 _scroll;
    Vector2 _gridScroll;
    bool _dragging;
    ParkPlantPlacementSquare _activeSquare;

    [MenuItem("Locomotion/Park Plant Planner")]
    public static void Open()
    {
        var w = GetWindow<ParkPlantPlannerWindow>("Park Plant Planner");
        w.minSize = new Vector2(640, 480);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Park Plant Planner", EditorStyles.boldLabel);

        _plan = (ParkPlantPlanAsset)EditorGUILayout.ObjectField("Plan Asset", _plan, typeof(ParkPlantPlanAsset), false);
        _plantDef = (LotGrassPlantDef)EditorGUILayout.ObjectField("Plant Def (optional)", _plantDef, typeof(LotGrassPlantDef), false);

        if (GUILayout.Button("Create New ParkPlantPlan Asset"))
        {
            var a = CreateInstance<ParkPlantPlanAsset>();
            a.EnsureDefaults();
            if (_plantDef != null && !a.plantSpeciesIds.Contains(_plantDef.speciesId))
                a.plantSpeciesIds.Insert(0, _plantDef.speciesId);
            var path = EditorUtility.SaveFilePanelInProject("Save Park Plant Plan", "ParkPlantPlan", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(a, path);
                _plan = a;
            }
        }

        if (_plan == null)
        {
            EditorGUILayout.HelpBox("Assign or create a ParkPlantPlanAsset.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        _plan.EnsureDefaults();
        DrawHeader();
        DrawTools();
        DrawTimeLayers();
        DrawGrid();
        DrawPlacementSquares();
        DrawPreviewHooks();

        if (GUILayout.Button("Mark Dirty"))
            EditorUtility.SetDirty(_plan);

        EditorGUILayout.EndScrollView();
    }

    void DrawHeader()
    {
        _plan.worldOrigin = EditorGUILayout.Vector3Field("World Origin", _plan.worldOrigin);
        EditorGUILayout.BeginHorizontal();
        _plan.width = Mathf.Max(1, EditorGUILayout.IntField("Width", _plan.width));
        _plan.height = Mathf.Max(1, EditorGUILayout.IntField("Height", _plan.height));
        EditorGUILayout.EndHorizontal();
        _plan.plantMinSizeM = EditorGUILayout.FloatField("Plant Min Size (m)", _plan.plantMinSizeM);
        _plan.cellWorldSize = EditorGUILayout.FloatField("Cell World Size", _plan.cellWorldSize);
        if (_plan.cellWorldSize < _plan.plantMinSizeM)
            _plan.cellWorldSize = _plan.plantMinSizeM;
        _cellDraw = EditorGUILayout.Slider("Cell Draw Size", _cellDraw, 4f, 28f);
        EditorGUILayout.LabelField($"Grid granularity default = plant min size ({_plan.plantMinSizeM:F2} m)");
    }

    void DrawTools()
    {
        EditorGUILayout.BeginVertical("box");
        _tool = (ToolMode)EditorGUILayout.EnumPopup("Tool", _tool);
        if (_plan.plantSpeciesIds.Count == 0)
            _plan.plantSpeciesIds.Add("lot_grass");
        string[] names = _plan.plantSpeciesIds.ToArray();
        _speciesIndex = Mathf.Clamp(_speciesIndex, 0, names.Length - 1);
        _speciesIndex = EditorGUILayout.Popup("Plant / Color Dropdown", _speciesIndex, names);
        _paintColor = EditorGUILayout.ColorField("Paint Color", _paintColor);
        _stage01 = EditorGUILayout.Slider("Stage 01", _stage01, 0f, 1f);
        EditorGUILayout.HelpBox("Pencil paints cells; Fill flood-fills; placement square auto-adds on move.", MessageType.None);
        EditorGUILayout.EndVertical();
    }

    void DrawTimeLayers()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Time Layer", GUILayout.Width(80));
        if (GUILayout.Button("−", GUILayout.Width(28)))
        {
            if (_plan.timeLayers.Count > 1)
            {
                _plan.timeLayers.RemoveAt(Mathf.Clamp(_timeLayer, 0, _plan.timeLayers.Count - 1));
                _timeLayer = Mathf.Clamp(_timeLayer, 0, _plan.timeLayers.Count - 1);
            }
        }
        string[] layerNames = new string[_plan.timeLayers.Count];
        for (int i = 0; i < layerNames.Length; i++)
            layerNames[i] = _plan.timeLayers[i].layerId + " @" + _plan.timeLayers[i].timeSec.ToString("0.#") + "s";
        _timeLayer = EditorGUILayout.Popup(_timeLayer, layerNames);
        if (GUILayout.Button("+", GUILayout.Width(28)))
        {
            float t = _plan.timeLayers.Count > 0
                ? _plan.timeLayers[_plan.timeLayers.Count - 1].timeSec + 10f
                : 0f;
            _plan.timeLayers.Add(new ParkPlantTimeLayer
            {
                layerId = "t" + _plan.timeLayers.Count,
                timeSec = t
            });
            _timeLayer = _plan.timeLayers.Count - 1;
        }
        EditorGUILayout.EndHorizontal();

        var layer = _plan.GetLayer(_timeLayer);
        layer.timeSec = EditorGUILayout.FloatField("Layer Time (sec)", layer.timeSec);
        _stage01 = EditorGUILayout.Slider("Layer Stage Bias", _stage01, 0f, 1f);
        EditorGUILayout.LabelField("+/− time layers control plant stage / staged changes.");
    }

    void DrawGrid()
    {
        float w = _plan.width * _cellDraw;
        float h = _plan.height * _cellDraw;
        _gridScroll = EditorGUILayout.BeginScrollView(_gridScroll, GUILayout.Height(Mathf.Min(420, h + 20)));
        var rect = GUILayoutUtility.GetRect(w, h);
        string species = _plan.plantSpeciesIds[Mathf.Clamp(_speciesIndex, 0, _plan.plantSpeciesIds.Count - 1)];

        for (int y = 0; y < _plan.height; y++)
        for (int x = 0; x < _plan.width; x++)
        {
            var r = new Rect(rect.x + x * _cellDraw, rect.y + y * _cellDraw, _cellDraw - 1, _cellDraw - 1);
            var cell = _plan.GetCell(_timeLayer, x, y);
            Color c = cell != null ? cell.color : new Color(0.15f, 0.15f, 0.15f);
            EditorGUI.DrawRect(r, c);

            Event e = Event.current;
            if (e == null || !r.Contains(e.mousePosition)) continue;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                _dragging = true;
                PaintAt(x, y, species);
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseDrag && _dragging && e.button == 0)
            {
                PaintAt(x, y, species);
                e.Use();
                Repaint();
            }
        }

        if (Event.current != null && Event.current.type == EventType.MouseUp)
            _dragging = false;

        EditorGUILayout.EndScrollView();
    }

    void PaintAt(int x, int y, string species)
    {
        switch (_tool)
        {
            case ToolMode.Pencil:
                _plan.SetCell(_timeLayer, x, y, species, _paintColor, _stage01);
                Vector3 world = _plan.worldOrigin + new Vector3(x * _plan.cellWorldSize, 0f, y * _plan.cellWorldSize);
                _activeSquare = _plan.AddOrMovePlacementSquare(_timeLayer, new Vector2Int(x, y), species, world);
                break;
            case ToolMode.Fill:
                _plan.FloodFill(_timeLayer, x, y, species, _paintColor, _stage01);
                break;
            case ToolMode.Eraser:
                _plan.ClearCell(_timeLayer, x, y);
                break;
        }
        EditorUtility.SetDirty(_plan);
    }

    void DrawPlacementSquares()
    {
        var layer = _plan.GetLayer(_timeLayer);
        EditorGUILayout.LabelField($"Placement Squares: {layer.placementSquares.Count}", EditorStyles.boldLabel);
        if (_activeSquare != null)
        {
            EditorGUILayout.LabelField("Active", _activeSquare.squareId);
            _activeSquare.worldPosition = EditorGUILayout.Vector3Field("World Position", _activeSquare.worldPosition);
            _activeSquare.worldRotation = Quaternion.Euler(
                EditorGUILayout.Vector3Field("World Euler", _activeSquare.worldRotation.eulerAngles));
            _activeSquare.worldScale = EditorGUILayout.Vector3Field("World Scale", _activeSquare.worldScale);
            _activeSquare.snapped = EditorGUILayout.Toggle("Snapped", _activeSquare.snapped);
        }
    }

    void DrawPreviewHooks()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pathfinding / Tool BT Preview Hooks", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Preview targets horticulture BTs: weed pull, seed spread, hand sow, watering, hoeing, flower tending.",
            MessageType.None);
        if (GUILayout.Button("Log Preview Narrative Actions"))
        {
            Debug.Log("[ParkPlantPlanner] park_weeding, park_seed_spread, park_hand_seed_sow, park_watering, park_hoeing, park_flower_tending");
        }
    }
}
