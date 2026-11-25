using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class SnowWizardWindow : EditorWindow
{
    SnowCover _cover;
    float _freezeMin = -2f;
    float _freezeMax = 0.5f;
    float _shellThickness = 0.04f;
    float _breakStress = 0.55f;
    Vector2 _scroll;

    [MenuItem("Window/System Drawer/Snow Wizard")]
    public static void Open()
    {
        GetWindow<SnowWizardWindow>("Snow Wizard");
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _cover = (SnowCover)EditorGUILayout.ObjectField("Cover", _cover, typeof(SnowCover), true);
        EditorGUILayout.LabelField("Ice parameters", EditorStyles.boldLabel);
        _freezeMin = EditorGUILayout.FloatField("Freeze min C", _freezeMin);
        _freezeMax = EditorGUILayout.FloatField("Freeze max C", _freezeMax);
        _shellThickness = EditorGUILayout.FloatField("Shell thickness m", _shellThickness);
        _breakStress = EditorGUILayout.Slider("Break stress", _breakStress, 0f, 1f);
        if (_cover != null)
        {
            _cover.pollenMix = EditorGUILayout.Slider("Pollen mix", _cover.pollenMix, 0f, 1f);
            _cover.depthInches = EditorGUILayout.FloatField("Depth inches", _cover.depthInches);
            if (_cover.layers == null)
                _cover.layers = new List<SnowGranularityLayer>();
            EditorGUILayout.LabelField("Granularity layers", EditorStyles.boldLabel);
            for (int i = 0; i < _cover.layers.Count; i++)
            {
                SnowGranularityLayer layer = _cover.layers[i];
                if (layer == null)
                    continue;
                EditorGUILayout.BeginVertical("box");
                layer.id = EditorGUILayout.TextField("Id", layer.id);
                layer.heightMinInches = EditorGUILayout.FloatField("Min inches", layer.heightMinInches);
                layer.heightMaxInches = EditorGUILayout.FloatField("Max inches", layer.heightMaxInches);
                layer.lowerBlend = EditorGUILayout.FloatField("Lower blend", layer.lowerBlend);
                layer.upperBlend = EditorGUILayout.FloatField("Upper blend", layer.upperBlend);
                layer.beadSize = EditorGUILayout.FloatField("Bead", layer.beadSize);
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add layer"))
                _cover.layers.Add(new SnowGranularityLayer { id = "layer_" + _cover.layers.Count });
            if (GUILayout.Button("Apply ice band"))
            {
                SnowVolume volume = _cover.GetComponent<SnowVolume>();
                if (volume != null)
                    volume.ApplyShellBand(_shellThickness);
            }
        }
        EditorGUILayout.EndScrollView();
    }
}
