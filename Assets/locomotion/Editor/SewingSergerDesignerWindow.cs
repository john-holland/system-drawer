#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class SewingSergerDesignerWindow : EditorWindow
{
    SewingMachineSpec _sew;
    SergerSpec _serger;
    PixelLightDesignerView _view = PixelLightDesignerView.Front;
    Bounds4SdfInclusionKind _inclusion = Bounds4SdfInclusionKind.Frame;
    Vector2 _scroll;

    [MenuItem("Locomotion/Sewing + Serger Designer")]
    public static void Open()
    {
        var w = GetWindow<SewingSergerDesignerWindow>("Sewing / Serger");
        w.minSize = new Vector2(460, 560);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _sew = (SewingMachineSpec)EditorGUILayout.ObjectField(
            "Sewing machine", _sew, typeof(SewingMachineSpec), false);
        if (_sew == null && GUILayout.Button("Create SewingMachineSpec"))
        {
            var path = EditorUtility.SaveFilePanelInProject("Save Sewing Machine", "SewingMachine", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                _sew = CreateInstance<SewingMachineSpec>();
                AssetDatabase.CreateAsset(_sew, path);
            }
        }
        if (_sew != null)
        {
            _sew.needleKind = (CutToolKind)EditorGUILayout.EnumPopup("Needle", _sew.needleKind);
            _sew.needleBarGearbox = (GearboxSpec)EditorGUILayout.ObjectField(
                "Needle-bar gearbox", _sew.needleBarGearbox, typeof(GearboxSpec), false);
            _sew.pixelLightCatalog = GearboxLathePixelLightDrawer.DrawCatalogField(
                _sew.pixelLightCatalog, "SewingPixelLight");
            _sew.frameMount = (Bounds4SdfInclusionPixelLightMount)EditorGUILayout.ObjectField(
                "Frame mount", _sew.frameMount, typeof(Bounds4SdfInclusionPixelLightMount), true);
            _sew.shellMount = (Bounds4SdfInclusionPixelLightMount)EditorGUILayout.ObjectField(
                "Shell mount", _sew.shellMount, typeof(Bounds4SdfInclusionPixelLightMount), true);
            var mount = _inclusion == Bounds4SdfInclusionKind.Frame ? _sew.frameMount : _sew.shellMount;
            GearboxLathePixelLightDrawer.DrawFrameShell(
                _sew.pixelLightCatalog, ref _view, ref _inclusion, ref mount);
            if (_inclusion == Bounds4SdfInclusionKind.Frame)
                _sew.frameMount = mount;
            else
                _sew.shellMount = mount;
            EditorGUILayout.LabelField("Thread guides", string.Join(" → ", _sew.threadGuides));
        }

        EditorGUILayout.Space();
        _serger = (SergerSpec)EditorGUILayout.ObjectField("Serger", _serger, typeof(SergerSpec), false);
        if (_serger == null && GUILayout.Button("Create SergerSpec"))
        {
            var path = EditorUtility.SaveFilePanelInProject("Save Serger", "Serger", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                _serger = CreateInstance<SergerSpec>();
                AssetDatabase.CreateAsset(_serger, path);
            }
        }
        if (_serger != null)
        {
            _serger.needlePhase01 = EditorGUILayout.Slider("Needle phase", _serger.needlePhase01, 0f, 1f);
            _serger.looperPhase01 = EditorGUILayout.Slider("Looper phase", _serger.looperPhase01, 0f, 1f);
            _serger.differentialFeed01 = EditorGUILayout.Slider("Differential", _serger.differentialFeed01, 0f, 1f);
            _serger.threadTension01 = EditorGUILayout.Slider("Thread tension", _serger.threadTension01, 0f, 1f);
            EditorGUILayout.LabelField("Differential feed", _serger.DifferentialFeed().ToString("0.00"));
            if (_serger.IsJammed)
                EditorGUILayout.HelpBox("Serger jammed — tension over limit.", MessageType.Warning);
            _serger.ikCatalog = (TayloringIkTrainingCatalog)EditorGUILayout.ObjectField(
                "IK catalog", _serger.ikCatalog, typeof(TayloringIkTrainingCatalog), false);
            _serger.pixelLightCatalog = GearboxLathePixelLightDrawer.DrawCatalogField(
                _serger.pixelLightCatalog, "SergerPixelLight");
        }
        EditorGUILayout.EndScrollView();
        if (GUI.changed)
        {
            if (_sew != null) EditorUtility.SetDirty(_sew);
            if (_serger != null) EditorUtility.SetDirty(_serger);
        }
    }
}
#endif
