#if UNITY_EDITOR
using System.Collections.Generic;
using Locomotion.Audio;
using Locomotion.Narrative.Music;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Window → System Drawer → Animation → Dance Animation Editor
/// Recipe-style left rail, quantized time ruler, dialogue sequences, rainbow mirror map.
/// </summary>
public sealed class DanceAnimationEditorWindow : EditorWindow
{
    enum Tab { Routine, Pairing, Webcam }

    const string Folder = "Assets/Dance";
    const float LeftWidth = 180f;
    const float TimeWidth = 88f;
    const float DialogWidth = 200f;
    const float CenterLinePx = 5f;

    Tab _tab = Tab.Routine;
    Vector2 _leftScroll;
    Vector2 _timeScroll;
    Vector2 _dialogScroll;
    Vector2 _mainScroll;
    DanceRoutineBehaviorTreeAsset _routine;
    readonly List<DanceRoutineBehaviorTreeAsset> _routines = new List<DanceRoutineBehaviorTreeAsset>();
    RagdollIKAnimationManager _ikManager;
    CausalityMusicBridge _musicBridge;
    BeatQuantizedActionBinder _beatBinder;
    DigitalEffectsMachine _fxMachine;
    string _status = "";
    int _pendingCallIndex;
    int _pendingResponseIndex;
    float _pendingCallSlot = 0.25f;
    float _pendingResponseSlot = 0.75f;
    double _playheadMs;
    bool _useLocalDetect = true;

    [MenuItem("Window/System Drawer/Animation/Dance Animation Editor", false, 104)]
    public static void ShowWindow()
    {
        var w = GetWindow<DanceAnimationEditorWindow>("Dance");
        w.minSize = new Vector2(1100, 560);
    }

    void OnEnable() => Refresh();

    void Refresh()
    {
        _routines.Clear();
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets", "Dance");
        foreach (var guid in AssetDatabase.FindAssets("t:DanceRoutineBehaviorTreeAsset", new[] { Folder, "Assets" }))
        {
            var a = AssetDatabase.LoadAssetAtPath<DanceRoutineBehaviorTreeAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (a != null)
                _routines.Add(a);
        }
    }

    void OnGUI()
    {
        _tab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "Routine", "Pairing", "Webcam" });
        EditorGUILayout.BeginHorizontal();
        DrawLeft();
        DrawTimeColumn();
        DrawDialogColumn();
        DrawThickCenterLine();
        _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
        switch (_tab)
        {
            case Tab.Routine: DrawRoutine(); break;
            case Tab.Pairing: DrawPairing(); break;
            case Tab.Webcam: DrawWebcamTab(); break;
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndHorizontal();
        if (!string.IsNullOrEmpty(_status))
            EditorGUILayout.HelpBox(_status, MessageType.Info);
    }

    void DrawLeft()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(LeftWidth));
        _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
        if (GUILayout.Button("Refresh"))
            Refresh();
        if (GUILayout.Button("+ New dance"))
        {
            _routine = CreateRoutine();
            Refresh();
        }
        foreach (var r in _routines)
        {
            if (r == null)
                continue;
            bool on = _routine == r;
            if (GUILayout.Toggle(on, r.displayName, "Button") && !on)
                _routine = r;
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawTimeColumn()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(TimeWidth));
        EditorGUILayout.LabelField("Time", EditorStyles.miniBoldLabel);
        _musicBridge = (CausalityMusicBridge)EditorGUILayout.ObjectField(
            _musicBridge, typeof(CausalityMusicBridge), true);
        _beatBinder = (BeatQuantizedActionBinder)EditorGUILayout.ObjectField(
            _beatBinder, typeof(BeatQuantizedActionBinder), true);
        _fxMachine = (DigitalEffectsMachine)EditorGUILayout.ObjectField(
            _fxMachine, typeof(DigitalEffectsMachine), true);

        if (_routine == null)
        {
            EditorGUILayout.HelpBox("Select a routine.", MessageType.None);
            EditorGUILayout.EndVertical();
            return;
        }

        if (GUILayout.Button("Pull BPM") && (_beatBinder != null || _musicBridge != null))
        {
            Undo.RecordObject(_routine, "Pull dance BPM");
            PullClockFromScene();
            EditorUtility.SetDirty(_routine);
        }
        double duration = Mathf.Max(1f, (float)_routine.TimelineDurationMs());
        _playheadMs = EditorGUILayout.DoubleField("ms", _playheadMs);
        _playheadMs = Mathf.Clamp((float)_playheadMs, 0f, (float)duration);

        if (GUILayout.Button("Quantize clips") && _routine.containsSong)
        {
            Undo.RecordObject(_routine, "Quantize song clips");
            var spans = DanceClipQuantizer.FromSources(
                _musicBridge, _fxMachine, _routine.bpm, _routine.subdivision,
                _routine.quantize01, _routine.TimelineGranularity);
            if (_routine.songSpans == null)
                _routine.songSpans = new List<DanceMediaSpan>();
            _routine.songSpans.Clear();
            _routine.songSpans.AddRange(spans);
            EditorUtility.SetDirty(_routine);
            _status = "Quantized " + spans.Count + " clip(s) onto mixer grid.";
        }

        _timeScroll = EditorGUILayout.BeginScrollView(_timeScroll);
        float height = Mathf.Max(240f, (float)(duration / 40.0));
        var rect = GUILayoutUtility.GetRect(TimeWidth - 12f, height, GUILayout.Width(TimeWidth - 12f), GUILayout.Height(height));
        DrawTimeRuler(rect, duration);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawTimeRuler(Rect rect, double durationMs)
    {
        EditorGUI.DrawRect(rect, new Color(0.08f, 0.08f, 0.1f, 1f));
        float bpm = EffectiveBpm();
        float barMs = 60f / Mathf.Max(1f, bpm) * Mathf.Max(1, _routine.beatsPerBar) * 1000f;
        Handles.BeginGUI();
        int bars = Mathf.Max(1, Mathf.CeilToInt((float)(durationMs / barMs)));
        for (int i = 0; i <= bars; i++)
        {
            float y = Mathf.Lerp(rect.y, rect.yMax, (float)((i * barMs) / durationMs));
            if (y > rect.yMax)
                break;
            Handles.color = new Color(0.35f, 0.55f, 0.95f, 0.55f);
            Handles.DrawLine(new Vector3(rect.x, y), new Vector3(rect.xMax, y));
            GUI.Label(new Rect(rect.x + 2, y, 40, 14), "b" + i, EditorStyles.miniLabel);
        }

        if (_routine.containsSong && _routine.songSpans != null)
        {
            for (int i = 0; i < _routine.songSpans.Count; i++)
            {
                var s = _routine.songSpans[i];
                if (s == null)
                    continue;
                float y0 = Mathf.Lerp(rect.y, rect.yMax, (float)(s.startMs / durationMs));
                float y1 = Mathf.Lerp(rect.y, rect.yMax, (float)(s.endMs / durationMs));
                EditorGUI.DrawRect(new Rect(rect.x + 28, y0, 18, Mathf.Max(2f, y1 - y0)), new Color(0.2f, 0.75f, 0.45f, 0.55f));
            }
        }

        float py = Mathf.Lerp(rect.y, rect.yMax, (float)(_playheadMs / durationMs));
        Handles.color = Color.white;
        Handles.DrawLine(new Vector3(rect.x, py), new Vector3(rect.xMax, py));
        Handles.EndGUI();
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            float t = Mathf.InverseLerp(rect.y, rect.yMax, Event.current.mousePosition.y);
            _playheadMs = DanceMediaSpan.SnapOne(
                t * durationMs, _routine.bpm, _routine.subdivision, _routine.quantize01,
                _routine.TimelineGranularity);
            Event.current.Use();
            Repaint();
        }
    }

    void DrawDialogColumn()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(DialogWidth));
        EditorGUILayout.LabelField("Dialogue", EditorStyles.miniBoldLabel);
        if (_routine == null)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        using (new EditorGUI.DisabledScope(!_routine.containsDialog))
        {
            if (!_routine.containsDialog)
                EditorGUILayout.HelpBox("Contains dialog off.", MessageType.None);
            _dialogScroll = EditorGUILayout.BeginScrollView(_dialogScroll);
            var spans = _routine.ActiveDialogSpans;
            double duration = Mathf.Max(1f, (float)_routine.TimelineDurationMs());
            for (int i = 0; i < spans.Count; i++)
            {
                var s = spans[i];
                if (s == null)
                    continue;
                EditorGUILayout.BeginVertical("box");
                s.label = EditorGUILayout.TextField(s.label);
                s.startMs = EditorGUILayout.DoubleField("In", s.startMs);
                s.endMs = EditorGUILayout.DoubleField("Out", s.endMs);
                s.dialogueSetId = EditorGUILayout.TextField("Set id", s.dialogueSetId);
                s.audioRef = EditorGUILayout.TextField("audioRef", s.audioRef);
                float y0 = (float)(s.startMs / duration);
                float y1 = (float)(s.endMs / duration);
                var bar = GUILayoutUtility.GetRect(DialogWidth - 24f, 8f);
                EditorGUI.DrawRect(bar, new Color(0.12f, 0.12f, 0.16f, 1f));
                float x0 = Mathf.Lerp(bar.x, bar.xMax, Mathf.Clamp01(y0));
                float x1 = Mathf.Lerp(bar.x, bar.xMax, Mathf.Clamp01(y1));
                EditorGUI.DrawRect(new Rect(x0, bar.y, Mathf.Max(2f, x1 - x0), bar.height), new Color(0.55f, 0.7f, 1f, 0.8f));
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    void DrawThickCenterLine()
    {
        var rect = GUILayoutUtility.GetRect(CenterLinePx, 4f, GUILayout.Width(CenterLinePx), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.2f, 1f));
        var inner = new Rect(rect.x + 1f, rect.y, rect.width - 2f, rect.height);
        EditorGUI.DrawRect(inner, new Color(0.35f, 0.55f, 0.95f, 1f));
    }

    void DrawRoutine()
    {
        _routine = (DanceRoutineBehaviorTreeAsset)EditorGUILayout.ObjectField(
            "Routine", _routine, typeof(DanceRoutineBehaviorTreeAsset), false);
        if (_routine == null)
            return;
        Undo.RecordObject(_routine, "Dance routine");
        _routine.displayName = EditorGUILayout.TextField("Name", _routine.displayName);
        _routine.catalogModeId = EditorGUILayout.TextField("Catalog mode id", _routine.catalogModeId);
        EditorGUILayout.HelpBox(
            "Moves are availableAnimations indices. DanceIkTrainingCatalog mode ids and LoveMakingAnimationGroup.DanceClose are secondary labels only.",
            MessageType.None);
        _ikManager = (RagdollIKAnimationManager)EditorGUILayout.ObjectField(
            "IK Animation Manager", _ikManager, typeof(RagdollIKAnimationManager), true);

        _routine.containsDialog = EditorGUILayout.Toggle("Contains dialog", _routine.containsDialog);
        _routine.containsSong = EditorGUILayout.Toggle("Contains song", _routine.containsSong);
        _routine.bpm = EditorGUILayout.FloatField("BPM", _routine.bpm);
        _routine.beatsPerBar = EditorGUILayout.IntField("Beats / bar", _routine.beatsPerBar);
        _routine.subdivision = EditorGUILayout.IntField("Subdivision", _routine.subdivision);
        _routine.quantize01 = EditorGUILayout.Slider("Quantize", _routine.quantize01, 0f, 1f);
        _useLocalDetect = EditorGUILayout.Toggle("Local detect (fast)", _useLocalDetect);

        if (_routine.containsDialog)
        {
            _routine.dialogAnalysisModelSpec = EditorGUILayout.TextField(
                "Dialog model_spec", _routine.dialogAnalysisModelSpec);
            DrawSpanList("Dialog spans", _routine.dialogSpans, true);
            if (GUILayout.Button("Detect dialog"))
                RunDetect(dialog: true);
        }

        if (_routine.containsSong)
        {
            _routine.songAnalysisModelSpec = EditorGUILayout.TextField(
                "Song model_spec", _routine.songAnalysisModelSpec);
            DrawSpanList("Song spans", _routine.songSpans, false);
            if (GUILayout.Button("Detect song"))
                RunDetect(dialog: false);
        }

        EditorGUILayout.LabelField("Moves (animation list indices)");
        if (_routine.moveAnimationIndices == null)
            _routine.moveAnimationIndices = new List<int>();
        int remove = -1;
        for (int i = 0; i < _routine.moveAnimationIndices.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            _routine.moveAnimationIndices[i] = AnimationIndexField(_routine.moveAnimationIndices[i]);
            if (GUILayout.Button("×", GUILayout.Width(24)))
                remove = i;
            EditorGUILayout.EndHorizontal();
        }
        if (remove >= 0)
            _routine.moveAnimationIndices.RemoveAt(remove);
        if (GUILayout.Button("+ Add move"))
            _routine.moveAnimationIndices.Add(0);
        EditorUtility.SetDirty(_routine);
    }

    void DrawSpanList(string title, List<DanceMediaSpan> list, bool dialog)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        if (list == null)
            return;
        int drop = -1;
        for (int i = 0; i < list.Count; i++)
        {
            var s = list[i];
            if (s == null)
                continue;
            EditorGUILayout.BeginHorizontal();
            s.label = EditorGUILayout.TextField(s.label, GUILayout.MinWidth(60));
            s.startMs = EditorGUILayout.DoubleField(s.startMs, GUILayout.Width(64));
            s.endMs = EditorGUILayout.DoubleField(s.endMs, GUILayout.Width(64));
            if (dialog)
                s.dialogueSetId = EditorGUILayout.TextField(s.dialogueSetId, GUILayout.Width(72));
            if (GUILayout.Button("×", GUILayout.Width(24)))
                drop = i;
            EditorGUILayout.EndHorizontal();
        }
        if (drop >= 0)
            list.RemoveAt(drop);
        if (GUILayout.Button("+ Add span"))
        {
            list.Add(new DanceMediaSpan
            {
                startMs = _playheadMs,
                endMs = _playheadMs + 1000,
                label = dialog ? "line" : "clip"
            });
        }
    }

    void RunDetect(bool dialog)
    {
        if (_routine == null)
            return;
        string spec = dialog ? _routine.dialogAnalysisModelSpec : _routine.songAnalysisModelSpec;
        string src = _routine.webcamTake != null ? _routine.webcamTake.localClipPath : "";
        IAudioSpanDetector det;
        if (_useLocalDetect)
            det = new LocalStubAudioSpanDetector();
        else if (dialog)
            det = new WhisperDialogSpanDetector();
        else
            det = new MusicAnalysisSpanDetector();
        var found = det.Detect(src, spec);
        Undo.RecordObject(_routine, "Detect dance spans");
        var target = dialog ? _routine.dialogSpans : _routine.songSpans;
        target.Clear();
        if (found != null)
            target.AddRange(found);
        _routine.SnapAllSpans();
        EditorUtility.SetDirty(_routine);
        _status = det.Id + " spans=" + target.Count;
    }

    void DrawPairing()
    {
        if (_routine == null)
        {
            EditorGUILayout.HelpBox("Select a routine.", MessageType.Info);
            return;
        }
        Undo.RecordObject(_routine, "Dance pairing");
        _routine.allowIntersect = EditorGUILayout.Toggle("Allow intersecting pairs", _routine.allowIntersect);
        EditorGUILayout.HelpBox(
            "Rainbow map: hue starts at blue for perpendicular (90°) associations, then gradients toward red as pairs go off-axis. " +
            "Diagonal line marks intersecting call/response segments. Default veto blocks those unless Allow intersecting pairs is on.",
            MessageType.None);

        DrawMirrorMap(_routine);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("New pairing", EditorStyles.boldLabel);
        _pendingCallIndex = AnimationIndexField(_pendingCallIndex, "Call index");
        _pendingResponseIndex = AnimationIndexField(_pendingResponseIndex, "Response index");
        _pendingCallSlot = EditorGUILayout.Slider("Call slot", _pendingCallSlot, 0f, 1f);
        _pendingResponseSlot = EditorGUILayout.Slider("Response slot", _pendingResponseSlot, 0f, 1f);
        if (GUILayout.Button("Add pairing"))
        {
            var p = new DancePairing
            {
                callAnimationIndex = _pendingCallIndex,
                responseAnimationIndex = _pendingResponseIndex,
                callSlot01 = _pendingCallSlot,
                responseSlot01 = _pendingResponseSlot,
                danceModeId = _routine.catalogModeId
            };
            if (_routine.TryAddPairing(p, out var err))
            {
                _status = "Added pairing.";
                EditorUtility.SetDirty(_routine);
            }
            else
                _status = err;
        }

        int drop = -1;
        for (int i = 0; i < _routine.pairings.Count; i++)
        {
            var p = _routine.pairings[i];
            if (p == null)
                continue;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"call {p.callAnimationIndex} → resp {p.responseAnimationIndex}");
            var col = DanceMirrorMap.HueFromOffset(DanceMirrorMap.OffsetFromPerpendicular(p));
            var sw = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18));
            EditorGUI.DrawRect(sw, col);
            if (GUILayout.Button("×", GUILayout.Width(24)))
                drop = i;
            EditorGUILayout.EndHorizontal();
        }
        if (drop >= 0)
        {
            _routine.pairings.RemoveAt(drop);
            EditorUtility.SetDirty(_routine);
        }
    }

    void DrawMirrorMap(DanceRoutineBehaviorTreeAsset routine)
    {
        const float size = 280f;
        var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
        EditorGUI.DrawRect(rect, new Color(0.08f, 0.08f, 0.12f, 1f));
        Handles.BeginGUI();
        var c0 = new Vector2(rect.x + 8, rect.yMax - 8);
        var c1 = new Vector2(rect.xMax - 8, rect.y + 8);
        Handles.color = new Color(1f, 1f, 1f, 0.25f);
        Handles.DrawLine(c0, c1);

        if (routine.pairings != null)
        {
            for (int i = 0; i < routine.pairings.Count; i++)
            {
                var p = routine.pairings[i];
                if (p == null)
                    continue;
                var a = SlotToPoint(rect, 0f, p.callSlot01);
                var b = SlotToPoint(rect, 1f, p.responseSlot01);
                Handles.color = DanceMirrorMap.HueFromOffset(DanceMirrorMap.OffsetFromPerpendicular(p));
                Handles.DrawAAPolyLine(3f, new Vector3(a.x, a.y, 0f), new Vector3(b.x, b.y, 0f));
                bool blocked = DanceMirrorMap.IsBlockedByIntersect(routine.pairings.ToArray(), p, routine.allowIntersect);
                if (!routine.allowIntersect && blocked)
                {
                    Handles.color = Color.white;
                    Handles.DrawDottedLine(a, b, 4f);
                }
            }
        }
        Handles.EndGUI();
        GUI.Label(new Rect(rect.x + 4, rect.y + 4, 120, 18), "mirror map", EditorStyles.miniLabel);
    }

    static Vector2 SlotToPoint(Rect rect, float x01, float y01)
    {
        float pad = 8f;
        float x = Mathf.Lerp(rect.x + pad, rect.xMax - pad, x01);
        float y = Mathf.Lerp(rect.yMax - pad, rect.y + pad, y01);
        return new Vector2(x, y);
    }

    void DrawWebcamTab()
    {
        EditorGUILayout.HelpBox("Opens the shared IK Webcam / Video Interpretation window with Dance kind.", MessageType.Info);
        if (GUILayout.Button("Open IK Webcam Video (Dance)"))
            IkWebcamVideoInterpretationWindow.OpenForKind(WebcamAnimKind.Dance);
        if (_routine != null)
        {
            _routine.webcamTake = (WebcamAnimRecordingAsset)EditorGUILayout.ObjectField(
                "Webcam take", _routine.webcamTake, typeof(WebcamAnimRecordingAsset), false);
        }
    }

    void PullClockFromScene()
    {
        if (_routine == null)
            return;
        if (_beatBinder != null && _beatBinder.bpm > 0f)
        {
            _routine.bpm = _beatBinder.bpm;
            if (_beatBinder.beatsPerBar > 0)
                _routine.beatsPerBar = _beatBinder.beatsPerBar;
            if (_beatBinder.subdivision > 0)
                _routine.subdivision = _beatBinder.subdivision;
        }
        else if (_musicBridge != null && _musicBridge.dialogueBpm > 0f)
        {
            _routine.bpm = _musicBridge.dialogueBpm;
            _routine.quantize01 = _musicBridge.playerInteractionQuantize01;
        }
    }

    float EffectiveBpm()
    {
        if (_beatBinder != null && _beatBinder.bpm > 0f)
            return _beatBinder.bpm;
        if (_musicBridge != null && _musicBridge.dialogueBpm > 0f)
            return _musicBridge.dialogueBpm;
        return _routine != null ? _routine.bpm : 120f;
    }

    int AnimationIndexField(int index, string label = null)
    {
        var labels = new List<string> { "(none)" };
        var values = new List<int> { -1 };
        if (_ikManager != null && _ikManager.availableAnimations != null)
        {
            for (int i = 0; i < _ikManager.availableAnimations.Count; i++)
            {
                var set = _ikManager.availableAnimations[i];
                string n = set != null && !string.IsNullOrEmpty(set.displayName) ? set.displayName : "set " + i;
                labels.Add(i + ": " + n);
                values.Add(i);
            }
        }
        else
        {
            labels.Add(index.ToString());
            values.Add(index);
        }
        int cur = values.IndexOf(index);
        if (cur < 0)
        {
            labels.Add("idx " + index);
            values.Add(index);
            cur = values.Count - 1;
        }
        int next = string.IsNullOrEmpty(label)
            ? EditorGUILayout.Popup(cur, labels.ToArray())
            : EditorGUILayout.Popup(label, cur, labels.ToArray());
        return values[next];
    }

    DanceRoutineBehaviorTreeAsset CreateRoutine()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets", "Dance");
        var a = CreateInstance<DanceRoutineBehaviorTreeAsset>();
        a.displayName = "Dance";
        a.routineId = "dance_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
        string path = AssetDatabase.GenerateUniqueAssetPath(Folder + "/DanceRoutine.asset");
        AssetDatabase.CreateAsset(a, path);
        AssetDatabase.SaveAssets();
        return a;
    }
}
#endif
