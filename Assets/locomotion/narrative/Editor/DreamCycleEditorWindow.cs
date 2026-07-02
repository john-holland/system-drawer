#if UNITY_EDITOR
using SystemDrawer.DreamCycle;
using UnityEditor;
using UnityEngine;

namespace Locomotion.Narrative.EditorTools
{
    /// <summary>Dream Cycle editor: day prompt, aspect meters, sleep wave preview, dream recall panel.</summary>
    public sealed class DreamCycleEditorWindow : EditorWindow
    {
        const string SamplePrompt = @"
{P:dream-day|aspect=need_physiological|spatial2d-slot=need_physiological|satisfied=0.7}
{P:dream-day|aspect=need_belonging|spatial2d-slot=need_belonging|satisfied=0.6}
";

        [MenuItem("Window/System Drawer/Dream Cycle")]
        public static void ShowWindow()
        {
            var win = GetWindow<DreamCycleEditorWindow>("Dream Cycle");
            win.minSize = new Vector2(640f, 480f);
        }

        string _dayPrompt = SamplePrompt;
        string _cityId = "earth-city";
        Vector2 _scroll;
        DreamDayCycleRunner _dayRunner;
        DreamNightCycleRunner _nightRunner;
        SleepWaveStatRenderer _sleepRenderer;
        Object _dreamMemoryLstm;
        NeedAspectRegistry _registry;
        string _status = "";

        void OnEnable()
        {
            _dayRunner = FindAnyObjectByType<DreamDayCycleRunner>();
            _nightRunner = FindAnyObjectByType<DreamNightCycleRunner>();
            _sleepRenderer = FindAnyObjectByType<SleepWaveStatRenderer>();
            var regs = Resources.FindObjectsOfTypeAll<NeedAspectRegistry>();
            if (regs != null && regs.Length > 0)
                _registry = regs[0];
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Dream Cycle", EditorStyles.boldLabel);
            _cityId = EditorGUILayout.TextField("City Id", _cityId);
            EditorGUILayout.LabelField("Day prompt (lemma dream-day spans)");
            _dayPrompt = EditorGUILayout.TextArea(_dayPrompt, GUILayout.MinHeight(80f));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Run day complete"))
                RunDay();
            if (GUILayout.Button("Run night complete"))
                RunNight();
            if (GUILayout.Button("Recall dream fragment"))
                RecallDream();
            EditorGUILayout.EndHorizontal();

            DrawServiceRefs();
            DrawAspectMeters();
            DrawSleepWavePreview();
            DrawSeedTreeReadOnly();

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, MessageType.Info);
        }

        void DrawServiceRefs()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene services", EditorStyles.boldLabel);
            _dayRunner = (DreamDayCycleRunner)EditorGUILayout.ObjectField("Day runner", _dayRunner, typeof(DreamDayCycleRunner), true);
            _nightRunner = (DreamNightCycleRunner)EditorGUILayout.ObjectField("Night runner", _nightRunner, typeof(DreamNightCycleRunner), true);
            _sleepRenderer = (SleepWaveStatRenderer)EditorGUILayout.ObjectField("Sleep renderer", _sleepRenderer, typeof(SleepWaveStatRenderer), true);
            _dreamMemoryLstm = EditorGUILayout.ObjectField("Dream memory LSTM", _dreamMemoryLstm, typeof(Object), true);
            _registry = (NeedAspectRegistry)EditorGUILayout.ObjectField("Need registry", _registry, typeof(NeedAspectRegistry), false);
        }

        void DrawAspectMeters()
        {
            if (_registry == null || _registry.aspects == null)
                return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Need satisfaction (derived / scene)", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(120f));
            for (int i = 0; i < _registry.aspects.Length; i++)
            {
                var a = _registry.aspects[i];
                float sat = 0.5f;
                if (_dayRunner != null)
                {
                    var slots = _dayRunner.slots;
                    if (slots != null)
                    {
                        for (int s = 0; s < slots.Length; s++)
                        {
                            if (slots[s] != null && slots[s].aspectId == a.aspectId)
                                sat = slots[s].satisfied01;
                        }
                    }
                }
                EditorGUILayout.LabelField(a.displayName, $"{sat:P0}");
                Rect r = EditorGUILayout.GetControlRect(false, 16f);
                EditorGUI.ProgressBar(r, sat, a.aspectId);
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawSleepWavePreview()
        {
            if (_sleepRenderer == null)
                return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sleep wave (electrical sheep → REM)", EditorStyles.boldLabel);
            if (_sleepRenderer.waveSamples != null && _sleepRenderer.waveSamples.Length > 0)
            {
                _sleepRenderer.RenderWave();
                var tex = typeof(SleepWaveStatRenderer).GetField("_tex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var t = tex?.GetValue(_sleepRenderer) as Texture2D;
                if (t != null)
                {
                    float h = 64f;
                    float w = position.width - 24f;
                    EditorGUI.DrawPreviewTexture(new Rect(12f, GUILayoutUtility.GetLastRect().yMax + 4f, w, h), t);
                    GUILayout.Space(h + 8f);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No wave samples yet. Run night complete.", MessageType.None);
            }
        }

        void DrawSeedTreeReadOnly()
        {
            var orch = FindAnyObjectByType<SpatialGenerator4DOrchestrator>();
            if (orch == null)
                return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Seed dependency (orchestrator)", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(orch.lockSeedDependencyTree);
            EditorGUILayout.IntField("Master seed", orch.masterSeed);
            EditorGUILayout.IntField("Day collapse seed", orch.dayCollapseSeed);
            EditorGUILayout.IntField("Sleep seed", orch.sleepSeed);
            EditorGUI.EndDisabledGroup();
            if (orch.lockSeedDependencyTree)
                EditorGUILayout.HelpBox("Seed tree locked — values are read-only.", MessageType.Warning);
        }

        void RunDay()
        {
            if (_dayRunner == null)
            {
                _status = "Assign DreamDayCycleRunner in scene.";
                return;
            }
            _dayRunner.cityId = _cityId;
            _dayRunner.dayPrompt = _dayPrompt;
            _dayRunner.RunDayComplete();
            _status = "Day complete requested (async). Protagonist aspects are derived, not per-citizen sim.";
        }

        void RunNight()
        {
            if (_nightRunner == null)
            {
                _status = "Assign DreamNightCycleRunner in scene.";
                return;
            }
            _nightRunner.RunNightComplete();
            _status = "Night complete requested (async).";
        }

        void RecallDream()
        {
            if (_dreamMemoryLstm == null)
            {
                _status = "Assign DreamMemoryLSTM.";
                return;
            }
            var recall = _dreamMemoryLstm.GetType().GetMethod("RecallDreamFragment");
            var fragment = recall?.Invoke(_dreamMemoryLstm, null);
            _status = fragment != null ? fragment.ToString() : "Recall invoked.";
        }
    }
}
#endif
