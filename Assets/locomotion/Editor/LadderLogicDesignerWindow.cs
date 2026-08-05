using UnityEditor;
using UnityEngine;

/// <summary>Edits TrafficDetailLadderAsset steps and TrafficLightLadderTiming for intersections.</summary>
public sealed class LadderLogicDesignerWindow : EditorWindow
{
    TrafficDetailLadderAsset _ladder;
    TrafficLightLadderTiming _timing = TrafficLightLadderTiming.Default();
    TrafficLightController _previewLight;
    Vector2 _scroll;
    int _selectedStep;

    [MenuItem("Locomotion/Ladder Logic Designer")]
    public static void Open()
    {
        var w = GetWindow<LadderLogicDesignerWindow>("Ladder Logic");
        w.minSize = new Vector2(420, 360);
    }

    public static void OpenWith(TrafficDetailLadderAsset ladder)
    {
        Open();
        var w = GetWindow<LadderLogicDesignerWindow>();
        w._ladder = ladder;
    }

    public static void OpenWithTiming(TrafficLightLadderTiming timing, TrafficLightController light = null)
    {
        Open();
        var w = GetWindow<LadderLogicDesignerWindow>();
        w._timing = timing;
        w._previewLight = light;
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Ladder Logic Designer", EditorStyles.boldLabel);

        _ladder = (TrafficDetailLadderAsset)EditorGUILayout.ObjectField(
            "Traffic Detail Ladder", _ladder, typeof(TrafficDetailLadderAsset), false);

        if (GUILayout.Button("New Default Ladder Asset"))
        {
            var a = TrafficDetailLadderAsset.CreateDefaultRuntime();
            var path = EditorUtility.SaveFilePanelInProject("Save Ladder", "TrafficDetailLadder", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(a, path);
                _ladder = a;
            }
        }

        if (_ladder != null)
        {
            if (_ladder.steps == null || _ladder.steps.Count == 0)
                _ladder.steps = TrafficDetailLadderAsset.CreateDefaultRuntime().steps;

            EditorGUILayout.LabelField("Detail Steps", EditorStyles.boldLabel);
            for (int i = 0; i < _ladder.steps.Count; i++)
            {
                var step = _ladder.steps[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Toggle(_selectedStep == i, "Step " + i, EditorStyles.miniButton))
                    _selectedStep = i;
                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    _ladder.steps.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();
                step.stepId = EditorGUILayout.TextField("Id", step.stepId);
                step.durationSec = EditorGUILayout.FloatField("Duration Sec", step.durationSec);
                step.lightLemmaOrPhase = EditorGUILayout.TextField("Light Lemma/Phase", step.lightLemmaOrPhase);
                step.dispatchKind = EditorGUILayout.TextField("Dispatch Kind", step.dispatchKind);
                step.notes = EditorGUILayout.TextField("Notes", step.notes);
                step.emitCard = (TrafficDetailEmitCard)EditorGUILayout.EnumPopup("Emit Card", step.emitCard);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Step"))
                _ladder.steps.Add(new TrafficDetailLadderStep { stepId = "step_" + _ladder.steps.Count });

            if (GUILayout.Button("Mark Ladder Dirty"))
                EditorUtility.SetDirty(_ladder);
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Light Ladder Timing", EditorStyles.boldLabel);
        _timing.mainGreenSec = EditorGUILayout.FloatField("Main Green", _timing.mainGreenSec);
        _timing.sideGreenSec = EditorGUILayout.FloatField("Side Green", _timing.sideGreenSec);
        _timing.yellowSec = EditorGUILayout.FloatField("Yellow", _timing.yellowSec);
        _timing.allRedSec = EditorGUILayout.FloatField("All Red", _timing.allRedSec);
        _timing.sideSensorExtendSec = EditorGUILayout.FloatField("Side Sensor Extend", _timing.sideSensorExtendSec);
        _previewLight = (TrafficLightController)EditorGUILayout.ObjectField(
            "Apply To Light", _previewLight, typeof(TrafficLightController), true);
        if (GUILayout.Button("Apply Timing To Light") && _previewLight != null)
        {
            _timing.ApplyTo(_previewLight);
            EditorUtility.SetDirty(_previewLight);
        }

        EditorGUILayout.EndScrollView();
    }
}
