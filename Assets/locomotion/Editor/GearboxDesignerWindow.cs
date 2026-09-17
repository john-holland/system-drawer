#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class GearboxDesignerWindow : EditorWindow
{
    GearboxSpec _spec;
    Bounds4SdfInclusionPixelLightMount _mount;
    PixelLightDesignerView _view = PixelLightDesignerView.Front;
    Bounds4SdfInclusionKind _scope = Bounds4SdfInclusionKind.Shell;
    Vector2 _scroll;
    Vector2 _slotScroll;

    [MenuItem("Locomotion/Gearbox Designer")]
    public static void Open()
    {
        var w = GetWindow<GearboxDesignerWindow>("Gearbox");
        w.minSize = new Vector2(480, 620);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _spec = (GearboxSpec)EditorGUILayout.ObjectField("Gearbox", _spec, typeof(GearboxSpec), false);

        if (_spec == null)
        {
            if (GUILayout.Button("Create GearboxSpec"))
            {
                var path = EditorUtility.SaveFilePanelInProject("Save Gearbox", "Gearbox", "asset", "");
                if (!string.IsNullOrEmpty(path))
                {
                    var s = CreateInstance<GearboxSpec>();
                    AssetDatabase.CreateAsset(s, path);
                    _spec = s;
                }
            }
            EditorGUILayout.EndScrollView();
            return;
        }

        _spec.catalog = GearboxLathePixelLightDrawer.DrawCatalogField(_spec.catalog, "GearboxPixelLight");
        _mount = (Bounds4SdfInclusionPixelLightMount)EditorGUILayout.ObjectField(
            "Frame/Shell mount", _mount, typeof(Bounds4SdfInclusionPixelLightMount), true);

        EditorGUILayout.Space();
        GearboxLathePixelLightDrawer.DrawFrameShell(_spec.catalog, ref _view, ref _scope, ref _mount);

        if (_spec.catalog != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selector / tumbler / back-gear slots", EditorStyles.boldLabel);
            PixelLightGridSlotAccordionDrawer.Draw(_spec.catalog, ref _slotScroll, null, null, 220f);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Connections (ratios)", EditorStyles.boldLabel);
        if (GUILayout.Button("Add gear pair"))
            _spec.AddGearPair(12, 24);
        for (int i = 0; i < _spec.connections.Count; i++)
        {
            var c = _spec.connections[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            c.label = EditorGUILayout.TextField("Label", c.label);
            c.kind = (GearboxDriveKind)EditorGUILayout.EnumPopup("Drive", c.kind);
            if (c.kind == GearboxDriveKind.Belt)
            {
                c.drivingPulleyM = EditorGUILayout.FloatField("Driving pulley", c.drivingPulleyM);
                c.drivenPulleyM = EditorGUILayout.FloatField("Driven pulley", c.drivenPulleyM);
                EditorGUILayout.LabelField("Path length", c.BeltPathLengthM().ToString("0.000"));
            }
            else if (c.kind == GearboxDriveKind.ChainLinkBelt)
            {
                c.chainBelt = (GarageChainSpec)EditorGUILayout.ObjectField(
                    "Chain belt", c.chainBelt, typeof(GarageChainSpec), false);
                EditorGUILayout.LabelField("Path length", c.ChainBeltPathLengthM().ToString("0.000"));
            }
            else
            {
                c.drivingTeeth = EditorGUILayout.IntField("Driving teeth", c.drivingTeeth);
                c.drivenTeeth = EditorGUILayout.IntField("Driven teeth", c.drivenTeeth);
            }
            EditorGUILayout.LabelField("Ratio", c.Ratio.ToString("0.###"));
            EditorGUILayout.EndVertical();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(_spec);
            if (_mount != null) EditorUtility.SetDirty(_mount);
            if (_spec.catalog != null) EditorUtility.SetDirty(_spec.catalog);
        }
        EditorGUILayout.EndScrollView();
    }
}
#endif
