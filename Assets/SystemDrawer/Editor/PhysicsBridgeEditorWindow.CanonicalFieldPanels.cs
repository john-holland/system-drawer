#if UNITY_EDITOR
using System.Collections.Generic;
using Planetary.Field;
using UnityEditor;
using UnityEngine;

public partial class PhysicsBridgeEditorWindow
{
    bool _showChartBlend = true;
    bool _showNearFieldViz = true;
    bool _showVolumeProbe = true;
    float _probeNarrativeTime;

    void DrawCanonicalFieldPanels()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Canonical field", EditorStyles.boldLabel);

        CanonicalSpatiotemporalField field = CanonicalSpatiotemporalField.Resolve();
        if (field == null)
        {
            EditorGUILayout.HelpBox("No CanonicalSpatiotemporalField in scene.", MessageType.Info);
            return;
        }

        _probeNarrativeTime = EditorGUILayout.FloatField("Probe narrative time", _probeNarrativeTime);
        _showChartBlend = EditorGUILayout.Foldout(_showChartBlend, "Chart blend at probe", true);
        if (_showChartBlend && _hasProbe)
        {
            if (field.TrySampleBlended(_probeWorld, _probeNarrativeTime, out SpatiotemporalSample blended))
            {
                EditorGUILayout.LabelField("Dominant chart", blended.dominantChart.ToString());
                EditorGUILayout.LabelField("Velocity", blended.velocityWorld.ToString("F2"));
                EditorGUILayout.LabelField("Friction", blended.surfaceFriction.ToString("F3"));
            }

            foreach (SpatiotemporalChart chart in System.Enum.GetValues(typeof(SpatiotemporalChart)))
            {
                if (field.TrySample(_probeWorld, _probeNarrativeTime, chart, out SpatiotemporalSample s))
                    EditorGUILayout.LabelField(chart.ToString(), $"μ={s.surfaceFriction:F3} v={s.velocityWorld.magnitude:F2}");
            }
        }

        _showNearFieldViz = EditorGUILayout.Foldout(_showNearFieldViz, "Near-field graph", true);
        if (_showNearFieldViz)
        {
            var graph = FindAnyObjectByType<Weather.NearField.NearFieldWindInteractionGraph>();
            if (graph == null)
                EditorGUILayout.HelpBox("No NearFieldWindInteractionGraph in scene.", MessageType.None);
            else
            {
                graph.nearFieldRadiusM = EditorGUILayout.Slider("Near radius (m)", graph.nearFieldRadiusM, 5f, 80f);
                if (GUILayout.Button("Select near-field graph"))
                    Selection.activeObject = graph;
            }
        }

        _showVolumeProbe = EditorGUILayout.Foldout(_showVolumeProbe, "Curved volumes at probe", true);
        if (_showVolumeProbe && _hasProbe)
        {
            int count = 0;
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb is ISpatiotemporalVolume vol && vol.Contains(_probeWorld, _probeNarrativeTime))
                {
                    EditorGUILayout.LabelField(mb.name, mb.GetType().Name);
                    count++;
                }
            }

            if (count == 0)
                EditorGUILayout.LabelField("(no volumes contain probe)", EditorStyles.miniLabel);
        }
    }
}
#endif
