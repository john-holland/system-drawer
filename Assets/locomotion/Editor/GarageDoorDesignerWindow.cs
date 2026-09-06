#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class GarageDoorDesignerWindow : EditorWindow
{
    DoorAssemblySpec _spec;
    PixelLightGridMountGameObject _mount;
    PixelLightMultiSlotCatalog _catalog;
    Vector2 _scroll;
    Vector2 _slotsScroll;

    [MenuItem("Locomotion/Garage Door Designer")]
    public static void Open()
    {
        var w = GetWindow<GarageDoorDesignerWindow>("Garage Door");
        w.minSize = new Vector2(460, 540);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _spec = (DoorAssemblySpec)EditorGUILayout.ObjectField("Door assembly", _spec, typeof(DoorAssemblySpec), false);
        _mount = (PixelLightGridMountGameObject)EditorGUILayout.ObjectField(
            "Piece mount", _mount, typeof(PixelLightGridMountGameObject), true);
        _catalog = (PixelLightMultiSlotCatalog)EditorGUILayout.ObjectField(
            "Piece catalog", _catalog, typeof(PixelLightMultiSlotCatalog), false);

        if (_spec == null)
        {
            if (GUILayout.Button("Create DoorAssemblySpec"))
            {
                var path = EditorUtility.SaveFilePanelInProject("Save Door Assembly", "DoorAssembly", "asset", "");
                if (!string.IsNullOrEmpty(path))
                {
                    var s = CreateInstance<DoorAssemblySpec>();
                    s.ApplyAutoFit();
                    AssetDatabase.CreateAsset(s, path);
                    _spec = s;
                }
            }
            EditorGUILayout.EndScrollView();
            return;
        }

        _spec.openingSize = EditorGUILayout.Vector2Field("Opening size (m)", _spec.openingSize);
        _spec.sectionCount = EditorGUILayout.IntSlider("Sections", Mathf.Max(1, _spec.sectionCount), 1, 12);
        _spec.railThickness = EditorGUILayout.FloatField("Rail thickness", _spec.railThickness);
        _spec.stileWidth = EditorGUILayout.FloatField("Stile width", _spec.stileWidth);
        _spec.mullionWidth = EditorGUILayout.FloatField("Mullion width", _spec.mullionWidth);
        _spec.topRail = EditorGUILayout.Toggle("Top rail", _spec.topRail);
        _spec.bottomRail = EditorGUILayout.Toggle("Bottom rail", _spec.bottomRail);
        _spec.lockStiles = EditorGUILayout.Toggle("Lock stiles", _spec.lockStiles);
        _spec.middleRail = EditorGUILayout.Toggle("Middle / lock rail", _spec.middleRail);
        _spec.friezeRail = EditorGUILayout.Toggle("Frieze rail", _spec.friezeRail);
        _spec.mullion = EditorGUILayout.Toggle("Mullion", _spec.mullion);
        _spec.moulding = EditorGUILayout.Toggle("Moulding", _spec.moulding);
        _spec.mouldingSides = EditorGUILayout.IntSlider("Moulding sides", Mathf.Max(3, _spec.mouldingSides), 3, 12);
        _spec.lemmaPackFragment = EditorGUILayout.TextField("Lemma pack", _spec.lemmaPackFragment);

        EditorGUILayout.Space();
        _spec.autoFitPixelLightGrid = EditorGUILayout.Toggle("Auto-fit PixelLight", _spec.autoFitPixelLightGrid);
        if (_spec.autoFitPixelLightGrid)
            _spec.ApplyAutoFit();
        else
        {
            _spec.pixelLightGridW = EditorGUILayout.IntField("Grid W", Mathf.Max(1, _spec.pixelLightGridW));
            _spec.pixelLightGridH = EditorGUILayout.IntField("Grid H", Mathf.Max(1, _spec.pixelLightGridH));
        }
        _spec.pixelLightCellSize = EditorGUILayout.FloatField("Cell size", _spec.pixelLightCellSize);
        EditorGUILayout.LabelField("Fitted grid", $"{_spec.pixelLightGridW}×{_spec.pixelLightGridH}");

        if (_mount != null)
            PixelLightRadialBrushDrawer.DrawOnMount(_mount);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Door pieces (PixelLight accordion)", EditorStyles.boldLabel);
        PixelLightGridSlotAccordionDrawer.Draw(_catalog, ref _slotsScroll, null, entry =>
        {
            if (entry?.mount != null)
            {
                Selection.activeGameObject = entry.mount.gameObject;
                _mount = entry.mount;
            }
        });

        if (GUILayout.Button("Apply grid to mount") && _mount != null)
        {
            _mount.gridWidth = _spec.pixelLightGridW;
            _mount.gridHeight = _spec.pixelLightGridH;
            _mount.cellSize = _spec.pixelLightCellSize;
            EditorUtility.SetDirty(_mount);
        }
        if (GUILayout.Button("Open Garage Chain Designer"))
            GarageChainDesignerWindow.Open();

        if (GUI.changed)
            EditorUtility.SetDirty(_spec);
        EditorGUILayout.EndScrollView();
    }
}
#endif
