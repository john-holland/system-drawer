using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative.Music
{
    /// <summary>MonoBehaviour bridge: causality leaf transitions → music assembly + playback.</summary>
    public sealed class CausalityMusicBridge : MonoBehaviour
    {
        [Header("References")]
        public MusicSectionLibrary library;
        public MusicCompositionPlanAsset compositionPlan;
        public MusicOverrideAsset overrideAsset;

        [Header("Playback")]
        public AudioSource backgroundSource;
        public AudioSource fontSource;
        public AudioSource accentSource;

        [Header("State")]
        public string currentLeafId;
        public int dayCollapseSeed;
        [Range(0f, 1f)] public float questEnergy = 0.5f;
        public int harmonicHueBias;
        public MusicAnimationTransitionCategory animationCategory = MusicAnimationTransitionCategory.Hold;

        [Header("Dialogue override")]
        public float activeDialogueSpanSeconds;
        public float dialogueBpm = 120f;

        [Header("State machine")]
        [Tooltip("When true and the plan has nodes, runs MusicCompositionPlayer.PlayMachine.")]
        public bool playCompositionStateMachine = true;

        [Range(0f, 1f)]
        [Tooltip("Player interaction quantization for mixer grid (0 free … 1 hard bar).")]
        public float playerInteractionQuantize01 = 1f;

        readonly MusicSectionAssembler _assembler = new MusicSectionAssembler();
        readonly MusicPlaybackMixer _mixer = new MusicPlaybackMixer();
        MusicCompositionPlayer _compositionPlayer;
        MusicSectionPlan _lastPlan;
        Coroutine _machineRoutine;

        void Awake()
        {
            _assembler.library = library;
            _assembler.SetCompositionPlan(compositionPlan);
            _compositionPlayer = new MusicCompositionPlayer(library);
            BindSources();
        }

        void BindSources()
        {
            _mixer.BindSource(MusicStemRole.Background, backgroundSource);
            _mixer.BindSource(MusicStemRole.Font, fontSource);
            _mixer.BindSource(MusicStemRole.Accent, accentSource);
        }

        void Update()
        {
            float bpm = dialogueBpm > 0f ? dialogueBpm : 120f;
            _mixer.PlayerInteractionQuantize01 = playerInteractionQuantize01;
            _mixer.Tick(Time.deltaTime, bpm);
        }

        public void OnCausalityLeafTransition(string fromLeafId, string toLeafId)
        {
            currentLeafId = toLeafId;
            _lastPlan = _assembler.BuildPlan(
                fromLeafId,
                toLeafId,
                dayCollapseSeed,
                questEnergy,
                harmonicHueBias,
                overrideAsset,
                activeDialogueSpanSeconds,
                dialogueBpm,
                animationCategory);

            if (compositionPlan != null)
            {
                compositionPlan.causalityFromLeaf = fromLeafId;
                compositionPlan.causalityToLeaf = toLeafId;
                compositionPlan.proceduralSnapshot = _assembler.CaptureSnapshot(_lastPlan, "lane");
            }

            float swell = activeDialogueSpanSeconds > 0f ? 1.2f : 1f;
            _mixer.PlayerInteractionQuantize01 = playerInteractionQuantize01;
            if (_lastPlan != null)
                _mixer.CrossfadeToSlots(_lastPlan.stemSlots, swell);

            if (playCompositionStateMachine && compositionPlan != null && compositionPlan.nodes != null &&
                compositionPlan.nodes.Count > 0)
            {
                if (_machineRoutine != null)
                    StopCoroutine(_machineRoutine);
                _machineRoutine = StartCoroutine(PlayPlanStateMachine(dialogueBpm > 0f ? dialogueBpm : 120f));
            }
        }

        IEnumerator PlayPlanStateMachine(float bpm)
        {
            var machine = new MusicCompositionStateMachine
            {
                machineId = compositionPlan != null ? compositionPlan.name : "composition",
                lane = MusicStemRole.Background,
                nodes = compositionPlan.nodes != null
                    ? new List<MusicBehaviorNode>(compositionPlan.nodes)
                    : new List<MusicBehaviorNode>(),
                overlayEdges = compositionPlan.overlayEdges != null
                    ? new List<MusicCompositionOverlayEdge>(compositionPlan.overlayEdges)
                    : new List<MusicCompositionOverlayEdge>(),
                proceduralEdges = compositionPlan.proceduralSnapshot != null &&
                                  compositionPlan.proceduralSnapshot.baselineEdges != null
                    ? new List<MusicCompositionOverlayEdge>(compositionPlan.proceduralSnapshot.baselineEdges)
                    : new List<MusicCompositionOverlayEdge>()
            };
            yield return _compositionPlayer.PlayMachine(machine, _mixer, bpm);
            _machineRoutine = null;
        }

        public MusicSectionPlan LastPlan => _lastPlan;

        public void SetQuestMood(float energy, int hueBias, float awkwardness)
        {
            questEnergy = energy;
            harmonicHueBias = hueBias;
            _assembler.scorer.awkwardness = awkwardness;
        }

        public void SetDialogueLineActive(float spanSeconds, float bpm)
        {
            activeDialogueSpanSeconds = spanSeconds;
            dialogueBpm = bpm;
        }

        public void ClearDialogueOverride()
        {
            activeDialogueSpanSeconds = 0f;
        }
    }
}
