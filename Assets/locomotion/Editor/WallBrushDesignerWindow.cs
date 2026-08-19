using UnityEditor;
using UnityEngine;

public sealed class WallBrushDesignerWindow : EditorWindow
{
    WallBrushCatalog _catalog;
    WallBrushSpec _spec;
    HouseConstructionPlan _plan;
    HouseWallBrushKind _addKind = HouseWallBrushKind.Custom;
    int _customLayerIndex;
    Vector2 _scroll;

    [MenuItem("Locomotion/Wall Brush Designer")]
    public static void Open()
    {
        var w = GetWindow<WallBrushDesignerWindow>("Wall Brushes");
        w.minSize = new Vector2(420, 520);
    }

    public static void OpenWith(WallBrushCatalog catalog)
    {
        Open();
        var w = GetWindow<WallBrushDesignerWindow>();
        w._catalog = catalog;
        if (catalog != null)
        {
            catalog.EnsureBuiltins();
            if (catalog.brushes != null && catalog.brushes.Count > 0)
                w._spec = catalog.brushes[0];
        }
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _catalog = (WallBrushCatalog)EditorGUILayout.ObjectField("Catalog", _catalog, typeof(WallBrushCatalog), false);
        _plan = (HouseConstructionPlan)EditorGUILayout.ObjectField("Construction Plan", _plan, typeof(HouseConstructionPlan), false);
        if (_plan != null && _catalog == null)
            _catalog = _plan.wallBrushes;

        if (_catalog == null)
        {
            if (GUILayout.Button("Create WallBrushCatalog"))
                CreateCatalogAsset();
            EditorGUILayout.EndScrollView();
            return;
        }

        _catalog.EnsureBuiltins();
        if (_plan != null && _plan.wallBrushes != _catalog)
        {
            _plan.wallBrushes = _catalog;
            EditorUtility.SetDirty(_plan);
        }

        EditorGUILayout.LabelField("Builtins", EditorStyles.miniBoldLabel);
        DrawBuiltinRow();

        EditorGUILayout.BeginHorizontal();
        _addKind = (HouseWallBrushKind)EditorGUILayout.EnumPopup("New kind", _addKind);
        if (GUILayout.Button("Add brush+!", GUILayout.Width(110)))
            AddBrushPlus();
        EditorGUILayout.EndHorizontal();
        if (_addKind == HouseWallBrushKind.Custom && _plan != null && _plan.layers != null && _plan.layers.Count > 0)
        {
            string[] names = new string[_plan.layers.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = _plan.layers[i] != null ? _plan.layers[i].layerId : "";
            _customLayerIndex = EditorGUILayout.Popup("Custom target layer", Mathf.Clamp(_customLayerIndex, 0, names.Length - 1), names);
        }

        _spec = (WallBrushSpec)EditorGUILayout.ObjectField("Brush", _spec, typeof(WallBrushSpec), false);
        if (_spec == null)
        {
            EditorGUILayout.HelpBox("Select a builtin or Add brush+! to author a discrete wall piece.", MessageType.Info);
            DrawOpenFoundation();
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUI.BeginChangeCheck();
        _spec.displayName = EditorGUILayout.TextField("Display Name", _spec.displayName);
        _spec.brushId = EditorGUILayout.TextField("Brush Id", _spec.brushId);
        var kind = (HouseWallBrushKind)EditorGUILayout.EnumPopup("Kind", _spec.kind);
        if (kind != _spec.kind)
        {
            _spec.kind = kind;
            _spec.ApplyKindDefaults();
        }
        _spec.targetLayerId = EditorGUILayout.TextField("Target Layer", _spec.targetLayerId);
        _spec.color = EditorGUILayout.ColorField("Color", _spec.color);
        _spec.thicknessM = EditorGUILayout.FloatField("Thickness (m)", _spec.thicknessM);
        _spec.bayWidthM = EditorGUILayout.FloatField("Bay Width (m)", _spec.bayWidthM);
        _spec.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", _spec.prefab, typeof(GameObject), false);
        EditorGUILayout.LabelField("Paint byte", _spec.paintByte.ToString());
        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(_spec);

        if (GUILayout.Button("Bake prefab"))
            BakePrefab();

        DrawOpenFoundation();
        EditorGUILayout.EndScrollView();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(_catalog);
            if (_spec != null)
                EditorUtility.SetDirty(_spec);
        }
    }

    void DrawBuiltinRow()
    {
        if (_catalog.brushes == null) return;
        EditorGUILayout.BeginHorizontal();
        int shown = 0;
        for (int i = 0; i < _catalog.brushes.Count; i++)
        {
            var b = _catalog.brushes[i];
            if (b == null) continue;
            if (shown > 0 && shown % 4 == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = _spec == b ? b.color : Color.Lerp(b.color, Color.black, 0.35f);
            if (GUILayout.Button(b.displayName))
                _spec = b;
            GUI.backgroundColor = old;
            shown++;
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawOpenFoundation()
    {
        if (GUILayout.Button("Open House Foundation Layers"))
            FoundationLayersDesignerWindow.OpenWith(_plan, _catalog);
    }

    void CreateCatalogAsset()
    {
        var path = EditorUtility.SaveFilePanelInProject("Save Wall Brush Catalog", "WallBrushCatalog", "asset", "");
        if (string.IsNullOrEmpty(path)) return;
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
        _catalog = cat;
        if (cat.brushes.Count > 0)
            _spec = cat.brushes[0];
        if (_plan != null)
        {
            _plan.wallBrushes = cat;
            EditorUtility.SetDirty(_plan);
        }
    }

    void AddBrushPlus()
    {
        string layerId = _addKind == HouseWallBrushKind.Custom
            ? CurrentLayerId()
            : WallBrushSpec.DefaultLayerId(_addKind);
        var spec = _catalog.AddBrush(_addKind, layerId);
        var path = EditorUtility.SaveFilePanelInProject(
            "Save Wall Brush", spec.brushId, "asset", "Add brush+!");
        if (string.IsNullOrEmpty(path))
        {
            _catalog.brushes.Remove(spec);
            DestroyImmediate(spec);
            return;
        }
        AssetDatabase.CreateAsset(spec, path);
        EditorUtility.SetDirty(_catalog);
        AssetDatabase.SaveAssets();
        _spec = spec;
    }

    string CurrentLayerId()
    {
        if (_plan != null && _plan.layers != null && _plan.layers.Count > 0)
        {
            int i = Mathf.Clamp(_customLayerIndex, 0, _plan.layers.Count - 1);
            var layer = _plan.layers[i];
            if (layer != null && !string.IsNullOrEmpty(layer.layerId))
                return layer.layerId;
        }
        return WallBrushSpec.DefaultLayerId(HouseWallBrushKind.Custom);
    }

    void BakePrefab()
    {
        if (_spec == null) return;
        var path = EditorUtility.SaveFilePanelInProject(
            "Save Wall Brush Prefab", _spec.brushId, "prefab", "Bake a discrete wall-piece prefab.");
        if (string.IsNullOrEmpty(path)) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = _spec.brushId;
        float t = Mathf.Max(0.005f, _spec.thicknessM);
        float bay = Mathf.Max(0.05f, _spec.bayWidthM);
        go.transform.localScale = new Vector3(bay, 2.4f, t);
        AttachKindComponents(go, _spec.kind, _spec);

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        _spec.prefab = prefab;
        EditorUtility.SetDirty(_spec);
        AssetDatabase.SaveAssets();
    }

    static void AttachKindComponents(GameObject go, HouseWallBrushKind kind, WallBrushSpec spec)
    {
        switch (kind)
        {
            case HouseWallBrushKind.Electrical:
                go.AddComponent<ElectricalSpanNode>();
                var span = go.AddComponent<HouseElectricalSpan>();
                span.inactivePrebake = true;
                break;
            case HouseWallBrushKind.Plumbing:
                go.AddComponent<FixturePlumbingNode>();
                break;
            case HouseWallBrushKind.Hvac:
                go.AddComponent<VentDuctNode>();
                var duct = go.AddComponent<HouseVentDuct>();
                duct.EnsureFullBore();
                break;
            case HouseWallBrushKind.Insulation:
                var batt = go.AddComponent<InsulationBattNode>();
                batt.inactiveUntilFrame = true;
                break;
            case HouseWallBrushKind.Drywall:
            case HouseWallBrushKind.Custom:
                go.AddComponent<WallVolumeNode>();
                break;
            case HouseWallBrushKind.Slats:
            case HouseWallBrushKind.Studs:
                var bay = go.AddComponent<StudBayNode>();
                bay.bayWidthM = spec.bayWidthM;
                break;
        }
    }
}
