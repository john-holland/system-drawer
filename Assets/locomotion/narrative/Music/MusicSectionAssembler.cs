using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

namespace Locomotion.Narrative.Music
{
    [Serializable]
    public sealed class MusicSectionPlan
    {
        public string fromLeafId;
        public string toLeafId;
        public MusicNarrativeBridge narrativeBridge;
        public List<MusicStemSlot> stemSlots = new List<MusicStemSlot>();
        public RhythmMeterTemplate rhythm;
        public List<string> sectionIdsUsed = new List<string>();
    }

    /// <summary>Orchestrates sectional assembly between causality nodes.</summary>
    public sealed class MusicSectionAssembler
    {
        public MusicSectionLibrary library;
        public TransitionScorer scorer = new TransitionScorer();
        public ModulationSavingsBank modulationBank = new ModulationSavingsBank();
        public MusicSectionGraph graph = new MusicSectionGraph();
        public RhythmMeterQuadTree rhythmTree = new RhythmMeterQuadTree();
        public MusicBurstCandidatePicker burstPicker = new MusicBurstCandidatePicker();

        readonly HashSet<string> _recentSectionIds = new HashSet<string>();
        MusicCompositionPlanAsset _compositionPlan;

        public void SetCompositionPlan(MusicCompositionPlanAsset plan) => _compositionPlan = plan;

        public MusicSectionPlan BuildPlan(
            string fromLeafId,
            string toLeafId,
            int dayCollapseSeed,
            float questEnergy,
            int harmonicHueBias,
            MusicOverrideAsset overrideAsset,
            float dialogueSpanSeconds,
            float dialogueBpm,
            MusicAnimationTransitionCategory animCategory)
        {
            if (library != null)
                graph.ConnectLibrary(library);

            if (overrideAsset != null)
            {
                scorer.smoothness = overrideAsset.smoothness;
                scorer.awkwardness = overrideAsset.awkwardness;
            }

            scorer.ApplyAnimationCategory(animCategory);

            int footOverride = dialogueSpanSeconds > 0f
                ? RhythmMeterQuadTree.FeetFromFareySpanDuration(dialogueSpanSeconds, dialogueBpm > 0 ? dialogueBpm : 120f)
                : 0;

            var rhythm = rhythmTree.Walk(
                toLeafId ?? fromLeafId ?? "R",
                dayCollapseSeed,
                questEnergy,
                harmonicHueBias / 12f,
                footOverride);

            var plan = new MusicSectionPlan
            {
                fromLeafId = fromLeafId,
                toLeafId = toLeafId,
                rhythm = rhythm,
                narrativeBridge = new MusicNarrativeBridge
                {
                    fromLeafId = fromLeafId,
                    toLeafId = toLeafId
                }
            };

            if (library == null || library.sections.Count == 0)
                return plan;

            var roles = new[] { MusicStemRole.Background, MusicStemRole.Font, MusicStemRole.Accent };
            MusicSectionAsset prev = null;

            for (int r = 0; r < roles.Length; r++)
            {
                MusicStemRole role = roles[r];
                MusicSectionAsset picked = PickSectionForRole(role, harmonicHueBias, questEnergy, overrideAsset, prev, rhythm);
                if (picked == null) continue;

                prev = picked;
                _recentSectionIds.Add(picked.StableId);
                plan.sectionIdsUsed.Add(picked.StableId);

                var slot = new MusicStemSlot
                {
                    role = role,
                    sectionId = picked.StableId,
                    clip = ResolveClip(picked),
                    transpositionSemitones = ComputeTransposition(picked, overrideAsset),
                    barPhase = picked.downbeatPhase,
                    volume = role == MusicStemRole.Accent ? 0.85f : 0.65f
                };
                plan.stemSlots.Add(slot);
                modulationBank.OnSectionAdvance(picked.TonicPc, picked.bars);
            }

            return plan;
        }

        MusicSectionAsset PickSectionForRole(
            MusicStemRole role,
            int harmonicHue,
            float energy,
            MusicOverrideAsset overrideAsset,
            MusicSectionAsset prev,
            RhythmMeterTemplate rhythm)
        {
            if (overrideAsset?.forceSectionIds != null && overrideAsset.forceSectionIds.Length > 0)
            {
                for (int i = 0; i < overrideAsset.forceSectionIds.Length; i++)
                {
                    if (library.TryGet(overrideAsset.forceSectionIds[i], out MusicSectionAsset forced) &&
                        forced.stemRole == role)
                        return forced;
                }
            }

            List<MusicSectionAsset> candidates = library.Query(role, harmonicHue, energy);
            if (candidates.Count == 0)
                candidates = library.AllExcept(_recentSectionIds);

            if (overrideAsset != null && overrideAsset.proceduralChaos && burstPicker.enabled)
            {
                MusicSectionAsset burst = burstPicker.Pick(candidates, prev, rhythm, rhythm, scorer, modulationBank);
                if (burst != null) return burst;
            }

            if (_compositionPlan != null && prev != null)
            {
                string overlaySectionId = _compositionPlan.ResolveOverlayNextSectionId(prev.StableId, role);
                if (!string.IsNullOrEmpty(overlaySectionId) && library.TryGet(overlaySectionId, out MusicSectionAsset overlay))
                    return overlay;
            }

            if (prev == null)
            {
                return candidates.Count > 0 ? candidates[0] : null;
            }

            return graph.PickBestNext(prev, candidates, _recentSectionIds, rhythm, rhythm, scorer, modulationBank);
        }

        static AudioClip ResolveClip(MusicSectionAsset section)
        {
            if (section.loopClip != null) return section.loopClip;
            if (section.proceduralGenerator is IProceduralAudioSource source)
                return source.ResolveAudioClip();
            return null;
        }

        int ComputeTransposition(MusicSectionAsset section, MusicOverrideAsset overrideAsset)
        {
            int target = overrideAsset != null && !string.IsNullOrEmpty(overrideAsset.forceKey)
                ? MusicTheory.TonicFromKeyName(overrideAsset.forceKey)
                : modulationBank.currentTonic;
            return (target - section.TonicPc + 12) % 12;
        }

        public ProceduralCompositionSnapshot CaptureSnapshot(MusicSectionPlan plan, string laneId)
        {
            return ProceduralCompositionSnapshot.FromPlan(plan, laneId);
        }
    }
}
