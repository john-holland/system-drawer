#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class GarageChainDesignerWindow : EditorWindow
{
    GarageChainSpec _spec;
    GarageChainAssembly _assembly;
    PixelLightGridMountGameObject _linkMount;
    GarageChainLinkKind _kind = GarageChainLinkKind.Chain;
    Vector2 _scroll;

    [MenuItem("Locomotion/Garage Chain Designer")]
    public static void Open()
    {
        var w = GetWindow<GarageChainDesignerWindow>("Garage Chain");
        w.minSize = new Vector2(440, 520);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _spec = (GarageChainSpec)EditorGUILayout.ObjectField("Chain spec", _spec, typeof(GarageChainSpec), false);
        _assembly = (GarageChainAssembly)EditorGUILayout.ObjectField(
            "Assembly", _assembly, typeof(GarageChainAssembly), true);
        if (_assembly != null && _spec == null)
            _spec = _assembly.spec;

        if (_spec == null)
        {
            if (GUILayout.Button("Create GarageChainSpec"))
            {
                var path = EditorUtility.SaveFilePanelInProject("Save Garage Chain", "GarageChain", "asset", "");
                if (!string.IsNullOrEmpty(path))
                {
                    var s = CreateInstance<GarageChainSpec>();
                    AssetDatabase.CreateAsset(s, path);
                    _spec = s;
                }
            }
            EditorGUILayout.EndScrollView();
            return;
        }

        _spec.totalLengthM = EditorGUILayout.FloatField("Length (m)", _spec.totalLengthM);
        _spec.linkPitchM = EditorGUILayout.FloatField("Link pitch (m)", _spec.linkPitchM);
        _kind = (GarageChainLinkKind)EditorGUILayout.EnumPopup("Link definition", _kind);
        _spec.selectedKind = _kind;
        DrawLinkDef(_spec.DefFor(_kind));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Axle / teeth", EditorStyles.boldLabel);
        _spec.axleDiameterM = EditorGUILayout.FloatField("Axle diameter (m)", _spec.axleDiameterM);
        _spec.axleLocalPosition = EditorGUILayout.Vector3Field("Axle local position", _spec.axleLocalPosition);
        _spec.axleLocalEuler = EditorGUILayout.Vector3Field("Axle local euler", _spec.axleLocalEuler);
        _spec.toothCount = EditorGUILayout.IntSlider("Tooth count", Mathf.Max(3, _spec.toothCount), 3, 32);
        _spec.pitchRadiusM = EditorGUILayout.FloatField("Pitch radius (m)", _spec.pitchRadiusM);
        _spec.toothDepthM = EditorGUILayout.FloatField("Tooth depth (m)", _spec.toothDepthM);
        _spec.SyncRadialFromTeeth();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Steel limits", EditorStyles.boldLabel);
        var st = _spec.steel ?? new GarageSteelLimits();
        st.chainYieldN = EditorGUILayout.FloatField("Chain yield N", st.chainYieldN);
        st.chainBreakN = EditorGUILayout.FloatField("Chain break N", st.chainBreakN);
        st.masterBreakN = EditorGUILayout.FloatField("Master break N", st.masterBreakN);
        st.brokenBreakN = EditorGUILayout.FloatField("Broken break N", st.brokenBreakN);
        _spec.steel = st;

        _linkMount = (PixelLightGridMountGameObject)EditorGUILayout.ObjectField(
            "Link / axle mount", _linkMount, typeof(PixelLightGridMountGameObject), true);
        if (_linkMount == null && _assembly != null)
            _linkMount = _assembly.axleMount;
        if (_linkMount != null)
            PixelLightRadialBrushDrawer.DrawOnMount(_linkMount);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Ensure axle + host") && _assembly != null)
        {
            _assembly.spec = _spec;
            _assembly.EnsureHost();
            _assembly.EnsureAxleMount();
            _linkMount = _assembly.axleMount;
            EditorUtility.SetDirty(_assembly);
        }
        if (GUILayout.Button("Stamp links") && _assembly != null)
        {
            _assembly.spec = _spec;
            _assembly.RebuildLinkMounts();
            EditorUtility.SetDirty(_assembly);
        }
        if (GUILayout.Button("Bake SPH pull") && _assembly != null)
        {
            _assembly.spec = _spec;
            _assembly.BakePull();
            EditorUtility.SetDirty(_assembly);
        }
        EditorGUILayout.EndHorizontal();

        if (_assembly != null && _assembly.pullField != null)
            EditorGUILayout.LabelField("SPH bins", _assembly.pullField.BinCount.ToString());

        if (GUI.changed)
            EditorUtility.SetDirty(_spec);
        EditorGUILayout.EndScrollView();
    }

    static void DrawLinkDef(GarageChainLinkDef def)
    {
        if (def == null) return;
        def.massKg = EditorGUILayout.FloatField("Mass kg", def.massKg);
        def.jointId = EditorGUILayout.TextField("Joint id", def.jointId ?? "");
        def.joinKind = (RadialJoinKind)EditorGUILayout.EnumPopup("Join kind", def.joinKind);
        def.joinOffset = EditorGUILayout.FloatField("Join offset", def.joinOffset);
        def.pieceCurve = (CustomRadialSideAsset)EditorGUILayout.ObjectField(
            "Piece curve", def.pieceCurve, typeof(CustomRadialSideAsset), false);
        def.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", def.prefab, typeof(GameObject), false);
    }
}
#endif
