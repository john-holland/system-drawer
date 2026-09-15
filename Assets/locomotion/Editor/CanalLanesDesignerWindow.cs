#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public sealed class CanalLanesDesignerWindow : EditorWindow
{
    CanalRibbonSpec _ribbon;
    CanalLockSpec _lock;
    CanalWaterBakeField _bake = new CanalWaterBakeField();
    CityPixelGrid _grid;
    CannalTravelAgent _agent;
    Vector2 _scroll;
    string _output = "No refresh yet.";

    [MenuItem("Locomotion/Canal Lanes Designer")]
    public static void Open()
    {
        var w = GetWindow<CanalLanesDesignerWindow>("Canal Lanes");
        w.minSize = new Vector2(460, 520);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Canal Lanes Designer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Derivative of Road Lanes: X = across channel, Y = along spline. Water bake stores speed/velocity bins; live Weather sim is optional.",
            MessageType.Info);
        _ribbon = (CanalRibbonSpec)EditorGUILayout.ObjectField("Ribbon", _ribbon, typeof(CanalRibbonSpec), false);
        _lock = (CanalLockSpec)EditorGUILayout.ObjectField("Lock", _lock, typeof(CanalLockSpec), false);
        _grid = (CityPixelGrid)EditorGUILayout.ObjectField("City pixel grid", _grid, typeof(CityPixelGrid), false);
        _agent = (CannalTravelAgent)EditorGUILayout.ObjectField("Cannal TA", _agent, typeof(CannalTravelAgent), true);

        if (_ribbon == null)
        {
            if (GUILayout.Button("Create CanalRibbonSpec"))
            {
                var path = EditorUtility.SaveFilePanelInProject("Save Canal Ribbon", "CanalRibbon", "asset", "");
                if (!string.IsNullOrEmpty(path))
                {
                    var s = CreateInstance<CanalRibbonSpec>();
                    AssetDatabase.CreateAsset(s, path);
                    _ribbon = s;
                }
            }
            EditorGUILayout.EndScrollView();
            return;
        }

        _ribbon.lengthM = EditorGUILayout.FloatField("Length (m)", _ribbon.lengthM);
        _ribbon.widthM = EditorGUILayout.FloatField("Width (m)", _ribbon.widthM);
        _ribbon.depthM = EditorGUILayout.FloatField("Depth (m)", _ribbon.depthM);
        _ribbon.wallAngleDeg = EditorGUILayout.Slider("Wall angle", _ribbon.wallAngleDeg, 0f, 60f);
        _ribbon.wallMaterial = EditorGUILayout.TextField("Wall material", _ribbon.wallMaterial ?? "");
        _ribbon.cementInnerCurveM = EditorGUILayout.FloatField("Cement inner curve", _ribbon.cementInnerCurveM);
        _ribbon.cementOuterCurveM = EditorGUILayout.FloatField("Cement outer curve", _ribbon.cementOuterCurveM);
        _ribbon.girderSpacingM = EditorGUILayout.FloatField("Girder spacing", _ribbon.girderSpacingM);
        _ribbon.fabricConcreteFill = EditorGUILayout.Toggle("Fabric concrete fill", _ribbon.fabricConcreteFill);
        _ribbon.authoredSpeedMps = EditorGUILayout.FloatField("Authored speed m/s", _ribbon.authoredSpeedMps);
        _ribbon.authoredVelocityMps = EditorGUILayout.FloatField("Authored velocity m/s", _ribbon.authoredVelocityMps);
        _ribbon.bakeBinM = EditorGUILayout.FloatField("Bake bin (m)", _ribbon.bakeBinM);
        _ribbon.bakeNotLiveSim = EditorGUILayout.Toggle("Bake (not live sim)", _ribbon.bakeNotLiveSim);

        if (GUILayout.Button("Bake water bins"))
        {
            _bake.Bake(_ribbon);
            PushBakeToAgent();
            WriteOutput(false);
        }
        EditorGUILayout.LabelField("Water bins", _bake.BinCount.ToString());
        if (_grid != null)
            EditorGUILayout.HelpBox("Stamp Flood on CityPixelGrid in the City Pixel Grid Designer (layer Flood).", MessageType.None);
        if (GUILayout.Button("Open Cannal Travel Agent"))
            CannalTravelAgentWindow.OpenWith(_agent);
        if (GUILayout.Button("Open Gearbox Designer (lock engine)"))
            GearboxDesignerWindow.Open();

        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh"))
        {
            _bake.Bake(_ribbon, true);
            PushBakeToAgent();
            WriteOutput(true);
        }
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(_output, MessageType.None);

        if (GUI.changed && _ribbon != null)
            EditorUtility.SetDirty(_ribbon);
        EditorGUILayout.EndScrollView();
    }

    void PushBakeToAgent()
    {
        if (_agent == null) return;
        _agent.ribbon = _ribbon;
        _agent.lockSpec = _lock;
        _agent.waterBake = _bake;
        EditorUtility.SetDirty(_agent);
    }

    void WriteOutput(bool forced)
    {
        string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string extra = _agent != null
            ? _agent.DiamondInterpretation()
            : "Assign a Cannal Travel Agent to interpret Speed / Size / Justice / Threat.";
        _output = stamp + (forced ? "  refresh" : "  bake") +
                  $"  ·  bins {_bake.BinCount}  ·  hash {(_bake.bakeHash ?? "")}\n" + extra;
    }
}
#endif
