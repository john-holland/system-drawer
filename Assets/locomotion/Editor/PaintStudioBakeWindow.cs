using UnityEditor;
using UnityEngine;
using SdfMax;

/// <summary>
/// Scaffolds paint studio assets: brush catalog builtins, tube SDF, canvas layer stack.
/// </summary>
public sealed class PaintStudioBakeWindow : EditorWindow
{
    string _outputFolder = "Assets/locomotion/painting/Baked";
    PaintBrushCatalog _catalog;
    PaintTubeConfig _tube;
    PaintCanvasLayerStack _stack;
    PaintingIkTrainingCatalog _ik;

    [MenuItem("Window/System Drawer/Paint Studio Bake")]
    public static void Open() => GetWindow<PaintStudioBakeWindow>("Paint Studio Bake");

    void OnGUI()
    {
        _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
        _catalog = (PaintBrushCatalog)EditorGUILayout.ObjectField("Brush Catalog", _catalog, typeof(PaintBrushCatalog), false);
        _tube = (PaintTubeConfig)EditorGUILayout.ObjectField("Tube Config", _tube, typeof(PaintTubeConfig), false);
        _stack = (PaintCanvasLayerStack)EditorGUILayout.ObjectField("Canvas Stack", _stack, typeof(PaintCanvasLayerStack), false);
        _ik = (PaintingIkTrainingCatalog)EditorGUILayout.ObjectField("IK Catalog", _ik, typeof(PaintingIkTrainingCatalog), false);

        if (GUILayout.Button("Create / Refresh All Defaults"))
            BakeAll();
    }

    void BakeAll()
    {
        EnsureFolder(_outputFolder);

        if (_catalog == null)
        {
            _catalog = CreateInstance<PaintBrushCatalog>();
            AssetDatabase.CreateAsset(_catalog, $"{_outputFolder}/PaintBrushCatalog.asset");
        }
        _catalog.EnsureBuiltins();
        for (int i = 0; i < _catalog.brushes.Count; i++)
        {
            var b = _catalog.brushes[i];
            if (b == null) continue;
            string path = $"{_outputFolder}/Brush_{b.kind}.asset";
            if (AssetDatabase.Contains(b)) continue;
            AssetDatabase.CreateAsset(b, AssetDatabase.GenerateUniqueAssetPath(path));
        }
        EditorUtility.SetDirty(_catalog);

        if (_tube == null)
        {
            _tube = CreateInstance<PaintTubeConfig>();
            AssetDatabase.CreateAsset(_tube, $"{_outputFolder}/PaintTubeConfig.asset");
        }
        var tubeSdf = PaintTubeSdfComposer.Compose(_tube);
        string tubePath = AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/PaintTubeSdf.asset");
        AssetDatabase.CreateAsset(tubeSdf, tubePath);

        if (_stack == null)
        {
            _stack = CreateInstance<PaintCanvasLayerStack>();
            AssetDatabase.CreateAsset(_stack, $"{_outputFolder}/PaintCanvasLayerStack.asset");
        }
        _stack.EnsureBaseLayer();
        if (_stack.layers[0].composition != null && !AssetDatabase.Contains(_stack.layers[0].composition))
        {
            AssetDatabase.AddObjectToAsset(_stack.layers[0].composition, _stack);
        }
        EditorUtility.SetDirty(_stack);

        if (_ik == null)
        {
            _ik = CreateInstance<PaintingIkTrainingCatalog>();
            AssetDatabase.CreateAsset(_ik, $"{_outputFolder}/PaintingIkTrainingCatalog.asset");
        }
        _ik.EnsureDefaults();
        EditorUtility.SetDirty(_ik);

        var map = CreateInstance<PaintInstrumentMap>();
        map.EnsureDefaults();
        AssetDatabase.CreateAsset(map, AssetDatabase.GenerateUniqueAssetPath($"{_outputFolder}/PaintInstrumentMap.asset"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Paint Studio", $"Assets ready under {_outputFolder}", "OK");
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string[] parts = folder.Replace("\\", "/").Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
