#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class LeafDesignerWindow : EditorWindow
{
    LeafSpec _leaf;
    KarmanVortexWindFit _fit;
    int _samples = 16;
    Vector2 _scroll;

    [MenuItem("Window/System Drawer/Leaf Designer")]
    public static void Open()
    {
        var w = GetWindow<LeafDesignerWindow>("Leaf Designer");
        w.minSize = new Vector2(420, 480);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _leaf = (LeafSpec)EditorGUILayout.ObjectField("Leaf", _leaf, typeof(LeafSpec), false);
        if (_leaf == null && GUILayout.Button("New leaf"))
            _leaf = CreateInstance<LeafSpec>();
        if (_leaf == null)
        {
            EditorGUILayout.HelpBox("Create or assign a LeafSpec.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        _leaf.averageWaterRequirement01 = EditorGUILayout.Slider(
            "Average water", _leaf.averageWaterRequirement01, 0f, 1f);
        _leaf.overwater = (Texture2D)EditorGUILayout.ObjectField("Overwater", _leaf.overwater, typeof(Texture2D), false);
        _leaf.underwater = (Texture2D)EditorGUILayout.ObjectField("Underwater", _leaf.underwater, typeof(Texture2D), false);
        _leaf.useRibbonSides = EditorGUILayout.Toggle("Ribbon sides", _leaf.useRibbonSides);
        _leaf.sdfMesh = (Mesh)EditorGUILayout.ObjectField("SDF mesh", _leaf.sdfMesh, typeof(Mesh), false);
        EditorGUILayout.LabelField("Center samples", _leaf.centerCurve != null ? _leaf.centerCurve.Count.ToString() : "0");
        if (GUILayout.Button("Reset center curve"))
        {
            _leaf.centerCurve = new System.Collections.Generic.List<Vector3>
            {
                Vector3.zero,
                new Vector3(0f, 0.5f, 0.02f),
                new Vector3(0f, 1f, 0f)
            };
        }

        _fit = (KarmanVortexWindFit)EditorGUILayout.ObjectField("Karman fit", _fit != null ? _fit : _leaf.windFit, typeof(KarmanVortexWindFit), false);
        if (_fit == null && GUILayout.Button("New wind fit"))
        {
            _fit = CreateInstance<KarmanVortexWindFit>();
            _leaf.windFit = _fit;
        }
        if (_fit != null)
        {
            _leaf.windFit = _fit;
            _fit.spacing = EditorGUILayout.FloatField("Spacing", _fit.spacing);
            _fit.sigma = EditorGUILayout.FloatField("Sigma", _fit.sigma);
            _fit.amplitude = EditorGUILayout.FloatField("Amplitude", _fit.amplitude);
            _samples = EditorGUILayout.IntSlider("Samples", _samples, 2, 64);
            if (GUILayout.Button("Prebake vortex street"))
            {
                _fit.Prebake(_samples);
                EditorUtility.SetDirty(_fit);
            }
            EditorGUILayout.LabelField("Baked", _fit.bakedBend != null ? _fit.bakedBend.Length.ToString() : "0");
        }

        EditorGUILayout.EndScrollView();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(_leaf);
            if (_fit != null)
                EditorUtility.SetDirty(_fit);
        }
    }
}
#endif
