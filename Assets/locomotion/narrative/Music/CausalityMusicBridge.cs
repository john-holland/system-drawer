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

        readonly MusicSectionAssembler _assembler = new MusicSectionAssembler();
        readonly MusicPlaybackMixer _mixer = new MusicPlaybackMixer();
        MusicSectionPlan _lastPlan;

        void Awake()
        {
            _assembler.library = library;
            _assembler.SetCompositionPlan(compositionPlan);
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
            float bpm = _lastPlan?.stemSlots.Count > 0 ? 120f : 120f;
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
            _mixer.CrossfadeToSlots(_lastPlan.stemSlots, swell);
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
