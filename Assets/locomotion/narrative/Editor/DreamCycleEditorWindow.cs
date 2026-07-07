#if UNITY_EDITOR
using SystemDrawer.DreamCycle;
using Locomotion.DreamCycle;
using UnityEditor;
using UnityEngine;

namespace Locomotion.Narrative.EditorTools
{
    /// <summary>Dream Cycle editor: double-day horizon, developer dream prompt, sleep wave, safe recall.</summary>
    public sealed class DreamCycleEditorWindow : EditorWindow
    {
        const string SampleDreamPrompt = @"
{P:dream-day|aspect=need_physiological|spatial2d-slot=need_physiological|satisfied=0.7}
{P:dream-day|aspect=need_belonging|spatial2d-slot=need_belonging|satisfied=0.6}
";

        [MenuItem("Window/System Drawer/Dream Cycle")]
        public static void ShowWindow()
        {
            var win = GetWindow<DreamCycleEditorWindow>("Dream Cycle");
            win.minSize = new Vector2(640f, 520f);
        }

        string _cityId = "earth-city";
        Vector2 _scroll;
        DreamDayCycleRunner _dayRunner;
        DreamNightCycleRunner _nightRunner;
        SleepWaveStatRenderer _sleepRenderer;
        Object _dreamMemoryLstm;
        NeedAspectRegistry _registry;
        DreamDaySimulationProfile _profile;
        string _status = "";

        void OnEnable()
        {
            _dayRunner = FindAnyObjectByType<DreamDayCycleRunner>();
            _nightRunner = FindAnyObjectByType<DreamNightCycleRunner>();
            _sleepRenderer = FindAnyObjectByType<SleepWaveStatRenderer>();
            if (_dayRunner != null)
                _profile = _dayRunner.profile;
            var regs = Resources.FindObjectsOfTypeAll<NeedAspectRegistry>();
            if (regs != null && regs.Length > 0)
                _registry = regs[0];
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Dream Cycle", EditorStyles.boldLabel);
            _cityId = EditorGUILayout.TextField("City Id", _cityId);

            DrawGoodDayHorizon();
            DrawDeveloperDreamDay();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Run double day"))
                RunDay(doubleDay: true);
            if (GUILayout.Button("Run single day"))
                RunDay(doubleDay: false);
            if (GUILayout.Button("Run night"))
                RunNight();
            if (GUILayout.Button("Recall with safe refrain"))
                RecallDream();
            EditorGUILayout.EndHorizontal();

            DrawServiceRefs();
            DrawAspectMeters();
            DrawSleepWavePreview();
            DrawSeedTreeReadOnly();

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, MessageType.Info);
        }

        void DrawGoodDayHorizon()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Good day horizon (statistical, no lemma hints)", EditorStyles.boldLabel);
            if (_profile == null)
            {
                EditorGUILayout.HelpBox("Assign DreamDaySimulationProfile on day runner.", MessageType.None);
                return;
            }
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle("Double day enabled", _profile.doubleDayEnabled);
            EditorGUILayout.Slider("Min satisfied", _profile.goodDayHorizon.minSatisfied, 0f, 1f);
            EditorGUILayout.Slider("Max satisfied", _profile.goodDayHorizon.maxSatisfied, 0f, 1f);
            EditorGUILayout.Slider("Society blend", _profile.goodDayHorizon.blendSocietyWeight, 0f, 1f);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.HelpBox(
                "Outer layer clamps need satisfaction from society snapshot only — no {P:dream-day} spans.",
                MessageType.Info);
        }

        void DrawDeveloperDreamDay()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Developer dream day (lemma hints)", EditorStyles.boldLabel);
            if (_profile != null)
            {
                _profile.dreamDayPrompt = EditorGUILayout.TextArea(
                    _profile.dreamDayPrompt ?? SampleDreamPrompt,
                    GUILayout.MinHeight(80f));
            }
            else if (_dayRunner != null)
            {
                _dayRunner.dayPrompt = EditorGUILayout.TextArea(_dayRunner.dayPrompt, GUILayout.MinHeight(80f));
            }
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
            _profile = (DreamDaySimulationProfile)EditorGUILayout.ObjectField(
                "Simulation profile", _profile, typeof(DreamDaySimulationProfile), false);
            if (_dayRunner != null && _profile != null && _dayRunner.profile != _profile)
                _dayRunner.profile = _profile;
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
            EditorGUILayout.LabelField("Sleep wave (wake from nested dream)", EditorStyles.boldLabel);
            if (_nightRunner != null && _nightRunner.wakeFromNestedDream)
                EditorGUILayout.HelpBox("Last night: wakeFromNestedDream (double-day stack).", MessageType.Info);
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
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Day collapse seeds", EditorStyles.boldLabel);
            if (_dayRunner != null)
            {
                EditorGUILayout.IntField("Good day seed", _dayRunner.goodDayCollapseSeed);
                EditorGUILayout.IntField("Dream day seed", _dayRunner.dreamDayCollapseSeed);
                EditorGUILayout.IntField("Active day seed", _dayRunner.dayCollapseSeed);
            }
            if (orch == null)
                return;
            EditorGUILayout.LabelField("Seed dependency (orchestrator)", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(orch.lockSeedDependencyTree);
            EditorGUILayout.IntField("Master seed", orch.masterSeed);
            EditorGUILayout.IntField("Day collapse seed", orch.dayCollapseSeed);
            EditorGUILayout.IntField("Sleep seed", orch.sleepSeed);
            EditorGUI.EndDisabledGroup();
            if (orch.lockSeedDependencyTree)
                EditorGUILayout.HelpBox("Seed tree locked — values are read-only.", MessageType.Warning);
        }

        void RunDay(bool doubleDay)
        {
            if (_dayRunner == null)
            {
                _status = "Assign DreamDayCycleRunner in scene.";
                return;
            }
            _dayRunner.cityId = _cityId;
            if (_profile != null)
            {
                _profile.doubleDayEnabled = doubleDay;
                if (string.IsNullOrEmpty(_profile.dreamDayPrompt))
                    _profile.dreamDayPrompt = SampleDreamPrompt;
                _dayRunner.profile = _profile;
            }
            else
            {
                _dayRunner.dayPrompt = SampleDreamPrompt;
            }
            _dayRunner.RunDayComplete();
            _status = doubleDay
                ? "Double day requested (good horizon + developer dream)."
                : "Single day requested (legacy path).";
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
            if (_profile != null)
            {
                var refrainField = _dreamMemoryLstm.GetType().GetField("safeRefrain");
                refrainField?.SetValue(_dreamMemoryLstm, _profile.safeRefrain);
            }
            var recall = _dreamMemoryLstm.GetType().GetMethod("RecallDreamFragment");
            var fragment = recall?.Invoke(_dreamMemoryLstm, null);
            _status = fragment != null ? fragment.ToString() : "Recall invoked.";
        }
    }
}
#endif
